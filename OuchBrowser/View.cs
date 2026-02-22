using Adw;
using WebKit;

namespace OuchBrowser;

public class View
{
	private readonly UI.Window Window;

	public View(UI.Window window)
	{
		Window = window;
		WebKit.Module.Initialize();
	}

	public WebView AddTab(string url)
	{
		WebView webview = WebView.New();
		webview.LoadUri(url);

		TabPage page = Window.view!.Append(webview);

		webview.OnNotify += (_, args) =>
		{
			switch (args.Pspec.GetName())
			{
				case "title":
					string title = webview.GetTitle();
					page.SetTitle(title);
					break;
				case "uri":
					string current_uri = webview.GetUri();
					Uri uri = new Uri(current_uri);
					Window.hostname!.SetLabel(uri.Host);
					break;
				/* apparently there is a bug where this doesn't fire at all
				case "favicon":
					var favicon = webview.GetFavicon();
					page.SetIcon(favicon);
					Console.WriteLine("favicon set");
					break; */
			}
		};

		webview.OnLoadChanged += (_, load_event) =>
		{
			switch (load_event.LoadEvent)
			{
				case LoadEvent.Started:
					page.SetLoading(true);
					break;
				case LoadEvent.Finished:
					page.SetLoading(false);
					break;
			}
		};

		return webview;
	}
}
