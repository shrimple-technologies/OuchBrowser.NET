// Utils/Bangs.cs
// Utilities for handling !bangs via Kagi.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
		bangsList = bangsList.Where(
			n =>
				n.Category != "Region search"
				|| n.WebsiteName.StartsWith("News in ") && n.Domain == "kagi.com"
		).ToList();

		foreach (Bang bang in bangsList)
		{
			bangs.Add(bang.Trigger, bang);

			if (bang.AdditionalTriggers != null)
				foreach (string trigger in bang.AdditionalTriggers) bangs.Add(trigger, bang);
		}
	}

	public string ExpandBang(string text)
	{
		string bangString = text.Trim().Split(' ')[0];
		string trigger = bangString.StartsWith('!') ? bangString.Substring(1) : bangString;
		string defaultSearch = settings.GetString("search-engine");

		bangs.TryGetValue(trigger, out Bang? bang);
		if (bang == null) return string.Format(defaultSearch, Uri.EscapeDataString(text));

		string templateUrl = bang.TemplateUrl;
		string query = string.Join(" ", text.Trim().Split(' ').Skip(1));

		if (query.IsWhiteSpace()) // implements open_base_path and open_snap_domain format flags
		{
			if (bang.SnapDomain != null) // prioritize snap domain over domain from template url as some !bangs work better with this
				return $"https://{bang.SnapDomain}";
			else return $"https://{new Uri(bang.TemplateUrl).Host}";
		}

		if (bang.TemplateUrl.StartsWith('/') && bang.Domain != "kagi.com" || bang.TemplateUrl.Contains("site:"))
		{
			var query_params = HttpUtility.ParseQueryString(templateUrl.Replace("/search", ""));
			templateUrl = string.Format(defaultSearch, query_params["q"]!);
		}
		else if (bang.TemplateUrl.StartsWith('/') && bang.Domain == "kagi.com")
		{
			templateUrl = "https://kagi.com" + templateUrl;
		}

		if (bang.RegexPattern != null)
		{
			Match match = Regex.Match(query, bang.RegexPattern);
			int i = 1;

			foreach (Group group in match.Groups.Cast<Group>().Skip(1))
			{
				templateUrl = templateUrl.Replace($"${i}", group.Value);
				i++;
			}

			return templateUrl;
		}
		else if (bang.Format == null || bang.Format.Contains(BangFormat.url_encode_space_to_plus)) // preferred over `url_encode_placeholder` by default as they both url encode
			return templateUrl.Replace("{{{s}}}", WebUtility.UrlEncode(query));
		else if (bang.Format != null && bang.Format.Contains(BangFormat.url_encode_placeholder))
			return templateUrl.Replace("{{{s}}}", Uri.EscapeDataString(query));
		else return templateUrl.Replace("{{{s}}}", query);
	}

	public Bang? GetBang(string text)
	{
		string trigger = text.Split(' ')[0];

		bangs.TryGetValue(trigger, out Bang? bang);
		return bang;
	}
}
