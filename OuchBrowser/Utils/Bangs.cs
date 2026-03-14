using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OuchBrowser.Types;

namespace OuchBrowser.Utils;

class Bangs
{
	private readonly List<Bang> bangs;
	private readonly string default_search;

	public Bangs(string fallback)
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new JsonStringEnumConverter() }
		};

		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("Bangs.json");
		using var reader = new StreamReader(stream!);
		string list = reader.ReadToEnd();
		bangs = JsonSerializer.Deserialize<List<Bang>>(list, options)!;
		default_search = fallback;
	}

	public string ExpandBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;
		foreach (Bang bang in bangs)
		{
			if (trigger == bang.Trigger || (bang.AdditionalTriggers != null && bang.AdditionalTriggers.Contains(trigger)))
			{
				string query = string.Join(" ", text.Split(' ').Skip(1));
				return bang.TemplateUrl.Replace("{{{s}}}", Uri.EscapeDataString(query));
			}
		}

		return $"{default_search}{text}";
	}

	public Bang[] AutocompleteBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;
		if (trigger == "") return []; // there are OVER 1000 BANGS, without this, the app will crash
		List<Bang> results = new List<Bang>();
		foreach (Bang bang in bangs)
		{
			if (bang.Trigger.StartsWith(trigger) || (bang.AdditionalTriggers != null && bang.AdditionalTriggers.Contains(trigger)))
			{
				results.Add(bang);
			}
		}
		return results.ToArray();
	}

	public Bang? GetBang(string text)
	{
		string text_bang = text.Split(' ')[0];
		string trigger = text_bang.StartsWith('!') ? text_bang.Substring(1) : text_bang;
		foreach (Bang bang in bangs)
		{
			if (trigger == bang.Trigger || (bang.AdditionalTriggers != null && bang.AdditionalTriggers.Contains(trigger)))
			{
				return bang;
			}
		}

		return null;
	}
}
