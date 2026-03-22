using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.UI;
using OuchBrowser.Utils;
using WebKit;
using Application = Adw.Application;
using Object = GObject.Object;

namespace OuchBrowser;

public class Window
{
	private string palette_state = "new_tab";
	private DateTime lastInvokeTime = DateTime.MinValue;
	private CancellationTokenSource? debounceCts;

	public Window() { }

	public void OnActivate(Object app, EventArgs args)
	{
		var application = (Application)app;
		var window = new UI.Window(application);
		var preferences = new Preferences(window);
		var cards = new Cards(window);
		var about = About.New();
		var view = new View(window.view!, window!);
		var bangs = new Bangs(window.settings.GetString("search-engine"));

		SetupActions(window, application, preferences, about);

		window.go_back!.SetSensitive(false);
		window.go_forward!.SetSensitive(false);
		window.refresh!.SetSensitive(false);
		window.copy_link!.SetSensitive(false);
		window.url_button!.SetSensitive(false);
		window.website_settings!.SetSensitive(false);

		// TODO: maybe make this a little bit less "hacky?"
		window.content_sidebar_toggle!.OnClicked += (_, _) =>
		{
			window.osv!.SetShowSidebar(true);
			window.sidebar_toggle!.SetActive(true);
			window.frame!.SetMarginStart(0);
		};
		window.sidebar_toggle!.OnClicked += (_, _) =>
		{
			window.frame!.SetMarginStart(10);
		};

		window.url_entry!.OnNotify += async (_, args) =>
		{
			if (args.Pspec.GetName() == "text")
			{
				string text = window.url_entry!.GetBuffer().GetText();
				if (text == "")
				{
					window.url_autocomplete!.SetRevealChild(false);
					window.url_stack!.SetVisibleChildName("main");
					window.url_disclosure!.SetVisibleChildName("none");
				}
				else if (text.StartsWith('!'))
				{

					if (1 < text.Split(' ').Length)
					{
						window.url_autocomplete!.SetRevealChild(false);
						window.url_disclosure!.SetVisibleChildName("custom");
						Bang? current_bang = bangs.GetBang(text)!;
						if (current_bang != null)
						{
							if (text.Split(' ')[1].Length == 0) window.url_stack!.SetVisibleChildName("spinner");
							window.url_custom_disclosure!.SetLabel(window.gettext.GetString("Searching using {0}", current_bang.WebsiteName));
							Gio.Icon icon = await Favicon.GetFavicon(current_bang.Domain);
							window.url_favicon!.SetFromGicon(icon);
							window.url_stack!.SetVisibleChildName("website");
						}
						else
						{
							window.url_stack!.SetVisibleChildName("bang");
							window.url_disclosure!.SetVisibleChildName("none");
						}
					}
					else
					{
						window.url_stack!.SetVisibleChildName("bang");
						window.url_disclosure!.SetVisibleChildName("bang");

						if (window.settings.GetBoolean("bang-autocomplete-enabled"))
						{
							window.url_autocomplete!.SetRevealChild(true);
							Box box = Box.New(Orientation.Vertical, 10);
							Label section_label = Label.New(window.gettext.GetString("BANGS"));
							ScrolledWindow sw = ScrolledWindow.New();
							sw.SetPropagateNaturalHeight(true);
							sw.SetVexpand(true);
							sw.SetMinContentHeight(399);
							sw.SetMaxContentHeight(400);
							sw.AddCssClass("undershoot-top");
							sw.AddCssClass("undershoot-bottom");
							section_label.SetCssClasses(["caption-heading", "dimmed"]);
							section_label.SetHalign(Align.Start);
							section_label.SetMarginStart(10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							box.Append(section_label);
							int i = 0;

							Bang[] bang = bangs.AutocompleteBang(text);
							if (bang.Length == 0) window.url_autocomplete!.SetRevealChild(false);
							foreach (Bang b in bang)
							{
								Button button = Button.New();
								Box button_box = Box.New(Orientation.Horizontal, 15);
								Label button_label = Label.New(b.WebsiteName);
								Label button_trigger;
								if (b.AdditionalTriggers != null)
								{
									List<string> triggers = new List<string>();
									foreach (string trigger in b.AdditionalTriggers)
									{
										triggers.Add($"!{trigger}");
									}

									button_trigger = Label.New($"!{b.Trigger}, {string.Join(", ", triggers.ToArray())} ");
								}
								else
								{
									button_trigger = Label.New($"!{b.Trigger}");
								}
								button.SetMarginStart(10);
								button.SetMarginEnd(10);
								button.SetHexpand(true);
								button.SetCssClasses(["flat"]);
								button_label.SetCssClasses(["body"]);
								button_label.SetEllipsize(Pango.EllipsizeMode.End);
								button_trigger.SetCssClasses(["body", "dimmed"]);
								button_box.Append(Image.NewFromIconName("box-dotted-symbolic"));
								button_box.Append(button_label);
								button_box.Append(button_trigger);
								button.SetChild(button_box);
								button.OnClicked += (_, _) =>
								{
									EntryBuffer buffer = EntryBuffer.New($"!{b.Trigger} ", -1);
									int length = Convert.ToInt32(buffer.GetLength());
									window.url_entry.SetBuffer(buffer);
									window.url_entry.GrabFocusWithoutSelecting();
									window.url_entry.SetPosition(length);
								};
								box.Append(button);
								i++;
							}

							if (i < 8)
							{
								window.url_autocomplete!.SetChild(box);
							}
							else
							{
								sw.SetChild(box);
								window.url_autocomplete!.SetChild(sw);
							}
						}
					}
				}
				else if (Url.IsUrl(text))
				{
					window.url_autocomplete!.SetRevealChild(false);
					window.url_stack!.SetVisibleChildName("website");
					window.url_disclosure!.SetVisibleChildName("none");
					window.url_custom_disclosure!.SetLabel("");
					window.url_favicon!.SetFromGicon(await Favicon.GetFavicon(text));
				}
				else
				{
					window.url_disclosure!.SetVisibleChildName("none");
					window.url_custom_disclosure!.SetLabel("");

					if (window.settings.GetBoolean("search-autocomplete-enabled"))
					{
						var now = DateTime.UtcNow;
						lastInvokeTime = now;

						debounceCts?.Cancel();
						debounceCts?.Dispose();
						debounceCts = new CancellationTokenSource();

						try
						{
							await Task.Delay(200, debounceCts.Token);
							if (lastInvokeTime != now) return;
							if (text.Length <= 1) window.url_stack!.SetVisibleChildName("spinner");

							string textNow = window.url_entry!.GetBuffer().GetText();

							if (textNow == "")
							{
								window.url_autocomplete!.SetRevealChild(false);
								window.url_stack!.SetVisibleChildName("main");
							}
							else if (textNow.StartsWith('!'))
							{
								window.url_autocomplete!.SetRevealChild(false);
								window.url_stack!.SetVisibleChildName("bang");
							}
							else if (Url.IsUrl(textNow))
							{
								window.url_autocomplete!.SetRevealChild(false);
								window.url_stack!.SetVisibleChildName("website");
							}

							Autocompletion[] ac = await Autocomplete.CompletionResults(textNow);
							if (ac.Length == 0)
							{
								window.url_autocomplete!.SetRevealChild(false);
								return;
							}

							Box box = Box.New(Orientation.Vertical, 10);
							Label section_label = Label.New(window.gettext.GetString("SUGGESTIONS"));
							section_label.SetCssClasses(["caption-heading", "dimmed"]);
							section_label.SetHalign(Align.Start);
							section_label.SetMarginStart(10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							box.Append(section_label);

							foreach (Autocompletion phrase in ac)
							{
								Button button = Button.New();
								Box button_box = Box.New(Orientation.Horizontal, 15);
								Label button_label = Label.New(phrase.phrase);
								button.SetMarginStart(10);
								button.SetMarginEnd(10);
								button.SetHexpand(true);
								button.SetCssClasses(["flat"]);
								button_label.SetCssClasses(["body"]);
								button_box.Append(Image.NewFromIconName("search-symbolic"));
								button_box.Append(button_label);
								button.SetChild(button_box);
								button.OnClicked += (_, _) =>
								{
									EntryBuffer buffer = EntryBuffer.New(phrase.phrase, -1);
									window.url_entry.SetBuffer(buffer);
									window.url_bar_button!.Activate();
								};
								box.Append(button);
							}

							window.url_autocomplete!.SetChild(box);
							window.url_autocomplete!.SetRevealChild(true);
							window.url_stack!.SetVisibleChildName("search");
						}
						catch (TaskCanceledException) { }
						finally
						{
							if (debounceCts?.IsCancellationRequested == false)
								debounceCts?.Dispose();
							debounceCts = null;
						}
					}
					else
					{
						window.url_stack!.SetVisibleChildName("search");
					}
				}
			}
		};

		window.url_entry.OnActivate += (_, _) => window.url_bar_button!.Activate();

		window.url_bar_button!.OnActivate += (_, _) =>
		{
			string query = window.url_entry.GetBuffer().GetText();

			if (query == "") return;

			if (palette_state == "new_tab")
			{
				Console.WriteLine($"url: {query}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
				Console.WriteLine("");

				if (Url.IsUrl(query))
				{
					if (query.StartsWith("https://") || query.StartsWith("http://"))
					{
						view.AddTab(query, false);
					}
					else
					{
						view.AddTab($"https://{query}", false);
					}
				}
				else
				{
					if (query.StartsWith('!'))
					{
						view.AddTab(bangs.ExpandBang(query), false);
					}
					else
					{
						view.AddTab($"{window.settings!.GetString("search-engine")}{Uri.EscapeDataString(query)}", false);
					}
				}
			}
			else
			{
				TabPage page = window.view!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				Console.WriteLine($"url: {query}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
				Console.WriteLine($"is bang: {query.StartsWith('!')}");
				Console.WriteLine("");

				if (Url.IsUrl(query))
				{
					if (query.StartsWith("https://") || query.StartsWith("http://"))
					{
						webview.LoadUri(query);
					}
					else
					{
						webview.LoadUri($"https://{query}");
					}
				}
				else
				{
					if (query.StartsWith('!'))
					{
						webview.LoadUri(bangs.ExpandBang(query));
					}
					else
					{
						webview.LoadUri($"{window.settings!.GetString("search-engine")}{Uri.EscapeDataString(query)}");
					}
				}
			}

			window.url_dialog!.ForceClose();
			window.overview!.SetOpen(false);
		};

		window.overview!.OnCreateTab += (_, _) =>
		{
			window.ActivateAction("palette-new", null);
			return window.view!.GetSelectedPage()!;
		};

		window.Present();

		if (window.settings.GetStrv("restore-tabs").Length == 0)
		{
			window.url_dialog!.Present(window);
		}
		else
		{
			foreach (string url in window.settings.GetStrv("restore-tabs")) view.AddTab(url, false);
		}
	}

	public void SetupActions(UI.Window window, Application application, Preferences preferences, Adw.AboutDialog about)
	{
		var actions = new Actions(window, application);

		actions.AddAction("palette-new", ["<Ctrl>t"], (_, _) =>
		{
			window.overview!.SetOpen(false);

			EntryBuffer buffer = EntryBuffer.New("", -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "new_tab";
		});

		actions.AddAction("palette", ["<Ctrl>l", "<Alt>d"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "current_tab";
		});

		actions.AddAction("sidebar-toggle", ["<Ctrl><Shift>s"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			if (window.osv!.GetShowSidebar())
			{
				window.sidebar_toggle!.Activate();
			}
			else
			{
				window.content_sidebar_toggle!.Activate();
			}
		});

		foreach (int i in new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
		{
			actions.AddAction($"tab-{i}", [$"<Ctrl>{i}"], (_, _) =>
			{
				if (window.view!.GetNPages() < i) return;
				if (window.view!.GetNthPage(i - 1) == window.view!.GetSelectedPage()) return;

				TabPage page = window.view!.GetNthPage(i - 1);
				window.view!.SetSelectedPage(page);

				if (!window.osv!.GetShowSidebar())
				{
					Toast toast = Toast.New(page.GetTitle());
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
				}
			});
		}

		actions.AddAction("preferences", ["<Ctrl>comma"], (action, parameter) =>
		{
			preferences.FocusPane("general");
			preferences.Present(window);
		});

		actions.AddAction("about", [], (action, parameter) =>
		{
			about.Present(window);
		});

		actions.AddAction("refresh", ["<Ctrl>r"], (_, _) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			if (window.refresh!.GetIconName() == "cross-large-symbolic")
			{
				webview.StopLoading();
			}
			else
			{
				webview.Reload();
			}
		});

		actions.AddAction("hard-refresh", ["<Ctrl><Shift>r"], (_, _) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.ReloadBypassCache();
		});

		actions.AddAction("zoom-in", ["<Ctrl>equal"], (_, _) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			// these levels correspond to what would be seen in chromium
			switch (webview.GetZoomLevel())
			{
				case 0.25: // 25%
					webview.SetZoomLevel(0.33); // 33%
					toast.SetTitle("33%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.33: // 33%
					webview.SetZoomLevel(0.5); // 50%
					toast.SetTitle("50%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.5: // 50%
					webview.SetZoomLevel(0.67); // 67%
					toast.SetTitle("67%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.67: // 67%
					webview.SetZoomLevel(0.75); // 75%
					toast.SetTitle("75%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.75: // 75%
					webview.SetZoomLevel(0.8); // 80%
					toast.SetTitle("80%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.8: // 80%
					webview.SetZoomLevel(0.9); // 90%
					toast.SetTitle("90%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.9: // 90%
					webview.SetZoomLevel(1); // 100%
					toast.SetTitle("100%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1: // 100%
					webview.SetZoomLevel(1.1); // 110%
					toast.SetTitle("110%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.1: // 110%
					webview.SetZoomLevel(1.25); // 125%
					toast.SetTitle("125%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.25: // 125%
					webview.SetZoomLevel(1.5); // 150%
					toast.SetTitle("150%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.5: // 150%
					webview.SetZoomLevel(1.75); // 175%
					toast.SetTitle("175%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.75: // 175%
					webview.SetZoomLevel(2); // 200%
					toast.SetTitle("200%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 2: // 200%
					webview.SetZoomLevel(2.5); // 250%
					toast.SetTitle("250%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 2.5: // 250%
					webview.SetZoomLevel(3); // 300%
					toast.SetTitle("300%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 3: // 300%
					webview.SetZoomLevel(4); // 400%
					toast.SetTitle("400%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 4: // 400%
					webview.SetZoomLevel(5); // 400%
					toast.SetTitle("500%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 5: // 500%
					Gdk.Display.GetDefault()!.Beep();
					break;
			}
		});

		actions.AddAction("zoom-out", ["<Ctrl>minus"], (_, _) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			// these levels correspond to what would be seen in chromium
			switch (webview.GetZoomLevel())
			{
				case 5: // 500%
					webview.SetZoomLevel(4); // 400%
					toast.SetTitle("400%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 4: // 400%
					webview.SetZoomLevel(3); // 300%
					toast.SetTitle("300%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 3: // 300%
					webview.SetZoomLevel(2.5); // 150%
					toast.SetTitle("250%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 2.5: // 250%
					webview.SetZoomLevel(2); // 200%
					toast.SetTitle("200%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 2: // 200%
					webview.SetZoomLevel(1.75); // 175%
					toast.SetTitle("175%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.75: // 175%
					webview.SetZoomLevel(1.5); // 150%
					toast.SetTitle("150%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.5: // 150%
					webview.SetZoomLevel(1.25); // 125%
					toast.SetTitle("125%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.25: // 125%
					webview.SetZoomLevel(1.1); // 400%
					toast.SetTitle("110%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1.1: // 110%
					webview.SetZoomLevel(1); // 400%
					toast.SetTitle("100%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 1: // 100%
					webview.SetZoomLevel(0.9); // 400%
					toast.SetTitle("90%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.9: // 90%
					webview.SetZoomLevel(0.8); // 80%
					toast.SetTitle("80%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.8: // 80%
					webview.SetZoomLevel(0.75); // 75%
					toast.SetTitle("75%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.75: // 75%
					webview.SetZoomLevel(0.67); // 67%
					toast.SetTitle("67%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.67: // 67%
					webview.SetZoomLevel(0.5); // 67%
					toast.SetTitle("50%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.5: // 50%
					webview.SetZoomLevel(0.33); // 33%
					toast.SetTitle("33%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.33: // 33%
					webview.SetZoomLevel(0.25); // 25%
					toast.SetTitle("25%");
					toast.SetTimeout(1);
					window.toast_overlay!.DismissAll();
					window.toast_overlay!.AddToast(toast);
					break;
				case 0.25: // 25%
					Gdk.Display.GetDefault()!.Beep();
					break;
			}
		});

		actions.AddAction("tab-close", ["<Ctrl>w"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0)
			{
				window.Close();
			}
			else
			{
				TabPage page = window.view!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				webview.TryClose();
				window.view!.ClosePage(window.view!.GetSelectedPage()!);
				if (window.view!.GetNPages() == 0)
				{
					window.refresh!.SetSensitive(false);
					window.go_back!.SetSensitive(false);
					window.go_forward!.SetSensitive(false);
					window.url_button!.SetSensitive(false);
					window.copy_link!.SetSensitive(false);
					window.website_settings!.SetSensitive(false);
					window.sidebar_toggle!.SetSensitive(false);
					window.sidebar_toggle!.SetActive(true);
					window.hostname!.SetLabel("");
					window.osv!.SetShowSidebar(true);
				}
			}
		});

		actions.AddAction("go-back", ["<Ctrl>Left"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoBack();
		});

		actions.AddAction("go-forward", ["<Ctrl>Right"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoForward();
		});

		actions.AddAction("copy-link", ["<Ctrl><Shift>c"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			string uri = webview.GetUri();
			Gdk.Display display = Gdk.Display.GetDefault()!;
			Gdk.Clipboard clipboard = display!.GetClipboard();
			clipboard.SetText(uri);

			toast.SetTitle(window.gettext.GetString("Link Copied"));
			toast.SetTimeout(1);
			window.toast_overlay!.DismissAll();
			window.toast_overlay!.AddToast(toast);

			webview.GoForward();
		});
	}
}
