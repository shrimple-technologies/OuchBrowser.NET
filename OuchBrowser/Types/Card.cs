namespace OuchBrowser.Types;

public class CardButton
{
	public required string IconName { get; set; }
	public required string Title { get; set; }
	public required GObject.SignalHandler<Adw.ButtonRow> OnActivated { get; set; }
}

public class Card
{
	public required string IconName { get; set; }
	public required string Title { get; set; }
	public string? Description { get; set; } = "";
	public CardButton[]? Buttons { get; set; } = [];
}
