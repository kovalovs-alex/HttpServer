using System.Net;
using System.Net.Sockets;
using System.Text;
using HttpServer.Extensions;

namespace HttpServer;

public class Server
{

    public Server()
    {

    }

    public void Start()
    {

        // var host = Dns.GetHostEntry("localhost");
        // var address = host.AddressList[0];
        var address = IPAddress.Parse("127.0.0.1");
        int port = 8080;
        var endpoint = new IPEndPoint(address, port);
        Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp);

        try
        {
            //bind to listen on localhost
            listener.Bind(endpoint);
            listener.Listen();

            while (true)
            {
                //socket to client
                var clientSocket = listener.Accept();

                ProcessConnection(clientSocket);


                clientSocket.Close();
                clientSocket.Dispose();
            }

        }
        catch (Exception)
        {
            throw;
        }
    }

    private void ProcessConnection(Socket clientSocket, int bufferSize = 1024)
    {
        var requestProcess = new HttpRequestProcessor();
        bool keepAlive = false;
        do
        {
            if(clientSocket.Available == 0)// change to async
                continue;
            Tuple<string, string> readResults = ReadSocketUntilDelimiter(clientSocket, "\r\n\r\n", false); // contains header section in first item and request body from buffer if body exists
            HttpRequest request = requestProcess.ProcessRequestHeaders(readResults.Item1);
            if (request == null)
                throw new Exception("Failed to Receive proper request");
            keepAlive = CheckConnectionHeader(request);

            if (!string.IsNullOrEmpty(readResults.Item2))
            {
                request.Body = readResults.Item2;
                ReadRequestBody(clientSocket, request);
            }

            SendResponse(clientSocket, request);

        } while (keepAlive);

        static bool CheckConnectionHeader(HttpRequest request)
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
    }

    private void ReadRequestBody(Socket clientSocket, HttpRequest request)
    {
        var headers = request.Headers;
        int contentLength;

        if (!int.TryParse(headers["Content-Length"], out contentLength))
            throw new ArgumentException("Failed to parse Content-Lenght header");

        //get current read position 
        //read for provided content length - already read part length
        int readFromBodyLength = request.Body.Length; //part of body that was read from buffer
        byte[] buffer = new byte[1024];
        var bodyStringBuilder = new StringBuilder();

        while (readFromBodyLength < contentLength)
        {

            int receivedBytes = clientSocket.Receive(buffer);
            if (receivedBytes == 0) break;
            byte[] bufferWithoutZeros = new byte[receivedBytes];
            Array.Copy(buffer, bufferWithoutZeros, receivedBytes); //bufferWithoutZeros will include data without trailing zeros from buffer

            string decodedBuffer = Encoding.UTF8.GetString(bufferWithoutZeros);
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

        for (int receivedBytes; (receivedBytes = socket.Receive(buffer)) > 0;)
        {
            if (receivedBytes == 0) break; //TODO Research if change to Timeout would be better
            byte[] bufferWithoutZeros = new byte[receivedBytes];
            Array.Copy(buffer, bufferWithoutZeros, receivedBytes); //bufferWithoutZeros will include data without trailing zeros from buffer

            string decodedBuffer = Encoding.UTF8.GetString(bufferWithoutZeros);

            int delimiterPosition = decodedBuffer.IndexOf(delimiter);
            if (delimiterPosition == -1)
            {
                stringBuilder.Append(decodedBuffer);
                continue;
            }

            string bufferBeforeDelimiter = decodedBuffer[..delimiterPosition];
            string bufferAfterDelimiter;
            if (includeDelimiterInResult)
                bufferAfterDelimiter = decodedBuffer[delimiterPosition..];
            else
                bufferAfterDelimiter = decodedBuffer[(delimiterPosition + delimiter.Length)..];

            stringBuilder.Append(bufferBeforeDelimiter);
            result = stringBuilder.ToString();
            leftoverFromBuffer = bufferAfterDelimiter;
            break;
        }
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
            Headers = new Dictionary<string, string>(),
        };

        string requestedResource = request.RequestLine.URI;
        string responseBody = Encoding.UTF8.GetString(File.ReadAllBytes(requestedResource));
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

        response.Body = responseBody;
        response.Headers["Content-Length"] = Encoding.UTF8.GetByteCount(response.Body).ToString();
        response.Headers["Content-Type"] = "text/html; charset=utf-8";
        response.Headers["Connection"] = "keep-alive";

        string responseString = response.ToString();
        socket.Send(Encoding.UTF8.GetBytes(responseString));
    }
}
