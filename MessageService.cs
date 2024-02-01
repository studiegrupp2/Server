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
    void Save(Message message);
    List<Message> GetAll(string receiver);
    public List<Message> GetAllPublic();
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

    public List<Message> GetAllPublic()
    {
        var filterPrivate = Builders<Message>.Filter.Eq(message => message.Receiver, "All");
        var sort = Builders<Message>.Sort.Descending("timestamp");
        var latest30Messages = collection.Find(filterPrivate).Sort(sort).Limit(30).ToList();
        return latest30Messages;
    }

    public List<Message> GetAll(string receiver)
    {
        var allMessages = this.GetAllPublic();
        var filterPrivate = Builders<Message>.Filter.Eq(message => message.Receiver, receiver);
        var sort = Builders<Message>.Sort.Descending("timestamp");
        var filter = Builders<Message>.Filter.Empty;
        var latest30PrivateMessages = collection.Find(filterPrivate).Sort(sort).Limit(30).ToList();
        latest30PrivateMessages.Reverse();

        int index = latest30PrivateMessages.Count();

        for (int i = index; i > 0; i--)
        {
            allMessages.RemoveAt((allMessages.Count()) - i);
        }

        int allMessageCount = allMessages.Count();

        if (index <= 0)
        {
            allMessages.Reverse();
            return allMessages;
        }
        else
        {
            allMessages.Reverse();
            for (int i = index; i > 0; i--) //range i = -1 
            {
                allMessages.Add(latest30PrivateMessages[i - 1]);
            }
            return allMessages;
        }
    }
}