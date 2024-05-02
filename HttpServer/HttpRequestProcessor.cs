using System.Text.RegularExpressions;

namespace HttpServer;

public class HttpRequestProcessor
{
    /// <summary>
    /// Validates HTTP requests
    /// </summary>
    /// <param name="headerString"></param>
    /// <returns>HttpRequest instance if request is validated, otherwise null</returns>
    public HttpRequest ProcessRequestHeaders(string headerString)
    {
        var request = new HttpRequest();
        string[] headers = SplitHeaderIntoStringArray(headerString);
        request.RequestLine = ProcessRequestLine(headers[0]);

        if (request.RequestLine.HttpVersion == HttpVersion.HTTP09)
            return request;
        
        request.Headers = ValidateHeadersSection(headers[1..]);

        return request;
    }

    private string[] SplitHeaderIntoStringArray(string headerString)
    {
        string[] headers = headerString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (headers.Length == 0)
            throw new ArgumentException("Request Header string is empty");

        return headers;
    }

    #region Verify request-line
    private RequestLine ProcessRequestLine(string requestLineString)
    {
        var requestLine = new RequestLine();
        string[] splitRequestLine = SplitRequestLineIntoArray(requestLineString);

        string requestLineVerb = splitRequestLine[0];
        string requestLineUri = splitRequestLine[1];
        string requestLineVersion = splitRequestLine.Length == 3 ? splitRequestLine[2] : "HTTP/0.9";

        requestLine.Verb = ParseRequestVerb(requestLineVerb);
        requestLine.URI = ValidateURI(requestLineUri);
        requestLine.HttpVersion = ParseRequestVersion(requestLineVersion); 

        return requestLine;
    }
    //TODO: Change to data object
    private string[] SplitRequestLineIntoArray(string requestLine)
    {
        string[] splitRequestLineArray = requestLine.Split(" ");
        //HTTP 0.9 request line consists only of verb and path to resource
        //HTTP 1.0 and above also has protocol version
        if(!(splitRequestLineArray.Length == 2 || splitRequestLineArray.Length == 3)) 
            throw new ArgumentException("Incorrect format of request-line");

        return splitRequestLineArray;
    }

    private HttpRequestVerb ParseRequestVerb(string verb)
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
    private string ValidateURI(string path)
    {
        if(String.IsNullOrEmpty(path)) 
            throw new ArgumentNullException($"Argument {nameof(path)} is null or empty");

        string processedPath = path;

        var regex = new Regex("\\.?/.*"); //matches paths like ./* and /*
        if(!regex.IsMatch(processedPath))
            throw new ArgumentException($"Provided Uri is not a valid path");

        if(processedPath[0] == '.')
            processedPath = processedPath[1..];

        if(processedPath[0]== '/') 
            processedPath = processedPath[1..];

        //if path become empty after alterations - it means it was either '.' './' or '/' 
        if(processedPath.Length == 0) 
            processedPath = "index.html";
            
        return !File.Exists(processedPath) ? "not_found.html" : processedPath;
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

    private Dictionary<string, string> ValidateHeadersSection(string[] headers)
    {
        var headersDict = new Dictionary<string, string>();
        foreach(string header in headers)
        {
            var headerTuple = SplitHeaderIntoKeyValueTuple(header);
            AddHeaderTupleToHeadersDictionary(headersDict, headerTuple);
        }
        return headersDict;
    }

    private Tuple<string, string> SplitHeaderIntoKeyValueTuple(string header)
    {
        int indexOfHeaderDelimiter = header.IndexOf(':');
        if(indexOfHeaderDelimiter == -1)
            throw new ArgumentException($"Request Header has incorrect format; Header = {header}");

        string headerKey = header[..indexOfHeaderDelimiter];
        string headerValue = header[(indexOfHeaderDelimiter+1)..].Trim();

        return new Tuple<string, string>(headerKey, headerValue);
    }

    private void AddHeaderTupleToHeadersDictionary(Dictionary<string, string> headersDict, Tuple<string, string> headerTuple)
    {
        bool addedSuccessfully = headersDict.TryAdd(headerTuple.Item1.Trim().ToLower(), headerTuple.Item2.Trim());
        if(!addedSuccessfully) 
            throw new ArgumentException("Request header already exists");
    }

    #endregion
}
