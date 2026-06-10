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
			string unit = settings.GetEnum("wolframalpha-unit") switch
			{
				1 => "&units=metric",
				2 => "&units=imperial",
				_ => ""
			};

			res = await http.GetAsync($"https://api.wolframalpha.com/v1/result?appid={settings.GetString("wolframalpha-app-id")}&timeout=1&i={Uri.EscapeDataString(text)}{unit}");
		}
		catch (HttpRequestException)
		{
			return null;
		}

		string output = await res.Content.ReadAsStringAsync()!;
		if (
			output == "\"No short answer available\""
			|| output == "\"Wolfram|Alpha did not understand your input\""
		) return null;
		else return output!;
	}
}
