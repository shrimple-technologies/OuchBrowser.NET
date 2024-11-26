using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Window : Adw.ApplicationWindow {
	private Window(Builder builder, string name) : base(
		builder.GetPointer(name),
		false
	) {
		builder.Connect(this);
	}

	public Window() : this(
		new Builder("UI/Window.ui"),
		"window"
	) {
	}
}
