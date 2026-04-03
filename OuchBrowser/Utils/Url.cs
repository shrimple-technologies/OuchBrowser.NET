using System.Text.RegularExpressions;
namespace OuchBrowser.Utils;

class Url
{
	public static bool IsUrl(string url)
	{
		// e.g. https://example.com
		// (also matches ports and paths)
		if (url.StartsWith("https://") || url.StartsWith("http://") && Regex.IsMatch(url, @"\.[a-zA-Z]{2,63}(\:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))?(\/([^?#\s]*))?$"))
		{
			return true;
		}
		// e.g. example.com
		// (also matches ports and paths)
		else if (url.Contains('.') && Regex.IsMatch(url, @"\.[a-zA-Z]{2,63}(\:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))?(\/([^?#\s]*))?$"))
		{
			return true;
		}
		// e.g. 127.0.0.1
		// (also matches schemes, ports, and paths)
		else if (IsIpAddress(url))
		{
			return true;
		}
		else // e.g. example com
		{
			return false;
		}
	}

	public static bool IsIpAddress(string url)
	{
		if (Regex.IsMatch(url, @"^((https|http)\:\/\/)?(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])(\:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))?(\/([^?#\s]*))?$")) return true;
		return false;
	}
}
