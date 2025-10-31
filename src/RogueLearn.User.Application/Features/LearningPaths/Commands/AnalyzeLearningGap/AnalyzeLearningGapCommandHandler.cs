// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/AnalyzeLearningGap/AnalyzeLearningGapCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.AnalyzeLearningGap;

public class AnalyzeLearningGapCommandHandler : IRequestHandler<AnalyzeLearningGapCommand, GapAnalysisResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    private readonly IClassSpecializationSubjectRepository _specSubjectRepository;
    private readonly ILogger<AnalyzeLearningGapCommandHandler> _logger;

    public AnalyzeLearningGapCommandHandler(IUserProfileRepository userProfileRepository, IClassRepository classRepository, IClassSpecializationSubjectRepository specSubjectRepository, ILogger<AnalyzeLearningGapCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _specSubjectRepository = specSubjectRepository;
        _logger = logger;
    }

    public async Task<GapAnalysisResponse> Handle(AnalyzeLearningGapCommand request, CancellationToken cancellationToken)
    {
        var user = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (user?.ClassId is null)
        {
            throw new BadRequestException("User has not selected a career class. Onboarding must be completed first.");
        }

        var careerClass = await _classRepository.GetByIdAsync(user.ClassId.Value, cancellationToken);
        if (careerClass is null)
        {
            throw new NotFoundException("Career Class", user.ClassId.Value);
        }

        var requiredSubjects = await _specSubjectRepository.FindAsync(s => s.ClassId == careerClass.Id, cancellationToken);
        var passedSubjects = request.VerifiedRecord.Subjects
            .Where(s => "Passed".Equals(s.Status, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.SubjectCode)
            .ToHashSet();

        var failedSubjects = request.VerifiedRecord.Subjects
            .Where(s => "Failed".Equals(s.Status, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.SubjectCode)
            .ToHashSet();

        var subjectGaps = requiredSubjects
            .Where(rs => !passedSubjects.Contains(rs.PlaceholderSubjectCode))
            .Select(rs => rs.PlaceholderSubjectCode)
            .ToList();

        var prioritySubject = subjectGaps.FirstOrDefault(sg => failedSubjects.Contains(sg)) ?? subjectGaps.FirstOrDefault();

        if (string.IsNullOrEmpty(prioritySubject))
        {
            return new GapAnalysisResponse
            {
                RecommendedFocus = "Your academic record already covers the requirements for your chosen path!",
                HighestPrioritySubject = "None",
                Reason = "You can explore advanced quests or select a new career path.",
                ForgingPayload = new ForgingPayload { SubjectGaps = new List<string>() }
            };
        }

        var reason = $"To align with your '{careerClass.Name}' goal, you need to cover several subjects. Your highest priority is {prioritySubject}, as it's a core requirement you haven't passed yet.";

        return new GapAnalysisResponse
        {
            RecommendedFocus = $"Focus on subjects related to {careerClass.Name}",
            HighestPrioritySubject = prioritySubject,
            Reason = reason,
            ForgingPayload = new ForgingPayload { SubjectGaps = subjectGaps }
        };
    }
}