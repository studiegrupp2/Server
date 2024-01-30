using MongoDB.Driver;
using Shared;

namespace Server;

public interface IUserService
{
    User Register(string userName, string password);
    User? Login(string userName, string password);
    //void Logout();
}

public interface IUserRepository
{
    void Save (User user);
    User? GetUserByUserNameAndPassword(string userName, string password);
    List<User> GetAll();

    User? GetUserByUserName(string userName);
}

public class UserService : IUserService
{
    public IUserRepository users;

    public UserService(IUserRepository repository)
    {
        this.users = repository;
        
    }

    public User Register(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Username cannot be null or empty");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty");
        }

        User? exsisting = this.users.GetUserByUserName(userName);
        if (exsisting != null)
        {
            throw new ArgumentException("Username taken");
        }

        User user = new User(userName, password);
        this.users.Save(user);
        return user;
    }

    public User? Login(string userName, string password)
    {
        
        User? user = this.users.GetUserByUserNameAndPassword(userName, password);
        if (user == null)
        {
            //fråga william
            throw new ArgumentException("Wrong username or password.");
            //todo throw exception?
            return null;
        }
        Console.WriteLine($"{user.UserName} successfully logged in.");
        return user;
        
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
