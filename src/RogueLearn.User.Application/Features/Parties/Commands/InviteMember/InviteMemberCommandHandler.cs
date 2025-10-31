using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, PartyInvitationDto>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyNotificationService _notificationService;
    private readonly IMapper _mapper;

    public InviteMemberCommandHandler(
        IPartyInvitationRepository invitationRepository, 
        IPartyNotificationService notificationService,
        IMapper mapper)
    {
        _invitationRepository = invitationRepository;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<PartyInvitationDto> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var invitation = new PartyInvitation
        {
            PartyId = request.PartyId,
            InviterId = request.InviterAuthUserId,
            InviteeId = request.InviteeAuthUserId,
            Message = request.Message,
            Status = InvitationStatus.Pending,
            ExpiresAt = request.ExpiresAt,
            InvitedAt = DateTimeOffset.UtcNow
        };

        invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);

        // Send notification
        await _notificationService.SendInvitationNotificationAsync(invitation, cancellationToken);

        return _mapper.Map<PartyInvitationDto>(invitation);
    }
}