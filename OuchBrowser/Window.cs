using Adw;
using Gio;
using Gtk;
using Application = Adw.Application;
using Object = GObject.Object;
using OuchBrowser;

namespace OuchBrowser;

public class Window {
	public static void OnActivate(Object app, EventArgs args) {
		var window = new UI.Window((Application)app);
		var view = new View(window.view!);

		view.AddTab("https://start.ubuntu.com");

		window.Present();
	}

	public static void OnStartup(Object app, EventArgs args) {
		return;
	}
}
