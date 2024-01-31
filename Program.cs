using System.Net;
using System.Net.Sockets;
using Shared;

namespace Server;

class Program
{
    public static UserService userService = new UserService(new DbUserRepository());
    public static MessageService messageService = new MessageService(new DbMessageRepository());

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

            //Console.WriteLine(userService.loggedIn.UserName);

            //TODO: Kanske fel med messageService, att loogedIn inte ligger i DbMessageRep...//

            // foreach (Message msg in messageService.messages.GetAll())
            //     {
            //         Console.WriteLine($"You have a new message from {msg.Sender}: {msg.Content}");
            //     }
            // foreach (User user in userService.users.GetAll())
            // {
            //     Console.WriteLine($"{user.UserName}");
            // }
        }
    }
}

public interface IConnectionHandler
{
    Shared.IConnection? Accept();
    void HandleReads();
}

// public class ChatApp
// {
//     public static UserService userService;
//     public ChatApp() 
//     {
//         this.userService = new UserService(new DbUserRepository());
//     }
// }

public class SocketConnectionHandler : IConnectionHandler
{
    private Socket serverSocket;

    public List<IConnection> connections;
    private Dictionary<int, ICommandHandler> handlers;

    public UserService userService;
    public MessageService messageService;

    public SocketConnectionHandler()
    {
        this.userService = new UserService(new DbUserRepository());
        this.messageService = new MessageService(new DbMessageRepository());
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
        this.handlers = new Dictionary<int, ICommandHandler>();
        this.handlers[10] = new RegisterHandler(); //service?
        this.handlers[11] = new LoginHandler();
        this.handlers[12] = new SendMessageHandler();
        this.handlers[13] = new SendPrivateMessageHandler();
        this.handlers[14] = new LogoutHandler();
        this.handlers[15] = new DisconnectHandler();
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

            //todo kolla om en klient disconnectar

            foreach (Shared.Command command in connection.Receive())
            {
                ICommandHandler handler = this.handlers[command.Id()];
                handler.Handle(connection, command, this);
            }
        }

        // connections.Remove(clientSocket)
    }
}

public interface ICommandHandler
{
    void Handle(IConnection connection, Command command, SocketConnectionHandler handler);
}

public class RegisterHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        Shared.RegisterUserCommand register = (Shared.RegisterUserCommand)command;//message
        handler.userService.Register(register.Name, register.Password);
    }
}

public class LoginHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        Shared.LoginCommand login = (Shared.LoginCommand)command;//message
        Shared.User? user = handler.userService.Login(login.Name, login.Password);
        if (user != null)
        {
            connection.SetUser(user);

            connection.Send(new SendMessageCommand($"Server", $"You Successfully logged in"));
            foreach (IConnection connectedClient in handler.connections)
            {
                if (connectedClient != connection)
                { //Kollar om users som är anslutna är samma user som loggar in 
                    connectedClient.Send(new SendMessageCommand("Server", $"{user.UserName} logged in."));
                }
            }
            foreach (Message message in handler.messageService.messages.GetAll(user.UserName))
            {
                if (message.Receiver == "All" || message.Receiver == user.UserName)
                {
                    connection.Send(new SendMessageCommand(message.Sender, message.Content));
                }

                // handler.messageService.Create(connection.GetUser().UserName, "reciever", message.Content);
                // connectedClient.Send(globalmsg);
            }
        }
        else
        {
            connection.Send(new SendMessageCommand($"Server", "Login failed. Wrong username or password."));
        }
    }
}

public class SendMessageHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        Console.WriteLine($"User {connection.GetUser().UserName} has sent a message.");
        Shared.SendMessageCommand globalmsg = (Shared.SendMessageCommand)command;
        handler.messageService.Create(connection.GetUser().UserName, "All", globalmsg.Content);

        foreach (IConnection connectedClient in handler.connections)
        {
            connectedClient.Send(new SendMessageCommand(globalmsg.Sender, globalmsg.Content));
        }
    }
}

public class SendPrivateMessageHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        Shared.SendPrivateMessageCommand privatemsg = (Shared.SendPrivateMessageCommand)command;
        handler.messageService.Create(connection.GetUser().UserName, privatemsg.Receiver, privatemsg.Content);
        foreach (IConnection connectedClient in handler.connections)
        {
            if (connectedClient.GetUser().UserName == privatemsg.Receiver)
            {
                connectedClient.Send(new SendMessageCommand(privatemsg.Sender, privatemsg.Content));
            }
        }
    }
}


public class LogoutHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        Shared.LogoutCommand logout = (Shared.LogoutCommand)command;//message
        handler.userService.Logout(logout.UserName);
        connection.Send(new SendMessageCommand($"Server", $"You logged out."));



        foreach (IConnection connectedClient in handler.connections)
        {
            if (connectedClient != connection)
            { //Kollar om users som är anslutna är samma user som loggar in 
                connectedClient.Send(new SendMessageCommand("Server", $"{logout.UserName} logged out."));
            }
        }
    }
}

public class DisconnectHandler : ICommandHandler
{
    public void Handle(IConnection connection, Command command, SocketConnectionHandler handler)
    {
        // connection.Remove();
        // connection.Clear()

        handler.connections.Remove(connection);
        Console.WriteLine("A client has Disconnected!");


    }
}