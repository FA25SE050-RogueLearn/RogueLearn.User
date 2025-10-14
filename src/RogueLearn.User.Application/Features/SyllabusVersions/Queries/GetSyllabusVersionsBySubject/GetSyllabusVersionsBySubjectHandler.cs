using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;

public class GetSyllabusVersionsBySubjectHandler : IRequestHandler<GetSyllabusVersionsBySubjectQuery, List<SyllabusVersionDto>>
{
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;

    public GetSyllabusVersionsBySubjectHandler(
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper)
    {
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
    }

    public async Task<List<SyllabusVersionDto>> Handle(GetSyllabusVersionsBySubjectQuery request, CancellationToken cancellationToken)
    {
        var syllabusVersions = await _syllabusVersionRepository.FindAsync(
            sv => sv.SubjectId == request.SubjectId, 
            cancellationToken);
        
        return _mapper.Map<List<SyllabusVersionDto>>(syllabusVersions.OrderByDescending(sv => sv.VersionNumber).ToList());
    }
}