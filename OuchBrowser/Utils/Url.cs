// I FUCKING HATE THE .NET DEVELOPERS FUCK FLURL FUCK SYSTEM.URI THERE IS
// NO POINT TO LIFE ANYMORE
//
// THIS FILE IS MY PERSONAL FUCK YOU TO THE DEVELOPERS TO EVERY SINGLE LIBRARY
// WHO HAS FAILED TO HANDLE SCHEMELESS URLS AND OTHER OBSCURE URL TYPES

namespace OuchBrowser.Utils;

class Url
{
	public static bool IsUrl(string url)
	{
		if (url.StartsWith("https://") || url.StartsWith("http://")) // e.g. https://example.com
		{
			return true;
		}
		else if (url.Contains('.')) // e.g. example.com
		{
			return true;
		}
		else // e.g. example com
		{
			return false;
		}
	}
}
