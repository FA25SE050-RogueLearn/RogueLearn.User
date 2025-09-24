using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Events;
using RogueLearn.User.Domain.ValueObjects;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("products")]
public class Product : BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("price_amount")]
    public decimal PriceAmount { get; set; }

    [Column("price_currency")]
    public string PriceCurrency { get; set; } = "USD";

    // Domain property that wraps the database columns
    public Money Price 
    { 
        get => new Money(PriceAmount, PriceCurrency);
        set 
        { 
            PriceAmount = value.Amount;
            PriceCurrency = value.Currency;
        }
    }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Product() : base() { } // For Supabase

    public Product(string name, Money price) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        Name = name;
        Price = price ?? throw new ArgumentNullException(nameof(price));

        AddDomainEvent(new ProductCreatedEvent(Id, Name, Price.Amount));
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdatePrice(Money price)
    {
        Price = price ?? throw new ArgumentNullException(nameof(price));
        UpdateTimestamp();
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}