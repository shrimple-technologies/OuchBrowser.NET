using Adw;
using Gtk;
using WebKit;
using Application = Adw.Application;
using Object = GObject.Object;
using OuchBrowser.Utils;
using OuchBrowser.UI;

namespace OuchBrowser;

public class Window
{
	private string palette_state = "new_tab";

	public Window() { }

	public void OnActivate(Object app, EventArgs args)
	{
		var application = (Application)app;
		var window = new UI.Window(application);
		var preferences = Preferences.New();
		var view = new View(window.view!, window!);

		SetupActions(window, application, preferences);

		window.url_entry!.OnActivate += (entry, _) =>
		{
			string query = entry.GetBuffer().GetText();

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
					view.AddTab($"https://google.com/search?q={query}", false);
				}
			}
			else
			{
				TabPage page = window.view!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				Console.WriteLine($"url: {query}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
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
					webview.LoadUri($"https://google.com/search?q={query}");
				}
			}

			window.url_dialog!.ForceClose();
			window.url_dialog!.SetCanClose(true);
		};

		window.overview!.OnCreateTab += (_, _) =>
		{
			window.ActivateAction("palette-new", null);
			return window.view!.GetSelectedPage()!;
		};

		window.Present();
		window.url_dialog!.Present(window);
	}

	public void SetupActions(UI.Window window, Application application, PreferencesDialog preferences)
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

		actions.AddAction("palette", ["<Ctrl>l"], (_, _) =>
		{
			if (window.view!.GetNPages() == 0) return;

			window.overview!.SetOpen(false);

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

			actions.AddAction("preferences", ["<Ctrl>comma"], (action, parameter) =>
			{
				preferences.Present(window);
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
						window.sidebar_toggle!.SetSensitive(false);
						window.sidebar_toggle!.SetActive(true);
						window.hostname!.SetLabel("");
						window.osv!.SetShowSidebar(true);
					}
				}
			});
		}
	}
}
