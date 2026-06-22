using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.Utils;

namespace OuchBrowser.UI;

internal class Preferences : Adw.Dialog
{
#pragma warning disable CS0649
	private readonly Builder? builder;
	[Connect] private readonly ToastOverlay? toast_overlay;
	[Connect] private readonly NavigationSplitView? nsv;
	[Connect] private readonly ViewStack? view;
	[Connect] private readonly SwitchRow? setting_search_autocomplete;
	[Connect] private readonly SwitchRow? setting_bang_autocomplete;
	[Connect] private readonly SwitchRow? setting_devtools;
	[Connect] private readonly ComboRow? setting_search_engine;
	[Connect] private readonly ComboRow? setting_zoom;
	[Connect] private readonly ComboRow? setting_peek_trigger;
	[Connect] private readonly ButtonRow? setting_clear_bang_rankings;
	[Connect] private readonly Button? setting_custom_bangs_new;
	[Connect] private readonly PreferencesGroup? setting_custom_bangs_list;
#pragma warning restore CS0649

	public Preferences(Window window) : base()
	{
		builder = new Builder();
		builder.SetTranslationDomain("OuchBrowser");
		builder.AddFromResource("/page/codeberg/shrimple/OuchBrowser/ui/preferences.ui");
		builder.Connect(this);

		nsv!.SetShowContent(true);

		Child = toast_overlay!;
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
				foreach (KeyValuePair<string, CustomBang> bang in Bangs.GetCustomBangs())
				{
					ActionRow row = ActionRow.New();
					Image image = Image.NewFromIconName("document-edit-symbolic");
					image.SetMarginEnd(12);
					row.SetTitle(bang.Value.WebsiteName);
					row.SetSubtitle($"!{bang.Key}");
					row.AddSuffix(image);
					row.SetActivatable(true);
					row.OnActivated += (_, _) =>
					{
						Adw.AlertDialog alert = Adw.AlertDialog.New(null, null);
						alert.AddResponse("cancel", __("Cancel"));
						alert.AddResponse("edit", __("Edit"));
						alert.SetResponseAppearance("edit", ResponseAppearance.Suggested);
						alert.SetDefaultResponse("edit");
						alert.SetCloseResponse("cancel");

						PreferencesGroup group = PreferencesGroup.New();
						EntryRow bang_trigger = EntryRow.New();
						EntryRow bang_name = EntryRow.New();
						EntryRow bang_url = EntryRow.New();
						ActionRow bang_instructions = ActionRow.New();

						bang_trigger.SetText(bang.Key);
						bang_name.SetText(bang.Value.WebsiteName);
						bang_url.SetText(bang.Value.TemplateUrl);

						bang_trigger.AddPrefix(Image.NewFromIconName("exclaimation-symbolic"));
						bang_name.AddPrefix(Image.NewFromIconName("document-edit-symbolic"));
						bang_url.AddPrefix(Image.NewFromIconName("web-symbolic"));
						bang_instructions.AddPrefix(Image.NewFromIconName("dialog-information-symbolic"));
						bang_trigger.SetTitle(__("Trigger"));
						bang_name.SetTitle(__("Name"));
						bang_url.SetTitle(__("URL"));
						bang_url.SetInputPurpose(InputPurpose.Url);
						bang_instructions.SetSubtitle("Enter the desired trigger, name, and URL for your custom !bang. Replace the query in the URL with <tt>{{{s}}}</tt>");
						bang_instructions.SetUseMarkup(true);

						group.Add(bang_trigger);
						group.Add(bang_name);
						group.Add(bang_url);
						group.Add(bang_instructions);
						alert.SetExtraChild(group);

						alert.Present(window);
						bang_trigger.GrabFocus();
					};
					setting_custom_bangs_list!.Add(row);
				}
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
				int npages = window.tabview!.GetNPages()!;

				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.tabview!.GetNthPage(i - 1);
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

				int npages = window.tabview!.GetNPages()!;
				for (int i = 1; i <= npages; i++)
				{
					TabPage page = window.tabview!.GetNthPage(i - 1);
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
			toast_overlay!.AddToast(Toast.New(__("Cleared All Ranks")));
		};

		setting_custom_bangs_new!.OnClicked += (_, _) =>
		{
			Adw.AlertDialog alert = Adw.AlertDialog.New(null, null);
			alert.AddResponse("cancel", __("Cancel"));
			alert.AddResponse("create", __("Create"));
			alert.SetResponseAppearance("create", ResponseAppearance.Suggested);
			alert.SetDefaultResponse("create");
			alert.SetCloseResponse("cancel");

			PreferencesGroup group = PreferencesGroup.New();
			EntryRow bang_trigger = EntryRow.New();
			EntryRow bang_name = EntryRow.New();
			EntryRow bang_url = EntryRow.New();
			ActionRow bang_instructions = ActionRow.New();

			bang_trigger.AddPrefix(Image.NewFromIconName("exclaimation-symbolic"));
			bang_name.AddPrefix(Image.NewFromIconName("document-edit-symbolic"));
			bang_url.AddPrefix(Image.NewFromIconName("web-symbolic"));
			bang_instructions.AddPrefix(Image.NewFromIconName("dialog-information-symbolic"));
			bang_trigger.SetTitle(__("Trigger"));
			bang_name.SetTitle(__("Name"));
			bang_url.SetTitle(__("URL"));
			bang_url.SetInputPurpose(InputPurpose.Url);
			// TRANSLATORS: Do not translate "<tt>{{{{{{s}}}}}}</tt>". This renders as {{{s}}} in the user interface.
			bang_instructions.SetSubtitle(__("Enter the desired trigger, name, and URL for your custom !bang. Replace the query in the URL with <tt>{{{{{{s}}}}}}</tt>"));
			bang_instructions.SetUseMarkup(true);

			group.Add(bang_trigger);
			group.Add(bang_name);
			group.Add(bang_url);
			group.Add(bang_instructions);
			alert.SetExtraChild(group);

			alert.Present(window);
			bang_trigger.GrabFocus();
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

		GObject.Value boolean = new();
		GObject.Value number = new();
		boolean.Init(GObject.Type.Boolean);
		number.Init(GObject.Type.Int);

		boolean.SetBoolean(true);
		breakpoint.AddSetter(nsv!, "collapsed", boolean);

		number.SetInt(1);
		breakpoint.AddSetter((Widget)builder!.GetObject("vss")!, "mode", number); // set Adw.ViewSwitcherSidebar:mode to Adw.SidebarMode.page (internally, we do not support libadwaita 1.9)

		return breakpoint;
	}
}
