using Adw;
using Gtk;
using OuchBrowser.UI;
using OuchBrowser.Utils;
using WebKit;
using Object = GObject.Object;

namespace OuchBrowser;

[GObject.Subclass<Adw.ApplicationWindow>("OuchWindow")]
[Template<GResource>("/page/codeberg/shrimple/OuchBrowser/ui/window.ui")]
internal partial class Window : Adw.ApplicationWindow
{
#pragma warning disable CS0649
	[Connect] public Adw.HeaderBar? contentHeaderBar;
	[Connect] public ToolbarView? contentToolbarView;
	[Connect] public Button? content_sidebar_toggle;
	[Connect] public Frame? frame;
	[Connect] public Label? hostname;
	[Connect] public OverlaySplitView? overlaySplitView;
	[Connect] public ToggleButton? sidebar_toggle;
	[Connect] public ToastOverlay? toastOverlay;
	[Connect] public Bin? topBarHoverTarget;
	[Connect] public Button? url_button;
	[Connect] public TabView? tabView;
	[Connect] public Button? goBackButton;
	[Connect] public Button? goForwardButton;
	[Connect] public Button? refresh;
	[Connect] public WindowControls? startWindowControls;
	[Connect] public WindowControls? endWindowControls;
	[Connect] public Button? copyLinkButton;
	[Connect] public MenuButton? websiteSettingsButton;
	[Connect] public Revealer? cardBoxRevealer;
	[Connect] public ListBox? cardBox;
	[Connect] public Box? urlDisplayOsd;
	[Connect] public Label? urlDisplayLabel;
	[Connect] public MultiLayoutView? multiLayoutView;
#pragma warning restore CS0649

	public string palette_state = "new_tab";
	private Preferences? preferences;
	private ShortcutsDialog? shortcuts;
	private RoomsOverview? rooms;
	public View? view;
	private CommandPalette? palette;

	partial void Initialize()
	{
		var hover_controller_topbar = EventControllerMotion.New();
		var hover_controller_headerbar = EventControllerMotion.New();
		topBarHoverTarget!.AddController(hover_controller_topbar);
		contentHeaderBar!.AddController(hover_controller_headerbar);
		SetupHoverController(hover_controller_topbar);
		SetupHoverController(hover_controller_headerbar);

		startWindowControls!.SetVisible(!startWindowControls!.GetEmpty());
		startWindowControls!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "empty") startWindowControls!.SetVisible(!startWindowControls!.GetEmpty());
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

