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
        request.RequestLine = ProcessRequestLine(headers[0]);

        if (request.RequestLine.HttpVersion == HttpVersion.HTTP09)
            return request;
        
        request.Headers = ValidateHeadersSection(headers[1..]);

        return request;
    }

    private static string[] SplitHeaderIntoStringArray(string headerString)
    {
        string[] headers = headerString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (headers.Length == 0)
            throw new ArgumentException("Request Header string is empty");

        return headers;
    }

    #region Verify request-line
    private static RequestLine ProcessRequestLine(string requestLineString)
    {
        var requestLine = new RequestLine();
        string[] splitRequestLine = SplitRequestLineIntoArray(requestLineString);

        string requestLineVerb = splitRequestLine[0];
        string requestLineUri = splitRequestLine[1];
        string requestLineVersion = splitRequestLine.Length == 3 ? splitRequestLine[2] : "HTTP/0.9";

        requestLine.Verb = ParseRequestVerb(requestLineVerb);
        //bool isUriValid = ValidateURI(requestLineUri);
        requestLine.URI = ValidateURI(requestLineUri) ? requestLineUri : "Not Found";
        requestLine.HttpVersion = ParseRequestVersion(requestLineVersion); 

        return requestLine;
    }
    //TODO: Change to data object
    private static string[] SplitRequestLineIntoArray(string requestLine)
    {
        string[] splitRequestLineArray = requestLine.Split(" ");
        //HTTP 0.9 request line consists only of verb and path to resource
        //HTTP 1.0 and above also has protocol version
        if(!(splitRequestLineArray.Length == 2 || splitRequestLineArray.Length == 3)) 
            throw new ArgumentException("Incorrect format of request-line");

        return splitRequestLineArray;
    }

    private static HttpRequestVerb ParseRequestVerb(string verb)
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
                throw new ArgumentException($"Provided HTTP verb is not supported : {verb}" );
        }
    }

    //Currenly supports only relative paths
    private static bool ValidateURI(string path)
    {
        if(String.IsNullOrEmpty(path)) 
            return false;

        var regex = new Regex("\\.?/.*"); //matches paths like ./* and /*
        return regex.IsMatch(path);
    }
    private static HttpVersion ParseRequestVersion(string version)
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
                throw new ArgumentException($"Provided HTTP Version is not supported : {version}");
        }
    }
    #endregion

    #region Verify rest of headers

    private static Dictionary<string, string> ValidateHeadersSection(string[] headers)
    {
        var headersDict = new Dictionary<string, string>();
        foreach(string header in headers)
        {
            var headerTuple = SplitHeaderIntoKeyValueTuple(header);
            AddHeaderTupleToHeadersDictionary(headersDict, headerTuple);
        }
        return headersDict;
    }

    private static Tuple<string, string> SplitHeaderIntoKeyValueTuple(string header)
    {
        int indexOfHeaderDelimiter = header.IndexOf(':');
        if(indexOfHeaderDelimiter == -1)
            throw new ArgumentException($"Request Header has incorrect format; Header = {header}");

        string headerKey = header[..indexOfHeaderDelimiter];
        string headerValue = header[(indexOfHeaderDelimiter+1)..].Trim();

        return new Tuple<string, string>(headerKey, headerValue);
    }

    private static void AddHeaderTupleToHeadersDictionary(Dictionary<string, string> headersDict, Tuple<string, string> headerTuple)
    {
        bool addedSuccessfully = headersDict.TryAdd(headerTuple.Item1.Trim().ToLower(), headerTuple.Item2.Trim());
        if(!addedSuccessfully) 
            throw new ArgumentException("Request header already exists");
    }

    #endregion
}
