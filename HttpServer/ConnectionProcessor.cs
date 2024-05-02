using System.Net.Sockets;
using System.Text;

namespace HttpServer;

public class ConnectionProcessor
{

    public void ProcessConnection(Socket clientSocket)
    {
        var requestProcess = new HttpRequestProcessor();
        try
        {
            bool keepAlive;
            do
            {
                var request = ProcessIncomingRequest(clientSocket, requestProcess);
                
                keepAlive = CheckIfKeepAlive(request);
                
                SendResponse(clientSocket, request);

            } while (keepAlive && clientSocket.Poll(1000, SelectMode.SelectRead) && clientSocket.Connected);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occured when processing connection : {e.Message}");
        }
    }

    private HttpRequest ProcessIncomingRequest(Socket clientSocket, HttpRequestProcessor requestProcess)
    {
        var readResults = ReadSocketUntilDelimiter(clientSocket, "\r\n\r\n", false); // contains header section in first item and request body from buffer if body exists
        if (readResults.Item1.Length == 0)
            throw new Exception("Failed to Receive proper request");
        
        var request = requestProcess.ProcessRequestHeaders(readResults.Item1);
        if (request == null)
            throw new Exception("Failed to Receive proper request");

        if (string.IsNullOrEmpty(readResults.Item2))
            return request;
        
        request.Body = readResults.Item2;
        ReadRequestBody(clientSocket, request);

        return request;
    }

    private bool CheckIfKeepAlive(HttpRequest request)
    {
        if(!request.Headers.TryGetValue("connection", out string? connectionHeaderString))
            return false; // TODO : Change to default value based on protocol version

        if (string.IsNullOrEmpty(connectionHeaderString))
            return false; // TODO : Change to default value based on protocol version
            
        if(connectionHeaderString == "close")
            return false; // TODO : Change to default value based on protocol version

        if(connectionHeaderString == "keep-alive")
            return true;

        return false; // TODO : Change to default value based on protocol version
    }

    private void ReadRequestBody(Socket clientSocket, HttpRequest request)
    {
        var headers = request.Headers;

        if (!int.TryParse(headers["Content-Length"], out int contentLength))
            throw new ArgumentException("Failed to parse Content-Length header");

        //get current read position 
        //read for provided content length - already read part length
        int readFromBodyLength = request.Body.Length; //part of body that was read from buffer
        byte[] buffer = new byte[1024];
        var bodyStringBuilder = new StringBuilder();

        while (readFromBodyLength < contentLength)
        {
            int receivedBytes = clientSocket.Receive(buffer);
            if (receivedBytes == 0) break;

            string decodedBuffer = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
            bodyStringBuilder.Append(decodedBuffer);
            readFromBodyLength += receivedBytes;
        }

        string body = bodyStringBuilder.ToString();
        request.Body += body;
    }

    private Tuple<string, string> ReadSocketUntilDelimiter(Socket socket, string delimiter, bool includeDelimiterInResult)
    {
        byte[] buffer = new byte[1024];
        string result = String.Empty;
        string leftoverFromBuffer = String.Empty;
        var stringBuilder = new StringBuilder();
        int receivedBytes;
        do
        {
            receivedBytes = socket.Receive(buffer);
            string decodedBuffer = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

            int delimiterPosition = decodedBuffer.IndexOf(delimiter, StringComparison.Ordinal);
            if (delimiterPosition == -1)
            {
                stringBuilder.Append(decodedBuffer);
                continue;
            }

            string bufferBeforeDelimiter = decodedBuffer[..delimiterPosition];
            var bufferAfterDelimiter = includeDelimiterInResult
                ? decodedBuffer[delimiterPosition..]
                : decodedBuffer[(delimiterPosition + delimiter.Length)..];

            stringBuilder.Append(bufferBeforeDelimiter);
            result = stringBuilder.ToString();
            leftoverFromBuffer = bufferAfterDelimiter;
            break;
        } while ( receivedBytes > 0 );
        return new Tuple<string, string>(result, leftoverFromBuffer);
    }

    private void SendResponse(Socket socket, HttpRequest request)
    {
        var response = new HttpResponse
        {
            StatusLine = new ResponseStatusLine
            {
                Version = request.RequestLine.HttpVersion
            },
        };

        string requestedResource = request.RequestLine.URI;
        string content = Encoding.UTF8.GetString(File.ReadAllBytes(requestedResource));
        if (requestedResource == "not_found.html") //TODO : Change from magic string
        {
            response.StatusLine.StatusCode = 404;
            response.StatusLine.StatusText = "Not Found";
        }
        else
        {
            response.StatusLine.StatusCode = 200;
            response.StatusLine.StatusText = "OK";
        }

        var responseBody = new HttpResponseBody(content);
        
        response.Body = responseBody;
        response.Headers["Connection"] = "keep-alive";
        response.Headers["Keep-Alive"] = $"timeout=1"; //TODO : Change to read from config file


        string responseString = response.ToString();
        socket.Send(Encoding.UTF8.GetBytes(responseString));
    }
}