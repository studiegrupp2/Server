using MongoDB.Driver;
using Shared;

namespace Server;

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
       var filter = Builders<Message>.Filter.Empty;
       return this.collection.Find(filter).ToList();
    }
    
}