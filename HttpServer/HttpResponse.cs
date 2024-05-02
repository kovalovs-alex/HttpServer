using System.Text;
using HttpServer.Extensions;

namespace HttpServer;

public class HttpResponse
{
    public ResponseStatusLine StatusLine {get; set;}
    public Dictionary<string, string> Headers = new Dictionary<string, string>();
    public HttpResponseBody? Body;
    // public string Body { get; set;}

    public override string ToString()
    {
        var headerStringBuilder = new StringBuilder();
        headerStringBuilder.Append(Headers.ConvertToHttpHeaders());
        if(Body != null)
            headerStringBuilder.Append(Body.ContentHeaders.ConvertToHttpHeaders());
            
        return $"{StatusLine}{headerStringBuilder}\r\n\r\n{Body?.Content}";
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

