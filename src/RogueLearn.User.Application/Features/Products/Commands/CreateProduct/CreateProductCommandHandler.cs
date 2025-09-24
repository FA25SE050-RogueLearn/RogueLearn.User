using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.ValueObjects;

namespace RogueLearn.User.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly IMessageBus _messageBus;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IMapper mapper,
        IMessageBus messageBus)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _messageBus = messageBus;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var money = new Money(request.Price, request.Currency);
        var product = new Product(request.Name, money);

        await _productRepository.AddAsync(product, cancellationToken);

        // Publish integration event (will be implemented in infrastructure)
        // var integrationEvent = new ProductCreatedIntegrationEvent(product.Id, product.Name);
        // await _messageBus.PublishAsync(integrationEvent, cancellationToken);

        return product.Id;
    }
}