using System.Reflection;
using Gio;
using GLib;

namespace OuchBrowser;

[GObject.Subclass<Adw.Application>]
internal partial class Application
{
	partial void Initialize()
	{
		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();

		var window = Window.NewWithProperties([]);

		ApplicationId = "page.codeberg.shrimple.OuchBrowser";
		Flags = ApplicationFlags.HandlesOpen;
		OnActivate += (self, args) =>
		{
			window.SetApplication(this);
			window.Start();
		};
	}
}
