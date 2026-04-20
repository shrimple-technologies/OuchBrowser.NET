using Adw;
using Gtk;
using OuchBrowser.Types;
using OuchBrowser.Utils;
using WebKit;

namespace OuchBrowser;

internal partial class Window
{
	private void HandlePaletteUpdate()
	{
		url_entry!.OnNotify += async (_, args) =>
		{
			if (args.Pspec.GetName() == "text")
			{
				string text = url_entry!.GetBuffer().GetText();
				if (text == "")
				{
					url_autocomplete!.SetRevealChild(false);
					url_stack!.SetVisibleChildName("main");
					url_disclosure!.SetVisibleChildName("none");
					url_disclosure_revealer!.SetRevealChild(false);
				}
				else if (text.StartsWith('!'))
				{

					if (1 < text.Split(' ').Length)
					{
						url_autocomplete!.SetRevealChild(false);
						Bang? current_bang = bangs!.GetBang(text)!;
						if (current_bang != null)
						{
							if (text.Split(' ')[1].Length == 0) url_stack!.SetVisibleChildName("spinner");
							url_custom_disclosure!.SetLabel(__("Searching using {0}", current_bang.WebsiteName));
							Gio.Icon icon = await Favicon.GetFavicon(current_bang.Domain);
							url_favicon!.SetFromGicon(icon);
							url_stack!.SetVisibleChildName("website");
							url_disclosure!.SetVisibleChildName("custom");
							url_disclosure_revealer!.SetRevealChild(true);
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
						url_disclosure!.SetVisibleChildName("bang");
						url_disclosure_revealer!.SetRevealChild(true);

						if (settings.GetBoolean("bang-autocomplete-enabled"))
						{
							url_autocomplete!.SetRevealChild(true);
							Box box = Box.New(Orientation.Vertical, 10);
							Label section_label = Label.New(__("BANGS"));
							ScrolledWindow sw = ScrolledWindow.New();
							sw.SetPropagateNaturalHeight(true);
							sw.SetVexpand(true);
							sw.SetMinContentHeight(399);
							sw.SetMaxContentHeight(400);
							sw.AddCssClass("undershoot-top");
							sw.AddCssClass("undershoot-bottom");
							section_label.SetCssClasses(["caption-heading", "dimmed"]);
							section_label.SetHalign(Align.Start);
							section_label.SetMarginStart(10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							box.Append(section_label);
							int i = 0;

							Bang[] bang = bangs!.AutocompleteBang(text);
							if (bang.Length == 0) url_autocomplete!.SetRevealChild(false);
							foreach (Bang b in bang)
							{
								Button button = Button.New();
								Box button_box = Box.New(Orientation.Horizontal, 15);
								Label button_label = Label.New(b.WebsiteName);
								Label button_trigger;
								if (b.AdditionalTriggers != null)
								{
									List<string> triggers = new List<string>();
									foreach (string trigger in b.AdditionalTriggers)
									{
										triggers.Add($"!{trigger}");
									}

									button_trigger = Label.New($"!{b.Trigger}, {string.Join(", ", triggers.ToArray())} ");
								}
								else
								{
									button_trigger = Label.New($"!{b.Trigger}");
								}
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
									EntryBuffer buffer = EntryBuffer.New($"!{b.Trigger} ", -1);
									int length = Convert.ToInt32(buffer.GetLength());
									url_entry.SetBuffer(buffer);
									url_entry.GrabFocusWithoutSelecting();
									url_entry.SetPosition(length);
								};
								box.Append(button);
								i++;
							}

							if (i < 8)
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
					url_custom_disclosure!.SetLabel("");
					url_favicon!.SetFromGicon(await Favicon.GetFavicon(text));
				}
				else
				{
					url_disclosure!.SetVisibleChildName("none");
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

							string textNow = url_entry!.GetBuffer().GetText();

							if (textNow == "")
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("main");
							}
							else if (textNow.StartsWith('!'))
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("bang");
							}
							else if (Url.IsUrl(textNow))
							{
								url_autocomplete!.SetRevealChild(false);
								url_stack!.SetVisibleChildName("website");
							}

							Autocompletion[] ac = await Autocomplete.CompletionResults(textNow);
							if (ac.Length == 0)
							{
								url_autocomplete!.SetRevealChild(false);
								return;
							}

							Box box = Box.New(Orientation.Vertical, 10);
							Label section_label = Label.New(__("SUGGESTIONS"));
							section_label.SetCssClasses(["caption-heading", "dimmed"]);
							section_label.SetHalign(Align.Start);
							section_label.SetMarginStart(10);
							box.SetMarginTop(10);
							box.SetMarginBottom(10);
							box.Append(section_label);

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
								button_box.Append(Image.NewFromIconName("search-symbolic"));
								button_box.Append(button_label);
								button.SetChild(button_box);
								button.OnClicked += (_, _) =>
								{
									EntryBuffer buffer = EntryBuffer.New(phrase.phrase, -1);
									url_entry.SetBuffer(buffer);
									url_bar_button!.Activate();
								};
								box.Append(button);
							}

							url_autocomplete!.SetChild(box);
							url_autocomplete!.SetRevealChild(true);
							url_stack!.SetVisibleChildName("search");
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
		url_bar_button!.OnActivate += (_, _) =>
		{
			string query = url_entry!.GetBuffer().GetText();

			if (query == "") return;

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
					}
					else
					{
						view!.AddTab($"{settings!.GetString("search-engine")}{Uri.EscapeDataString(query)}", false);
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
					}
					else
					{
						webview.LoadUri($"{settings!.GetString("search-engine")}{Uri.EscapeDataString(query)}");
					}
				}
			}

			url_dialog!.Close();
			overview!.SetOpen(false);
		};
	}
}
