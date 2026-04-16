// Utils/Gettext.cs
// Global wrapper for Gettext.

global using static Gettext;

using GetText;

internal static class Gettext
{
	private static readonly Catalog catalog = new Catalog("OuchBrowser", "/usr/share/locale");

	public static string __(string msgid)
		=> catalog.GetString(msgid);

	public static string __(string context, string msgid)
		=> catalog.GetParticularString(context, msgid);

	public static string __n(string msgid, string plural, long n)
		=> catalog.GetPluralString(msgid, plural, n);
}
