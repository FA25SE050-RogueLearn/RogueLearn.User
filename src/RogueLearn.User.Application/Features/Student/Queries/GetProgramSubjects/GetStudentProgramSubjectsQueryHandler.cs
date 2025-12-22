using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Queries.GetProgramSubjects;

public class GetStudentProgramSubjectsQueryHandler : IRequestHandler<GetStudentProgramSubjectsQuery, List<SubjectDto>>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public GetStudentProgramSubjectsQueryHandler(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<List<SubjectDto>> Handle(GetStudentProgramSubjectsQuery request, CancellationToken cancellationToken)
    {
        // Reusing the repository method that joins via curriculum_program_subjects
        var subjects = await _subjectRepository.GetSubjectsByRoute(request.ProgramId, cancellationToken);

        // Sort by semester, then code
        var sorted = subjects.OrderBy(s => s.Semester ?? 0).ThenBy(s => s.SubjectCode);

        return _mapper.Map<List<SubjectDto>>(sorted);
    }
}