using Adw;
using Gtk;
using System.Reflection;

namespace OuchBrowser.UI;

public class Window : Adw.ApplicationWindow {
	[Connect] public readonly TabView? view;

	public Window(Adw.Application app) : base() {
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Window.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(this);

		Content = builder.GetObject("overview") as Widget;
		Application = app;

		DefaultWidth = 1000;
		DefaultHeight = 600;
		WidthRequest = 350;
		HeightRequest = 500;
		Title = "Ouch Browser";
	}
}
