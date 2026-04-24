namespace MonadoBlade.Core.Services;

/// <summary>
/// Implementation of IQueryService providing read-only operations with caching.
/// Segregation Pattern: Focused on efficient data retrieval without side effects.
/// </summary>
public class QueryService : ServiceBase, IQueryService
{
    private readonly IDataAccessLayer _dataAccessLayer;

    public QueryService(ILogger logger, IDataAccessLayer dataAccessLayer) : base(logger)
    {
        _dataAccessLayer = dataAccessLayer ?? throw new ArgumentNullException(nameof(dataAccessLayer));
    }

    public async Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Retrieving entity of type {EntityType} with ID {EntityId}", typeof(T).Name, id);

            var cacheKey = $"entity:{typeof(T).Name}:{id}";
            return await GetCachedOrComputeAsync(cacheKey, async () =>
            {
                var dbSet = _dataAccessLayer.DbContext.Set<T>();
                return await dbSet.FindAsync(new object[] { id }, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to retrieve entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
            throw new OperationFailedException($"Get{typeof(T).Name}ById", innerException: ex);
        }
    }

    public async Task<List<T>> QueryAsync<T>(
        IQueryable<T> query,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Executing query for entity type {EntityType}", typeof(T).Name);

            if (cacheKey != null)
            {
                return await GetCachedOrComputeAsync(cacheKey, async () =>
                {
                    return await query.ToListAsync(cancellationToken);
                }, cacheDuration) ?? new List<T>();
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to execute query for entity type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"Query{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<PagedResult<T>> PageAsync<T>(
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Executing paginated query for entity type {EntityType}, page {Page}, pageSize {PageSize}",
                typeof(T).Name, page, pageSize);

            if (page < 1)
                throw new ArgumentException("Page number must be >= 1", nameof(page));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var query = dbSet.AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to execute paginated query for entity type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"Page{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<List<SearchResult<T>>> SearchAsync<T>(
        string searchText,
        string[] searchFields,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Executing search for entity type {EntityType} with text '{SearchText}'", typeof(T).Name, searchText);

            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var results = new List<SearchResult<T>>();

            // Simple search implementation - in production, consider using full-text search
            var items = await dbSet.ToListAsync(cancellationToken);

            // Note: This is a simplified implementation. For production, use EF.Functions.Like or full-text search
            foreach (var item in items)
            {
                var matchedFields = new List<string>();
                float relevanceScore = 0;

                foreach (var field in searchFields)
                {
                    var property = typeof(T).GetProperty(field);
                    if (property != null)
                    {
                        var value = property.GetValue(item)?.ToString() ?? "";
                        if (value.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedFields.Add(field);
                            relevanceScore += 1.0f;
                        }
                    }
                }

                if (matchedFields.Count > 0)
                {
                    results.Add(new SearchResult<T>
                    {
                        Entity = item,
                        RelevanceScore = Math.Min(1.0f, relevanceScore / searchFields.Length),
                        MatchedFields = matchedFields
                    });
                }
            }

            return results.OrderByDescending(r => r.RelevanceScore).ToList();
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to search entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"Search{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Executing count query for entity type {EntityType}", typeof(T).Name);
            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to count entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"Count{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<bool> ExistsAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var entity = await dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
            return entity != null;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to check existence of entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
            throw new OperationFailedException($"Exists{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task<List<T>> GetByIdsAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            LogInfo("Retrieving multiple entities of type {EntityType}", typeof(T).Name);

            var idList = ids.ToList();
            var dbSet = _dataAccessLayer.DbContext.Set<T>();
            var results = new List<T>();

            foreach (var id in idList)
            {
                var entity = await dbSet.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
                if (entity != null)
                    results.Add(entity);
            }

            return results;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to retrieve multiple entities of type {EntityType}", typeof(T).Name);
            throw new OperationFailedException($"GetByIds{typeof(T).Name}", innerException: ex);
        }
    }

    public async Task ClearCacheAsync()
    {
        LogInfo("Clearing query cache");
        ClearCache();
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        return await Task.FromResult(new Dictionary<string, object>
        {
            { "CachedItemCount", _cache.Count },
            { "CacheEnabled", true }
        });
    }
}
