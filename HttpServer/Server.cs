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
                //socket to client
                var clientSocket = listener.Accept();

                var incomingRequest = ReadSocketToEnd(clientSocket);
                //Console.WriteLine("Request : " + incomingRequest);

                var response = new HttpResponse {

                    StatusLine = new ResponseStatusLine{ 
                        Version = incomingRequest.RequestLine.HttpVersion,
                        StatusCode = 200,
                        StatusText = "OK"
                    },
                    Headers = new Dictionary<string, string>(),
                    Body = $"<!doctype html><html><body><p>paragraph 1</p><p>paragraph 2</p><p>Requested URI = {incomingRequest.RequestLine.URI}</p></body</html>"
                };

                clientSocket.Send(Encoding.UTF8.GetBytes($"{response.StatusLine.Version.StringValue()} {response.StatusLine.StatusCode} {response.StatusLine.StatusText}\r\n\r\n{response.Body}"));

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
        byte[] buffer = new byte[bufferSize];
        var stringBuilder = new StringBuilder();
        HttpRequest? request = null;

        for(int receivedBytes; (receivedBytes = clientSocket.Receive(buffer)) > 0;)
        {    
            if (receivedBytes == 0) break;
            byte[] bufferWithoutZeros = new byte[receivedBytes];
            Array.Copy(buffer, bufferWithoutZeros, receivedBytes); //bufferWithoutZeros will include data without trailing zeros from buffer

            string decodedBuffer = Encoding.UTF8.GetString(bufferWithoutZeros);
            stringBuilder.Append(decodedBuffer);

            int headerEndPosition = decodedBuffer.IndexOf("\r\n\r\n"); //HTTP header list always ends with 2 CRLF

            if(headerEndPosition == -1) continue;
            
            string intermediateString = stringBuilder.ToString();

            int headerLength = intermediateString.Length-receivedBytes+headerEndPosition;
            string headerString = intermediateString[..headerLength];

            //because of reading from buffer, part of request body might be read without knowing it's length
            //this body part is separated from header part and if body exists(Content-Length header is provided and != 0) it is prepended to body that read in ProcessRequestBody part
            string requestBodyFromBuffer = intermediateString[headerLength..];

            request = HttpRequestValidator.ProcessRequest(headerString);

            //TODO: Read header content-length and read request body to end
            if (!request.Headers.TryGetValue("Content-Length", out string? value) || String.IsNullOrEmpty(value))
                return request;
                
            request.Body = requestBodyFromBuffer;
            ProcessRequestBody(clientSocket, request);

        }    

        if (request == null)
            throw new Exception("Failed to Receive proper request");

        return request;
    }

    private void ProcessRequestBody(Socket clientSocket, HttpRequest request )
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
