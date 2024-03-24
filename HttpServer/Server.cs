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

            while(true)
            {
                string responseBody = "";
                
                //socket to client
                var clientSocket = listener.Accept();

                var incomingRequest = ReadSocketToEnd(clientSocket);

                var response = new HttpResponse {
                    StatusLine = new ResponseStatusLine{ 
                        Version = incomingRequest.RequestLine.HttpVersion
                    },
                    Headers = new Dictionary<string, string>(),
                };

                string requestedResource = incomingRequest.RequestLine.URI;
                if(File.Exists(requestedResource))
                {
                    responseBody = Encoding.UTF8.GetString(File.ReadAllBytes(requestedResource));
                    response.StatusLine.StatusCode = 200;
                    response.StatusLine.StatusText = "OK";
                }
                else
                {
                    responseBody = Encoding.UTF8.GetString(File.ReadAllBytes("not_found.html"));
                    response.StatusLine.StatusCode = 404;
                    response.StatusLine.StatusText = "Not Found";
                }
                response.Body = responseBody;


                clientSocket.Send(Encoding.UTF8.GetBytes(response.ToString()));

                clientSocket.Close();
                clientSocket.Dispose();
            }

        }
        catch (Exception)
        {
            throw;
        }
    }

    //TODO: Rename method to better reflect what it does
    private HttpRequest ReadSocketToEnd(Socket clientSocket, int bufferSize = 1024)
    {
        var requestProcess = new HttpRequestProcessor();
        
        Tuple<string, string> readResults = ReadSocketUntilDelimiter(clientSocket, "\r\n\r\n", false); // contains header section in first item and request body from buffer if body exists
        HttpRequest request = requestProcess.ProcessRequestHeaders(readResults.Item1);
        if (request == null)
            throw new Exception("Failed to Receive proper request");
        
        if(string.IsNullOrEmpty(readResults.Item2))
            return request;

        request.Body = readResults.Item2;
        ReadRequestBody(clientSocket, request);
        return request;
    }

    private void ReadRequestBody(Socket clientSocket, HttpRequest request )
    {
        var headers = request.Headers;
        int contentLength;

        if(!int.TryParse(headers["Content-Length"], out contentLength))
            throw new ArgumentException("Failed to parse Content-Lenght header");

        //get current read position 
        //read for provided content length - already read part length
        int readFromBodyLength = request.Body.Length; //part of body that was read from buffer
        byte[] buffer = new byte[1024];
        var bodyStringBuilder = new StringBuilder();

        while (readFromBodyLength < contentLength){
            
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

    private Tuple<string,string> ReadSocketUntilDelimiter(Socket socket, string delimiter, bool includeDelimiterInResult)
    {
        byte[] buffer = new byte[1024];
        string result = String.Empty;
        string leftoverFromBuffer = String.Empty;
        var stringBuilder = new StringBuilder();

        for(int receivedBytes; (receivedBytes = socket.Receive(buffer)) > 0;)
        {    
            if (receivedBytes == 0) break; //TODO Research if change to Timeout would be better
            byte[] bufferWithoutZeros = new byte[receivedBytes];
            Array.Copy(buffer, bufferWithoutZeros, receivedBytes); //bufferWithoutZeros will include data without trailing zeros from buffer

            string decodedBuffer = Encoding.UTF8.GetString(bufferWithoutZeros);

            int delimiterPosition = decodedBuffer.IndexOf(delimiter);
            if(delimiterPosition == -1)
            {
                stringBuilder.Append(decodedBuffer);
                continue;
            }

            string bufferBeforeDelimiter = decodedBuffer[..delimiterPosition];
            string bufferAfterDelimiter;
            if(includeDelimiterInResult)
                bufferAfterDelimiter = decodedBuffer[delimiterPosition..];
            else
                bufferAfterDelimiter = decodedBuffer[(delimiterPosition+delimiter.Length)..];

            stringBuilder.Append(bufferBeforeDelimiter);
            result = stringBuilder.ToString();
            leftoverFromBuffer = bufferAfterDelimiter;
            break;
        }  
        return new Tuple<string, string>(result, leftoverFromBuffer);
    }

}
