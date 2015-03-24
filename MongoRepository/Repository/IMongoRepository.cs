using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MongoRepository
{
    interface IMongoRepository<T, TKey>
     where T : IEntity<TKey>
    {
        void AddAsync(global::System.Collections.Generic.IEnumerable<T> entities);
        Task<T> AddAsync(T entity);
        IMongoCollection<T> Collection { get; }
        Task<long> CountAsync();
        void DeleteAllAsync();
        void DeleteAsync(global::MongoDB.Bson.ObjectId id);
        void DeleteAsync(global::System.Linq.Expressions.Expression<Func<T, bool>> predicate);
        void DeleteAsync(T entity);
        void DeleteAsync(TKey id);
        Task<T> GetById(global::MongoDB.Bson.ObjectId id);
        Task<T> GetById(TKey id);
        void Update(global::System.Collections.Generic.IEnumerable<T> entities);
        Task<T> Update(T entity);
    }
}
