namespace HttpServer;

public class HttpRequest
{
    HttpRequestVerb verb;
    HttpVersion version;
    string uri;
    public Dictionary<string, string> headers;

}
