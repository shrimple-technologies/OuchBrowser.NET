using System.Reflection;
using Adw;
using Gtk;

namespace OuchBrowser.UI;

public class About
{
	public static Adw.AboutDialog New()
	{
		var dialog = Adw.AboutDialog.New();
		
		var builder = new Builder();
		builder.AddFromResource("/site/srht/shrimple/OuchBrowser/ui/about.ui");
		builder.Connect(dialog);

		return (Adw.AboutDialog)builder!.GetObject("about")!;
	}
}
