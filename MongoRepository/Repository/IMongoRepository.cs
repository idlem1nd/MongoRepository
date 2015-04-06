using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoRepository
{
    public interface IMongoRepository<T, TKey>
       where T : IEntity<TKey>
    {
        Task AddAsync(IEnumerable<T> entities);
        Task<T> AddAsync(T entity);
        IMongoCollection<T> Collection { get; }
        Task<long> CountAsync();
        Task DeleteAllAsync();
        Task DeleteAsync(ObjectId id);
        Task DeleteAsync(Expression<Func<T, bool>> predicate);
        Task DeleteAsync(T entity);
        Task DeleteAsync(TKey id);
        Task<T> GetByIdAsync(ObjectId id);
        Task<T> GetByIdAsync(TKey id);
        Task<IAsyncCursor<T>> FindAsync(Expression<Func<T, bool>> filter);
        Task UpdateAsync(IEnumerable<T> entities);
        Task<T> UpdateAsync(T entity);
        Task<List<T>> ToListAsync();

        [Obsolete("Use the new fluent API on IMongoCollection instead")]
        IEnumerable<BsonDocument> Aggregate(AggregateArgs args);    
    }

    public interface IMongoRepository<T> : IMongoRepository<T, string>
        where T : IEntity<string> { }
}
