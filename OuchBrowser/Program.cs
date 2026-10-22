global using static Lunaris.Gettext.Gettext;
using Lunaris.Gettext;

namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
		BindTextDomain("OuchBrowser", "/usr/share/locale");
		SetTextDomain("OuchBrowser");
		BindTextDomainCodeset("OuchBrowser", "UTF-8");
		
		Gtk.Module.Initialize();
		GirCore.Integration.Initialize();

		var resourceBasePath = new GObject.Value(GObject.Type.String);
		resourceBasePath.SetString("/one/lunaris/OuchBrowser");

		var app = Application.NewWithProperties([
			new GObject.ConstructArgument("resource-base-path", resourceBasePath)
		]);
		return app.RunWithSynchronizationContext(args);
	}
}
