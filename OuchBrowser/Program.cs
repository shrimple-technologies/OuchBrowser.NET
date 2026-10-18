namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
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
