using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;

public record RevokePartyRoleCommand(Guid PartyId, Guid MemberAuthUserId, PartyRole RoleToRevoke, Guid ActorAuthUserId, bool IsAdminOverride = false) : IRequest<Unit>;