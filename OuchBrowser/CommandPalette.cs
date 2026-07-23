using System.Text.Json;
using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.Utils;
using WebKit;

namespace OuchBrowser;

[GObject.Subclass<Adw.Dialog>("CommandPalette")]
[Template<GResource>("/page/codeberg/shrimple/OuchBrowser/ui/command-palette.ui")]
internal partial class CommandPalette
{
#pragma warning disable CS0649
	[Connect] public Entry? commandPaletteEntry;
	[Connect] public Revealer? commandPaletteAutocompleteRevealer;
	[Connect] public Stack? url_stack;
	[Connect] public Stack? commandPaletteDisclosureStack;
	[Connect] public Label? commandPaletteCustomDisclosure;
	[Connect] public Button? commandPaletteButton;
	[Connect] public Image? commandPaletteWebsiteFavicon;
	[Connect] public Revealer? commandPaletteDisclosureRevealer;
	[Connect] public Revealer? commandPaletteButtonRevealer;
#pragma warning restore CS0649
	private Window? window;
	private DateTime lastInvokeTime = DateTime.MinValue;
	private CancellationTokenSource? debounceCts;
	private readonly Bangs? bangs = new();

	public static CommandPalette NewWithWindow(Window window)
	{
		var obj = NewWithProperties([]);
		obj.window = window;

		return obj;
	}

	partial void Initialize()
	{
		commandPaletteEntry!.OnActivate += (_, _) => commandPaletteButton!.Activate();

		HandlePaletteUpdate();
		HandlePaletteActivate();
	}

