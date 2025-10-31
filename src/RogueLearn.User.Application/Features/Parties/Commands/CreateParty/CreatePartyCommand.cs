using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.CreateParty;

public record CreatePartyCommand : IRequest<CreatePartyResponse>
{
    public Guid CreatorAuthUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsPublic { get; init; } = true;
    public int MaxMembers { get; init; } = 6;
}

public record CreatePartyResponse
{
    public Guid PartyId { get; init; }
    public string RoleGranted { get; init; } = "PartyLeader";
}