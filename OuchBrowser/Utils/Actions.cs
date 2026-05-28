// Utils/Actions.cs
// Utilities for creating SimpleActions.

using Gio;

namespace OuchBrowser.Utils;

internal class Actions
{
	private readonly Window? win;
	private readonly Adw.Application? app;

	public Actions(Window window, Adw.Application application)
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
