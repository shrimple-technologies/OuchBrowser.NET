using Gio;

namespace OuchBrowser.Utils;

public class Actions
{
	private readonly UI.Window? win;
	private readonly Adw.Application? app;

	public Actions(UI.Window window, Adw.Application application)
	{
		win = window;
		app = application;
	}

	public void AddAction(string name, string[] accels, GObject.SignalHandler<SimpleAction, SimpleAction.ActivateSignalArgs> action)
	{
		var simpleaction = SimpleAction.New(name, null);
		simpleaction.OnActivate += action;
		win!.AddAction(simpleaction);
		app!.SetAccelsForAction($"win.{name}", accels);
	}
}
