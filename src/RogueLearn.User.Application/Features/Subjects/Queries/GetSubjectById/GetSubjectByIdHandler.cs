using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdHandler : IRequestHandler<GetSubjectByIdQuery, SubjectDto?>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public GetSubjectByIdHandler(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<SubjectDto?> Handle(GetSubjectByIdQuery request, CancellationToken cancellationToken)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        return subject == null ? null : _mapper.Map<SubjectDto>(subject);
    }
}