using Adw;
using Gio;
using Gtk;
using Application = Adw.Application;
using Object = GObject.Object;

namespace OuchBrowser;

public class Window
{
	public void OnActivate(Object app, EventArgs args)
	{
		var window = new UI.Window();

		window.SetApplication((Application)app);
		window.Show();
	}

	public void OnStartup(Object app, EventArgs args)
	{
	}
}
