using Adw;
using WebKit;

namespace OuchBrowser;

public class View
{
	private readonly TabView view;
	private readonly UI.Window Window;

	public View(TabView tabview, UI.Window window)
	{
		view = tabview;
		Window = window;
		WebKit.Module.Initialize();
	}

	private static Settings InitSettings()
	{
		Settings settings = Settings.New();

		settings.SetDefaultFontFamily("serif");
		settings.SetSansSerifFontFamily("Adwaita Sans");
		settings.SetSerifFontFamily("Noto Serif");
		settings.SetMonospaceFontFamily("Adwaita Mono");
		settings.SetEnableBackForwardNavigationGestures(true);
		settings.SetEnableDeveloperExtras(true);

		return settings;
	}

	public WebView AddTab(string url, bool pinned)
	{
		Settings settings = InitSettings();
		WebView webview = WebView.New();

		webview.SetSettings(settings);
		webview.LoadUri(url);
		TabPage page;

		if (pinned)
		{
			page = view.AppendPinned(webview);
		}
		else
		{
			page = view.Append(webview);
			view.SetSelectedPage(page);
		}

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
				case "favicon":
					/* this doesn't work for some reason
					var favicon = webview.GetFavicon();
					page.SetIcon(favicon);
					Console.WriteLine("favicon set"); */
					break;
			}
		};

		webview.OnLoadChanged += (_, load_event) =>
		{
			switch (load_event.LoadEvent)
			{
				case LoadEvent.Started:
					Window.refresh!.SetSensitive(true);
					Window.url_button!.SetSensitive(true);
					Window.sidebar_toggle!.SetSensitive(true);
					Window.refresh!.SetIconName("cross-large-symbolic");
					page.SetLoading(true);
					break;
				case LoadEvent.Finished:
					Window.refresh!.SetIconName("view-refresh-symbolic");
					page.SetLoading(false);
					break;
			}
		};

		return webview;
	}
}
