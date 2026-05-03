namespace OuchBrowser.UI;

internal class About
{
	internal static Adw.AboutDialog New()
	{
		var dialog = Adw.AboutDialog.NewFromAppdata("/site/srht/shrimple/OuchBrowser/site.srht.shrimple.OuchBrowser.metainfo.xml", null);

		return dialog;
	}
}
