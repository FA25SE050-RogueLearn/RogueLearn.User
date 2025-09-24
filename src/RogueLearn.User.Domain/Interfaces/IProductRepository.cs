using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    // Add any Product-specific repository methods here if needed
    Task<IEnumerable<Product>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);
}