using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;

public class UpdateCurriculumProgramCommandHandler : IRequestHandler<UpdateCurriculumProgramCommand, UpdateCurriculumProgramResponse>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IMapper _mapper;

    public UpdateCurriculumProgramCommandHandler(ICurriculumProgramRepository curriculumProgramRepository, IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _mapper = mapper;
    }

    public async Task<UpdateCurriculumProgramResponse> Handle(UpdateCurriculumProgramCommand request, CancellationToken cancellationToken)
    {
        var program = await _curriculumProgramRepository.GetByIdAsync(request.Id, cancellationToken);
        if (program == null)
        {
            throw new NotFoundException("CurriculumProgram", request.Id);
        }

        program.ProgramName = request.ProgramName;
        program.ProgramCode = request.ProgramCode;
        program.Description = request.Description;
        program.DegreeLevel = request.DegreeLevel;
        program.TotalCredits = request.TotalCredits;
        program.DurationYears = request.DurationYears;
        program.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedProgram = await _curriculumProgramRepository.UpdateAsync(program, cancellationToken);
        return _mapper.Map<UpdateCurriculumProgramResponse>(updatedProgram);
    }
}