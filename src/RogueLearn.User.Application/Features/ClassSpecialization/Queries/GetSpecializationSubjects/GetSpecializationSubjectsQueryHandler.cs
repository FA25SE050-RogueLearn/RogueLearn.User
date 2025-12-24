using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

public class GetSpecializationSubjectsQueryHandler : IRequestHandler<GetSpecializationSubjectsQuery, List<SpecializationSubjectDto>>
{
    private readonly IClassSpecializationSubjectRepository _repository;
    private readonly IMapper _mapper;

    public GetSpecializationSubjectsQueryHandler(IClassSpecializationSubjectRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<SpecializationSubjectDto>> Handle(GetSpecializationSubjectsQuery request, CancellationToken cancellationToken)
    {
        // Use the specialized method that joins and returns Subject entities directly
        var subjects = await _repository.GetSubjectByClassIdAsync(request.ClassId, cancellationToken);

        // Map Subject entities to SpecializationSubjectDto
        // We do this manually or via mapper configuration update. 
        // For simplicity and clarity here, manual projection ensures correct mapping.
        return subjects.Select(s => new SpecializationSubjectDto
        {
            Id = s.Id, // Subject ID
            SubjectId = s.Id,
            ClassId = request.ClassId, // Contextual ID
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName,
            Semester = s.Semester
        }).OrderBy(s => s.Semester).ThenBy(s => s.SubjectCode).ToList();
    }
}