using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;

/// <summary>
/// Admin command to iterate all Subjects and ensure a Master Quest exists for each.
/// This populates the "Quest Pool" that users will link to.
/// </summary>
public record EnsureMasterQuestsCommand : IRequest<EnsureMasterQuestsResponse>;

public record EnsureMasterQuestsResponse(int CreatedCount, int ExistingCount);