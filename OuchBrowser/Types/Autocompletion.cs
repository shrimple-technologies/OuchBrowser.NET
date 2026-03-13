using System.Text.Json.Serialization;

namespace OuchBrowser.Types;

public class Autocompletion
{
	[JsonPropertyName("phrase")]
	public required string phrase { get; set; }
}
