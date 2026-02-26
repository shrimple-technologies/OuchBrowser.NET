using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Window : Adw.ApplicationWindow
{
	[Connect] public readonly Adw.HeaderBar? content_headerbar;
	[Connect] public readonly ToolbarView? content_toolbar;
	[Connect] public readonly Button? content_sidebar_toggle;
	[Connect] public readonly Frame? frame;
	[Connect] public readonly Label? hostname;
	[Connect] public readonly OverlaySplitView? osv;
	[Connect] public readonly ToggleButton? sidebar_toggle;
	[Connect] public readonly ToolbarView? sidebar_toolbar;
	[Connect] public readonly Bin? topbar_hover;
	[Connect] public readonly Adw.Dialog? url_dialog;
	[Connect] public readonly Entry? url_entry;
	[Connect] public readonly TabView? view;

	public Window(Adw.Application app) : base()
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Window.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
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

		// TODO: maybe make this a little bit less "hacky?"
		content_sidebar_toggle!.OnClicked += (_, _) =>
		{
			osv!.SetShowSidebar(true);
			sidebar_toggle!.SetActive(true);
		};

		DefaultWidth = 1000;
		DefaultHeight = 600;
		WidthRequest = 350;
		HeightRequest = 500;
		Title = "Ouch Browser";
	}

	private void SetupHoverController(EventControllerMotion controller)
	{
		controller.OnEnter += (_, _) =>
		{
			content_toolbar!.SetRevealTopBars(true);
		};
		controller.OnLeave += (_, _) =>
		{
			content_toolbar!.SetRevealTopBars(false);
		};
	}
}
