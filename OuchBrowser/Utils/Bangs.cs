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

		EmbeddedResource.Load("Bangs.json", out string list);
		EmbeddedResource.Load("Bangs.Kagi.json", out string list_kagi);
		List<Bang> bangs_kagi = JsonSerializer.Deserialize<List<Bang>>(list_kagi, options)!;
		List<Bang> bangs_list = JsonSerializer.Deserialize<List<Bang>>(list, options)!;
		bangs_list.AddRange(bangs_kagi);
		bangs_list = bangs_list.Where(n => n.Category != "Region search").ToList();

		foreach (Bang bang in bangs_list)
		{
			bangs.Add(bang.Trigger, bang);

			if (bang.AdditionalTriggers != null)
			{
				foreach (string trigger in bang.AdditionalTriggers) bangs.Add(trigger, bang);
			}
		}
	}

	public string ExpandBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;
		string default_search = settings.GetString("search-engine");

		bangs.TryGetValue(trigger, out Bang? bang);
		if (bang == null) return string.Format(default_search, Uri.EscapeDataString(text));

		string template_url = bang.TemplateUrl;

		if (bang.TemplateUrl.StartsWith('/') && bang.Domain != "kagi.com" || bang.TemplateUrl.Contains("site:"))
		{
			var query_params = HttpUtility.ParseQueryString(template_url.Replace("/search", ""));
			template_url = string.Format(default_search, query_params["q"]!);
		}
		else if (bang.TemplateUrl.StartsWith('/') && bang.Domain == "kagi.com")
		{
			template_url = "https://kagi.com" + template_url;
		}

		string query = string.Join(" ", text.Split(' ').Skip(1));
		return template_url.Replace("{{{s}}}", Uri.EscapeDataString(query));

	}

	public Bang[] AutocompleteBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;

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
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;

		bangs.TryGetValue(trigger, out Bang? bang);
		return bang;
	}
}
