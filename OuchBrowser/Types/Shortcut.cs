using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OuchBrowser.Types;

public class Shortcut
{
	public required string Name { get; set; }
	public required string Command { get; set; }
	public required string Description { get; set; }
	public string? IconName { get; set; }
	public required string[] Aliases { get; set; }
	public Shortcut[]? Subcommands { get; set; }
}
