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
					window.sidebar_toggle!.SetSensitive(true);
					window.refresh!.SetIconName("cross-large-symbolic");
					page.SetLoading(true);
					page.SetIcon(await GetFavicon(uri.Host));
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
					break;
			}
		};
	}

	private static async Task<Gio.Icon> GetFavicon(string domain)
	{
		try
		{
			using var http = new HttpClient();
			using var remoteStream = await http.GetStreamAsync($"https://www.google.com/s2/favicons?domain={domain}&sz=16");
			using var memoryStream = new MemoryStream();

			await remoteStream.CopyToAsync(memoryStream);
			byte[] bytes = memoryStream.ToArray();

			// we are in a try-catch block, we can just simply throw, and set the placeholder icon
			if (bytes.Length == 0) throw new Exception();

			using var gBytes = GLib.Bytes.New(bytes);
			Gio.Icon icon = Gio.BytesIcon.New(gBytes);

			return icon;
		}
		catch // there is no icon
		{
			return Gio.ThemedIcon.New("box-dotted-symbolic");
		}
	}
}
