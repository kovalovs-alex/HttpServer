using System.Text.RegularExpressions;

namespace HttpServer;

public static class HttpRequestValidator
{
    private static readonly string[] supportedVerbs = ["GET", "POST", "DELETE", "PUT"]; //TODO : Add the rest of verbs
    private static readonly string[] supportedVersions = ["HTTP/0.9","HTTP/1.0", "HTTP/1.1"]; //TODO : Add the rest of verbs


    public static bool ValidateRequestHeaders(string headerString)
    {
        string[] headers = headerString.Split("\r\n");
        if (headers.Length == 0) return false;

        ValidateRequestHeaderBase(headers[0]);

        return true;
    }

    private static bool ValidateRequestHeaderBase(string headerBase)
    {
        string[] splitHeaders = headerBase.Split(" ");
        //HTTP 0.9 first header line consists only of verb and path to resource
        //HTTP 1.0 and above also has protocol version
        if(!(splitHeaders.Length == 2 || splitHeaders.Length == 3)) return false;

        string verb = splitHeaders[0];
        string resourcePath = splitHeaders[1];
        string protocolVersion = splitHeaders.Length == 3 ? splitHeaders[2] : String.Empty;
        
        if(!ValidateVerb(verb)) return false;
        if(!ValidatePath(resourcePath)) return false;
        if(!ValidateVersion(protocolVersion)) return false;

        return true;
    }

    private static bool ValidateVerb(string verb)
    {
        if(String.IsNullOrEmpty(verb)) return false;
        return supportedVerbs.Contains(verb);
    }

    //Currenly supports only relative paths
    private static bool ValidatePath(string path)
    {
        if(String.IsNullOrEmpty(path)) return false;
        var regex = new Regex("\\.?/.*"); //matches paths like ./* and /*
        return regex.IsMatch(path);
    }

    private static bool ValidateVersion(string version)
    {
        if(String.IsNullOrEmpty(version)) version = "HTTP/0.9";
        return supportedVersions.Contains(version);
    }
}
