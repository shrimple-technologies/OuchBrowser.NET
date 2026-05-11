namespace OuchBrowser.UI;

internal class About
{
	internal static Adw.AboutDialog New()
	{
		var dialog = Adw.AboutDialog.NewFromAppdata("/site/srht/shrimple/OuchBrowser/page.codeberg.shrimple.OuchBrowser.metainfo.xml", null);

		// TRANSLATORS: This is not a string that is a part of the source code. This is your name (or username), followed by your email enclosed in angles (<example@domain.com>) or your website. This will be shown in Ouch Browser's credits.
		dialog.SetTranslatorCredits(__("translator-credits"));
		dialog.SetDevelopers(["Maxine Naomi Lunaris"]);
		dialog.SetDesigners(["Maxine Naomi Lunaris"]);

		return dialog;
	}
}
