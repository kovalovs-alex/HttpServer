using System.Text;
using HttpServer.Extensions;

namespace HttpServer;

public class HttpResponse
{
    public ResponseStatusLine StatusLine {get;set;}
    public Dictionary<string, string> Headers;
    public string Body { get; set;}

    public override string ToString()
    {
        return $"{StatusLine}{HeaderToString()}{Body}\r\n";
    }

    //TODO: Rename
    private string HeaderToString()
    {
        var stringBuilder = new StringBuilder();
        foreach(string key in Headers.Keys)
        {
            stringBuilder.Append($"{key}: {Headers[key]}\r\n");
        }
        return stringBuilder.ToString();
    }
}

public class ResponseStatusLine
{
    public HttpVersion Version {get; set; }
    public int StatusCode {get; set; }
    public string StatusText {get; set; }

    public override string ToString()
    {
        return $"{Version.StringValue()} {StatusCode} {StatusText}\r\n";
    }
}

