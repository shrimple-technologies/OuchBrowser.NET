using Adw;
using Gtk;
using WebKit;
using Application = Adw.Application;
using Object = GObject.Object;

namespace OuchBrowser;

public class Window
{
	public static void OnActivate(Object app, EventArgs args)
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
		});
		actions.AddAction("palette", (action, parameter) =>
		{
			TabPage page = window.view!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
			window.url_entry!.SetBuffer(buffer);
			window.url_dialog!.Present(window);
			window.url_entry!.GrabFocus();
		});
		application.SetAccelsForAction("win.palette-new", ["<Ctrl>t"]);
		application.SetAccelsForAction("win.palette", ["<Ctrl>l"]);

		window.url_entry!.OnActivate += (entry, _) =>
		{
			view.AddTab($"https://google.com/search?q={entry.GetBuffer().GetText()}", false);

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
