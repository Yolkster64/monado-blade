namespace MonadoBlade.Core.Services;

/// <summary>
/// Implementation of IMutationService providing write operations with cache invalidation.
/// Segregation Pattern: Focused on mutations with automatic cache management.
/// </summary>
public class MutationService : ServiceBase, IMutationService
{
    private readonly IDataAccessLayer _dataAccessLayer;

    public MutationService(ILogger logger, IDataAccessLayer dataAccessLayer) : base(logger)
    {
        _dataAccessLayer = dataAccessLayer ?? throw new ArgumentNullException(nameof(dataAccessLayer));
    }

    public async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Creating new entity of type {EntityType}", typeof(T).Name);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            dbSet.Add(entity);
            await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
            InvalidateCachePattern(typeof(T).Name);
            return entity;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to create entity of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"Create{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<T> UpdateAsync<T>(string id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Updating entity of type {EntityType} with ID {EntityId}", typeof(T).Name, id);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            dbSet.Update(entity);
            await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
            InvalidateCache($"entity:{typeof(T).Name}:{id}");
            return entity;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to update entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
            throw new OperationFailedException($"Update{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Deleting entity of type {EntityType} with ID {EntityId}", typeof(T).Name, id);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var entity = await dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
            if (entity != null)
            {
                dbSet.Remove(entity);
                await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
                InvalidateCache($"entity:{typeof(T).Name}:{id}");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to delete entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
            throw new OperationFailedException($"Delete{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<List<T>> CreateBatchAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Creating batch of {Count} entities of type {EntityType}", entities.Count(), typeof(T).Name);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var entityList = entities.ToList();
            dbSet.AddRange(entityList);
            await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
            InvalidateCachePattern(typeof(T).Name);
            return entityList;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to create batch of entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"CreateBatch{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<List<T>> UpdateBatchAsync<T>(Dictionary<string, T> entities, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Updating batch of {Count} entities of type {EntityType}", entities.Count, typeof(T).Name);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            dbSet.UpdateRange(entities.Values);
            await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
            InvalidateCachePattern(typeof(T).Name);
            return entities.Values.ToList();
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to update batch of entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"UpdateBatch{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task DeleteBatchAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Deleting batch of {Count} entities of type {EntityType}", ids.Count(), typeof(T).Name);
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var idList = ids.ToList();

            foreach (var id in idList)
            {
                var entity = await dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
                if (entity != null)
                    dbSet.Remove(entity);
            }

            await _dataAccessLayer.DbContext.SaveChangesAsync(cancellationToken);
            InvalidateCachePattern(typeof(T).Name);
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to delete batch of entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"DeleteBatch{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<ICollection<string>> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var errors = new List<string>();
        // Placeholder for validation logic
        return await Task.FromResult(errors);
    }

    public async Task<bool> ExecuteInTransactionAsync(Func<ITransaction, Task> operationFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            LogInfo("Starting transaction");
            var transaction = await _dataAccessLayer.BeginTransactionAsync(cancellationToken: cancellationToken);
            await using (transaction)
            {
                await operationFactory(transaction);
                await transaction.CommitAsync();
                LogInfo("Transaction committed successfully");
                return true;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Transaction failed");
            throw new OperationFailedException("ExecuteInTransaction", innerException: ex);
        }
    }

    public async Task InvalidateCacheAsync<T>(string? specificKey = null) where T : class
    {
        LogInfo("Invalidating cache for type {EntityType}", typeof(T).Name);
        if (specificKey != null)
            InvalidateCache(specificKey);
        else
            InvalidateCachePattern(typeof(T).Name);
        await Task.CompletedTask;
    }

    public async Task SoftDeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Soft deleting entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
        // Placeholder for soft delete logic
        await Task.CompletedTask;
    }

    public async Task<T> RestoreAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        LogInfo("Restoring entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
        // Placeholder for restore logic
        throw new NotImplementedException("Restore logic not yet implemented");
    }
}
