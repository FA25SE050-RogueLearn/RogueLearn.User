using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusQueryHandler : IRequestHandler<GetAcademicStatusQuery, GetAcademicStatusResponse?>
{
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassRepository _classRepository;
    private readonly ILogger<GetAcademicStatusQueryHandler> _logger;

    public GetAcademicStatusQueryHandler(
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        IClassRepository classRepository,
        ILogger<GetAcademicStatusQueryHandler> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _classRepository = classRepository;
        _logger = logger;
    }

    public async Task<GetAcademicStatusResponse?> Handle(GetAcademicStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching academic status for user {AuthUserId}", request.AuthUserId);

        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == request.AuthUserId, cancellationToken);
        if (enrollment == null)
        {
            _logger.LogInformation("No enrollment found for user {AuthUserId}", request.AuthUserId);
            return null;
        }

        var semesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken)).ToList();

        var subjectIds = semesterSubjects.Select(ss => ss.SubjectId).Distinct().ToList();
        var allSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionary(s => s.Id);

        var response = new GetAcademicStatusResponse
        {
            EnrollmentId = enrollment.Id,
            // VIRTUAL ID: Use UserID
            LearningPathId = request.AuthUserId
        };

        // Calculate GPA and subject counts
        var completedSubjects = semesterSubjects.Where(ss => ss.Status == SubjectEnrollmentStatus.Passed).ToList();
        response.CompletedSubjects = completedSubjects.Count;
        response.InProgressSubjects = semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Studying);
        response.FailedSubjects = semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.NotPassed);

        if (completedSubjects.Any())
        {
            double totalWeightedGrade = 0;
            int totalCredits = 0;

            foreach (var ss in completedSubjects)
            {
                if (double.TryParse(ss.Grade, out var grade) && allSubjects.TryGetValue(ss.SubjectId, out var subject))
                {
                    totalWeightedGrade += grade * subject.Credits;
                    totalCredits += subject.Credits;
                }
            }
            response.CurrentGpa = totalCredits > 0 ? Math.Round(totalWeightedGrade / totalCredits, 2) : 0;
        }

        return response;
    }
}