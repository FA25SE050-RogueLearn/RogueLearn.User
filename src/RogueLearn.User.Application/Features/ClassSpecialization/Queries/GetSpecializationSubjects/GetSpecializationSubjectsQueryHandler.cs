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
        var mappings = await _repository.FindAsync(m => m.ClassId == request.ClassId, cancellationToken);
        return _mapper.Map<List<SpecializationSubjectDto>>(mappings);
    }
}