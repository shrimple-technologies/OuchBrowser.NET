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

	public View(TabView tabView, Window window)
	{
		view = tabView;
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
		Gio.SimpleAction go_back_action = (Gio.SimpleAction)win.LookupAction("go-back")!;
		Gio.SimpleAction go_forward_action = (Gio.SimpleAction)win.LookupAction("go-forward")!;

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

		go_back_action.SetEnabled(false);
		go_forward_action.SetEnabled(false);

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
				go_back_action.SetEnabled(webview.CanGoBack());
				go_forward_action.SetEnabled(webview.CanGoForward());

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
			Gio.SimpleAction sidebar_action = (Gio.SimpleAction)window.LookupAction("sidebar-toggle")!;
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
				window.copyLinkButton!.SetSensitive(true);
				window.websiteSettingsButton!.SetSensitive(true);
				sidebar_action.SetEnabled(true);
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
					window.copyLinkButton!.SetSensitive(true);
					window.websiteSettingsButton!.SetSensitive(true);
					sidebar_action.SetEnabled(true);
					window.refresh!.SetIconName("cross-large-symbolic");
					page.SetLoading(true);
					page.SetIcon(await Favicon.GetFavicon(uri.Host));
					break;
				case LoadEvent.Committed:
					Gio.SimpleAction go_back_action = (Gio.SimpleAction)window.LookupAction("go-back")!;
					Gio.SimpleAction go_forward_action = (Gio.SimpleAction)window.LookupAction("go-forward")!;

					go_back_action.SetEnabled(webview.CanGoBack());
					go_forward_action.SetEnabled(webview.CanGoForward());

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
					break;
			}
		};

		webview.OnMouseTargetChanged += (_, res) =>
		{
			if (res.HitTestResult.ContextIsLink())
			{
				window.urlDisplayOsd!.SetVisible(true);
				window.urlDisplayLabel!.SetLabel(res.HitTestResult.GetLinkUri());
			}
			else
			{
				window.urlDisplayOsd!.SetVisible(false);
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
					else
					{
						if (args.DecisionType == PolicyDecisionType.NavigationAction) return false;
						AddTab(req.GetUri(), false);
						return true;
					}
			}

			return false;
		};

		window.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "fullscreened")
			{
				if (window.IsFullscreen())
				{
					layout = window.multiLayoutView!.GetLayoutName()!;
					window.multiLayoutView!.SetLayoutName("fullscreen");
				}
				else
				{
					window.multiLayoutView!.SetLayoutName(layout);
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
			if (args.Error.Code == (int)NetworkError.Cancelled) return false;
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

					entry.SetCssClasses(["card", "script-dialog-prompt"]);

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
		Gtk.Box box = Gtk.Box.New(Gtk.Orientation.Horizontal, 15);
		Gtk.Box actionsBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
		Gtk.Button expandButton = Gtk.Button.NewFromIconName("view-fullscreen-symbolic");
		Gtk.Button copyLinkButton = Gtk.Button.NewFromIconName("chain-link-loose-symbolic");
		Gtk.Button closeButton = Gtk.Button.NewFromIconName("cross-large-symbolic");
		ToastOverlay toastOverlay = ToastOverlay.New();
		bool transferringToMain = false;

		webview.SetSettings(InitSettings());
		webview.LoadRequest(req);
		webview.SetZoomLevel(settings.GetDouble("zoom"));

		closeButton.SetTooltipText(__("Close Tab"));
		closeButton.SetCssClasses(["image-button", "circular", "raised", "card", "view"]);
		closeButton.OnClicked += (_, _) =>
		{
			dialog.Close();
		};

		expandButton.SetTooltipText(__("Expand Tab"));
		expandButton.SetCssClasses(["image-button", "circular", "raised", "card", "view"]);
		expandButton.OnClicked += async (_, _) =>
		{
			toastOverlay.SetChild(Bin.New()); // make webview parentless so that we can append it to the main tab view
			transferringToMain = true;
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

		copyLinkButton.SetTooltipText(__("Copy Link"));
		copyLinkButton.SetCssClasses(["image-button", "circular", "raised", "card", "view"]);
		copyLinkButton.OnClicked += (_, _) =>
		{
			Toast toast = Toast.New(__("Link Copied"));

			string uri = webview.GetUri();
			Gdk.Display display = Gdk.Display.GetDefault()!;
			Gdk.Clipboard clipboard = display!.GetClipboard();
			clipboard.SetText(uri);

			toast.SetTimeout(1);
			toastOverlay!.DismissAll();
			toastOverlay!.AddToast(toast);
		};

		frame.SetVexpand(true);
		frame.SetHexpand(true);
		frame.SetCssClasses(["card", "view"]);
		frame.SetChild(toastOverlay);
		toastOverlay.SetChild(webview);

		actionsBox.Append(closeButton);
		actionsBox.Append(expandButton);
		actionsBox.Append(copyLinkButton);
		actionsBox.SetMarginTop(25);
		box.Append(frame);
		box.Append(actionsBox);
		box.SetMarginTop(5);
		box.SetMarginBottom(5);
		box.SetMarginStart(5);
		box.SetMarginEnd(5);

		dialog.HeightRequest = 360;
		dialog.WidthRequest = 360;
		dialog.SetContentHeight(1000);
		dialog.SetContentWidth(900);
		dialog.SetChild(box);
		dialog.AddCssClass("peek");
		dialog.SetPresentationMode(DialogPresentationMode.Floating);
		dialog.Present(win);
		win.frame!.AddCssClass("inactive");
		webview.GrabFocus();

		dialog.OnClosed += (_, _) =>
		{
			if (!transferringToMain) webview.TryClose();
			peek_tab_trigger_held = false; // sometimes it forgets to fire Gtk.EventControllerKey.OnKeyReleased
			win.frame!.RemoveCssClass("inactive");
		};

		// equivalent to condition ("max-width: 600sp") in blueprint
		BreakpointCondition breakpointCondition = BreakpointCondition.NewLength(
			BreakpointConditionLengthType.MaxWidth,
			600,
			LengthUnit.Sp
		);
		Breakpoint breakpoint = Breakpoint.New(breakpointCondition);

		GObject.Value number = new();
		number.Init(GObject.Type.Int);

		number.SetInt(1); // vertical
		breakpoint.AddSetter(box!, "orientation", number);

		number.SetInt(0); // horizontal
		breakpoint.AddSetter(actionsBox!, "orientation", number);

		number.SetInt(0);
		breakpoint.AddSetter(actionsBox!, "margin-top", number);

		number.SetInt(3); // center
		breakpoint.AddSetter(actionsBox!, "halign", number);

		dialog.AddBreakpoint(breakpoint);

		return webview;
	}
}
