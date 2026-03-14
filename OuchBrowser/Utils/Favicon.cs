namespace OuchBrowser.Utils;

class Favicon
{
	public static async Task<Gio.Icon> GetFavicon(string domain)
	{
		try
		{
			using var http = new HttpClient();
			using var remoteStream = await http.GetStreamAsync($"https://www.google.com/s2/favicons?domain={Uri.EscapeDataString(domain)}&sz=16");
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
