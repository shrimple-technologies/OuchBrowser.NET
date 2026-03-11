using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OuchBrowser.Types;

public class Bang
{
	[JsonPropertyName("s")]
	public required string WebsiteName { get; set; }

	[JsonPropertyName("d")]
	public required string Domain { get; set; }

	[JsonPropertyName("ad")]
	public string? SnapDomain { get; set; }

	[JsonPropertyName("t")]
	public required string Trigger { get; set; }

	[JsonPropertyName("ts")]
	public List<string>? AdditionalTriggers { get; set; }

	[JsonPropertyName("u")]
	public required string TemplateUrl { get; set; }

	[JsonPropertyName("x")]
	public string? RegexPattern { get; set; }

	[JsonPropertyName("c")]
	public string? Category { get; set; }

	[JsonPropertyName("sc")]
	public string? Subcategory { get; set; }

	[JsonPropertyName("fmt")]
	public List<BangFormat>? Format { get; set; }

	[JsonPropertyName("skip_tests")]
	public bool SkipTests { get; set; } = false;
}

public enum BangCategory
{
	Entertainment,
	[EnumMember(Value = "Man Page")]
	ManPage,
	Multimedia,
	News,
	[EnumMember(Value = "Online Services")]
	OnlineServices,
	[EnumMember(Value = "Region Search")]
	RegionSearch,
	Research,
	Shopping,
	Tech,
	Translation
}

public enum BangFormat
{
	open_base_path,
	open_snap_domain,
	url_encode_placeholder,
	url_encode_space_to_plus
}
