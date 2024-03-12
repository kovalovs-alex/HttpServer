namespace HttpServer;

public class HttpResponse
{
    public ResponseStatusLine StatusLine {get;set;}
    public Dictionary<string, string> Headers;
    public string Body { get; set;}
}

public class ResponseStatusLine
{
    public HttpVersion Version {get; set; }
    public int StatusCode {get; set; }
    public string StatusText {get; set; }
}

