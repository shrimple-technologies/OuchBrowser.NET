// Utils/Bangs.cs
// Utilities for handling !bangs via Kagi.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using GLib;
using OuchBrowser.Types;
using Uri = System.Uri;

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

		settings.OnChanged((_, args) =>
		{
			if (args.Key == "custom-bangs") OnCustomBangsChanged();
		});

		OnCustomBangsChanged(); // call this once on startup to initially populate
		
		ExpireBangs();
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

		if (bang.Format != null && bang.Format.Contains(BangFormat.open_base_path) || query.IsWhiteSpace())
			return $"https://{new Uri(bang.TemplateUrl).Host}/";
		if (bang.SnapDomain != null && bang.Format != null && bang.Format.Contains(BangFormat.open_snap_domain) || query.IsWhiteSpace())
			return $"https://{bang.SnapDomain}/";

		if (bang.TemplateUrl.StartsWith('/') && bang.Domain != "kagi.com" || bang.TemplateUrl.Contains("site:"))
		{
			var query_params = HttpUtility.ParseQueryString(templateUrl.Replace("/search", ""));
			templateUrl = string.Format(defaultSearch, query_params["q"]!);
		}
		else if (bang.TemplateUrl.StartsWith('/') && bang.Domain == "kagi.com")
		{
			templateUrl = "https://kagi.com" + templateUrl;
		}

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
			.OrderByDescending(bang => GetRankings().TryGetValue(bang.Trigger, out RankedBang? rank) ? rank.Rank : 0)
			.ToArray();
	}

	public Bang? GetBang(string text)
	{
		string bangString = text.Split(' ')[0];
		string trigger = bangString.StartsWith('!') ? bangString.Substring(1) : bangString;

		bangs.TryGetValue(trigger, out Bang? bang);
		return bang;
	}

	private static Dictionary<string, RankedBang> GetRankings()
	{
		Dictionary<string, RankedBang> dict = new();
		Variant ranks = settings.GetValue("bang-rankings");
		VariantIter iter = ranks.IterNew();
		Variant currentValue;

		for (int i = 0; i < (int)ranks.NChildren(); i++)
		{
			currentValue = iter.NextValue()!;
			dict.Add(currentValue.GetChildValue(0).GetString(out _), new RankedBang
			{
				Rank = currentValue.GetChildValue(1).GetChildValue(0).GetInt32(),
				Timestamp = currentValue.GetChildValue(1).GetChildValue(1).GetInt64()
			});
		}

		return dict;
	}

	public static void IncrementRanking(string bang)
	{
		Variant ranks = settings.GetValue("bang-rankings");
		VariantIter iter = ranks.IterNew();
		VariantBuilder builder = VariantBuilder.New(VariantType.New("a{s(ix)}"));
		Variant currentValue;
		bool found = false;

		// since there isn't a clean way to modify the dictionary of ranks,
		// we instead rebuild the dictionary and increment the rank by 1 to
		// push the !bang higher in autocompletion
		for (int i = 0; i < (int)ranks.NChildren(); i++)
		{
			currentValue = iter.NextValue()!;

			if (currentValue.GetChildValue(0).GetString(out _) == bang)
			{
				builder.AddValue(
					Variant.NewDictEntry(
						currentValue.GetChildValue(0),
						Variant.NewTuple([
							Variant.NewInt32(currentValue.GetChildValue(1).GetChildValue(0).GetInt32() + 1),
							Variant.NewInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
						])
					)
				);
				found = true;
			}
			else builder.AddValue(Variant.NewDictEntry(currentValue.GetChildValue(0), currentValue.GetChildValue(1)));
		}

		if (found != true) builder.AddValue(Variant.NewDictEntry(Variant.NewString(bang), Variant.NewTuple([
			Variant.NewInt32(1),
			Variant.NewInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
		])));

		settings.SetValue("bang-rankings", builder.End());
	}

	public static void ExpireBangs()
	{
		Variant ranks = settings.GetValue("bang-rankings");
		VariantIter iter = ranks.IterNew();
		VariantBuilder builder = VariantBuilder.New(VariantType.New("a{s(ix)}"));
		Variant currentValue;

		// since there isn't a clean way to modify the dictionary of ranks,
		// we instead rebuild the dictionary and increment the rank by 1 to
		// push the !bang higher in autocompletion
		for (int i = 0; i < (int)ranks.NChildren(); i++)
		{
			currentValue = iter.NextValue()!;

			if ((DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(currentValue.GetChildValue(1).GetChildValue(1).GetInt64())).TotalDays > 7)
			{
				if (currentValue.GetChildValue(1).GetChildValue(0).GetInt32() == 0) continue;
				builder.AddValue(
					Variant.NewDictEntry(
						currentValue.GetChildValue(0),
						Variant.NewTuple([
							Variant.NewInt32(currentValue.GetChildValue(1).GetChildValue(0).GetInt32() - 1),
							currentValue.GetChildValue(1).GetChildValue(1),
						])
					)
				);
			}
			else builder.AddValue(Variant.NewDictEntry(currentValue.GetChildValue(0), currentValue.GetChildValue(1)));
		}

		settings.SetValue("bang-rankings", builder.End());
	}

	public static Dictionary<string, CustomBang> GetCustomBangs()
	{
		Dictionary<string, CustomBang> dict = new();
		Variant customBangs = settings.GetValue("custom-bangs");
		VariantIter iter = customBangs.IterNew();
		Variant currentValue;

		for (int i = 0; i < (int)customBangs.NChildren(); i++)
		{
			currentValue = iter.NextValue()!;
			dict.Add(currentValue.GetChildValue(0).GetString(out _), new CustomBang
			{
				WebsiteName = currentValue.GetChildValue(1).GetChildValue(0).GetString(out _),
				TemplateUrl = currentValue.GetChildValue(1).GetChildValue(1).GetString(out _),
			});
		}

		return dict;
	}

	private void OnCustomBangsChanged()
	{
		Dictionary<string, CustomBang> customBangs = GetCustomBangs();

		foreach (KeyValuePair<string, CustomBang> bang in customBangs)
		{
			if (!bangs.ContainsKey(bang.Key)) bangs.Add(bang.Key, new Bang
			{
				WebsiteName = bang.Value.WebsiteName,
				Trigger = bang.Key,
				Domain = new Uri(bang.Value.TemplateUrl).Host,
				TemplateUrl = bang.Value.TemplateUrl
			});
		}
	}
}
