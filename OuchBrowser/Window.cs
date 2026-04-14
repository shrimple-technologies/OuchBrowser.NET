using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.UI;
using OuchBrowser.Utils;
using WebKit;
using Application = Adw.Application;
using Object = GObject.Object;

namespace OuchBrowser;

internal partial class Window
{
	private string palette_state = "new_tab";
	private DateTime lastInvokeTime = DateTime.MinValue;
	private CancellationTokenSource? debounceCts;

	private UI.Window? window;
	private Application? application;
	private Preferences? preferences;
	private Adw.AboutDialog? about;
	private ShortcutsDialog? shortcuts;
	private View? view;
	private Bangs? bangs;

	public Window() { }

	public void OnActivate(Object app, EventArgs args)
	{
		application = (Application)app;
		window = new UI.Window(application);
		preferences = new Preferences(window);
		about = About.New();
		shortcuts = Shortcuts.New();
		view = new View(window.view!, window!);
		bangs = new Bangs(window.settings.GetString("search-engine"));

		SetupActions();

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
			window.url_preview!.SetMarginStart(30);
		};
		window.sidebar_toggle!.OnClicked += (_, _) =>
		{
			window.frame!.SetMarginStart(10);
			window.url_preview!.SetMarginStart(30);
		};

		window.url_entry!.OnActivate += (_, _) => window.url_bar_button!.Activate();

		HandlePaletteUpdate();
		HandlePaletteActivate();

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

	private void SetupActions()
	{
		var actions = new Actions(window!, application!);

		actions.AddAction("palette-new", ["<Ctrl>t"], (_, _) =>
		{
			window!.overview!.SetOpen(false);

			EntryBuffer buffer = EntryBuffer.New("", -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "new_tab";
		});

		actions.AddAction("palette", ["<Ctrl>l", "<Alt>d"], (_, _) =>
		{
			if (window!.view!.GetNPages() == 0)
			{
				window.ActivateAction("palette-new", null);
			}
			else
			{
				TabPage page = window.view!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;
				EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
				window.url_entry!.SetBuffer(buffer);
				window.url_dialog!.Present(window);
				window.url_entry!.GrabFocus();
				palette_state = "current_tab";
			}
		});

		actions.AddAction("sidebar-toggle", ["<Ctrl><Shift>s"], (_, _) =>
		{
			if (window!.view!.GetNPages() == 0) return;

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
				if (window!.view!.GetNPages() < i) return;
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
			preferences!.FocusPane("general");
			preferences.Present(window);
		});

		actions.AddAction("about", [], (action, parameter) =>
		{
			about!.Present(window);
		});

		actions.AddAction("shortcuts", ["<Ctrl>question"], (action, parameter) =>
		{
			shortcuts!.Present(window);
		});

		actions.AddAction("refresh", ["<Ctrl>r"], (_, _) =>
		{
			TabPage page = window!.view!.GetSelectedPage()!;
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
			TabPage page = window!.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.ReloadBypassCache();
		});

		actions.AddAction("zoom-in", ["<Ctrl>equal"], (_, _) =>
		{
			TabPage page = window!.view!.GetSelectedPage()!;
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
			TabPage page = window!.view!.GetSelectedPage()!;
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

		actions.AddAction("zoom-reset", ["<Ctrl>0"], (_, _) =>
		{
			TabPage page = window!.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			if (webview.GetZoomLevel() == window.settings.GetDouble("zoom")) return;

			webview.SetZoomLevel(window.settings.GetDouble("zoom"));
			toast.SetTitle($"{window.settings.GetDouble("zoom") * 100}%");
			toast.SetTimeout(1);
			window.toast_overlay!.DismissAll();
			window.toast_overlay!.AddToast(toast);
		});

		actions.AddAction("tab-close", ["<Ctrl>w"], (_, _) =>
		{
			if (window!.view!.GetNPages() == 0)
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
			if (window!.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoBack();
		});

		actions.AddAction("go-forward", ["<Ctrl>Right"], (_, _) =>
		{
			if (window!.view!.GetNPages() == 0) return;

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoForward();
		});

		actions.AddAction("copy-link", ["<Ctrl><Shift>c"], (_, _) =>
		{
			if (window!.view!.GetNPages() == 0) return;

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
