using Adw;
using Gtk;

namespace OuchBrowser;

[GObject.Subclass<Adw.Dialog>("RoomsOverview")]
[Template<GResource>("/page/codeberg/shrimple/OuchBrowser/ui/rooms-overview.ui")]
internal partial class RoomsOverview
{
#pragma warning disable CS0649
	[Connect] private NavigationSplitView? nsv;
	[Connect] private ViewStack? view;
	[Connect] private ListBox? lb;
	[Connect] private ViewStack? tab_stack;
	[Connect] private Button? new_tab_button;
	[Connect] private WindowTitle? wt;
#pragma warning restore CS0649
	private Window? window;

	public static RoomsOverview NewWithWindow(Window window)
	{
		var obj = NewWithProperties([]);
		obj.window = window;

		return obj;
	}

	partial void Initialize()
	{
		ViewStackPage page = view!.GetPage(view!.GetVisibleChild()!);
		nsv!.GetContent()!.SetTitle(page!.GetTitle()!);

		new_tab_button!.OnClicked += (_, _) =>
		{
			Close();
			window!.ActivateAction("palette-new", null);
		};

		OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "parent")
			{
				tab_stack!.SetVisibleChildName("placeholder");
				nsv!.SetShowContent(true);

				lb!.RemoveAll();

				for (int i = 0; i < window!.tabView!.GetNPages(); i++)
				{
					tab_stack.SetVisibleChildName("tabs");
					TabPage tabpage = window.tabView!.GetNthPage(i)!;
					ActionRow row = ActionRow.New();
					Button btn = Button.NewFromIconName("cross-large-symbolic");

					btn.AddCssClass("flat");
					btn.SetValign(Align.Center);
					btn.SetTooltipText(__("Close Tab"));
					btn.OnClicked += (_, _) =>
					{
						Close();
						window.tabView.SetSelectedPage(tabpage);
						window.ActivateAction("tab-close", null);
					};

					row.SetActivatable(true);
					row.SetUseMarkup(false);
					row.SetTitle(tabpage.GetTitle());
					row.SetSubtitle(tabpage.GetKeyword()!);
					row.AddPrefix(Image.NewFromGicon(tabpage.GetIcon()!));
					row.AddSuffix(btn);
					row.OnActivated += (_, _) =>
					{
						Close();
						window.tabView.SetSelectedPage(tabpage);
					};

					lb.Append(row);
				}

				if (window.tabView!.GetNPages() > 0)
				{
					wt!.SetSubtitle(N__("{0} tab", "{0} tabs", window.tabView!.GetNPages()));
				}
				else
				{
					wt!.SetSubtitle("");
				}
			}
		};
	}
}
