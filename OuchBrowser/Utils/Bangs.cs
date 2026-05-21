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
	private readonly string default_search;

	public Bangs(string fallback)
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

		default_search = fallback;
	}

	public string ExpandBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;

		bangs.TryGetValue(trigger, out Bang? bang);
		if (bang == null) return $"{default_search}{text}";

		string template_url = bang.TemplateUrl;

		if (bang.TemplateUrl.StartsWith('/') && bang.Domain != "kagi.com")
		{
			var query_params = HttpUtility.ParseQueryString(template_url.Replace("/search", ""));
			template_url = default_search + query_params["q"]!;
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
		return [];
	}

	public Bang? GetBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;

		bangs.TryGetValue(trigger, out Bang? bang);
		return bang;
	}
}
