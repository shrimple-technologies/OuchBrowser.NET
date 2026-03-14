using Adw;
using WebKit;
using OuchBrowser.Utils;

namespace OuchBrowser;

public class View
{
	public readonly TabView view;
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
		settings.SetSansSerifFontFamily("Noto Sans");
		settings.SetSerifFontFamily("Noto Serif");
		settings.SetMonospaceFontFamily("Noto Mono");
		settings.SetEnableDeveloperExtras(true);

		return settings;
	}

	public WebView AddTab(string url, bool pinned)
	{
		Settings settings = InitSettings();
		WebView webview = WebView.New();

		webview.SetSettings(settings);
		webview.LoadUri(url);
		webview.SetZoomLevel(Window.settings.GetDouble("zoom"));
		TabPage page;

		if (pinned)
		{
			page = view.AppendPinned(webview);
		}
		else
		{
			page = view.Append(webview);
		}

		view.SetSelectedPage(page);

		Uri uri = new Uri(url);
		if (uri.IsLoopback)
		{
			Window.hostname!.SetLabel(uri.Host + ":" + uri.Port);
			page.SetTitle(uri.Host + ":" + uri.Port);
		}
		else
		{
			// TODO: this should also show the search query too maybe
			Window.hostname!.SetLabel(uri.Host);
			page.SetTitle(uri.Host);
		}

		Connect(webview, Window, page);

		page.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				string current_uri = webview.GetUri();
				Uri uri = new Uri(current_uri);
				if (uri.IsLoopback)
				{
					Window.hostname!.SetLabel(uri.Host + ":" + uri.Port);
				}
				else
				{
					// TODO: this should also show the search query too maybe
					Window.hostname!.SetLabel(uri.Host);
				}

				if (webview.GetIsLoading())
				{
					Window.refresh!.SetTooltipText(Window.gettext.GetString("Stop loading"));
					Window.refresh!.SetIconName("cross-large-symbolic");
				}
				else
				{
					Window.refresh!.SetTooltipText(Window.gettext.GetString("Refresh"));
					Window.refresh!.SetIconName("view-refresh-symbolic");
				}

				Connect(webview, Window, page);
			}
		};

		return webview;
	}

	private static void Connect(WebView webview, UI.Window window, TabPage page)
	{
		webview.OnNotify += (_, args) =>
		{
			switch (args.Pspec.GetName())
			{
				case "title":
					string title = webview.GetTitle();
					page.SetTitle(title);
					break;
			}
		};

		webview.OnLoadChanged += async (_, load_event) =>
		{
			string current_uri = webview.GetUri();
			Uri uri = new Uri(current_uri);

			switch (load_event.LoadEvent)
			{
				case LoadEvent.Started:
					page.SetIcon(Gio.ThemedIcon.New("box-dotted-symbolic")); // set this placeholder first
					window.refresh!.SetSensitive(true);
					window.refresh!.SetTooltipText(window.gettext.GetString("Stop loading"));
					window.url_button!.SetSensitive(true);
					window.copy_link!.SetSensitive(true);
					window.website_settings!.SetSensitive(true);
					window.sidebar_toggle!.SetSensitive(true);
					window.refresh!.SetIconName("cross-large-symbolic");
					page.SetLoading(true);
					page.SetIcon(await Favicon.GetFavicon(uri.Host));
					break;
				case LoadEvent.Committed:
					if (uri.IsLoopback)
					{
						window.hostname!.SetLabel(uri.Host + ":" + uri.Port);
					}
					else
					{
						// TODO: this should also show the search query too maybe
						window.hostname!.SetLabel(uri.Host);
					}
					break;
				case LoadEvent.Finished:
					window.refresh!.SetTooltipText(window.gettext.GetString("Refresh"));
					window.refresh!.SetIconName("view-refresh-symbolic");
					page.SetLoading(false);

					if (webview.CanGoBack())
					{
						window.go_back!.SetSensitive(true);
					}
					else
					{
						window.go_back!.SetSensitive(false);
					}

					if (webview.CanGoForward())
					{
						window.go_forward!.SetSensitive(true);
					}
					else
					{
						window.go_forward!.SetSensitive(false);
					}

					break;
			}
		};
	}

	public string[] GetAllTabUrls()
	{
		List<string> urls = new List<string>();

		if (view.GetNPages() != 0)
		{
			for (int i = 1; i < view.GetNPages(); i++)
			{
				TabPage page = view.GetNthPage(i);
				WebView web_view = (WebView)page.Child!;

				urls.Add(web_view.GetUri());
			}
		}

		return urls.ToArray();
	}
}