		OnCloseRequest += (_, _) =>
		{
			if (tabView!.GetNPages() >= 2)
			{
				Adw.AlertDialog alert = Adw.AlertDialog.New(
					__("Exit and Close All Tabs?"),
					__("You are about to close {0} tabs. Are you sure you want to continue?", tabView!.GetNPages())
				);
				alert.AddResponse("cancel", __("Cancel"));
				alert.AddResponse("close", __("Close Tabs"));
				alert.SetCloseResponse("cancel");
				alert.SetDefaultResponse("cancel");
				alert.SetResponseAppearance("close", ResponseAppearance.Destructive);

				alert.OnResponse += (_, args) =>
				{
					if (args.Response == "close")
					{
						for (int i = 0; i < tabView.GetNPages(); i++)
						{
							TabPage page = tabView!.GetNthPage(i)!;
							WebView webview = (WebView)page.Child!;

							webview.TryClose();
							tabView!.ClosePage(page);
						}
						Destroy();
					}
				};

				alert.Present(this);
			}
			else
			{
				for (int i = 0; i < tabView.GetNPages(); i++)
				{
					TabPage page = tabView!.GetNthPage(i)!;
					WebView webview = (WebView)page.Child!;

					webview.TryClose();
					tabView!.ClosePage(page);
				}
				return false;
			}

			return true;
		};
	}

	public void Start()
	{
		preferences = Preferences.NewWithWindow(this);
		// shortcuts = Shortcuts.New();
		rooms = RoomsOverview.NewWithWindow(this);
		view = new View(tabView!, this);
		palette = CommandPalette.NewWithWindow(this);
		var cards = new Cards(this);

		SetupActions();

		Gio.SimpleAction sidebar_action = (Gio.SimpleAction)LookupAction("sidebar-toggle")!;

		goBackButton!.SetSensitive(false);
		goForwardButton!.SetSensitive(false);
		refresh!.SetSensitive(false);
		copyLinkButton!.SetSensitive(false);
		url_button!.SetSensitive(false);
		websiteSettingsButton!.SetSensitive(false);
		sidebar_action.SetEnabled(false);

		Present();

		if (settings.GetStrv("restore-tabs").Length == 0) palette!.Present(this);
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
			palette.commandPaletteEntry!.DeleteText(0, -1);
			palette!.Present(this);
			palette.commandPaletteEntry!.GrabFocus();
			palette_state = "new_tab";
		});

		actions.AddAction("palette", ["<Ctrl>l", "<Alt>d"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0)
			{
				ActivateAction("palette-new", null);
			}
			else
			{
				TabPage page = tabView!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;
				palette.commandPaletteEntry!.SetText(webview.GetUri());
				palette!.Present(this);
				palette.commandPaletteEntry!.GrabFocus();
				palette_state = "current_tab";
			}
		});

		actions.AddAction("sidebar-toggle", ["<Ctrl><Shift>s"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0) return;

			if (overlaySplitView!.GetShowSidebar())
			{
				frame!.SetMarginStart(10);
				urlDisplayOsd!.SetMarginStart(20);
				overlaySplitView!.SetShowSidebar(false);
			}
			else
			{
				sidebar_toggle!.SetActive(true);
				frame!.SetMarginStart(0);
				urlDisplayOsd!.SetMarginStart(20);
				overlaySplitView!.SetShowSidebar(true);
			}
		});

		foreach (int i in new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
		{
			actions.AddAction($"tab-{i}", [$"<Ctrl>{i}"], (_, _) =>
			{
				if (tabView!.GetNPages() < i) return;
				if (tabView!.GetNthPage(i - 1) == tabView!.GetSelectedPage()) return;

				TabPage page = tabView!.GetNthPage(i - 1);
				tabView!.SetSelectedPage(page);

				if (!overlaySplitView!.GetShowSidebar())
				{
					Toast toast = Toast.New(page.GetTitle());
					toast.SetTimeout(1);
					toastOverlay!.DismissAll();
					toastOverlay!.AddToast(toast);
				}
			});
		}

		actions.AddAction("preferences", ["<Ctrl>comma"], (_, _) =>
		{
			preferences!.FocusPane("general");
			preferences!.Present(this);
		});

		actions.AddAction("about", [], (_, _) =>
		{
			var about = Adw.AboutDialog.NewFromAppdata("/page/codeberg/shrimple/OuchBrowser/page.codeberg.shrimple.OuchBrowser.metainfo.xml", null);

			// TRANSLATORS: This is not a string that is a part of the source code.
			// This is your name (or username), followed by your email enclosed in
			// angles (<example@domain.com>) or your website. This will be shown in
			// Ouch Browser's credits. See
			// <https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1-latest/class.AboutDialog.html#credits-and-acknowledgements>
			// for more details.
			about.SetTranslatorCredits(__("translator-credits"));
			about.SetDevelopers(["Maxine Naomi Lunaris https://woof.monster/"]);
			about.SetDesigners(["Maxine Naomi Lunaris https://woof.monster/"]);
			about.SetDocumenters(["Maxine Naomi Lunaris https://woof.monster/"]);
			about.SetArtists(["Maxine Naomi Lunaris https://woof.monster/"]);
			about.AddCreditSection(__("Icon design by"), ["Jakub Steiner https://jimmac.eu/"]);
			about.AddAcknowledgementSection(__("Shrimple Technologies members"), [
				"Maxine Naomi Lunaris https://woof.monster/",
				"Jase Maxine Lunaris",
			]);
			about.AddAcknowledgementSection(__("Inspired by"), [
				"Arc Browser https://arc.net/",
				"Zen Browser https://zen-browser.app/",
			]);

			if (DateTime.Now.Month == 6) about!.SetApplicationIcon("page.codeberg.shrimple.OuchBrowser.Pride");
			else about!.SetApplicationIcon("page.codeberg.shrimple.OuchBrowser");

			about!.Present(this);
		});

		actions.AddAction("shortcuts", ["<Ctrl>question"], (_, _) =>
		{
			shortcuts!.Present(this);
		});

		actions.AddAction("rooms", ["<Ctrl><Shift>bar"], (_, _) =>
		{
			rooms!.Present(this);
		});

		actions.AddAction("refresh", ["<Ctrl>r"], (_, _) =>
		{
			TabPage page = tabView!.GetSelectedPage()!;
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
			TabPage page = tabView!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.ReloadBypassCache();
		});

		actions.AddAction("zoom-in", ["<Ctrl>equal"], (_, _) =>
		{
			TabPage page = tabView!.GetSelectedPage()!;
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
						toastOverlay!.DismissAll();
						toastOverlay!.AddToast(toast);
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
			TabPage page = tabView!.GetSelectedPage()!;
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
						toastOverlay!.DismissAll();
						toastOverlay!.AddToast(toast);
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
			TabPage page = tabView!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			if (webview.GetZoomLevel() == settings.GetDouble("zoom")) return;

			webview.SetZoomLevel(settings.GetDouble("zoom"));
			toast.SetTitle($"{settings.GetDouble("zoom") * 100}%");
			toast.SetTimeout(1);
			toastOverlay!.DismissAll();
			toastOverlay!.AddToast(toast);
		});

		actions.AddAction("tab-close", ["<Ctrl>w"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0)
			{
				Close();
			}
			else
			{
				TabPage page = tabView!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;
				Gio.SimpleAction sidebar_action = (Gio.SimpleAction)LookupAction("sidebar-toggle")!;

				webview.TryClose();
				tabView!.ClosePage(tabView!.GetSelectedPage()!);
				if (tabView!.GetNPages() == 0)
				{
					refresh!.SetSensitive(false);
					goBackButton!.SetSensitive(false);
					goForwardButton!.SetSensitive(false);
					url_button!.SetSensitive(false);
					copyLinkButton!.SetSensitive(false);
					websiteSettingsButton!.SetSensitive(false);
					sidebar_action.SetEnabled(false);
					sidebar_toggle!.SetActive(true);
					hostname!.SetLabel("");
					overlaySplitView!.SetShowSidebar(true);
				}
			}
		});

		actions.AddAction("go-back", ["<Ctrl>Left"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0) return;

			TabPage page = tabView!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoBack();
		});

		actions.AddAction("go-forward", ["<Ctrl>Right"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0) return;

			TabPage page = tabView!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;

			webview.GoForward();
		});

		actions.AddAction("copy-link", ["<Ctrl><Shift>c"], (_, _) =>
		{
			if (tabView!.GetNPages() == 0) return;

			TabPage page = tabView!.GetSelectedPage()!;
			WebView webview = (WebView)page.Child!;
			Toast toast = Toast.New("");

			string uri = webview.GetUri();
			Gdk.Display display = Gdk.Display.GetDefault()!;
			Gdk.Clipboard clipboard = display!.GetClipboard();
			clipboard.SetText(uri);

			toast.SetTitle(__("Link Copied"));
			toast.SetTimeout(1);
			toastOverlay!.DismissAll();
			toastOverlay!.AddToast(toast);
		});

		actions.AddAction("palette-shortcuts", ["<Ctrl><Shift>k"], (_, _) =>
		{
			EntryBuffer buffer = EntryBuffer.New(">", -1);
			palette.commandPaletteEntry!.SetBuffer(buffer);
			palette!.Present(this);
			palette.commandPaletteEntry!.GrabFocusWithoutSelecting();
			palette.commandPaletteEntry!.SetPosition(-1);
		});

		actions.AddAction("new-window", ["<Ctrl>n"], (_, _) =>
		{
			NewWithProperties([]);
		});
	}

	private void SetupHoverController(EventControllerMotion controller)
	{
		controller.OnEnter += (_, _) =>
		{
			contentToolbarView!.SetRevealTopBars(!(endWindowControls!.GetEmpty() && overlaySplitView!.GetShowSidebar() == true));
		};
		controller.OnLeave += (_, _) =>
		{
			contentToolbarView!.SetRevealTopBars(false);
		};
	}
}
