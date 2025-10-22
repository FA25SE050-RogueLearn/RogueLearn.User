using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkills;

/// <summary>
/// Handles retrieval of all Skills.
/// Emits structured logs for observability and returns a response DTO containing the list of skills.
/// </summary>
public sealed class GetSkillsQueryHandler : IRequestHandler<GetSkillsQuery, GetSkillsResponse>
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<GetSkillsQueryHandler> _logger;

    public GetSkillsQueryHandler(ISkillRepository skillRepository, ILogger<GetSkillsQueryHandler> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all skills and maps them into DTOs.
    /// </summary>
    public async Task<GetSkillsResponse> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetSkillsQuery");

        var all = await _skillRepository.GetAllAsync(cancellationToken);
        var skills = all.Select(s => new SkillDto
        {
            Id = s.Id,
            Name = s.Name,
            Domain = s.Domain,
            Tier = s.Tier,
            Description = s.Description
        }).ToList();

        _logger.LogInformation("Retrieved {Count} skills", skills.Count);
        return new GetSkillsResponse { Skills = skills };
    }
}