using Adw;
using Gtk;
using WebKit;
using Application = Adw.Application;
using Object = GObject.Object;
using OuchBrowser.Utils;

namespace OuchBrowser;

public class Window
{
	private string palette_state = "new_tab";

	public Window() { }

	public void OnActivate(Object app, EventArgs args)
	{
		var application = (Application)app;
		var window = new UI.Window(application);
		var view = new View(window.view!, window!);
		var actions = new Actions(window);

		//view.AddTab("https://start.ubuntu.com", true);

		actions.AddAction("palette-new", (action, parameter) =>
		{
			EntryBuffer buffer = EntryBuffer.New("", -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "new_tab";
		});
		actions.AddAction("palette", (action, parameter) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
			palette_state = "current_tab";
		});
		application.SetAccelsForAction("win.palette-new", ["<Ctrl>t"]);
		application.SetAccelsForAction("win.palette", ["<Ctrl>l"]);

		window.url_entry!.OnActivate += (entry, _) =>
		{
			string query = entry.GetBuffer().GetText();

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

		window.Present();
		window.url_dialog!.Present(window);
	}

	public static void OnStartup(Object app, EventArgs args)
	{
		return;
	}
}
