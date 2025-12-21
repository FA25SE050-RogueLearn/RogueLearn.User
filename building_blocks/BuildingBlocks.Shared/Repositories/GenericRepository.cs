using BuildingBlocks.Shared.Common;
using BuildingBlocks.Shared.Interfaces;
using Supabase;
using System.Linq.Expressions;
using static Supabase.Postgrest.Constants;

namespace BuildingBlocks.Shared.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity, new()
{
    protected readonly Client _supabaseClient;
    protected readonly string _tableName;

    // The Scoped Supabase client is now configured with the user's JWT upon creation
    public GenericRepository(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
        _tableName = GetTableName();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(x => x.Id == id)
            .Single(cancellationToken);



        return response;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Get(cancellationToken);



        return response.Models;
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        var response = await _supabaseClient
            .From<T>()
            .Range(offset, offset + pageSize - 1)
            .Get(cancellationToken);



        return response.Models;
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(predicate)
            .Get(cancellationToken);



        return response.Models;
    }

    public virtual async Task<IEnumerable<T>> FindPagedAsync(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        var response = await _supabaseClient
            .From<T>()
            .Where(predicate)
            .Range(offset, offset + pageSize - 1)
            .Get(cancellationToken);



        return response.Models;
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(predicate)
            .Single(cancellationToken);



        return response;
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Insert(entity, cancellationToken: cancellationToken);

        return response.Models.First();
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return Enumerable.Empty<T>();

        var response = await _supabaseClient
            .From<T>()
            .Insert(entitiesList, cancellationToken: cancellationToken);

        return response.Models;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            throw new InvalidOperationException("Cannot update an entity with an empty Id. Use AddAsync for new entities.");

        var response = await _supabaseClient
            .From<T>()
            .Where(x => x.Id == entity.Id)
            .Update(entity, cancellationToken: cancellationToken);

        if (response.Models == null || !response.Models.Any())
            throw new InvalidOperationException($"Update operation returned no results for entity with Id {entity.Id}. The entity may not exist in the database.");

        return response.Models.First();
    }

    public virtual async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();

        if (!entitiesList.Any())
            return Enumerable.Empty<T>();

        var updatedEntities = new List<T>();

        // Supabase doesn't support bulk updates with different conditions, so we batch them
        foreach (var entity in entitiesList)
        {
            if (entity.Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Cannot update entity of type {typeof(T).Name} with an empty Id. " +
                    "Use AddAsync or AddRangeAsync for new entities.");
            }

            var response = await _supabaseClient
                .From<T>()
                .Where(x => x.Id == entity.Id)
                .Update(entity, cancellationToken: cancellationToken);

            if (response.Models == null || !response.Models.Any())
            {
                throw new InvalidOperationException(
                    $"Update operation returned no results for {typeof(T).Name} with Id {entity.Id}. " +
                    "The entity may not exist in the database.");
            }

            updatedEntities.AddRange(response.Models);
        }

        return updatedEntities;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _supabaseClient
            .From<T>()
            .Where(x => x.Id == id)
            .Delete(cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (!idList.Any())
            return;

        // ARCHITECTURAL FIX: The Supabase client's LINQ provider does not support .Contains() for where clauses.
        // We must use the explicit Filter method with the "In" operator to perform a bulk delete.
        // This correctly translates to a "id=in.(guid1,guid2,...)" PostgREST query.
        await _supabaseClient
            .From<T>()
            .Filter("id", Operator.In, idList.Select(id => id.ToString()).ToList())
            .Delete(cancellationToken: cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(x => x.Id == id)
            .Get(cancellationToken);

        return response.Models.Count != 0;
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(predicate)
            .Get(cancellationToken);



        return response.Models.Count != 0;
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Get(cancellationToken);

        return response.Models.Count;
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(predicate)
            .Get(cancellationToken);



        return response.Models.Count;
    }

    protected virtual string GetTableName()
    {
        // Convert class name to snake_case for table name
        var className = typeof(T).Name;
        return string.Concat(className.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}