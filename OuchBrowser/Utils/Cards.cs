// Utils/Cards.cs
// Utilities for Cards.

using Adw;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

public class Cards
{
	private readonly UI.Window window;

	public Cards(UI.Window win)
	{
		window = win;
	}

	public void ShowCard(Card card)
	{
		ActionRow row = ActionRow.New();
		row.SetSelectable(false);
		row.SetFocusable(false);
		row.SetIconName(card.IconName);
		row.SetTitle(card.Title);
		row.SetSubtitle(card.Description!);

		foreach (CardButton button in card.Buttons!)
		{
			ButtonRow btn = ButtonRow.New();
			btn.SetStartIconName(button.IconName);
			btn.SetTitle(button.Title);
			btn.OnActivated += button.OnActivated;
			btn.OnActivated += (_, _) => HideCard();
			window.card_listbox!.Append(btn);
		}

		ButtonRow close_btn = ButtonRow.New();
		close_btn.SetStartIconName("cross-large-symbolic");
		close_btn.SetTitle(window.gettext.GetString("Close"));
		close_btn.OnActivated += (_, _) => HideCard();
		window.card_listbox!.Prepend(row);
		window.card_listbox!.Append(close_btn);

		window.card_revealer!.SetRevealChild(true);
	}

	public void HideCard()
	{
		window.card_revealer!.SetRevealChild(false);
		window.card_revealer!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "child-revealed" && !window.card_revealer!.GetChildRevealed()) window.card_listbox!.RemoveAll();
		};
	}
}
