using System;
using System.Reflection;

namespace OuchBrowser.Utils;

class EmbeddedResource
{
	public EmbeddedResource() { }

	public static void Load(string filename, out string result)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(filename);
		using var reader = new StreamReader(stream!);
		result = reader.ReadToEnd();
	}
}
