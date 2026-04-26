using Adw;
using Gtk;
using OuchBrowser.UI;
using OuchBrowser.Utils;
using WebKit;
using Object = GObject.Object;

namespace OuchBrowser;

internal partial class Window : Adw.ApplicationWindow
{
#pragma warning disable CS0649
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
	[Connect] public readonly TabView? tabview;
	[Connect] public readonly Button? go_back;
	[Connect] public readonly Button? go_forward;
	[Connect] public readonly Button? refresh;
	[Connect] public readonly WindowControls? left_controls;
	[Connect] public readonly WindowControls? right_controls;
	[Connect] public readonly Button? copy_link;
	[Connect] public readonly MenuButton? website_settings;
	[Connect] public readonly Revealer? url_autocomplete;
	[Connect] public readonly Stack? url_stack;
	[Connect] public readonly Stack? url_disclosure;
	[Connect] public readonly Label? url_custom_disclosure;
	[Connect] public readonly Button? url_bar_button;
	[Connect] public readonly Image? url_favicon;
	[Connect] public readonly Revealer? url_disclosure_revealer;
	[Connect] public readonly Box? url_preview;
	[Connect] public readonly Label? url_preview_label;
	[Connect] public readonly MultiLayoutView? mlv;
#pragma warning restore CS0649

	private string palette_state = "new_tab";
	private DateTime lastInvokeTime = DateTime.MinValue;
	private CancellationTokenSource? debounceCts;

	public Gio.Settings settings;
	private Preferences? preferences;
	private Adw.AboutDialog? about;
	private ShortcutsDialog? shortcuts;
	private Bangs? bangs;
	private View? view;

	public Window(Adw.Application app) : base()
	{
		settings = Gio.Settings.New("site.srht.shrimple.OuchBrowser");

		var builder = new Builder();
		builder.SetTranslationDomain("OuchBrowser");
		builder.AddFromResource("/site/srht/shrimple/OuchBrowser/ui/window.ui");
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
		DefaultWidth = settings.GetValue("initial-size").GetChildValue(0).GetInt32();
		DefaultHeight = settings.GetValue("initial-size").GetChildValue(1).GetInt32();
		WidthRequest = 360;
		HeightRequest = 360;
		Title = __("Ouch Browser");

		OnNotify += (_, args) =>
		{
			switch (args.Pspec.GetName())
			{
				case "maximized":
					settings.SetBoolean("maximized", Maximized);
					break;
				case "default-width":
				case "default-height":
					settings.SetValue(
						"initial-size",
						GLib.Variant.NewTuple([
							GLib.Variant.NewInt32(DefaultWidth),
								GLib.Variant.NewInt32(DefaultHeight)
						])
					);
					break;
			}
		};
	}

	public void OnActivate(Object app, EventArgs args)
	{
		preferences = new Preferences(this);
		about = About.New();
		shortcuts = Shortcuts.New();
		view = new View(tabview!, this);
		bangs = new Bangs(settings.GetString("search-engine"));

		SetupActions();

		go_back!.SetSensitive(false);
		go_forward!.SetSensitive(false);
		refresh!.SetSensitive(false);
		copy_link!.SetSensitive(false);
		url_button!.SetSensitive(false);
		website_settings!.SetSensitive(false);

		// TODO: maybe make this a little bit less "hacky?"
		content_sidebar_toggle!.OnClicked += (_, _) =>
		{
			osv!.SetShowSidebar(true);
			sidebar_toggle!.SetActive(true);
			frame!.SetMarginStart(0);
			url_preview!.SetMarginStart(30);
		};
		sidebar_toggle!.OnClicked += (_, _) =>
		{
			frame!.SetMarginStart(10);
			url_preview!.SetMarginStart(30);
		};

		url_entry!.OnActivate += (_, _) => url_bar_button!.Activate();

		HandlePaletteUpdate();
		HandlePaletteActivate();

		overview!.OnCreateTab += (_, _) =>
		{
			ActivateAction("palette-new", null);
			return tabview!.GetSelectedPage()!;
		};

		Present();

		if (settings.GetStrv("restore-tabs").Length == 0)
		{
			url_dialog!.Present(this);
		}
		else
		{
			foreach (string url in settings.GetStrv("restore-tabs")) view.AddTab(url, false);
		}
	}

	private void SetupActions()
	{
		var actions = new Actions(this, (Adw.Application)Application!);

		actions.AddAction("palette-new", ["<Ctrl>t"], (_, _) =>
		{
			overview!.SetOpen(false);

			EntryBuffer buffer = EntryBuffer.New("", -1);
			url_entry!.SetBuffer(buffer);
			url_dialog!.Present(this);
			url_entry!.GrabFocus();
			palette_state = "new_tab";
		});

		actions.AddAction("palette", ["<Ctrl>l", "<Alt>d"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0)
			{
				ActivateAction("palette-new", null);
			}
			else
			{
				TabPage page = tabview!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;
				EntryBuffer buffer = EntryBuffer.New(webview.GetUri(), -1);
				url_entry!.SetBuffer(buffer);
				url_dialog!.Present(this);
				url_entry!.GrabFocus();
				palette_state = "current_tab";
			}
		});

