// Utils/Autocomplete.cs
// Utilities for interacting with the DuckDuckGo autocomplete API.

using System.Net.Http.Json;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

internal class Autocomplete
{
	public static async Task<Autocompletion[]> CompletionResults(string text)
	{
		using HttpClient http = new HttpClient();
		HttpResponseMessage res;
		try
		{
			res = await http.GetAsync($"https://duckduckgo.com/ac/?q={Uri.EscapeDataString(text)}");
		}
		catch (HttpRequestException)
		{
			return [];
		}
		Autocompletion[] ac = await res.Content.ReadFromJsonAsync<Autocompletion[]>() ?? [];

		// on duckduckgo, if you type an exclaimation mark *anywhere* in the
		// search query, it will trigger duckduckgo's !bangs, which is
		// extremely redundant, as we have our own !bangs system. we cannot do
		// anything about this, so we must filter out duckduckgo !bangs
		// suggestions.
		return ac.Where((res) => !res.phrase.StartsWith('!')).ToArray();
	}
}
