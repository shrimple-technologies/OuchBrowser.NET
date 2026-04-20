using System.Reflection;
using Gio;
using GLib;

namespace OuchBrowser;

internal class Application : Adw.Application
{
	public Application()
	{
		ApplicationId = "site.srht.shrimple.OuchBrowser";
		Flags = ApplicationFlags.DefaultFlags;
		ResourceBasePath = "/site/srht/shrimple/OuchBrowser";
		OnActivate += (self, args) => {
			var window = new Window(this);
			window.OnActivate(self, args);
		};

		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();
	}
}
