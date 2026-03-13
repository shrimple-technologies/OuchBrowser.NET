using System.Text.RegularExpressions;
namespace OuchBrowser.Utils;

class Url
{
	public static bool IsUrl(string url)
	{
		if (url.StartsWith("https://") || url.StartsWith("http://")) // e.g. https://example.com
		{
			return true;
		}
		else if (url.Contains('.') && Regex.IsMatch(url, @"\.[a-zA-Z]{2,63}$")) // e.g. example.com
		{
			return true;
		}
		else // e.g. example com
		{
			return false;
		}
	}
}
