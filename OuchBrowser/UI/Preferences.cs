using System.Reflection;
using Adw;
using GetText;
using Gtk;

namespace OuchBrowser.UI;

public class Preferences : Adw.Dialog
{
	[Connect] public readonly NavigationSplitView? nsv;
	[Connect] public readonly NavigationView? nv;
	[Connect] public readonly ViewStack? view;
	[Connect] public readonly SwitchRow? setting_search_autocomplete;
	[Connect] public readonly SwitchRow? setting_bang_autocomplete;
	[Connect] public readonly SwitchRow? setting_devtools;
	[Connect] public readonly ComboRow? setting_search_engine;
	[Connect] public readonly ComboRow? setting_zoom;

	public Preferences(UI.Window window) : base()
	{
		var builder = new Builder();
		builder.SetTranslationDomain("OuchBrowser");
		builder.AddFromString("/site/srht/shrimple/OuchBrowser/ui/preferences.ui");
		builder.Connect(this);

		nsv!.SetShowContent(true);

		Child = nsv!;
		HeightRequest = 360;
		WidthRequest = 360;
		ContentHeight = 500;
		ContentWidth = 800;

		AddBreakpoint(SetupBreakpoint());

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
				setting_search_autocomplete!.SetActive(window.settings.GetBoolean("search-autocomplete-enabled"));
				setting_bang_autocomplete!.SetActive(window.settings.GetBoolean("bang-autocomplete-enabled"));
				setting_devtools!.SetActive(window.settings.GetBoolean("devtools-enabled"));
				switch (window.settings.GetString("search-engine"))
				{
					case "https://bing.com/search?q=":
						setting_search_engine!.SetSelected(0);
						break;
					case "https://duckduckgo.com/?q=":
						setting_search_engine!.SetSelected(1);
						break;
					case "https://ecosia.org/search?q=":
						setting_search_engine!.SetSelected(2);
						break;
					case "https://google.com/search?q=":
						setting_search_engine!.SetSelected(3);
						break;
					case "https://kagi.com/search?q=":
						setting_search_engine!.SetSelected(4);
						break;
				}
				switch (window.settings.GetDouble("zoom"))
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
			}
		};

		view!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "visible-child")
			{
				ViewStackPage page = view!.GetPage(view!.GetVisibleChild()!);
				nsv!.GetContent()!.SetTitle(window.gettext.GetString(page!.GetTitle()!));
				if (view!.GetVisibleChildName() == "extensions")
				{
					nv!.PushByTag("extensions");
				}
				else
				{
					nsv!.SetShowContent(true);
				}
			}
		};

		setting_search_autocomplete!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active") window.settings.SetBoolean("search-autocomplete-enabled", setting_search_autocomplete.GetActive());
		};

		setting_bang_autocomplete!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active") window.settings.SetBoolean("bang-autocomplete-enabled", setting_bang_autocomplete.GetActive());
		};

		setting_devtools!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "active")
			{
				window.settings.SetBoolean("devtools-enabled", setting_devtools.GetActive());
				int npages = window.view!.GetNPages()!;

				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.view!.GetNthPage(i - 1);
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
						window.settings.SetString("search-engine", "https://bing.com/search?q=");
						break;
					case 1:
						window.settings.SetString("search-engine", "https://duckduckgo.com/?q=");
						break;
					case 2:
						window.settings.SetString("search-engine", "https://ecosia.org/search?q=");
						break;
					case 3:
						window.settings.SetString("search-engine", "https://google.com/search?q=");
						break;
					case 4:
						window.settings.SetString("search-engine", "https://kagi.com/search?q=");
						break;
				}
			}
		};

		setting_zoom!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				double prev_default = window.settings.GetDouble("zoom");

				switch (setting_zoom!.GetSelected())
				{
					case 0:
						window.settings.SetDouble("zoom", 0.25);
						break;
					case 1:
						window.settings.SetDouble("zoom", 0.33);
						break;
					case 2:
						window.settings.SetDouble("zoom", 0.5);
						break;
					case 3:
						window.settings.SetDouble("zoom", 0.67);
						break;
					case 4:
						window.settings.SetDouble("zoom", 0.75);
						break;
					case 5:
						window.settings.SetDouble("zoom", 0.8);
						break;
					case 6:
						window.settings.SetDouble("zoom", 0.9);
						break;
					case 7:
						window.settings.SetDouble("zoom", 1);
						break;
				}

				int npages = window.view!.GetNPages()!;
				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.view!.GetNthPage(i - 1);
					WebKit.WebView webview = (WebKit.WebView)page.GetChild();
					if (prev_default == webview.GetZoomLevel()) webview.SetZoomLevel(window.settings.GetDouble("zoom"));
				}
			}
		};
	}

	public void FocusPane(string section)
	{
		view!.SetVisibleChildName(section);
	}

	private Breakpoint SetupBreakpoint()
	{
		// equivalent to condition ("max-width: 600sp") in blueprint
		BreakpointCondition condition = BreakpointCondition.NewLength(
			BreakpointConditionLengthType.MaxWidth,
			600,
			LengthUnit.Sp
		);
		Breakpoint breakpoint = Breakpoint.New(condition);

		GObject.Value boolean = new GObject.Value();
		GObject.Value number = new GObject.Value();
		boolean.Init(GObject.Type.Boolean);
		number.Init(GObject.Type.Int);

		boolean.SetBoolean(true);
		breakpoint.AddSetter(nsv!, "collapsed", boolean);

		return breakpoint;
	}
}
