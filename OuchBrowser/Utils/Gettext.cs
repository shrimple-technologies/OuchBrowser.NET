// Utils/Gettext.cs
// Global wrapper for Gettext.

global using static Gettext;

using GetText;

internal static class Gettext
{
	private static readonly Catalog catalog = new Catalog("OuchBrowser", "/usr/share/locale");

	public static string __(string msgid, params object[] args)
		=> catalog.GetString(msgid, args);

	public static string __n(string msgid, string plural, long n)
		=> catalog.GetPluralString(msgid, plural, n);
}
