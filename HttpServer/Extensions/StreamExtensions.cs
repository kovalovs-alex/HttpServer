namespace HttpServer;

public static class StreamExtensions
{
    /// <summary>
    /// Creates a stream reader with parameter stream and leaveOpen:true. Allows using stream reader in <code>using</code> statement without closing underlying Stream
    /// </summary>
    /// <param name="stream"></param>
    /// <returns>StreamReader instance with default settings and leaveOpen:true</returns>
    public static StreamReader WrapInStreamReader(this Stream stream)
    {
        return new StreamReader(stream, leaveOpen:true);
    }

}
