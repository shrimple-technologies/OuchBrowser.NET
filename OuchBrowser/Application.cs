using System.Reflection;
using Gio;
using GLib;

namespace OuchBrowser;

internal class Application : Adw.Application
{
	public Application()
	{
		var window = new Window(this);

		ApplicationId = "site.srht.shrimple.OuchBrowser";
		Flags = ApplicationFlags.DefaultFlags;
		ResourceBasePath = "/site/srht/shrimple/OuchBrowser";
		OnActivate += window.OnActivate;

		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();
	}
}
