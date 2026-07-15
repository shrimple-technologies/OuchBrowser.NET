namespace OuchBrowser.UI;

internal class About
{
	internal static Adw.AboutDialog New()
	{
		var dialog = Adw.AboutDialog.NewFromAppdata("/page/codeberg/shrimple/OuchBrowser/page.codeberg.shrimple.OuchBrowser.metainfo.xml", null);

		// TRANSLATORS: This is not a string that is a part of the source code.
		// This is your name (or username), followed by your email enclosed in
		// angles (<example@domain.com>) or your website. This will be shown in
		// Ouch Browser's credits. See
		// <https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1-latest/class.AboutDialog.html#credits-and-acknowledgements>
		// for more details.
		dialog.SetTranslatorCredits(__("translator-credits"));
		dialog.SetDevelopers(["Maxine Naomi Lunaris https://woof.monster/"]);
		dialog.SetDesigners(["Maxine Naomi Lunaris https://woof.monster/"]);
		dialog.SetDocumenters(["Maxine Naomi Lunaris https://woof.monster/"]);
		dialog.SetArtists(["Maxine Naomi Lunaris https://woof.monster/"]);
		dialog.AddCreditSection(__("Icon design by"), ["Jakub Steiner https://jimmac.eu/"]);
		dialog.AddAcknowledgementSection(__("Shrimple Technologies members"), [
			"Maxine Naomi Lunaris https://woof.monster/",
			"Jase Maxine Lunaris",
		]);
		dialog.AddAcknowledgementSection(__("Inspired by"), [
			"Arc Browser https://arc.net/",
			"Zen Browser https://zen-browser.app/",
		]);

		return dialog;
	}
}
