using System.Runtime.Versioning;
using Adw;

namespace OuchBrowser;

public class View {
	private readonly TabView VIEW;

	[SupportedOSPlatform("linux")]
	public View(TabView view) {
		VIEW = view;
		WebKit.Module.Initialize();
	}

	[SupportedOSPlatform("linux")]
	public WebKit.WebView AddTab(string url) {
		WebKit.WebView webview = WebKit.WebView.New();
		webview.LoadUri(url);

		TabPage page = VIEW.Append(webview);

		return webview;
	}
}
