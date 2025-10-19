// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Commands/AddSpecializationSubject/AddSpecializationSubjectCommandHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

public class AddSpecializationSubjectCommandHandler : IRequestHandler<AddSpecializationSubjectCommand, SpecializationSubjectDto>
{
    private readonly IClassSpecializationSubjectRepository _repository;
    private readonly IClassRepository _classRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public AddSpecializationSubjectCommandHandler(
        IClassSpecializationSubjectRepository repository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper)
    {
        _repository = repository;
        _classRepository = classRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<SpecializationSubjectDto> Handle(AddSpecializationSubjectCommand request, CancellationToken cancellationToken)
    {
        // Validate that both the class and subject exist
        if (!await _classRepository.ExistsAsync(request.ClassId, cancellationToken))
            throw new NotFoundException(nameof(Class), request.ClassId);

        if (!await _subjectRepository.ExistsAsync(request.SubjectId, cancellationToken))
            throw new NotFoundException(nameof(Subject), request.SubjectId);

        // Prevent duplicate mappings
        var existing = await _repository.FirstOrDefaultAsync(m => m.ClassId == request.ClassId && m.SubjectId == request.SubjectId, cancellationToken);
        if (existing != null)
            throw new BadRequestException("This subject is already mapped to this specialization class.");

        var newMapping = _mapper.Map<ClassSpecializationSubject>(request);
        var createdMapping = await _repository.AddAsync(newMapping, cancellationToken);

        return _mapper.Map<SpecializationSubjectDto>(createdMapping);
    }
}