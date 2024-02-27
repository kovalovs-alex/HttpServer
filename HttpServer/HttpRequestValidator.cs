using System.Text.RegularExpressions;

namespace HttpServer;

public static class HttpRequestValidator
{
    /// <summary>
    /// Validates HTTP requests
    /// </summary>
    /// <param name="headerString"></param>
    /// <returns>HttpRequest instance if request is validated, otherwise null</returns>
    public static bool ValidateRequestHeaders(string headerString)
    {
        string[] headers = headerString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (headers.Length == 0) return false;

        ValidateRequestLine(headers[0]);

        if (headers.Length > 1) return false;
            ValidateHeadersDictionary(headers[1..]);

        return true;
    }

    #region Verify request-line
    private static bool ValidateRequestLine(string headerBase)
    {
        string[] splitHeaders = headerBase.Split(" ");
        //HTTP 0.9 first header line consists only of verb and path to resource
        //HTTP 1.0 and above also has protocol version
        if(!(splitHeaders.Length == 2 || splitHeaders.Length == 3)) return false;

        string verb = splitHeaders[0];
        string resourcePath = splitHeaders[1];
        string protocolVersion = splitHeaders.Length == 3 ? splitHeaders[2] : String.Empty;
        
        // if(ValidateVerb(verb) ) return false;
        // if(!ValidatePath(resourcePath)) return false;
        // if(!ValidateVersion(protocolVersion)) return false;

        return true;
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
    private static bool ValidatePath(string path)
    {
        if(String.IsNullOrEmpty(path)) return false;
        var regex = new Regex("\\.?/.*"); //matches paths like ./* and /*
        return regex.IsMatch(path);
    }
    private static HttpVersion ValidateVersion(string version)
    {
        // if(String.IsNullOrEmpty(version)) version = "HTTP/0.9";
        // return supportedVersions.Contains(version);
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

    private static Dictionary<string, string>? ValidateHeadersDictionary(string[] headers)
    {
        var headersDict = new Dictionary<string, string>();
        foreach(string header in headers)
        {
            string[] splitHeader = header.Split(':');
            if(splitHeader.Length != 2) throw new ArgumentException();
            bool addedheader = headersDict.TryAdd(splitHeader[0].Trim(), splitHeader[1].Trim());
            if(!addedheader) throw new ArgumentException();
        }
        return headersDict;
    }

    #endregion
}
