using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class About
{
	public static Adw.AboutDialog New()
	{
		var dialog = Adw.AboutDialog.New();

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/About.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(dialog);

		return (Adw.AboutDialog)builder!.GetObject("about")!;
	}
}
