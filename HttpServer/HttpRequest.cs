namespace HttpServer;

public class HttpRequest
{
    public RequestLine RequestLine;
    public Dictionary<string, string> Headers;
    public string Body;

}
