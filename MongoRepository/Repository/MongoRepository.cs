namespace MongoRepository
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Linq;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public class MongoRepository<T, TKey> : IMongoRepository<T,TKey>
        where T : IEntity<TKey>
    {
        /// <summary>
        /// IMongoCollection field.
        /// </summary>
        protected internal IMongoCollection<T> collection;

        /// <summary>
        /// Collection for the legacy driver.
        /// </summary>
        protected internal MongoCollection<T> legacyCollection;

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        public MongoRepository()
            : this(Util<TKey>.GetDefaultConnectionString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
        {
            this.collection = Util<TKey>.GetCollectionFromConnectionString<T>(connectionString);
            this.legacyCollection = LegacyUtil<TKey>.GetCollectionFromConnectionString<T>(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
        {
            this.collection = Util<TKey>.GetCollectionFromConnectionString<T>(connectionString, collectionName);
            this.legacyCollection = LegacyUtil<TKey>.GetCollectionFromConnectionString<T>(connectionString, collectionName);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
        {
            this.collection = Util<TKey>.GetCollectionFromUrl<T>(url);
            this.legacyCollection = LegacyUtil<TKey>.GetCollectionFromUrl<T>(url);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
        {
            this.collection = Util<TKey>.GetCollectionFromUrl<T>(url, collectionName);
            this.legacyCollection = LegacyUtil<TKey>.GetCollectionFromUrl<T>(url);
        }

        /// <summary>
        /// Gets the Mongo collection (to perform advanced operations).
        /// </summary>
        /// <remarks>
        /// One can argue that exposing this property (and with that, access to it's Database property for instance
        /// (which is a "parent")) is not the responsibility of this class. Use of this property is highly discouraged;
        /// for most purposes you can use the MongoRepositoryManager&lt;T&gt;
        /// </remarks>
        /// <value>The Mongo collection (to perform advanced operations).</value>
        public IMongoCollection<T> Collection
        {
            get { return this.collection; }
        }

        /// <summary>
        /// Allows use of the legacy aggregate API where necessary.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("Use the new fluent API on IMongoCollection instead")]
        public virtual IEnumerable<BsonDocument> Aggregate(AggregateArgs args)
        {
            return this.legacyCollection.Aggregate(args);
        }

        /// <summary>
        /// Returns the T by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <returns>The Entity T.</returns>
        public virtual async Task<T> GetById(TKey id)
        {
            // TODO: I think this will always work, but the original did something weirder...
            var result = await this.collection.Find<T>(t => t.Id.Equals(id)).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Returns the T by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <returns>The Entity T.</returns>
        public virtual async Task<T> GetById(ObjectId id)
        {
            var key = id.ToString();
            var result = await this.collection.Find<T>(t => t.Id.Equals(key)).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity T.</param>
        /// <returns>The added entity including its new ObjectId.</returns>
        public virtual async Task<T> AddAsync(T entity)
        {
            await this.collection.InsertOneAsync(entity);
            return entity;
        }

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        public virtual async Task AddAsync(IEnumerable<T> entities)
        {
            await this.collection.InsertManyAsync(entities);
        }

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        public virtual async Task<IAsyncCursor<T>> FindAsync(Expression<Func<T, bool>> filter)
        {
            return await this.collection.FindAsync(filter);
        }

        public virtual async Task<List<T>> ToListAsync()
        {
            return await this.collection.Find(t => true).ToListAsync();
        }

        /// <summary>
        /// Upserts an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The updated entity.</returns>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            var options = new UpdateOptions { IsUpsert = true };
            await this.collection.ReplaceOneAsync<T>((T) => T.Id.Equals(entity.Id), entity, options);
            return entity;
        }

        /// <summary>
        /// Upserts the entities.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        public virtual async Task UpdateAsync(IEnumerable<T> entities)
        {
            // TODO: There may be a better way to batch upsert - we'll find out when the 
            // 2.0 docs are more up to date. For now this has the same behaviour and performance
            // as the previous version which looped over calling Save() synchronously.
            // There should be some benefit to yeilding the thread for the async operations.
            await Task.WhenAll(entities.Select(t => UpdateAsync(t)));
        }

        /// <summary>
        /// Deletes an entity from the repository by its id.
        /// </summary>
        /// <param name="id">The entity's id.</param>
        public virtual async Task DeleteAsync(TKey id)
        {
            await this.DoDeleteAsync(id);
        }

        protected virtual async Task<DeleteResult> DoDeleteAsync(TKey id)
        {
            if (typeof(T).IsSubclassOf(typeof(Entity)))
            {
                var objId = new ObjectId(id as string);
                return await this.collection.DeleteOneAsync((T) => T.Id.Equals(objId));
            }
            else
            {
                var objId = BsonValue.Create(id);
                return await this.collection.DeleteOneAsync((T) => T.Id.Equals(objId));
            }
        }

        /// <summary>
        /// Deletes an entity from the repository by its ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the entity.</param>
        public virtual async Task DeleteAsync(ObjectId id)
        {
            await this.collection.DeleteOneAsync((T) => T.Id.Equals(id));
        }

        /// <summary>
        /// Deletes the given entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public virtual async Task DeleteAsync(T entity)
        {
            await this.DoDeleteAsync(entity.Id);
        }

        /// <summary>
        /// Deletes the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        public virtual async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            await this.collection.DeleteManyAsync(predicate);
        }

        /// <summary>
        /// Deletes all entities in the repository.
        /// </summary>
        public virtual async Task DeleteAllAsync()
        {
            await this.collection.DeleteManyAsync((T) => true);
        }

        /// <summary>
        /// Counts the total entities in the repository.
        /// </summary>
        /// <returns>Count of entities in the collection.</returns>
        public virtual async Task<long> CountAsync()
        {
            return await this.collection.CountAsync((T) => true);
        }
    }

    /// <summary>
    /// Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public class MongoRepository<T> : MongoRepository<T, string>
        where T : IEntity<string>
    {
        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        public MongoRepository()
            : base() { }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
            : base(url) { }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
            : base(url, collectionName) { }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
            : base(connectionString) { }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName) { }
    }
}