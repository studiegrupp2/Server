using MongoDB.Driver;
using Shared;

namespace Server;

public interface IUserService
{
    User Register(string userName, string password);
    User? Login(string userName, string password);
    void Logout(string userName);
}

public interface IUserRepository
{
    void Save(User user);
    User? GetUserByUserNameAndPassword(string userName, string password);
    List<User> GetAll();

    User? GetUserByUserName(string userName);
}

public class UserService : IUserService
{
    public IUserRepository users;
    private User? loggedIn;

    public UserService(IUserRepository repository)
    {
        this.users = repository;
        this.loggedIn = null;
    }

    public User Register(string userName, string password)
    {
        User? existing = this.users.GetUserByUserName(userName);
        if (existing != null)
            {
                Console.WriteLine("Username taken");
                return null;
            }
        try
        {
            User user = new User(userName, password);
            this.users.Save(user);
            return user;
        }
        catch (FormatException exception)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Username cannot be null or empty");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty");
            }
            return null;
        }
    }

    public User? Login(string userName, string password)
    {
        try
        {
            User user = this.users.GetUserByUserNameAndPassword(userName, password);
            Console.WriteLine($"{user.UserName} successfully logged in.");
            this.loggedIn = user;
            return user;
        }
        catch (Exception exception)
        {
            Console.WriteLine("Wrong username or password.");
            return null;
        }
    }

    public void Logout(string userName)
    {
        User? user = this.users.GetUserByUserName(userName);
        Console.WriteLine($"{user.UserName} successfully logged out.");
        this.loggedIn = null;
    }
}

public class DbUserRepository : IUserRepository
{
    MongoClient dbClient;
    IMongoDatabase db;
    IMongoCollection<User> collection;

    public DbUserRepository()
    {
        this.dbClient = new MongoClient("mongodb://localhost:27017/chattprogram");
        this.db = dbClient.GetDatabase("chattprogram");
        this.collection = db.GetCollection<User>("users");
    }

    public List<User> GetAll()
    {
        var filter = Builders<User>.Filter.Empty;
        return this.collection.Find(filter).ToList();
    }

    public User? GetUserByUserNameAndPassword(string userName, string password)
    {
        try
        {
            var filter = Builders<User>.Filter.Where(u => u.UserName == userName && u.Password == password);
            return this.collection.Find(filter).First();
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Wrong username or password.");
            return null;
        }

    }

    public User? GetUserByUserName(string userName)
    {
        var filter = Builders<User>.Filter.Where(u => u.UserName == userName);
        var result = this.collection.Find(filter);
        if (result.CountDocuments() == 0)
        {
            return null;
        }
        return result.First();
    }

    public void Save(User user)
    {
        this.collection.InsertOne(user);
    }
}
