using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;

public class GetSubjectsByProgramQueryHandler : IRequestHandler<GetSubjectsByProgramQuery, List<SubjectDto>>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubjectsByProgramQueryHandler> _logger;

    public GetSubjectsByProgramQueryHandler(
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<GetSubjectsByProgramQueryHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<SubjectDto>> Handle(GetSubjectsByProgramQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving subjects for ProgramId={ProgramId}", request.ProgramId);

        // We leverage the existing repository method that does exactly this join
        var subjects = await _subjectRepository.GetSubjectsByRoute(request.ProgramId, cancellationToken);

        // Sort for better UI display (Semester first, then Code)
        var sortedSubjects = subjects
            .OrderBy(s => s.Semester ?? 0)
            .ThenBy(s => s.SubjectCode)
            .ToList();

        return _mapper.Map<List<SubjectDto>>(sortedSubjects);
    }
}