using System.Text.RegularExpressions;
namespace OuchBrowser.Utils;

class Url
{
	public static bool IsUrl(string url)
	{
		if (url.StartsWith("https://") || url.StartsWith("http://") && Regex.IsMatch(url, @"\.[a-zA-Z]{2,63}(\:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))?(\/([^?#\s]*))?$")) // e.g. https://example.com
		{
			return true;
		}
		else if (url.Contains('.') && Regex.IsMatch(url, @"\.[a-zA-Z]{2,63}(\:(6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[0-9]{1,4}))?(\/([^?#\s]*))?$")) // e.g. example.com
		{
			return true;
		}
		else // e.g. example com
		{
			return false;
		}
	}
}
