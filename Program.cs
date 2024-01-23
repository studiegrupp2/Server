using System.Net;
using System.Net.Sockets;
using Shared;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        IConnectionHandler connectionHandler = new SocketConnectionHandler();

        while (true)
        {
            Shared.IConnection? potentialClient = connectionHandler.Accept();
            if (potentialClient != null)
            {
                Console.WriteLine("A client has connected!");
            }

            connectionHandler.HandleReads();
        }
    }
}

public interface IConnectionHandler
{
    Shared.IConnection? Accept();
    void HandleReads();
}

public class SocketConnectionHandler : IConnectionHandler
{
    private Socket serverSocket;

    private List<IConnection> connections;
    private Dictionary<int, IMessageHandler> handlers;

    public SocketConnectionHandler()
    {
        IPAddress iPAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, 27800);

        this.serverSocket = new Socket(
            iPAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        this.serverSocket.Bind(iPEndPoint);
        this.serverSocket.Listen();

        this.connections = new List<IConnection>();
        this.handlers = new Dictionary<int, IMessageHandler>();
        this.handlers[10] = new RegisterHandler();
        this.handlers[11] = new LoginHandler();
    }

    public Shared.IConnection? Accept()
    {
        if (!this.serverSocket.Poll(50, SelectMode.SelectRead))
        {
            return null;
        }

        Socket clientSocket = this.serverSocket.Accept();
        IConnection connection = new Shared.SocketConnection(clientSocket);
        this.connections.Add(connection);
        return connection;
    }

    public void HandleReads()
    {
        for (int i = 0; i < this.connections.Count; i++)
        {
            IConnection connection = this.connections[i];

            foreach (Shared.Message message in connection.Receive())
            {
                IMessageHandler handler = this.handlers[message.Id()];
                handler.Handle(connection, message);
            }
        }
    }
}

public interface IMessageHandler
{
    void Handle(IConnection connection, Message message);
}

public class RegisterHandler : IMessageHandler
{
    public void Handle(IConnection connection, Message message)
    {
        Console.WriteLine("Do register logic!");
    }
}

public class LoginHandler : IMessageHandler
{
    public void Handle(IConnection connection, Message message)
    {
        Shared.LoginMessage login = (Shared.LoginMessage)message;
        Console.WriteLine("Do login logic! Name: " + login.Name);
    }
}
