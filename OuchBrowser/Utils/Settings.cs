// Utils/Settings.cs
// Global wrapper for GSettings.

#pragma warning disable CS8981
global using settings = Settings;
#pragma warning restore CS8981

internal static class Settings
{
	private static readonly Gio.Settings settings = Gio.Settings.New("page.codeberg.shrimple.OuchBrowser");

	public static void Reset(string key) => settings.Reset(key);

	public static string GetString(string key) => settings.GetString(key);
	public static bool GetBoolean(string key) => settings.GetBoolean(key);
	public static double GetDouble(string key) => settings.GetDouble(key);
	public static int GetEnum(string key) => settings.GetEnum(key);
	public static string[] GetStrv(string key) => settings.GetStrv(key);
	public static GLib.Variant GetValue(string key) => settings.GetValue(key);

	public static bool SetString(string key, string value) => settings.SetString(key, value);
	public static bool SetBoolean(string key, bool value) => settings.SetBoolean(key, value);
	public static bool SetDouble(string key, double value) => settings.SetDouble(key, value);
	public static bool SetEnum(string key, int value) => settings.SetEnum(key, value);
	public static bool SetStrv(string key, string[] value) => settings.SetStrv(key, value);
	public static bool SetValue(string key, GLib.Variant value) => settings.SetValue(key, value);
}
