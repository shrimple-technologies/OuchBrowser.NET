using Gio;

namespace OuchBrowser;

public class Actions
{
	private readonly UI.Window? win;

	public Actions(UI.Window window)
	{
		win = window;
	}

	public void AddAction(string name, GObject.SignalHandler<SimpleAction, SimpleAction.ActivateSignalArgs> action)
	{
		var simpleaction = SimpleAction.New(name, null);
		simpleaction.OnActivate += action;
		win!.AddAction(simpleaction);
	}
}
