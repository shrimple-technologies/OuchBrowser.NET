// Utils/Favicon.cs
// Utilities for loading favicons via DuckDuckGo.
// This is currently pending deprecation to switch to built-in ways to load the
// favicon

namespace OuchBrowser.Utils;

internal class Favicon
{
	public static async Task<Gio.Icon> GetFavicon(string domain)
	{
		try
		{
			Uri hostname;
			if (domain.StartsWith("https://") || domain.StartsWith("http://"))
			{
				hostname = new Uri(domain);
			}
			else
			{
				hostname = new Uri($"https://{domain}");
			}
			using var http = new HttpClient();
			using var remoteStream = await http.GetStreamAsync($"https://icons.duckduckgo.com/ip3/{Uri.EscapeDataString(hostname.Host)}.ico");
			using var memoryStream = new MemoryStream();

			await remoteStream.CopyToAsync(memoryStream);
			byte[] bytes = memoryStream.ToArray();

			// we are in a try-catch block, we can just simply throw, and set the placeholder icon
			if (bytes.Length == 0) throw new Exception();

			using var gBytes = GLib.Bytes.New(bytes);
			Gio.Icon icon = Gio.BytesIcon.New(gBytes);

			return icon;
		}
		catch // there is no icon
		{
			return Gio.ThemedIcon.New("box-dotted-symbolic");
		}
	}
}
