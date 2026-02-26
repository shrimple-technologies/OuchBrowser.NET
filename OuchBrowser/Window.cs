using Application = Adw.Application;
using Object = GObject.Object;

namespace OuchBrowser;

public class Window
{
	public static void OnActivate(Object app, EventArgs args)
	{
		var window = new UI.Window((Application)app);
		var view = new View(window.view!, window!);
		var actions = new Actions(window);

		view.AddTab("https://start.ubuntu.com", true);

		actions.AddAction("palette", (action, parameter) =>
		{
			window.url_dialog!.Present(window);
		});

		window.Present();
		window.url_dialog!.Present(window);
	}

	public static void OnStartup(Object app, EventArgs args)
	{
		return;
	}
}
