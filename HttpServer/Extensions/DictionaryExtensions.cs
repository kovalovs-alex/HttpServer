using System.Text;

namespace HttpServer.Extensions;

public static class DictionaryExtensions
{
    public static string ConvertToHttpHeaders(this Dictionary<string, string> dict)
    {
        var stringBuilder = new StringBuilder();
        foreach(string key in dict.Keys)
        {
            stringBuilder.Append($"{key}: {dict[key]}\r\n");
        }
        return stringBuilder.ToString();
    }
}