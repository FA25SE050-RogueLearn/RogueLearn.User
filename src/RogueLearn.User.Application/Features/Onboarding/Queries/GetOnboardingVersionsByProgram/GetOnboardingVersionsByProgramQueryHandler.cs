// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetOnboardingVersionsByProgram/GetOnboardingVersionsByProgramQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetOnboardingVersionsByProgram;

public class GetOnboardingVersionsByProgramQueryHandler : IRequestHandler<GetOnboardingVersionsByProgramQuery, List<OnboardingVersionDto>>
{
    private readonly ICurriculumVersionRepository _versionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOnboardingVersionsByProgramQueryHandler> _logger;

    public GetOnboardingVersionsByProgramQueryHandler(
        ICurriculumVersionRepository versionRepository,
        IMapper mapper,
        ILogger<GetOnboardingVersionsByProgramQueryHandler> logger)
    {
        _versionRepository = versionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<OnboardingVersionDto>> Handle(GetOnboardingVersionsByProgramQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching active curriculum versions for onboarding for ProgramId {ProgramId}", request.ProgramId);

        // MODIFICATION: Changed implicit boolean check to an explicit one to prevent a NullReferenceException in the Supabase LINQ provider.
        // BEFORE: .FindAsync(v => v.ProgramId == request.ProgramId && v.IsActive, cancellationToken);
        // The LINQ translator struggles with implicit booleans (&& v.IsActive).
        var versions = await _versionRepository.FindAsync(
            v => v.ProgramId == request.ProgramId && v.IsActive == true,
            cancellationToken);

        var orderedVersions = versions.OrderByDescending(v => v.EffectiveYear).ThenByDescending(v => v.CreatedAt).ToList();

        _logger.LogInformation("Found {Count} active versions for ProgramId {ProgramId}", orderedVersions.Count, request.ProgramId);

        return _mapper.Map<List<OnboardingVersionDto>>(orderedVersions);
    }
}