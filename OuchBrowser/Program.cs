namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
		GirCore.Integration.Initialize();
		
		var app = Application.NewWithProperties([]);
		return app.RunWithSynchronizationContext(args);
	}
}
