using MongoDB.Driver;
using Shared;

namespace Server;

public interface IUserService
{
    User Register(string userName, string password);
    User? Login(string userName, string password);
    void Logout();
}

public interface IUserRepository
{
    void Save (User user);
    User? GetUserByUserNameAndPassword(string userName, string password);
    List<User> GetAll();
}

public class UserService : IUserService
{
    private IUserRepository users;
    private User? loggedIn;

    public UserService(IUserRepository repository)
    {
        this.users = repository;
        this.loggedIn = null;
    }

    public User Register(string userName, string password)
    {
        User user = new User(userName, password);
        this.users.Save(user);
        return user;
    }

    public User? Login(string userName, string password)
    {
        User? user = this.users.GetUserByUserNameAndPassword(userName, password);
        if (user == null)
        {
            //todo throw exception?
            Console.WriteLine("Wrong username or password.");
            return null;
        }

        this.loggedIn = user;
        Console.WriteLine($"{user.UserName} successfully logged in.");
        return user;
    }

    public void Logout()
    {
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
        var filter = Builders<User>.Filter.Where(u => u.UserName == userName && u.Password == password);
        return this.collection.Find(filter).First();
    }

    public void Save(User user)
    {
        this.collection.InsertOne(user);
    }
}
