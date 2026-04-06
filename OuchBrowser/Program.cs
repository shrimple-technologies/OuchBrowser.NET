using System.Reflection;
using Gdk;
using Gio;
using GLib;
using Gtk;
using Application = Adw.Application;

namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
		var app =
			Application.New(
				"site.srht.shrimple.OuchBrowser",
				ApplicationFlags.FlagsNone
			);
		var window = new Window();

		RegisterResources();

		app.OnActivate += window.OnActivate;

		return app.RunWithSynchronizationContext(null);
	}

	private static void RegisterResources()
	{
		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();
	}
}
