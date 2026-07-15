// Utils/Cards.cs
// Utilities for Cards.

using Adw;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

internal class Cards
{
	private readonly Window window;

	public Cards(Window win)
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
			window.cardBox!.Append(btn);
		}

		ButtonRow close_btn = ButtonRow.New();
		close_btn.SetStartIconName("cross-large-symbolic");
		close_btn.SetTitle(__("Close"));
		close_btn.OnActivated += (_, _) => HideCard();
		window.cardBox!.Prepend(row);
		window.cardBox!.Append(close_btn);

		window.cardBoxRevealer!.SetRevealChild(true);
	}

	public void HideCard()
	{
		window.cardBoxRevealer!.SetRevealChild(false);
		window.cardBoxRevealer!.OnNotify += (_, args) =>
		{
			if (args.Pspec.GetName() == "child-revealed" && !window.cardBoxRevealer!.GetChildRevealed()) window.cardBox!.RemoveAll();
		};
	}
}
