using System.Net;
using System.Net.Sockets;
using Shared;

namespace Server;

class Program
{
    private UserService userService = new UserService(new DbUserRepository());
    private DbMessageRepository messageRepository = new DbMessageRepository();
    static void Main(string[] args)
    {
        // ChatApp.userService.Register("evelina", "hej123");
        
        // ChatApp.userService.Login("evelina", "hej123");
        // userService.Login("christian", "hej");
        
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

    private List<IConnection> connections;
    private Dictionary<int, ICommandHandler> handlers;

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
        this.handlers = new Dictionary<int, ICommandHandler>();
        this.handlers[10] = new RegisterHandler();
        this.handlers[11] = new LoginHandler();
        // this.handlers[12] = new SendMessageHandler();
        // this.handlers[13] = new SendPrivateMessageHandler();
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

            foreach (Shared.Command command in connection.Receive())
            {
                ICommandHandler handler = this.handlers[command.Id()];
                handler.Handle(connection, command);
            }
        }
    }
}

public interface ICommandHandler
{
    void Handle(IConnection connection, Command command);
}

public class RegisterHandler : ICommandHandler
{
    private UserService userService = new UserService(new DbUserRepository());
    public void Handle(IConnection connection, Command command)
    {
        Shared.RegisterUserCommand register = (Shared.RegisterUserCommand)command;//message
        userService.Register(register.Name, register.Password);
    }
}

public class LoginHandler : ICommandHandler
{
    private UserService userService = new UserService(new DbUserRepository());
    
    public void Handle(IConnection connection, Command command)
    {
        Shared.LoginCommand login = (Shared.LoginCommand)command;//message
        userService.Login(login.Name, login.Password);
    }
}

// public class SendMessageHandler : ICommandHandler
// {
//     // private MessageService messageService = new MessageService(new DbMessageRepository())
//     // 
//     private DbMessageRepository messageRepository = new DbMessageRepository();

//     public void Handle(IConnection connection, Command command)
//     {
//         Shared.SendMessageCommand globalmsg = (Shared.SendMessageCommand)command;
//         messageRepository.Save(globalmsg);
//     } 
// }

// public class SendPrivateMessageHandler : ICommandHandler
// {
//     // private DbMessageRepository messageRepository = new DbMessageRepository();
//     private DbMessageRepository messageRepository = new DbMessageRepository();

//     public void Handle(IConnection connection, Command command)
//     {
//         Shared.SendMessageCommand privatemsg = (Shared.SendMessageCommand)command;
//         messageRepository.Save(privatemsg);
        
//     } 
// }

