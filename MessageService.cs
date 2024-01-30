using MongoDB.Driver;
using Shared;

namespace Server;

public interface IMessageService
{
    Message Create(string sender, string receiver, string content);
}

public class MessageService : IMessageService
{
    public IMessageRepository messages;
    private Message? created;

    public MessageService(IMessageRepository repository)
    {
        this.messages = repository;
    }

    public Message Create(string sender, string receiver, string content)
    {
        Message message = new Message(sender, receiver, content);
        this.messages.Save(message);
        return message;
    }
}
public interface IMessageRepository
{
    
    void Save (Message message);
    List<Message> GetAll();

}

public class DbMessageRepository : IMessageRepository
{
    MongoClient dbClient;
    IMongoDatabase db;
    IMongoCollection<Message> collection;

    
    public DbMessageRepository()
    {
        this.dbClient = new MongoClient("mongodb://localhost:27017/chattprogram");
        this.db = dbClient.GetDatabase("chattprogram");
        this.collection = db.GetCollection<Message>("messages");
    }
    
    public void Save(Message message)
    {
        this.collection.InsertOne(message);
    }

    public List<Message> GetAll()
    {
       var sort = Builders<Message>.Sort.Descending("timestamp");
       var filter = Builders<Message>.Filter.Empty;
        var latest30Messages = collection.Find(filter).Sort(sort).Limit(30).ToList();
        latest30Messages.Reverse();
        return latest30Messages;
    //    return this.collection.Find(filter).Sort(sort).Limit(30).ToList(); 
    }
    
}