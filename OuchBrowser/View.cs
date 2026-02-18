using Adw;

namespace OuchBrowser;

public class View {
	private readonly TabView VIEW;

	public View(TabView view) {
		VIEW = view;
		WebKit.Module.Initialize();
	}

	public WebKit.WebView AddTab(string url) {
		WebKit.WebView webview = WebKit.WebView.New();
		webview.LoadUri(url);

		TabPage page = VIEW.Append(webview);

		return webview;
	}
}
