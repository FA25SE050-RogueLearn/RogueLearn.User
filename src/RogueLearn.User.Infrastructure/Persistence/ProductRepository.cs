using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Product>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<Product>()
            .Where(p => p.Name.Contains(name))
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<Product>()
            .Where(p => p.PriceAmount >= minPrice && p.PriceAmount <= maxPrice)
            .Get(cancellationToken);

        return response.Models;
    }
}