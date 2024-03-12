using HttpServer.Attributes;

namespace HttpServer;

public enum HttpVersion
{
    [StringValue("HTTP/0.9")]
    HTTP09,
    [StringValue("HTTP/1.0")]
    HTTP10,
    [StringValue("HTTP/1.1")]
    HTTP11

}
