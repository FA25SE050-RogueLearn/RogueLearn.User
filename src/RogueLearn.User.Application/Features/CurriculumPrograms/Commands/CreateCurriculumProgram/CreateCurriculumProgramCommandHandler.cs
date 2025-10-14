using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;

public class CreateCurriculumProgramCommandHandler : IRequestHandler<CreateCurriculumProgramCommand, CreateCurriculumProgramResponse>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;

    public CreateCurriculumProgramCommandHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
    }

    public async Task<CreateCurriculumProgramResponse> Handle(CreateCurriculumProgramCommand request, CancellationToken cancellationToken)
    {
        var program = new CurriculumProgram
        {
            ProgramName = request.ProgramName,
            ProgramCode = request.ProgramCode,
            Description = request.Description,
            DegreeLevel = request.DegreeLevel,
            TotalCredits = request.TotalCredits,
            DurationYears = request.DurationYears
        };

        var createdProgram = await _curriculumProgramRepository.AddAsync(program, cancellationToken);
        return _mapper.Map<CreateCurriculumProgramResponse>(createdProgram);
    }
}