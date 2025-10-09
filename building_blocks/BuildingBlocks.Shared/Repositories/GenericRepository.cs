using BuildingBlocks.Shared.Common;
using BuildingBlocks.Shared.Interfaces;
using Supabase;
using System.Linq.Expressions;

namespace BuildingBlocks.Shared.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity, new()
{
    protected readonly Client _supabaseClient;
    protected readonly string _tableName;

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

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<T>()
            .Where(x => x.Id == entity.Id)
            .Update(entity, cancellationToken: cancellationToken);

        return response.Models.First();
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _supabaseClient
            .From<T>()
            .Where(x => x.Id == id)
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