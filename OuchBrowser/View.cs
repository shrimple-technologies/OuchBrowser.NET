using Adw;
using OuchBrowser.Utils;
using WebKit;

namespace OuchBrowser;

internal class View
{
	public readonly TabView view;
	private readonly Window win;
	private string layout = "default";
	private bool peek_tab_trigger_held = false;

	public View(TabView tabview, Window window)
	{
		view = tabview;
		win = window;
		WebKit.Module.Initialize();
	}

	private static WebKit.Settings InitSettings()
	{
		WebKit.Settings web_settings = WebKit.Settings.New();

		web_settings.SetDefaultFontFamily("serif");
		web_settings.SetEnableDeveloperExtras(settings.GetBoolean("devtools-enabled"));

		return web_settings;
	}

	public WebView AddTab(string url, bool pinned)
	{
		WebView webview = WebView.New();

		webview.SetSettings(InitSettings());
		webview.LoadUri(url);
		webview.SetZoomLevel(settings.GetDouble("zoom"));
		TabPage page;

		if (pinned)
		{
			page = view.AppendPinned(webview);
		}
		else
		{
			page = view.Append(webview);
		}

		view.SetSelectedPage(page);

		Uri uri = new Uri(url);
		if (uri.IsLoopback)
		{
			win.hostname!.SetLabel(uri.Host + ":" + uri.Port);
			page.SetTitle(uri.Host + ":" + uri.Port);
		}
		else
		{
			// TODO: this should also show the search query too maybe
			win.hostname!.SetLabel(uri.Host);
			page.SetTitle(uri.Host);
		}

		Connect(webview, win, page);

		page.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected" && page.GetSelected() == true)
			{
				string current_uri = webview.GetUri();
				Uri uri = new Uri(current_uri);
				if (uri.IsLoopback)
				{
					win.hostname!.SetLabel(uri.Host + ":" + uri.Port);
				}
				else
				{
					// TODO: this should also show the search query too maybe
					win.hostname!.SetLabel(uri.Host);
				}

				if (webview.GetIsLoading())
				{
					win.refresh!.SetTooltipText(__("Stop Loading"));
					win.refresh!.SetIconName("cross-large-symbolic");
				}
				else
				{
					win.refresh!.SetTooltipText(__("Refresh"));
					win.refresh!.SetIconName("view-refresh-symbolic");
				}

				Connect(webview, win, page);
			}
		};

