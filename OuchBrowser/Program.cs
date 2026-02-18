using System.Reflection;
using Gdk;
using Gio;
using GLib;
using Gtk;
using Application = Adw.Application;

namespace OuchBrowser;

internal class Program {
	private static int Main(string[] args) {
		var app =
			Application.New(
				"site.srht.shrimple.OuchBrowserNET",
				ApplicationFlags.FlagsNone
			);
		var window = new Window();
		RegisterResources();
		RegisterCss();

		app.OnStartup += Window.OnStartup;
		app.OnActivate += Window.OnActivate;

		return app.RunWithSynchronizationContext(null);
	}

	private static void RegisterResources() {
		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();

		var display = Gdk.Display.GetDefault();
		if (display is null) return;

		var iconTheme = Gtk.IconTheme.GetForDisplay(display);
		iconTheme.AddResourcePath("/site/srht/shrimple/OuchBrowserNET/icons");
	}

	// adapted from <https://git.sr.ht/~shrimple/ouch/tree/main/item/src/css.rs>
	private static void RegisterCss() {
		using var provider = CssProvider.New();
		provider.LoadFromResource("/site/srht/shrimple/OuchBrowserNET/styles.css");

		StyleContext.AddProviderForDisplay(Display.GetDefault()!, provider, 600);
	}
}
