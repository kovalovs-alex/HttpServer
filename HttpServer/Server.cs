using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HttpServer;

public class Server
{
    public Server()
    {
    }

    public void Start()
    {

        var host = Dns.GetHostEntry("localhost");
        var address = host.AddressList[0];
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

                string incomingRequest = ReadSocketToEnd(clientSocket);
                Console.WriteLine("Request : " + incomingRequest);

                clientSocket.Send(Encoding.UTF8.GetBytes("Hello world"));
                Console.WriteLine("Response : " + "Hello world");
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
    private string ReadSocketToEnd(Socket clientSocket, int bufferSize = 1024)
    {
        byte[] buffer = new byte[bufferSize];
        var stringBuilder = new StringBuilder();

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
            string headerString = intermediateString.Substring(0, intermediateString.Length-receivedBytes+headerEndPosition);

            var request = HttpRequestValidator.ProcessRequest(headerString);
            
            //TODO: Read header content-length and read request body to end

        }    

        string receivedRequest = stringBuilder.ToString();
        return receivedRequest;
    }

}
