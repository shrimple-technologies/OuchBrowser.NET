using System.Net.Http.Json;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

class Autocomplete
{
	public static async Task<Autocompletion[]> CompletionResults(string text)
	{
		using HttpClient http = new HttpClient();
		HttpResponseMessage res = await http.GetAsync($"https://duckduckgo.com/ac/?q={Uri.EscapeDataString(text)}");
		Autocompletion[] ac = await res.Content.ReadFromJsonAsync<Autocompletion[]>() ?? [];
		return ac!;
	}
}
