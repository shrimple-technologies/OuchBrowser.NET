using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebKit;

namespace OuchBrowser;

internal partial class Window
{
	private void HandlePaletteUpdate()
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

		url_entry!.OnNotify += async (_, args) =>
		{
			if (args.Pspec.GetName() == "text")
			{
				string text = url_entry!.GetText();
				if (text == "")
				{
					url_autocomplete!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("main");
					url_disclosure!.SetVisibleChildName("none");
					url_disclosure_revealer!.SetRevealChild(false);
				}
				else if (text.StartsWith('!'))
				{
					url_disclosure_revealer!.SetRevealChild(false);

					if (1 < text.Split(' ').Length)
					{
						url_autocomplete!.SetRevealChild(false);
						Bang? current_bang = bangs!.GetBang(text)!;
						if (current_bang != null)
						{
							if (text.Split(' ')[1].Length == 0) url_stack!.SetVisibleChildName("spinner");
							url_custom_disclosure!.SetLabel(__("Searching using {0}", current_bang.WebsiteName));
							url_disclosure!.SetVisibleChildName("custom");
							url_disclosure_revealer!.SetRevealChild(true);
							Gio.Icon icon = await Favicon.GetFavicon(current_bang.Domain);
							url_favicon!.SetFromGicon(icon);
							url_stack!.SetVisibleChildName("website");
						}
						else
						{
							url_stack!.SetVisibleChildName("bang");
							url_disclosure!.SetVisibleChildName("none");
							url_disclosure_revealer!.SetRevealChild(false);
						}
					}
					else
					{
						url_stack!.SetVisibleChildName("bang");

						if (settings.GetBoolean("bang-autocomplete-enabled"))
						{
							url_autocomplete!.SetRevealChild(true);
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
							if (bang.Length == 0) url_autocomplete!.SetRevealChild(false);
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
									url_entry.SetText($"!{b.Trigger} ");
									url_entry.GrabFocusWithoutSelecting();
									url_entry.SetPosition(-1);
								};
								box.Append(button);
							}

							if (bang.Length < 8)
							{
								url_autocomplete!.SetChild(box);
							}
							else
							{
								sw.SetChild(box);
								url_autocomplete!.SetChild(sw);
							}
						}
					}
				}
				else if (Url.IsUrl(text))
				{
					url_autocomplete!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("website");
					url_disclosure!.SetVisibleChildName("none");
					url_disclosure_revealer!.SetRevealChild(false);
					url_custom_disclosure!.SetLabel("");
					url_favicon!.SetFromGicon(await Favicon.GetFavicon(text));
				}
				else if (text.StartsWith('>'))
				{
					url_autocomplete!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("main");
					url_disclosure!.SetVisibleChildName("none");
					url_disclosure_revealer!.SetRevealChild(false);

					url_autocomplete!.SetRevealChild(true);
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

					if (results.Count == 0) url_autocomplete!.SetRevealChild(false);
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
							url_entry.SetText($">{s.Command} ");
							url_entry.GrabFocusWithoutSelecting();
							url_entry.SetPosition(-1);
						};
						box.Append(button);
					}

					if (results.Count < 8)
					{
						url_autocomplete!.SetChild(box);
					}
					else
					{
						sw.SetChild(box);
						url_autocomplete!.SetChild(sw);
					}
				}
				else
				{
					url_disclosure!.SetVisibleChildName("none");
					url_disclosure_revealer!.SetRevealChild(false);
					url_custom_disclosure!.SetLabel("");

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

							string textNow = url_entry!.GetText();

							if (textNow == "")
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("main");
								return;
							}
							else if (textNow.StartsWith('!'))
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("bang");
								return;
							}
							else if (Url.IsUrl(textNow))
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("website");
								return;
							}
							else if (textNow.StartsWith('>'))
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("actions");
								return;
							}

							Autocompletion[] ac = await Autocomplete.CompletionResults(textNow);
							Box box = Box.New(Orientation.Vertical, 10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							if (ac.Length == 0)
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("search");
							}
							else
							{
								foreach (Autocompletion phrase in ac)
								{
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
										url_entry.SetText(phrase.phrase);
										url_bar_button!.Activate();
									};
									box.Append(button);
								}

								url_autocomplete!.SetChild(box);
								url_autocomplete!.SetRevealChild(true);
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

	private void HandlePaletteActivate()
	{
		url_bar_button!.OnClicked += (_, _) => url_bar_button!.Activate();
		url_bar_button!.OnActivate += (_, _) =>
		{
			string query = url_entry!.GetText();

			if (query == "") return;

			if (query.StartsWith('>'))
			{
				url_dialog!.Close();

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

			if (palette_state == "new_tab")
			{
				Console.WriteLine($"url: {query}");
				Console.WriteLine($"isURL: {Url.IsUrl(query)}");
				Console.WriteLine($"starts with https or http: {query.StartsWith("https://") || query.StartsWith("http://")}");
				Console.WriteLine("");

				if (Url.IsUrl(query) && !query.StartsWith('!'))
				{
					if (query.StartsWith("https://") || query.StartsWith("http://"))
					{
						view!.AddTab(query, false);
					}
					else
					{
						view!.AddTab($"https://{query}", false);
					}
				}
				else
				{
					if (query.StartsWith('!'))
					{
						view!.AddTab(bangs!.ExpandBang(query), false);

						Bang? bang = bangs!.GetBang(query)!;
						if (bang != null) Bangs.IncrementRanking(bang.Trigger);
					}
					else
					{
						view!.AddTab(string.Format(settings.GetString("search-engine"), Uri.EscapeDataString(query)), false);
					}
				}
			}
			else
			{
				TabPage page = tabview!.GetSelectedPage()!;
				WebView webview = (WebView)page.Child!;

				Console.WriteLine($"url: {query}");
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
						if (bang != null) Bangs.IncrementRanking(bang.Trigger);
					}
					else
					{
						webview.LoadUri(string.Format(settings.GetString("search-engine"), Uri.EscapeDataString(query)));
					}
				}
			}

			url_dialog!.Close();
		};
	}
}
