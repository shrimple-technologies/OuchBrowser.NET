// Utils/EmbeddedResource.cs
// Utilities for loading embedded resources. Please use GResources instead
// of embedded resources whenever possible.

using System.Reflection;

namespace OuchBrowser.Utils;

internal class EmbeddedResource
{
	public static void Load(string filename, out string result)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(filename);
		using var reader = new StreamReader(stream!);
		result = reader.ReadToEnd();
	}
}
