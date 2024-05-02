using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HttpServer.Extensions;

namespace HttpServer;

public class Server
{
    private readonly int _timeout = 500;
    private readonly ConnectionProcessor _connectionProcessor;
    private readonly int _port;
    private readonly IPAddress _address;

    public Server(IPAddress address, int port)
    {
        _address = address;
        _port = port;
        _connectionProcessor = new ConnectionProcessor();
    }
    
    public Server(string address, int port)
    {
        _address = IPAddress.Parse(address);
        _port = port;
        _connectionProcessor = new ConnectionProcessor();
    }

    public void Start()
    {
        var endpoint = new IPEndPoint(_address, _port);
        var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        int connectionNumber = 1;

        try
        {
            listener.Bind(endpoint);
            listener.Listen();

            while (true)
            {
                var clientSocket = listener.Accept();
                clientSocket.ReceiveTimeout = _timeout; //TODO : Change to read from config file
                clientSocket.SendTimeout = _timeout;

                _connectionProcessor.ProcessConnection(clientSocket);
                
                Console.WriteLine($"Connection Number : {connectionNumber}");
                clientSocket.Close();
                clientSocket.Dispose();
                connectionNumber++;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception Caught : {ex.Message}");
        }
    }
}
