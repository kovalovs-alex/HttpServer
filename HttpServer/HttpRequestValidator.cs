using System.Text.RegularExpressions;

namespace HttpServer;

public static class HttpRequestValidator
{
    /// <summary>
    /// Validates HTTP requests
    /// </summary>
    /// <param name="headerString"></param>
    /// <returns>HttpRequest instance if request is validated, otherwise null</returns>
    public static HttpRequest ProcessRequest(string headerString)
    {
        var request = new HttpRequest();
        string[] headers = SplitHeaderIntoStringArray(headerString);
        request.requestLine = ProcessRequestLine(headers[0]);

        if (request.requestLine.httpVersion == HttpVersion.HTTP09) return request;
        
        request.headers = ValidateHeadersSection(headers[1..]);

        return request;
    }

    private static string[] SplitHeaderIntoStringArray(string headerString)
    {
        string[] headers = headerString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (headers.Length == 0) throw new ArgumentException("Request Header string is empty");
        return headers;
    }

    #region Verify request-line
    private static RequestLine ProcessRequestLine(string requestLine)
    {
        string[] splitRequestLine = SplitRequestLine(requestLine);

        HttpRequestVerb verb = ValidateVerb(splitRequestLine[0]);
        bool isURIValid = ValidateURI(splitRequestLine[1]);
        HttpVersion protocolVersion = ValidateVersion(splitRequestLine.Length == 3 ? splitRequestLine[2] : "HTTP/0.9"); 

        return new RequestLine {verb = verb, URI = splitRequestLine[1], httpVersion = protocolVersion};
    }
    //TODO: Change to data object
    private static string[] SplitRequestLine(string requestLine)
    {
        string[] splitRequestLine = requestLine.Split(" ");
        //HTTP 0.9 request line consists only of verb and path to resource
        //HTTP 1.0 and above also has protocol version
        if(!(splitRequestLine.Length == 2 || splitRequestLine.Length == 3)) throw new ArgumentException("Incorrect format of request-line");
        return splitRequestLine;
    }

    private static HttpRequestVerb ValidateVerb(string verb)
    {
        switch(verb)
        {
            case "GET":
                return HttpRequestVerb.GET;
            case "POST":
                return HttpRequestVerb.POST;
            case "DELETE":
                return HttpRequestVerb.DELETE;
            case "PUT":
                return HttpRequestVerb.PUT;
            default:
                throw new ArgumentException();
        }
    }

    //Currenly supports only relative paths
    private static bool ValidateURI(string path)
    {
        if(String.IsNullOrEmpty(path)) return false;
        var regex = new Regex("\\.?/.*"); //matches paths like ./* and /*
        return regex.IsMatch(path);
    }
    private static HttpVersion ValidateVersion(string version)
    {
        switch(version)
        {
            case "HTTP/0.9":
                return HttpVersion.HTTP09;
            case "HTTP/1.0":
                return HttpVersion.HTTP10;
            case "HTTP/1.1":
                return HttpVersion.HTTP11;
            default:
                throw new ArgumentException();
        }
    }
    #endregion

    #region Verify rest of headers

    private static Dictionary<string, string> ValidateHeadersSection(string[] headers)
    {
        var headersDict = new Dictionary<string, string>();
        foreach(string header in headers)
        {
            ProcessHeader(headersDict ,header);
        }
        return headersDict;
    }

    private static void ProcessHeader(Dictionary<string, string> headersDict, string header)
    {
        var headerTuple = SplitHeaderIntoTuple(header);
        AddHeaderToHeadersDictionary(headersDict, headerTuple);
    }

    private static Tuple<string, string> SplitHeaderIntoTuple(string header)
    {
        string[] splitHeader = header.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if(splitHeader.Length != 2) throw new ArgumentException($"Request Header has incorrect format; Header {String.Join("|", splitHeader)}");
        return new Tuple<string, string>(splitHeader[0], splitHeader[1]);
    }

    private static void AddHeaderToHeadersDictionary(Dictionary<string, string> headersDict, Tuple<string, string> headerTuple)
    {
        bool addedSuccessfully = headersDict.TryAdd(headerTuple.Item1.Trim(), headerTuple.Item2.Trim());
        if(!addedSuccessfully) throw new ArgumentException("Request header already exists");
    }

    #endregion
}
