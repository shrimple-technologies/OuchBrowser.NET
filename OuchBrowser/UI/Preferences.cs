using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class Preferences
{
	public static PreferencesDialog New()
	{
		var dialog = PreferencesDialog.New();

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("UI/Preferences.ui");
		using var reader = new StreamReader(stream!);
		string xml = reader.ReadToEnd();

		var builder = new Builder();
		builder.AddFromString(xml, -1);
		builder.Connect(dialog);

		return (PreferencesDialog)builder!.GetObject("preferences")!;
	}
}
