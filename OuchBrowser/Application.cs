using System.Reflection;
using Gio;
using GLib;

namespace OuchBrowser;

[GObject.Subclass<Adw.Application>("OuchApplication")]
internal partial class Application
{
	partial void Initialize()
	{
		BindTextDomain("OuchBrowser", "/usr/share/locale");
		TextDomain("OuchBrowser");
		BindTextDomainCodeset("OuchBrowser", "UTF-8");

		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("OuchBrowser.app.gresource");

		var buffer = new byte[stream!.Length];
		stream.ReadExactly(buffer);

		using var bytes = Bytes.New(buffer);
		using var resource = Resource.NewFromData(bytes);
		resource.Register();

		var window = Window.NewWithProperties([]);

		ApplicationId = "one.lunaris.OuchBrowser";
		Flags = ApplicationFlags.DefaultFlags;
		OnActivate += (self, args) =>
		{
			window.SetApplication(this);
			window.Start();
		};
	}
}