		return webview;
	}

	private void Connect(WebView webview, Window window, TabPage page)
	{
		webview.OnNotify += (_, args) =>
		{
			switch (args.Pspec.GetName())
			{
				case "title":
					string title = webview.GetTitle();
					page.SetTitle(title);
					break;
			}
		};

		webview.OnLoadChanged += async (_, load_event) =>
		{
			string current_uri = webview.GetUri();
			Uri uri;
			try
			{
				uri = new Uri(current_uri);
			}
			catch
			{
				page.SetIcon(Gio.ThemedIcon.New("box-dotted-symbolic")); // set this placeholder first
				window.refresh!.SetSensitive(true);
				window.refresh!.SetTooltipText(__("Stop Loading"));
				window.url_button!.SetSensitive(true);
				window.copy_link!.SetSensitive(true);
				window.website_settings!.SetSensitive(true);
				window.sidebar_toggle!.SetSensitive(true);
				window.refresh!.SetIconName("cross-large-symbolic");
				page.SetLoading(true);
				return;
			}

			switch (load_event.LoadEvent)
			{
				case LoadEvent.Started:
					page.SetIcon(Gio.ThemedIcon.New("box-dotted-symbolic")); // set this placeholder first
					window.refresh!.SetSensitive(true);
					window.refresh!.SetTooltipText(__("Stop Loading"));
					window.url_button!.SetSensitive(true);
					window.copy_link!.SetSensitive(true);
					window.website_settings!.SetSensitive(true);
					window.sidebar_toggle!.SetSensitive(true);
					window.refresh!.SetIconName("cross-large-symbolic");
					page.SetLoading(true);
					page.SetIcon(await Favicon.GetFavicon(uri.Host));
					break;
				case LoadEvent.Committed:
					if (Url.IsIpAddress(uri.AbsoluteUri))
					{
						window.hostname!.SetLabel(uri.Host + ":" + uri.Port);
					}
					else
					{
						// TODO: this should also show the search query too maybe
						window.hostname!.SetLabel(uri.Host);
					}
					break;
				case LoadEvent.Finished:
					page.SetKeyword(current_uri);
					window.refresh!.SetTooltipText(__("Refresh"));
					window.refresh!.SetIconName("view-refresh-symbolic");
					page.SetLoading(false);

					if (webview.CanGoBack())
					{
						window.go_back!.SetSensitive(true);
					}
					else
					{
						window.go_back!.SetSensitive(false);
					}

					if (webview.CanGoForward())
					{
						window.go_forward!.SetSensitive(true);
					}
					else
					{
						window.go_forward!.SetSensitive(false);
					}

					break;
			}
		};

		webview.OnMouseTargetChanged += (_, res) =>
		{
			if (res.HitTestResult.ContextIsLink())
			{
				window.url_preview!.SetVisible(true);
				window.url_preview_label!.SetLabel(res.HitTestResult.GetLinkUri());
			}
			else
			{
				window.url_preview!.SetVisible(false);
			}
		};

		Gtk.EventControllerKey eck = Gtk.EventControllerKey.New();
		window.AddController(eck);
		eck.OnKeyPressed += (_, args) =>
		{
			if (args.Keyval == settings.GetEnum("peek-trigger")) peek_tab_trigger_held = true;
			return true;
		};
		eck.OnKeyReleased += (_, args) =>
		{
			if (args.Keyval == settings.GetEnum("peek-trigger")) peek_tab_trigger_held = false;
		};

		webview.OnDecidePolicy += (_, args) =>
		{
			switch (args.DecisionType)
			{
				case PolicyDecisionType.NavigationAction:
				case PolicyDecisionType.NewWindowAction:
					// SORRY NOT SORRY FOR USING DEPRECATED APIS THAT ACTUALLY SERVED USE
					NavigationPolicyDecision navigation_policy = (NavigationPolicyDecision)args.Decision;
					URIRequest req = navigation_policy.GetNavigationAction().GetRequest();

					if (navigation_policy.GetNavigationAction().GetNavigationType() != NavigationType.LinkClicked) return false;

					if (peek_tab_trigger_held)
					{
						AddPeekTab(req);
						return true;
					}
					else return false;
			}

			return false;
		};

		window.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "fullscreened")
			{
				if (window.IsFullscreen())
				{
					layout = window.mlv!.GetLayoutName()!;
					window.mlv!.SetLayoutName("fullscreen");
				}
				else
				{
					window.mlv!.SetLayoutName(layout);
				}
			}
		};

		webview.OnContextMenu += (_, args) =>
		{
			if (args.HitTestResult.ContextIsLink())
			{
				ContextMenu menu = args.ContextMenu;
				Gio.SimpleAction action = Gio.SimpleAction.New("peek", null);
				action.OnActivate += (_, _) =>
				{
					AddPeekTab(URIRequest.New(args.HitTestResult.GetLinkUri()));
				};

				menu.Insert(ContextMenuItem.NewFromGaction(action, __("Open Link in Peek Tab"), null), 2);
				menu.Insert(ContextMenuItem.NewSeparator(), 3);
			}

			return false;
		};

		webview.OnLoadFailed += (_, args) =>
		{
			EmbeddedResource.Load("ErrorPage.html", out string err_page);
			webview.LoadAlternateHtml(
				string.Format(
					err_page,
					args.FailingUri,
					__("There was a problem loading this website"),
					args.Error.Message
				),
				args.FailingUri,
				null
			);
			return true;
		};

		webview.OnScriptDialog += (_, args) =>
		{
			AlertDialog alert = AlertDialog.New(null, null);

			switch (args.Dialog.GetDialogType())
			{
				case ScriptDialogType.Alert:
					// TRANSLATORS: This is in the format of "example.com says"
					alert.SetHeading(__("{0} says", new Uri(webview.GetUri()).Host));
					alert.SetBody(args.Dialog.GetMessage());
					alert.AddResponse("ok", __("OK"));
					alert.SetCloseResponse("ok");
					alert.SetDefaultResponse("ok");

					alert.OnResponse += (_, argss) =>
					{
						if (argss.Response == "ok") args.Dialog.Close();
					};

					alert.Present(window);
					break;
				case ScriptDialogType.Confirm:
					// TRANSLATORS: This is in the format of "example.com says"
					alert.SetHeading(__("{0} says", new Uri(webview.GetUri()).Host));
					alert.SetBody(args.Dialog.GetMessage());
					alert.AddResponse("cancel", __("Cancel"));
					alert.AddResponse("ok", __("OK"));
					alert.SetCloseResponse("cancel");
					alert.SetResponseAppearance("ok", ResponseAppearance.Suggested);
					alert.SetDefaultResponse("ok");

					alert.OnResponse += (_, argss) =>
					{
						switch (argss.Response)
						{
							case "cancel":
								args.Dialog.ConfirmSetConfirmed(false);
								args.Dialog.Close();
								break;
							case "ok":
								args.Dialog.ConfirmSetConfirmed(true);
								args.Dialog.Close();
								break;
						}
					};

					alert.Present(window);
					break;
				case ScriptDialogType.Prompt:
					// TRANSLATORS: This is in the format of "example.com says"
					Gtk.Entry entry = Gtk.Entry.New();
					alert.SetHeading(__("{0} says", new Uri(webview.GetUri()).Host));
					alert.SetBody(args.Dialog.GetMessage());
					alert.AddResponse("cancel", __("Cancel"));
					alert.AddResponse("ok", __("OK"));
					alert.SetCloseResponse("cancel");
					alert.SetResponseAppearance("ok", ResponseAppearance.Suggested);
					alert.SetDefaultResponse("ok");
					alert.SetExtraChild(entry);

					entry.SetText(args.Dialog.PromptGetDefaultText());

					entry.OnActivate += (_, _) => alert.ActivateDefault();

					alert.OnResponse += (_, argss) =>
					{
						switch (argss.Response)
						{
							case "cancel":
								args.Dialog.PromptSetText(null!);
								args.Dialog.Close();
								break;
							case "ok":
								args.Dialog.PromptSetText(entry.GetText());
								args.Dialog.Close();
								break;
						}
					};

					alert.Present(window);
					entry.GrabFocus();
					break;
				default:
					return false;
			}

			return true;
		};
	}

	public string[] GetAllTabUrls()
	{
		List<string> urls = new List<string>();

		if (view.GetNPages() != 0)
		{
			for (int i = 1; i < view.GetNPages(); i++)
			{
				TabPage page = view.GetNthPage(i);
				WebView web_view = (WebView)page.Child!;

				urls.Add(web_view.GetUri());
			}
		}

		return urls.ToArray();
	}

	public WebView AddPeekTab(URIRequest req)
	{
		WebView webview = WebView.New();
		Dialog dialog = Dialog.New();
		Gtk.Frame frame = Gtk.Frame.New(null);
		ToolbarView toolbarview = ToolbarView.New();
		HeaderBar headerbar = HeaderBar.New();
		Gtk.Button expand_button = Gtk.Button.NewFromIconName("view-fullscreen-symbolic");
		Gtk.Button copy_link_button = Gtk.Button.NewFromIconName("chain-link-loose-symbolic");
		ToastOverlay toast_overlay = ToastOverlay.New();
		Gtk.Separator hb_separator = Gtk.Separator.New(Gtk.Orientation.Vertical);
		bool transferring_to_main = false;

		webview.SetSettings(InitSettings());
		webview.LoadRequest(req);
		webview.SetZoomLevel(settings.GetDouble("zoom"));

		expand_button.SetTooltipText(__("Expand Tab"));
		expand_button.OnClicked += async (_, _) =>
		{
			frame.SetChild(Bin.New()); // make webview parentless so that we can append it to the main tabview
			transferring_to_main = true;
			TabPage page = view.Append(webview);

			page.SetTitle(webview.GetTitle());
			page.SetKeyword(webview.GetUri());

			Uri uri = new Uri(webview.GetUri());
			if (Url.IsIpAddress(uri.AbsoluteUri))
			{
				win.hostname!.SetLabel(uri.Host + ":" + uri.Port);
			}
			else
			{
				// TODO: this should also show the search query too maybe
				win.hostname!.SetLabel(uri.Host);
			}

			dialog.Close();
			Connect(webview, win, page);
			view.SetSelectedPage(page);

			page.SetIcon(Gio.ThemedIcon.New("box-dotted-symbolic")); // set this placeholder first
			page.SetIcon(await Favicon.GetFavicon(webview.GetUri()));
		};

		copy_link_button.SetTooltipText(__("Copy Link"));
		copy_link_button.OnClicked += (_, _) =>
		{
			Toast toast = Toast.New(__("Link Copied"));

			string uri = webview.GetUri();
			Gdk.Display display = Gdk.Display.GetDefault()!;
			Gdk.Clipboard clipboard = display!.GetClipboard();
			clipboard.SetText(uri);

			toast.SetTimeout(1);
			toast_overlay!.DismissAll();
			toast_overlay!.AddToast(toast);
		};

		hb_separator.SetMarginTop(5);
		hb_separator.SetMarginBottom(5);

		headerbar.PackEnd(expand_button);
		headerbar.PackEnd(hb_separator);
		headerbar.PackEnd(copy_link_button);

		toolbarview.AddTopBar(headerbar);
		toolbarview.SetContent(frame);

		frame.SetMarginBottom(10);
		frame.SetMarginStart(10);
		frame.SetMarginEnd(10);
		frame.SetVexpand(true);
		frame.SetHexpand(true);
		frame.SetChild(webview);

		toast_overlay.SetChild(toolbarview);

		dialog.HeightRequest = 360;
		dialog.WidthRequest = 360;
		dialog.SetContentHeight(650);
		dialog.SetContentWidth(900);
		dialog.SetChild(toast_overlay);
		dialog.Present(win);
		webview.GrabFocus();

		dialog.OnClosed += (_, _) =>
		{
			if (!transferring_to_main) webview.TryClose();
			peek_tab_trigger_held = false; // sometimes it forgets to fire Gtk.EventControllerKey.OnKeyReleased
		};

		return webview;
	}
}