		actions.AddAction("sidebar-toggle", ["<Ctrl><Shift>s"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0) return;

			if (osv!.GetShowSidebar())
			{
				sidebar_toggle!.Activate();
			}
			else
			{
				content_sidebar_toggle!.Activate();
			}
		});

		foreach (int i in new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
		{
			actions.AddAction($"tab-{i}", [$"<Ctrl>{i}"], (_, _) =>
			{
				if (tabview!.GetNPages() < i) return;
				if (tabview!.GetNthPage(i - 1) == tabview!.GetSelectedPage()) return;

				TabPage page = tabview!.GetNthPage(i - 1);
				tabview!.SetSelectedPage(page);

				if (!osv!.GetShowSidebar())
				{
					Toast toast = Toast.New(page.GetTitle());
					toast.SetTimeout(1);
					toast_overlay!.DismissAll();
					toast_overlay!.AddToast(toast);
				}
			});
		}

		actions.AddAction("preferences", ["<Ctrl>comma"], (action, parameter) =>
		{
			preferences!.FocusPane("general");
			preferences.Present(this);
		});

		actions.AddAction("about", [], (action, parameter) =>
		{
			about!.Present(this);
		});

		actions.AddAction("shortcuts", ["<Ctrl>question"], (action, parameter) =>
		{
			shortcuts!.Present(this);
		});

		actions.AddAction("refresh", ["<Ctrl>r"], (_, _) =>
		{
			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			if (refresh!.GetIconName() == "cross-large-symbolic")
			{
				webview.StopLoading();
			}
			else
			{
				webview.Reload();
			}
		});

		actions.AddAction("hard-refresh", ["<Ctrl><Shift>r"], (_, _) =>
		{
			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.ReloadBypassCache();
		});

		actions.AddAction("zoom-in", ["<Ctrl>equal"], (_, _) =>
		{
			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			double[] levels = { 0.25, 0.33, 0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5 };

			for (int i = 0; i < levels.Length; i++)
			{
				try
				{
					if (levels[i] == webview.GetZoomLevel())
					{
						webview.SetZoomLevel(levels[i + 1]);
						toast.SetTitle($"{Math.Round(levels[i + 1] * 100)}%");
						toast.SetTimeout(1);
						toast_overlay!.DismissAll();
						toast_overlay!.AddToast(toast);
						break;
					}
				}
				catch (System.IndexOutOfRangeException)
				{
					Gdk.Display.GetDefault()!.Beep();
					break;
				}
			}
		});

		actions.AddAction("zoom-out", ["<Ctrl>minus"], (_, _) =>
		{
			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			double[] levels = { 0.25, 0.33, 0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5 };

			for (int i = levels.Length - 1; i >= 0; i--)
			{
				try
				{
					if (levels[i] == webview.GetZoomLevel())
					{
						webview.SetZoomLevel(levels[i - 1]);
						toast.SetTitle($"{Math.Round(levels[i - 1] * 100)}%");
						toast.SetTimeout(1);
						toast_overlay!.DismissAll();
						toast_overlay!.AddToast(toast);
						break;
					}
				}
				catch (System.IndexOutOfRangeException)
				{
					Gdk.Display.GetDefault()!.Beep();
					break;
				}
			}
		});

		actions.AddAction("zoom-reset", ["<Ctrl>0"], (_, _) =>
		{
			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			if (webview.GetZoomLevel() == settings.GetDouble("zoom")) return;

			webview.SetZoomLevel(settings.GetDouble("zoom"));
			toast.SetTitle($"{settings.GetDouble("zoom") * 100}%");
			toast.SetTimeout(1);
			toast_overlay!.DismissAll();
			toast_overlay!.AddToast(toast);
		});

		actions.AddAction("tab-close", ["<Ctrl>w"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0)
			{
				Close();
			}
			else
			{
				TabPage page = tabview!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				webview.TryClose();
				tabview!.ClosePage(tabview!.GetSelectedPage()!);
				if (tabview!.GetNPages() == 0)
				{
					refresh!.SetSensitive(false);
					go_back!.SetSensitive(false);
					go_forward!.SetSensitive(false);
					url_button!.SetSensitive(false);
					copy_link!.SetSensitive(false);
					website_settings!.SetSensitive(false);
					sidebar_toggle!.SetSensitive(false);
					sidebar_toggle!.SetActive(true);
					hostname!.SetLabel("");
					osv!.SetShowSidebar(true);
				}
			}
		});

		actions.AddAction("go-back", ["<Ctrl>Left"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0) return;

			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoBack();
		});

		actions.AddAction("go-forward", ["<Ctrl>Right"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0) return;

			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoForward();
		});

		actions.AddAction("copy-link", ["<Ctrl><Shift>c"], (_, _) =>
		{
			if (tabview!.GetNPages() == 0) return;

			TabPage page = tabview!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			string uri = webview.GetUri();
			Gdk.Display display = Gdk.Display.GetDefault()!;
			Gdk.Clipboard clipboard = display!.GetClipboard();
			clipboard.SetText(uri);

			toast.SetTitle(__("Link Copied"));
			toast.SetTimeout(1);
			toast_overlay!.DismissAll();
			toast_overlay!.AddToast(toast);

			webview.GoForward();
		});
	}

	private void SetupHoverController(EventControllerMotion controller)
	{
		controller.OnEnter += (_, _) =>
		{
			content_toolbar!.SetRevealTopBars(!(right_controls!.GetEmpty() && osv!.GetShowSidebar() == true));
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

		number.SetInt(0);
		breakpoint.AddSetter(hostname!, "halign", number); // halign = fill

		return breakpoint;
	}
}
