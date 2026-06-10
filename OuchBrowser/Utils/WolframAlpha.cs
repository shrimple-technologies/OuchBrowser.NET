// Utils/Autocomplete.cs
// Utilities for interacting with the DuckDuckGo autocomplete API.

namespace OuchBrowser.Utils;

internal class WolframAlpha
{
	public static async Task<string?> Query(string text)
	{
		using HttpClient http = new HttpClient();
		HttpResponseMessage res;
		try
		{
			res = await http.GetAsync($"https://api.wolframalpha.com/v1/result?appid={settings.GetString("wolframalpha-app-id")}&i={Uri.EscapeDataString(text)}");
		}
		catch (HttpRequestException)
		{
			return null;
		}
		
		string output = await res.Content.ReadAsStringAsync()!;
		if (output == "\"No short answer available\"") return null;
		else return output!;
	}
}
