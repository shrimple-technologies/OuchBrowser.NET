using System.Reflection;
using Adw;
using GetText;
using Gtk;

namespace OuchBrowser.UI;

public class Preferences : Adw.Dialog
{
	[Connect] public readonly NavigationSplitView? nsv;
	[Connect] public readonly ViewStack? view;
	[Connect] public readonly SwitchRow? setting_search_autocomplete;
	[Connect] public readonly SwitchRow? setting_bang_autocomplete;
	[Connect] public readonly SwitchRow? setting_devtools;
	[Connect] public readonly ComboRow? setting_search_engine;

	public Preferences(Gio.Settings settings, ICatalog gettext) : base()
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Preferences.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(this);

		nsv!.SetShowContent(true);

		Child = nsv!;
		HeightRequest = 500;
		WidthRequest = 360;
		ContentWidth = 800;

		AddBreakpoint(SetupBreakpoint());

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
			}
		};

		view!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "visible-child") nsv!.SetShowContent(true);
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
			if (args.Pspec.GetName() == "active") settings.SetBoolean("devtools-enabled", setting_devtools.GetActive());
		};

		setting_search_engine!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "selected")
			{
				switch (setting_search_engine!.GetSelected())
				{
					case 0:
						settings.SetString("search-engine", "https://bing.com/search?q=");
						break;
					case 1:
						settings.SetString("search-engine", "https://duckduckgo.com/?q=");
						break;
					case 2:
						settings.SetString("search-engine", "https://ecosia.org/search?q=");
						break;
					case 3:
						settings.SetString("search-engine", "https://google.com/search?q=");
						break;
					case 4:
						settings.SetString("search-engine", "https://kagi.com/search?q=");
						break;
				}
			}
		};
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

		boolean.SetBoolean(false);
		breakpoint.AddSetter(view!, "enable-transitions", boolean);

		return breakpoint;
	}
}
