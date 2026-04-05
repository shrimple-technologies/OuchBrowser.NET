using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Shortcuts
{
	public static Adw.ShortcutsDialog New()
	{
		var dialog = Adw.ShortcutsDialog.New();

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Shortcuts.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(dialog);

		return (Adw.ShortcutsDialog)builder!.GetObject("shortcuts")!;
	}
}
