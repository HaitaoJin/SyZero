using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Domain.Entities;
using SyZero.Domain.Repository;

namespace SyZero.MongoDB
{
    /// <summary>
    /// 默认 long 主键的 Mongo 仓储（兼容历史用法）。
    /// </summary>
    public class MongoRepository<TEntity> : MongoRepository<TEntity, long>, IRepository<TEntity>
        where TEntity : class, IEntity
    {
        public MongoRepository(IMongoContext context) : base(context)
        {
        }
    }

    /// <summary>
    /// 通用 Mongo 仓储（自定义主键类型）。
    /// </summary>
    public class MongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        private readonly IMongoContext _Context;
        private IMongoCollection<TEntity> _collection;

        public MongoRepository(IMongoContext context)
        {
            _Context = context;
            _collection = _Context.Set<TEntity>();
        }

        #region Insert
        public TEntity Add(TEntity entity)
        {
            _collection.InsertOne(entity);
            return entity;
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            return entity;
        }

        public int AddList(IQueryable<TEntity> entities)
        {
            _collection.InsertMany(entities);
            return entities.Count();
        }

        public async Task<int> AddListAsync(IQueryable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
            return entities.Count();
        }
        #endregion

        #region Count
        public int Count(Expression<Func<TEntity, bool>> where)
        {
            return checked((int)_collection.CountDocuments(where));
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default(CancellationToken))
        {
            return checked((int)await _collection.CountDocumentsAsync(where, cancellationToken: cancellationToken));
        }
        #endregion

        #region Delete
        public long Delete(TKey id)
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
            return _collection.DeleteOne(filter).DeletedCount;
        }

        public long Delete(Expression<Func<TEntity, bool>> where)
        {
            return _collection.DeleteOne(where).DeletedCount;
        }

        public async Task<long> DeleteAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            return result.DeletedCount;
        }

        public async Task<long> DeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await _collection.DeleteOneAsync(where, cancellationToken);
            return result.DeletedCount;
        }
        #endregion

        #region Select
        public IQueryable<TEntity> GetList()
        {
            return _collection.AsQueryable();
        }

        public IQueryable<TEntity> GetList(Expression<Func<TEntity, bool>> where)
        {
            return _collection.FindSync(where).ToEnumerable().AsQueryable();
        }

        public Task<IQueryable<TEntity>> GetListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => GetList(), cancellationToken);
        }

        public async Task<IQueryable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reulst = await _collection.FindAsync(where, cancellationToken: cancellationToken);
            return reulst.ToEnumerable().AsQueryable();
        }

        public TEntity GetModel(TKey id)
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
            return _collection.FindSync(filter).FirstOrDefault();
        }

        public TEntity GetModel(Expression<Func<TEntity, bool>> where)
        {
            var result = _collection.FindSync(where);
            return result.FirstOrDefault();
        }

        public async Task<TEntity> GetModelAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, id);
            var result = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            return result.FirstOrDefault(cancellationToken);
        }

        public async Task<TEntity> GetModelAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await _collection.FindAsync(where, cancellationToken: cancellationToken);
            return result.FirstOrDefault(cancellationToken);
        }

        public IQueryable<TEntity> GetPaged(int pageIndex, int pageSize, Expression<Func<TEntity, object>> sortBy, bool isDesc = false)
        {
            SortDefinition<TEntity> sorts = new ObjectSortDefinition<TEntity>(new { });
            if (isDesc)
                sorts = sorts.Descending(sortBy);
            else
                sorts = sorts.Ascending(sortBy);
            return _collection.Find(p => true).Sort(sorts).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToEnumerable().AsQueryable();
        }

        public IQueryable<TEntity> GetPaged(int pageIndex, int pageSize, Expression<Func<TEntity, object>> sortBy, Expression<Func<TEntity, bool>> where, bool isDesc = false)
        {
            SortDefinition<TEntity> sorts = new ObjectSortDefinition<TEntity>(new { });
            if (isDesc)
                sorts = sorts.Descending(sortBy);
            else
                sorts = sorts.Ascending(sortBy);
            return _collection.Find(where).Sort(sorts).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToEnumerable().AsQueryable();
        }

        public Task<IQueryable<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, object>> sortBy, bool isDesc = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => GetPaged(pageIndex, pageSize, sortBy, isDesc), cancellationToken);
        }

        public Task<IQueryable<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, object>> sortBy, Expression<Func<TEntity, bool>> where, bool isDesc = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => GetPaged(pageIndex, pageSize, sortBy, where, isDesc), cancellationToken);
        }
        #endregion

        #region Update
        public long Update(TEntity entity)
        {
            var doc = entity.ToBsonDocument();
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            var result = _collection.UpdateOne(filter, new BsonDocumentUpdateDefinition<TEntity>(doc));
            return result.ModifiedCount;
        }

        public long Update(IQueryable<TEntity> entitys)
        {
            long reulst = 0;
            foreach (var item in entitys)
                reulst += Update(item);
            return reulst;
        }

        public async Task<long> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            var doc = entity.ToBsonDocument();
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            var result = await _collection.UpdateOneAsync(filter, new BsonDocumentUpdateDefinition<TEntity>(doc), cancellationToken: cancellationToken);
            return result.ModifiedCount;
        }

        public async Task<long> UpdateAsync(IQueryable<TEntity> entitys, CancellationToken cancellationToken = default(CancellationToken))
        {
            long reulst = 0;
            foreach (var item in entitys)
                reulst += await UpdateAsync(item, cancellationToken);
            return reulst;
        }
        #endregion
    }
}
