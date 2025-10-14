using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;

public class GetCurriculumProgramByIdQueryHandler : IRequestHandler<GetCurriculumProgramByIdQuery, CurriculumProgramDto>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;

    public GetCurriculumProgramByIdQueryHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
    }

    public async Task<CurriculumProgramDto> Handle(GetCurriculumProgramByIdQuery request, CancellationToken cancellationToken)
    {
        var program = await _curriculumProgramRepository.GetByIdAsync(request.Id, cancellationToken);
        if (program == null)
        {
            throw new NotFoundException("CurriculumProgram", request.Id);
        }

        return _mapper.Map<CurriculumProgramDto>(program);
    }
}