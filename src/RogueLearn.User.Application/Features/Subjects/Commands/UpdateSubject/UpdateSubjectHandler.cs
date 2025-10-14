using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;

public class UpdateSubjectHandler : IRequestHandler<UpdateSubjectCommand, UpdateSubjectResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public UpdateSubjectHandler(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<UpdateSubjectResponse> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (subject == null)
            throw new ArgumentException($"Subject with ID {request.Id} not found.");

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.Credits = request.Credits;
        subject.Description = request.Description;
        subject.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedSubject = await _subjectRepository.UpdateAsync(subject, cancellationToken);
        return _mapper.Map<UpdateSubjectResponse>(updatedSubject);
    }
}