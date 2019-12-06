using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using core.Infrastructure;
using MongoDB.Driver;

namespace core.Storage
{
    public class MongoDataStore
    {
        private readonly IMongoDatabase _database;

        public MongoDataStore(DataStoreConfiguration configuration)
        {
            var client = new MongoClient(configuration.ConnectionString);

            _database = client.GetDatabase(configuration.DatabaseName);
        }

        public async Task Add<T>(T entity)
            where T : class
        {
            var collection = _database.GetCollection<T>(typeof(T).CollectionName());

            await collection.InsertOneAsync(entity);
        }

        public async Task<IEnumerable<TEntity>> GetAll<TEntity>()
                where TEntity : class
        {
            var collection = _database.GetCollection<TEntity>(typeof(TEntity).CollectionName());

            var entries = await collection.FindAsync(FilterDefinition<TEntity>.Empty);

            return entries.ToEnumerable();
        }

        public async Task<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> filter)
            where TEntity : class
        {
            var collection = _database.GetCollection<TEntity>(typeof(TEntity).CollectionName());

            var entries = await collection.FindAsync(filter);

            return await entries.FirstOrDefaultAsync();
        }

        public async Task Update<TEntity>(
            Expression<Func<TEntity, bool>> filter,
            UpdateDefinition<TEntity> updateDefinition)
            where TEntity : class
        {
            var collection = _database.GetCollection<TEntity>(typeof(TEntity).CollectionName());

            await collection.FindOneAndUpdateAsync(filter, updateDefinition);
        }
    }
}