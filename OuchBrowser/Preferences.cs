using Adw;
using Gtk;

namespace OuchBrowser;

[GObject.Subclass<Adw.Dialog>("OuchPreferencesDialog")]
[Template<GResource>("/page/codeberg/shrimple/OuchBrowser/ui/preferences.ui")]
internal partial class Preferences
{
#pragma warning disable CS0649
	[Connect] private ToastOverlay? toastOverlay;
	[Connect] private NavigationSplitView? nsv;
	[Connect] private ViewStack? view;
	[Connect] private SwitchRow? setting_search_autocomplete;
	[Connect] private SwitchRow? setting_bang_autocomplete;
	[Connect] private SwitchRow? setting_devtools;
	[Connect] private ComboRow? setting_search_engine;
	[Connect] private ComboRow? setting_zoom;
	[Connect] private ComboRow? setting_peek_trigger;
	[Connect] private ButtonRow? setting_clear_bang_rankings;
#pragma warning restore CS0649
	private Window? window;

	public static Preferences NewWithWindow(Window window)
	{
		var obj = NewWithProperties([]);
		obj.window = window;

		return obj;
	}

	partial void Initialize()
	{
		nsv!.SetShowContent(true);

		ViewStackPage page = view!.GetPage(view!.GetVisibleChild()!);
		nsv!.GetContent()!.SetTitle(page!.GetTitle()!);

		// the user has the option to modify settings via means, such as dconf
		// Editor and the gsettings command, while Ouch Browser is open.
		// Every time the dialog is presented, it should use the current
		// values.
		OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "parent")
			{
				setting_search_autocomplete!.SetActive(settings.GetBoolean("search-autocomplete-enabled"));
				setting_bang_autocomplete!.SetActive(settings.GetBoolean("bang-autocomplete-enabled"));
				setting_devtools!.SetActive(settings.GetBoolean("devtools-enabled"));
				switch (settings.GetString("search-engine"))
				{
					case "https://duckduckgo.com/?q={0}":
						setting_search_engine!.SetSelected(0);
						break;
					case "https://ecosia.org/search?q={0}":
						setting_search_engine!.SetSelected(1);
						break;
					case "https://google.com/search?q={0}":
						setting_search_engine!.SetSelected(2);
						break;
					case "https://kagi.com/search?q={0}":
						setting_search_engine!.SetSelected(3);
						break;
				}
				switch (settings.GetDouble("zoom"))
				{
					case 0.25:
						setting_zoom!.SetSelected(0);
						break;
					case 0.33:
						setting_zoom!.SetSelected(1);
						break;
					case 0.5:
						setting_zoom!.SetSelected(2);
						break;
					case 0.67:
						setting_zoom!.SetSelected(3);
						break;
					case 0.75:
						setting_zoom!.SetSelected(4);
						break;
					case 0.8:
						setting_zoom!.SetSelected(5);
						break;
					case 0.9:
						setting_zoom!.SetSelected(6);
						break;
					case 1:
						setting_zoom!.SetSelected(7);
						break;
					case 1.1:
						setting_zoom!.SetSelected(8);
						break;
					case 1.25:
						setting_zoom!.SetSelected(9);
						break;
					case 1.5:
						setting_zoom!.SetSelected(10);
						break;
					case 1.75:
						setting_zoom!.SetSelected(11);
						break;
					case 2:
						setting_zoom!.SetSelected(12);
						break;
					case 2.5:
						setting_zoom!.SetSelected(13);
						break;
					case 3:
						setting_zoom!.SetSelected(14);
						break;
					case 4:
						setting_zoom!.SetSelected(15);
						break;
					case 5:
						setting_zoom!.SetSelected(16);
						break;
				}
				switch (settings.GetString("peek-trigger"))
				{
					case "65505":
						setting_peek_trigger!.SetSelected(0);
						break;
					case "65507":
						setting_search_engine!.SetSelected(1);
						break;
					case "65513":
						setting_search_engine!.SetSelected(2);
						break;
				}
				if ((int)settings.GetValue("bang-rankings").NChildren() == 0) setting_clear_bang_rankings!.SetSensitive(false);
				else setting_clear_bang_rankings!.SetSensitive(true);
			}
		};

		view!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "visible-child")
			{
				ViewStackPage page = view!.GetPage(view!.GetVisibleChild()!);
				nsv!.GetContent()!.SetTitle(__(page!.GetTitle()!));
				nsv!.SetShowContent(true);
			}
		};

		setting_search_autocomplete!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active") settings.SetBoolean("search-autocomplete-enabled", setting_search_autocomplete.GetActive());
		};

		setting_bang_autocomplete!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active") settings.SetBoolean("bang-autocomplete-enabled", setting_bang_autocomplete.GetActive());
		};

		setting_devtools!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active")
			{
				settings.SetBoolean("devtools-enabled", setting_devtools.GetActive());
				int npages = window!.tabView!.GetNPages()!;

				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.tabView!.GetNthPage(i - 1);
					WebKit.WebView webview = (WebKit.WebView)page.GetChild();
					WebKit.Settings settings = webview.GetSettings();
					settings.SetEnableDeveloperExtras(setting_devtools.GetActive());
				}
			}
		};

		setting_search_engine!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				switch (setting_search_engine!.GetSelected())
				{
					case 0:
						settings.SetString("search-engine", "https://duckduckgo.com/?q={0}");
						break;
					case 1:
						settings.SetString("search-engine", "https://ecosia.org/search?q={0}");
						break;
					case 2:
						settings.SetString("search-engine", "https://google.com/search?q={0}");
						break;
					case 3:
						settings.SetString("search-engine", "https://kagi.com/search?q={0}");
						break;
				}
			}
		};

		setting_zoom!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				double prev_default = settings.GetDouble("zoom");

				switch (setting_zoom!.GetSelected())
				{
					case 0:
						settings.SetDouble("zoom", 0.25);
						break;
					case 1:
						settings.SetDouble("zoom", 0.33);
						break;
					case 2:
						settings.SetDouble("zoom", 0.5);
						break;
					case 3:
						settings.SetDouble("zoom", 0.67);
						break;
					case 4:
						settings.SetDouble("zoom", 0.75);
						break;
					case 5:
						settings.SetDouble("zoom", 0.8);
						break;
					case 6:
						settings.SetDouble("zoom", 0.9);
						break;
					case 7:
						settings.SetDouble("zoom", 1);
						break;
				}

				int npages = window!.tabView!.GetNPages()!;
				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.tabView!.GetNthPage(i - 1);
					WebKit.WebView webview = (WebKit.WebView)page.GetChild();
					if (prev_default == webview.GetZoomLevel()) webview.SetZoomLevel(settings.GetDouble("zoom"));
				}
			}
		};

		setting_peek_trigger!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				switch (setting_peek_trigger!.GetSelected())
				{
					case 0:
						settings.SetEnum("peek-trigger", 65505);
						break;
					case 1:
						settings.SetEnum("peek-trigger", 65507);
						break;
					case 2:
						settings.SetEnum("peek-trigger", 65513);
						break;
				}
			}
		};

		setting_clear_bang_rankings!.OnActivated += (_, _) =>
		{
			settings.Reset("bang-rankings");
			setting_clear_bang_rankings.SetSensitive(false);
			toastOverlay!.AddToast(Toast.New(__("Cleared All Ranks")));
		};
	}

	public void FocusPane(string section)
	{
		view!.SetVisibleChildName(section);
	}
}
