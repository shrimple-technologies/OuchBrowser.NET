using Adw;
using Gtk;

namespace OuchBrowser.UI;

internal class RoomsOverview : Adw.Dialog
{
#pragma warning disable CS0649
	[Connect] private readonly NavigationSplitView? nsv;
	[Connect] private readonly ViewStack? view;
	[Connect] private readonly ListBox? lb;
	[Connect] private readonly ViewStack? tab_stack;
	[Connect] private readonly Button? new_tab_button;
#pragma warning restore CS0649

	public RoomsOverview(Window window) : base()
	{
		var builder = new Builder();
		builder.SetTranslationDomain("OuchBrowser");
		builder.AddFromResource("/site/srht/shrimple/OuchBrowser/ui/rooms_overview.ui");
		builder.Connect(this);

		Child = nsv!;
		HeightRequest = 360;
		WidthRequest = 360;
		ContentHeight = 500;
		ContentWidth = 800;

		AddBreakpoint(SetupBreakpoint());

		ViewStackPage page = view!.GetPage(view!.GetVisibleChild()!);
		nsv!.GetContent()!.SetTitle(page!.GetTitle()!);
		
		new_tab_button.OnClicked += (_, _) =>
		{
			Close();
			window.ActivateAction("palette-new", null);
		};

		OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "parent")
			{
				tab_stack.SetVisibleChildName("placeholder");
				nsv!.SetShowContent(true);

				lb!.RemoveAll();

				for (int i = 0; i < window.tabview!.GetNPages(); i++)
				{
					tab_stack.SetVisibleChildName("tabs");
					TabPage tabpage = window.tabview!.GetNthPage(i)!;
					Adw.ActionRow row = Adw.ActionRow.New();

					row.SetUseMarkup(false);
					row.SetTitle(tabpage.GetTitle());
					row.SetSubtitle(tabpage.GetKeyword()!);
					row.AddPrefix(Gtk.Image.NewFromGicon(tabpage.GetIcon()!));
					row.OnActivate += (_, _) =>
					{
						window.tabview.SetSelectedPage(tabpage);
						Close();
					};

					lb.Append(row);
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
		boolean.Init(GObject.Type.Boolean);

		boolean.SetBoolean(true);
		breakpoint.AddSetter(nsv!, "collapsed", boolean);

		return breakpoint;
	}
}
