using System.Text.Json;
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;

public class AddPartyResourceCommandHandler : IRequestHandler<AddPartyResourceCommand, PartyStashItemDto>
{
    private readonly IPartyStashItemRepository _stashItemRepository;
    private readonly IPartyNotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IPartyMemberRepository _memberRepository;

    public AddPartyResourceCommandHandler(
        IPartyStashItemRepository stashItemRepository, 
        IPartyNotificationService notificationService,
        IMapper mapper,
        IPartyMemberRepository memberRepository)
    {
        _stashItemRepository = stashItemRepository;
        _notificationService = notificationService;
        _mapper = mapper;
        _memberRepository = memberRepository;
    }

    public async Task<PartyStashItemDto> Handle(AddPartyResourceCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetMemberAsync(request.PartyId, request.SharedByUserId, cancellationToken);
        if (member is null || member.Status != MemberStatus.Active)
        {
            throw new Application.Exceptions.ForbiddenException("Actor is not an active party member");
        }

        if (member.Role != PartyRole.Leader)
        {
            throw new Application.Exceptions.ForbiddenException("Only party leaders may add resources");
        }

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
