using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class GetAllSubjectsHandler : IRequestHandler<GetAllSubjectsQuery, List<SubjectDto>>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public GetAllSubjectsHandler(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<List<SubjectDto>> Handle(GetAllSubjectsQuery request, CancellationToken cancellationToken)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<SubjectDto>>(subjects);
    }
}