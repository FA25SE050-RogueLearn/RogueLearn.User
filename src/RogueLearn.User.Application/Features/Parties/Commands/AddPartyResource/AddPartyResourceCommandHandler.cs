using System.Text.Json;
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;

public class AddPartyResourceCommandHandler : IRequestHandler<AddPartyResourceCommand, PartyStashItemDto>
{
    private readonly IPartyStashItemRepository _stashItemRepository;
    private readonly IPartyNotificationService _notificationService;
    private readonly IMapper _mapper;

    public AddPartyResourceCommandHandler(
        IPartyStashItemRepository stashItemRepository, 
        IPartyNotificationService notificationService,
        IMapper mapper)
    {
        _stashItemRepository = stashItemRepository;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<PartyStashItemDto> Handle(AddPartyResourceCommand request, CancellationToken cancellationToken)
    {
        var stashItem = new PartyStashItem
        {
            PartyId = request.PartyId,
            OriginalNoteId = request.OriginalNoteId,
            SharedByUserId = request.SharedByUserId,
            Title = request.Title,
            // Accept JSON data from frontend as-is; repository/DB will handle JSONB serialization
            Content = request.Content,
            Tags = request.Tags.ToArray(),
            SharedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _stashItemRepository.AddAsync(stashItem, cancellationToken);

        return _mapper.Map<PartyStashItemDto>(stashItem);
    }
}