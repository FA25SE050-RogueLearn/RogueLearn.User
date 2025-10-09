using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;

public class CreateCurriculumVersionCommandHandler : IRequestHandler<CreateCurriculumVersionCommand, CreateCurriculumVersionResponse>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly IMapper _mapper;

    public CreateCurriculumVersionCommandHandler(ICurriculumVersionRepository curriculumVersionRepository, IMapper mapper)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _mapper = mapper;
    }

    public async Task<CreateCurriculumVersionResponse> Handle(CreateCurriculumVersionCommand request, CancellationToken cancellationToken)
    {
        var version = new CurriculumVersion
        {
            ProgramId = request.ProgramId,
            VersionCode = request.VersionCode,
            EffectiveYear = request.EffectiveYear,
            IsActive = request.IsActive,
            Description = request.Description
        };

        var created = await _curriculumVersionRepository.AddAsync(version, cancellationToken);
        return _mapper.Map<CreateCurriculumVersionResponse>(created);
    }
}