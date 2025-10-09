using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

public class CreateSubjectHandler : IRequestHandler<CreateSubjectCommand, CreateSubjectResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public CreateSubjectHandler(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<CreateSubjectResponse> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credits = request.Credits,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdSubject = await _subjectRepository.AddAsync(subject, cancellationToken);
        return _mapper.Map<CreateSubjectResponse>(createdSubject);
    }
}