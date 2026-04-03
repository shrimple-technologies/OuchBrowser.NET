using System.Reflection;
using Adw;
using GetText;
using Gtk;

namespace OuchBrowser.UI;

public class Window : Adw.ApplicationWindow
{
	public Gio.Settings settings;
	public ICatalog gettext;
	[Connect] public readonly Adw.HeaderBar? content_headerbar;
	[Connect] public readonly ToolbarView? content_toolbar;
	[Connect] public readonly Button? content_sidebar_toggle;
	[Connect] public readonly Frame? frame;
	[Connect] public readonly Label? hostname;
	[Connect] public readonly OverlaySplitView? osv;
	[Connect] public readonly TabOverview? overview;
	[Connect] public readonly ToggleButton? sidebar_toggle;
	[Connect] public readonly ToolbarView? sidebar_toolbar;
	[Connect] public readonly ToastOverlay? toast_overlay;
	[Connect] public readonly Bin? topbar_hover;
	[Connect] public readonly Adw.Dialog? url_dialog;
	[Connect] public readonly Entry? url_entry;
	[Connect] public readonly Button? url_button;
	[Connect] public readonly TabView? view;
	[Connect] public readonly Button? go_back;
	[Connect] public readonly Button? go_forward;
	[Connect] public readonly Button? refresh;
	[Connect] public readonly WindowControls? left_controls;
	[Connect] public readonly Button? copy_link;
	[Connect] public readonly MenuButton? website_settings;
	[Connect] public readonly Revealer? url_autocomplete;
	[Connect] public readonly Stack? url_stack;
	[Connect] public readonly Stack? url_disclosure;
	[Connect] public readonly Label? url_custom_disclosure;
	[Connect] public readonly Button? url_bar_button;
	[Connect] public readonly Image? url_favicon;
	[Connect] public readonly Revealer? card_revealer;
	[Connect] public readonly ListBox? card_listbox;
	[Connect] public readonly Revealer? url_disclosure_revealer;
	[Connect] public readonly Box? url_preview;
	[Connect] public readonly Label? url_preview_label;
	[Connect] public readonly MultiLayoutView? mlv;

	public Window(Adw.Application app) : base()
	{
		settings = Gio.Settings.New("site.srht.shrimple.OuchBrowser");
		gettext = new Catalog("OuchBrowser", "/usr/share/locale");

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Window.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.SetTranslationDomain("OuchBrowser");
		builder.AddFromString(xml, -1);
		builder.Connect(this);

		Content = builder.GetObject("overview") as Widget;
		Application = app;

		var hover_controller_topbar = EventControllerMotion.New();
		var hover_controller_headerbar = EventControllerMotion.New();
		topbar_hover!.AddController(hover_controller_topbar);
		content_headerbar!.AddController(hover_controller_headerbar);
		SetupHoverController(hover_controller_topbar);
		SetupHoverController(hover_controller_headerbar);

		AddBreakpoint(SetupBreakpoint());

		left_controls!.SetVisible(!left_controls!.GetEmpty());
		left_controls!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "empty") left_controls!.SetVisible(!left_controls!.GetEmpty());
		};

		Maximized = settings.GetBoolean("maximized");
		DefaultWidth = 1000;
		DefaultHeight = 600;
		WidthRequest = 360;
		HeightRequest = 500;
		Title = gettext.GetString("Ouch Browser");
	}

	private void SetupHoverController(EventControllerMotion controller)
	{
		controller.OnEnter += (_, _) =>
		{
			content_toolbar!.SetRevealTopBars(!(left_controls!.GetEmpty() == false && osv!.GetShowSidebar() == true));
		};
		controller.OnLeave += (_, _) =>
		{
			content_toolbar!.SetRevealTopBars(false);
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

		GObject.Value number = new GObject.Value();
		GObject.Value str = new GObject.Value();
		number.Init(GObject.Type.Int);
		str.Init(GObject.Type.String);

		str.SetString("mobile");
		breakpoint.AddSetter(mlv!, "layout-name", str);

		number.SetInt(10);
		breakpoint.AddSetter(frame!, "margin-start", number);

		number.SetInt(0);
		breakpoint.AddSetter(frame!, "margin-bottom", number);

		number.SetInt(-1);
		breakpoint.AddSetter(url_entry!, "width-request", number);

		return breakpoint;
	}
}
