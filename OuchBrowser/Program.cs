namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
		var app = new Application();
		return app.RunWithSynchronizationContext(args);
	}
}
