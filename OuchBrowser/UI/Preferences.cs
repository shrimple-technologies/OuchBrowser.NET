using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Preferences
{
	public static Adw.Dialog New()
	{
		var dialog = Adw.Dialog.New();

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Preferences.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(dialog);

		return (Adw.Dialog)builder!.GetObject("preferences")!;
	}
}
