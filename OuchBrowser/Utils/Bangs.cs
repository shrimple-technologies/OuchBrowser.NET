// Utils/Bangs.cs
// Utilities for handling !bangs via Kagi.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

internal class Bangs
{
	private readonly Dictionary<string, Bang> bangs = new();

	public Bangs()
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new JsonStringEnumConverter() }
		};

		EmbeddedResource.Load("Bangs.json", out string bangsRaw);
		EmbeddedResource.Load("Bangs.Kagi.json", out string bangsKagiRaw);
		List<Bang> bangsKagi = JsonSerializer.Deserialize<List<Bang>>(bangsKagiRaw, options)!;
		List<Bang> bangsList = JsonSerializer.Deserialize<List<Bang>>(bangsRaw, options)!;
		bangsList.AddRange(bangsKagi);
		bangsList = bangsList.Where(n => n.Category != "Region search").ToList();

		foreach (Bang bang in bangsList)
		{
			bangs.Add(bang.Trigger, bang);

			if (bang.AdditionalTriggers != null)
			foreach (string trigger in bang.AdditionalTriggers) bangs.Add(trigger, bang);
		}
	}

	public string ExpandBang(string text)
	{
		string bangString = text.Split(' ')[0];
		string trigger = bangString.StartsWith('!') ? bangString.Substring(1) : bangString;
		string defaultSearch = settings.GetString("search-engine");

		bangs.TryGetValue(trigger, out Bang? bang);
		if (bang == null) return string.Format(defaultSearch, Uri.EscapeDataString(text));

		string templateUrl = bang.TemplateUrl;

		if (bang.TemplateUrl.StartsWith('/') && bang.Domain != "kagi.com" || bang.TemplateUrl.Contains("site:"))
		{
			var query_params = HttpUtility.ParseQueryString(templateUrl.Replace("/search", ""));
			templateUrl = string.Format(defaultSearch, query_params["q"]!);
		}
		else if (bang.TemplateUrl.StartsWith('/') && bang.Domain == "kagi.com")
		{
			templateUrl = "https://kagi.com" + templateUrl;
		}

		string query = string.Join(" ", text.Split(' ').Skip(1));
		return templateUrl.Replace("{{{s}}}", Uri.EscapeDataString(query));

	}

	public Bang[] AutocompleteBang(string text)
	{
		string bangString = text.Split(' ')[0];
		string trigger = bangString.StartsWith('!') ? bangString.Substring(1) : bangString;

		if (trigger == "") return []; // there are OVER 1000 BANGS, without this, the app will crash
		return bangs
			.Where(pair =>
				pair.Key.StartsWith(
					trigger,
					StringComparison.OrdinalIgnoreCase
				)
			)
			.Select(pair => pair.Value)
			.DistinctBy(bang => bang.Trigger)
			.ToArray();
	}

	public Bang? GetBang(string text)
	{
		string bangString = text.Split(' ')[0];
		string trigger = bangString.StartsWith('!') ? bangString.Substring(1) : bangString;

		bangs.TryGetValue(trigger, out Bang? bang);
		return bang;
	}
}
