using MongoDB.Driver;

namespace SyZero.MongoDB
{
    public interface IMongoContext
    {
        IMongoCollection<T> Set<T>();
    }


}
