using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

internal class Shortcuts
{
	public static Adw.ShortcutsDialog New()
	{
		var dialog = Adw.ShortcutsDialog.New();

		var builder = new Builder();
		builder.AddFromResource("/page/codeberg/shrimple/OuchBrowser/ui/shortcuts.ui");
		builder.Connect(dialog);

		return (Adw.ShortcutsDialog)builder!.GetObject("shortcuts")!;
	}
}
