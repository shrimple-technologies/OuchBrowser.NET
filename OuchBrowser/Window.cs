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

		actions.AddAction("palette-new", ["<Ctrl>t"], (action, parameter) =>
		{
			window.overview!.SetOpen(false);

			EntryBuffer buffer = EntryBuffer.New("", -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "new_tab";
		});

		actions.AddAction("palette", ["<Ctrl>l"], (action, parameter) =>
		{
			window.overview!.SetOpen(false);

			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "current_tab";
		});

		actions.AddAction("sidebar-toggle", ["<Ctrl><Shift>s"], (action, parameter) =>
		{
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
			actions.AddAction($"tab-{i}", [$"<Ctrl>{i}"], (action, parameter) =>
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
		}
	}
}
