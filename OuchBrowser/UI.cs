using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Window : Adw.ApplicationWindow {
	[Connect] public readonly TabView view;

	private Window(Builder builder, string name) : base(
		builder.GetPointer(name),
		false
	) {
		builder.Connect(this);
	}

	public Window() : this(
		new Builder("UI/Window.ui"),
		"window"
	) { }
}
