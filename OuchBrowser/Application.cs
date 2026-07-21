using System.Reflection;
using Gio;
using GLib;

namespace OuchBrowser;

[GObject.Subclass<Adw.Application>]
internal partial class Application
{
	partial void Initialize()
	{
		ApplicationId = "page.codeberg.shrimple.OuchBrowser";
		Flags = ApplicationFlags.DefaultFlags;
		ResourceBasePath = "/page/codeberg/shrimple/OuchBrowser";
		OnActivate += (self, args) =>
		{
			var window = Window.NewWithProperties([]);
			window.SetApplication(this);
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
