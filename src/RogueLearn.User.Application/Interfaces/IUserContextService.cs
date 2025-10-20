using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Interfaces;

public interface IUserContextService
{
    Task<UserContextDto?> BuildForAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}