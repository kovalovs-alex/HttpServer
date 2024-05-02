using System.Text;

namespace HttpServer;

public class HttpResponseBody
{
    public HttpResponseBody(string content)
    {
        Content = content;
        ContentHeaders = new Dictionary<string, string>
        {
            ["Content-Length"] = Encoding.UTF8.GetByteCount(content).ToString(),
            ["Content-Type"] = "text/html; charset=utf-8"
        };
    }
    public string Content { get; }
    public Dictionary<string, string> ContentHeaders { get; }


}