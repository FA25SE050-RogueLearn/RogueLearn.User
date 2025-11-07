// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/EstablishSkillDependencies/EstablishSkillDependenciesCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
// MODIFIED: Removed dependencies on AI plugin and repositories that are no longer needed for this user-facing command.

namespace RogueLearn.User.Application.Features.Student.Commands.EstablishSkillDependencies;

public class EstablishSkillDependenciesCommandHandler : IRequestHandler<EstablishSkillDependenciesCommand, EstablishSkillDependenciesResponse>
{
    private readonly ILogger<EstablishSkillDependenciesCommandHandler> _logger;

    public EstablishSkillDependenciesCommandHandler(ILogger<EstablishSkillDependenciesCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<EstablishSkillDependenciesResponse> Handle(EstablishSkillDependenciesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "EstablishSkillDependencies command received for User {AuthUserId}. " +
            "This process is now part of the admin-curated master data setup and no longer runs per-user. " +
            "Returning a successful response.",
            request.AuthUserId);

        // MODIFIED: The logic has been removed. This command now succeeds immediately.
        // The skill dependency graph is now considered master data, managed by administrators,
        // and is no longer generated on a per-user basis.
        var response = new EstablishSkillDependenciesResponse
        {
            IsSuccess = true,
            Message = "Skill dependencies are pre-configured by administrators and are ready to use.",
            TotalDependenciesCreated = 0, // No dependencies are created here.
            TotalDependenciesSkipped = 0
        };

        return Task.FromResult(response);
    }
}