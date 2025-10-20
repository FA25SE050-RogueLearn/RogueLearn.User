using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;

public class GetUserContextByAuthIdQueryHandler : IRequestHandler<GetUserContextByAuthIdQuery, UserContextDto?>
{
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GetUserContextByAuthIdQueryHandler> _logger;

    public GetUserContextByAuthIdQueryHandler(
        IUserContextService userContextService,
        ILogger<GetUserContextByAuthIdQueryHandler> logger)
    {
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<UserContextDto?> Handle(GetUserContextByAuthIdQuery request, CancellationToken cancellationToken)
    {
        var context = await _userContextService.BuildForAuthUserAsync(request.AuthId, cancellationToken);
        if (context is null)
        {
            _logger.LogWarning("No user context found for auth user id {AuthUserId}", request.AuthId);
        }
        return context;
    }
}