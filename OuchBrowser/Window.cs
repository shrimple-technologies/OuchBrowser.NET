using Adw;
using Gio;
using Gtk;
using Application = Adw.Application;
using Object = GObject.Object;
using OuchBrowser;

namespace OuchBrowser;

public class Window {
	public void OnActivate(Object app, EventArgs args) {
		var window = new UI.Window();
		var view = new View(window.view);

		view.AddTab("https://start.ubuntu.com");

		window.SetApplication((Application)app);
		window.Show();
	}

	public void OnStartup(Object app, EventArgs args) {
		return;
	}
}