	public void HandlePaletteUpdate()
	{
		EmbeddedResource.Load("ShortcutsList.json", out string shortcuts_json);
		List<Types.Shortcut> shortcuts_list = JsonSerializer.Deserialize<List<Types.Shortcut>>(shortcuts_json, JsonSerializerOptions.Default)!;
		Dictionary<string, Types.Shortcut> shortcuts = new();

		foreach (Types.Shortcut shortcut in shortcuts_list)
		{
			shortcuts.Add(shortcut.Command, shortcut);

			if (shortcut.Aliases.Length != 0)
			{
				foreach (string command in shortcut.Aliases) shortcuts.Add(command, shortcut);
			}
		}

		commandPaletteEntry!.OnNotify += async (_, args) =>
		{
			Console.WriteLine("sjjkskjsjkd");
			if (args.Pspec.GetName() == "text")
			{
				string text = commandPaletteEntry!.GetText().TrimStart();
				if (text == "")
				{
					commandPaletteAutocompleteRevealer!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("main");
					commandPaletteDisclosureStack!.SetVisibleChildName("none");
					commandPaletteDisclosureRevealer!.SetRevealChild(false);
					if (window.multiLayoutView!.GetLayoutName() == "mobile") commandPaletteButtonRevealer!.SetRevealChild(false);
				}
				else if (text.StartsWith('!'))
				{
					commandPaletteDisclosureRevealer!.SetRevealChild(false);
					if (window.multiLayoutView!.GetLayoutName() == "mobile") commandPaletteButtonRevealer!.SetRevealChild(true);

					if (1 < text.Split(' ').Length)
					{
						commandPaletteAutocompleteRevealer!.SetRevealChild(false);
						Bang? current_bang = bangs!.GetBang(text.Substring(1))!;
						if (current_bang != null)
						{
							if (text.Split(' ')[1].Length == 0) url_stack!.SetVisibleChildName("spinner");
							Gio.Icon icon;
							commandPaletteCustomDisclosure!.SetLabel(__("Searching using {0}", current_bang.WebsiteName));
							commandPaletteDisclosureStack!.SetVisibleChildName("custom");
							commandPaletteDisclosureRevealer!.SetRevealChild(true);
							if (current_bang.SnapDomain != null) icon = await Favicon.GetFavicon(current_bang.SnapDomain);
							else icon = await Favicon.GetFavicon(current_bang.Domain);
							commandPaletteWebsiteFavicon!.SetFromGicon(icon);
							url_stack!.SetVisibleChildName("website");
						}
						else
						{
							url_stack!.SetVisibleChildName("bang");
							commandPaletteDisclosureStack!.SetVisibleChildName("none");
							commandPaletteDisclosureRevealer!.SetRevealChild(false);
						}
					}
					else
					{
						url_stack!.SetVisibleChildName("bang");

						if (settings.GetBoolean("bang-autocomplete-enabled") && text.Length >= 4)
						{
							commandPaletteAutocompleteRevealer!.SetRevealChild(true);
							Box box = Box.New(Orientation.Vertical, 10);
							ScrolledWindow sw = ScrolledWindow.New();
							sw.SetPropagateNaturalHeight(true);
							sw.SetVexpand(true);
							sw.SetMinContentHeight(387);
							sw.SetMaxContentHeight(387);
							sw.AddCssClass("undershoot-top");
							box.SetMarginTop(10);
							box.SetMarginBottom(10);

							Bang[] bang = bangs!.AutocompleteBang(text);
							if (bang.Length == 0) commandPaletteAutocompleteRevealer!.SetRevealChild(false);
							foreach (Bang b in bang)
							{
								Button button = Button.New();
								Box button_box = Box.New(Orientation.Horizontal, 15);
								Label button_label = Label.New(b.WebsiteName);
								Label button_trigger = Label.New($"!{b.Trigger}");
								button.SetMarginStart(10);
								button.SetMarginEnd(10);
								button.SetHexpand(true);
								button.SetCssClasses(["flat"]);
								button_label.SetCssClasses(["body"]);
								button_label.SetEllipsize(Pango.EllipsizeMode.End);
								button_trigger.SetCssClasses(["body", "dimmed"]);
								button_box.Append(Image.NewFromIconName("box-dotted-symbolic"));
								button_box.Append(button_label);
								button_box.Append(button_trigger);
								button.SetChild(button_box);
								button.OnClicked += (_, _) =>
								{
									commandPaletteEntry.SetText($"!{b.Trigger} ");
									commandPaletteEntry.GrabFocusWithoutSelecting();
									commandPaletteEntry.SetPosition(-1);
								};
								box.Append(button);
							}

							if (bang.Length < 8)
							{
								commandPaletteAutocompleteRevealer!.SetChild(box);
							}
							else
							{
								sw.SetChild(box);
								commandPaletteAutocompleteRevealer!.SetChild(sw);
							}
						}
						else commandPaletteAutocompleteRevealer!.SetRevealChild(false);
					}
				}
				else if (Url.IsUrl(text))
				{
					commandPaletteAutocompleteRevealer!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("website");
					commandPaletteDisclosureStack!.SetVisibleChildName("none");
					commandPaletteDisclosureRevealer!.SetRevealChild(false);
					if (window.multiLayoutView!.GetLayoutName() == "mobile") commandPaletteButtonRevealer!.SetRevealChild(true);
					commandPaletteCustomDisclosure!.SetLabel("");
					commandPaletteWebsiteFavicon!.SetFromGicon(await Favicon.GetFavicon(text));
				}
				else if (text.StartsWith('>'))
				{
					commandPaletteAutocompleteRevealer!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("main");
					commandPaletteDisclosureStack!.SetVisibleChildName("none");
					commandPaletteDisclosureRevealer!.SetRevealChild(false);
					if (window.multiLayoutView!.GetLayoutName() == "mobile") commandPaletteButtonRevealer!.SetRevealChild(true);

					commandPaletteAutocompleteRevealer!.SetRevealChild(true);
					Box box = Box.New(Orientation.Vertical, 10);
					ScrolledWindow sw = ScrolledWindow.New();
					sw.SetPropagateNaturalHeight(true);
					sw.SetVexpand(true);
					sw.SetMinContentHeight(387);
					sw.SetMaxContentHeight(387);
					sw.AddCssClass("undershoot-top");
					sw.AddCssClass("undershoot-bottom");
					box.SetMarginTop(10);
					box.SetMarginBottom(10);

					string text_split = text.Substring(1);
					List<Types.Shortcut> results = shortcuts
						.Where(pair =>
							pair.Key.StartsWith(
								text_split,
								StringComparison.OrdinalIgnoreCase
							)
						)
						.Select(pair => pair.Value)
						.DistinctBy(shortcut => shortcut.Command)
						.ToList();

					if (results.Count == 0) commandPaletteAutocompleteRevealer!.SetRevealChild(false);
					foreach (Types.Shortcut s in results)
					{
						Button button = Button.New();
						Box button_box = Box.New(Orientation.Horizontal, 15);
						Label button_label = Label.New(s.Name);
						Label button_trigger = Label.New(s.Description);
						Image image = Image.NewFromIconName(s.IconName);
						image.AddCssClass("dimmed");
						button.SetMarginStart(10);
						button.SetMarginEnd(10);
						button.SetHexpand(true);
						button.SetCssClasses(["flat"]);
						button_label.SetCssClasses(["body"]);
						button_label.SetEllipsize(Pango.EllipsizeMode.End);
						button_trigger.SetCssClasses(["body", "dimmed"]);
						button_box.Append(image);
						button_box.Append(button_label);
						button_box.Append(button_trigger);
						button.SetChild(button_box);
						button.OnClicked += (_, _) =>
						{
							commandPaletteEntry.SetText($">{s.Command} ");
							commandPaletteEntry.GrabFocusWithoutSelecting();
							commandPaletteEntry.SetPosition(-1);
						};
						box.Append(button);
					}

					if (results.Count < 8)
					{
						commandPaletteAutocompleteRevealer!.SetChild(box);
					}
					else
					{
						sw.SetChild(box);
						commandPaletteAutocompleteRevealer!.SetChild(sw);
					}
				}
				else
				{
					commandPaletteDisclosureStack!.SetVisibleChildName("none");
					commandPaletteDisclosureRevealer!.SetRevealChild(false);
					if (window.multiLayoutView!.GetLayoutName() == "mobile") commandPaletteButtonRevealer!.SetRevealChild(true);
					commandPaletteCustomDisclosure!.SetLabel("");

					if (settings.GetBoolean("search-autocomplete-enabled"))
					{
						var now = DateTime.UtcNow;
						lastInvokeTime = now;

						debounceCts?.Cancel();
						debounceCts?.Dispose();
						debounceCts = new CancellationTokenSource();

						try
						{
							await Task.Delay(200, debounceCts.Token);
							if (lastInvokeTime != now) return;
							if (text.Length <= 1) url_stack!.SetVisibleChildName("spinner");

							string textNow = commandPaletteEntry!.GetText();

							if (textNow == "")
							{
								commandPaletteAutocompleteRevealer!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("main");
								return;
							}
							else if (textNow.StartsWith('!'))
							{
								commandPaletteAutocompleteRevealer!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("bang");
								return;
							}
							else if (Url.IsUrl(textNow))
							{
								commandPaletteAutocompleteRevealer!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("website");
								return;
							}
							else if (textNow.StartsWith('>'))
							{
								commandPaletteAutocompleteRevealer!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("actions");
								return;
							}

							Autocompletion[] ac = await Autocomplete.CompletionResults(textNow);
							Box box = Box.New(Orientation.Vertical, 10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							if (ac.Length == 0)
							{
								commandPaletteAutocompleteRevealer!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("search");
							}
							else
							{
								foreach (Autocompletion phrase in ac)
								{
									if (ac.IndexOf(phrase) > 8) break;

									Button button = Button.New();
									Box button_box = Box.New(Orientation.Horizontal, 15);
									Label button_label = Label.New(phrase.phrase);
									button.SetMarginStart(10);
									button.SetMarginEnd(10);
									button.SetHexpand(true);
									button.SetCssClasses(["flat"]);
									button_label.SetCssClasses(["body"]);
									button_label.SetEllipsize(Pango.EllipsizeMode.End);
									button_box.Append(Image.NewFromIconName("search-symbolic"));
									button_box.Append(button_label);
									button.SetChild(button_box);
									button.OnClicked += (_, _) =>
									{
										commandPaletteEntry.SetText(phrase.phrase);
										commandPaletteButton!.Activate();
									};
									box.Append(button);
								}

								commandPaletteAutocompleteRevealer!.SetChild(box);
								commandPaletteAutocompleteRevealer!.SetRevealChild(true);
								url_stack!.SetVisibleChildName("search");
							}

						}
						catch (TaskCanceledException) { }
						finally
						{
							if (debounceCts?.IsCancellationRequested == false)
								debounceCts?.Dispose();
							debounceCts = null;
						}
					}
					else
					{
						url_stack!.SetVisibleChildName("search");
					}
				}
			}
		};
	}

	public void HandlePaletteActivate()
	{
		commandPaletteButton!.OnClicked += (_, _) => commandPaletteButton!.Activate();
		commandPaletteButton!.OnActivate += (_, _) =>
		{
			string query = commandPaletteEntry!.GetText();

			if (query == "") return;

			if (query.StartsWith('>'))
			{
				Close();

				switch (query)
				{
					case string _ when query.StartsWith(">preferences"):
					case string _ when query.StartsWith(">settings"):
					case string _ when query.StartsWith(">prefs"):
						ActivateAction("preferences", null);
						break;
					case ">copy":
					case ">cp":
					case ">url":
						ActivateAction("copy-link", null);
						break;
					case string _ when query.StartsWith(">refresh"):
					case string _ when query.StartsWith(">reload"):
						ActivateAction("refresh", null);
						break;
					case ">close":
					case ">w":
						ActivateAction("tab-close", null);
						break;
					case ">rooms":
					case ">room":
					case ">tabs":
					case ">overview":
						ActivateAction("rooms", null);
						break;
				}

				return;
			}

			if (window.palette_state == "new_tab")
			{
				Console.WriteLine($"url: {query}");
				Console.WriteLine($"bang url: {bangs!.ExpandBang(query)}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
				Console.WriteLine("");

				if (Url.IsUrl(query) && !query.StartsWith('!'))
				{
					if (query.StartsWith("https://") || query.StartsWith("http://"))
					{
						window.view!.AddTab(query, false);
					}
					else
					{
						window.view!.AddTab($"https://{query}", false);
					}
				}
				else
				{
					if (query.StartsWith('!'))
					{
						window.view!.AddTab(bangs!.ExpandBang(query), false);

						Bang? bang = bangs!.GetBang(query.Substring(1))!;
						if (bang != null && settings.GetBoolean("bang-autocomplete-enabled")) Bangs.IncrementRanking(bang.Trigger);
					}
					else
					{
						window.view!.AddTab(string.Format(settings.GetString("search-engine"), Uri.EscapeDataString(query)), false);
					}
				}
			}
			else
			{
				TabPage page = window.tabView!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				Console.WriteLine($"url: {query}");
				Console.WriteLine($"bang url: {bangs!.ExpandBang(query)}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
				Console.WriteLine($"is bang: {query.StartsWith('!')}");
				Console.WriteLine("");

				if (Url.IsUrl(query))
				{
					if (query.StartsWith("https://") || query.StartsWith("http://"))
					{
						webview.LoadUri(query);
					}
					else
					{
						webview.LoadUri($"https://{query}");
					}
				}
				else
				{
					if (query.StartsWith('!'))
					{
						webview.LoadUri(bangs!.ExpandBang(query));

						Bang? bang = bangs!.GetBang(query)!;
						if (bang != null && settings.GetBoolean("bang-autocomplete-enabled")) Bangs.IncrementRanking(bang.Trigger);
					}
					else
					{
						webview.LoadUri(string.Format(settings.GetString("search-engine"), Uri.EscapeDataString(query)));
					}
				}
			}

			Close();
		};
	}
}
