// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Queries/GetClassSubjects/GetStudentClassSubjectsQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Queries.GetClassSubjects;

public class GetStudentClassSubjectsQueryHandler : IRequestHandler<GetStudentClassSubjectsQuery, List<SubjectDto>>
{
    private readonly IClassSpecializationSubjectRepository _specializationRepo;
    private readonly IMapper _mapper;

    public GetStudentClassSubjectsQueryHandler(IClassSpecializationSubjectRepository specializationRepo, IMapper mapper)
    {
        _specializationRepo = specializationRepo;
        _mapper = mapper;
    }

    public async Task<List<SubjectDto>> Handle(GetStudentClassSubjectsQuery request, CancellationToken cancellationToken)
    {
        // Use the specialized repository method that fetches Subjects linked to a Class
        var subjects = await _specializationRepo.GetSubjectByClassIdAsync(request.ClassId, cancellationToken);

        // Sort by semester, then code
        var sorted = subjects.OrderBy(s => s.Semester ?? 0).ThenBy(s => s.SubjectCode);

        return _mapper.Map<List<SubjectDto>>(sorted);
    }
}