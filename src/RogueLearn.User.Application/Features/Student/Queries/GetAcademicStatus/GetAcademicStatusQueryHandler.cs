using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusQueryHandler : IRequestHandler<GetAcademicStatusQuery, GetAcademicStatusResponse?>
{
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestProgressRepository _questProgressRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly ILogger<GetAcademicStatusQueryHandler> _logger;

    public GetAcademicStatusQueryHandler(
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        IUserQuestProgressRepository questProgressRepository,
        IUserSkillRepository userSkillRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ICurriculumProgramRepository curriculumProgramRepository,
        ILogger<GetAcademicStatusQueryHandler> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _questProgressRepository = questProgressRepository;
        _userSkillRepository = userSkillRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _curriculumProgramRepository = curriculumProgramRepository;
        _logger = logger;
    }

    public async Task<GetAcademicStatusResponse?> Handle(GetAcademicStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching academic status for user {AuthUserId}", request.AuthUserId);

        // Get enrollment
        var enrollment = request.CurriculumVersionId.HasValue
            ? await _enrollmentRepository.FirstOrDefaultAsync(
                e => e.AuthUserId == request.AuthUserId && e.CurriculumVersionId == request.CurriculumVersionId.Value,
                cancellationToken)
            : (await _enrollmentRepository.FindAsync(e => e.AuthUserId == request.AuthUserId, cancellationToken))
                .OrderByDescending(e => e.EnrollmentDate)
                .FirstOrDefault();

        if (enrollment == null)
        {
            _logger.LogInformation("No enrollment found for user {AuthUserId}", request.AuthUserId);
            return null;
        }

        // Get curriculum info
        var curriculumVersion = await _curriculumVersionRepository.GetByIdAsync(enrollment.CurriculumVersionId, cancellationToken);
        var curriculumProgram = curriculumVersion != null
            ? await _curriculumProgramRepository.GetByIdAsync(curriculumVersion.ProgramId, cancellationToken)
            : null;

        // Get learning path
        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(
            lp => lp.CreatedBy == request.AuthUserId && lp.CurriculumVersionId == enrollment.CurriculumVersionId,
            cancellationToken);

        // Get all subjects in curriculum
        var structures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == enrollment.CurriculumVersionId,
            cancellationToken)).ToList();

        var subjectIds = structures.Select(s => s.SubjectId).Distinct().ToList();
        var allSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionary(s => s.Id);

        // Get user's semester subjects
        var semesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.EnrollmentId == enrollment.Id,
            cancellationToken)).ToList();

        // Get quests and progress
        var quests = (await _questRepository.FindAsync(
            q => q.CreatedBy == request.AuthUserId && subjectIds.Contains(q.SubjectId ?? Guid.Empty),
            cancellationToken)).ToList();

        var questProgress = (await _questProgressRepository.FindAsync(
            qp => qp.AuthUserId == request.AuthUserId,
            cancellationToken)).ToDictionary(qp => qp.QuestId);

        // Get chapters
        List<Domain.Entities.QuestChapter> chapters = new();
        if (learningPath != null)
        {
            chapters = (await _questChapterRepository.FindAsync(
                qc => qc.LearningPathId == learningPath.Id,
                cancellationToken)).OrderBy(qc => qc.Sequence).ToList();
        }

        // Get user skills
        var userSkills = (await _userSkillRepository.FindAsync(
            us => us.AuthUserId == request.AuthUserId,
            cancellationToken)).ToList();

        // Build response
        var response = new GetAcademicStatusResponse
        {
            EnrollmentId = enrollment.Id,
            CurriculumVersionId = enrollment.CurriculumVersionId,
            CurriculumProgramName = curriculumProgram?.ProgramName ?? "Unknown Program",
            TotalSubjects = structures.Count,
            LearningPathId = learningPath?.Id,
            TotalQuests = quests.Count,
            CompletedQuests = questProgress.Values.Count(qp => qp.Status == QuestStatus.Completed),
            SkillInitialization = new SkillInitializationInfo
            {
                IsInitialized = userSkills.Any(),
                TotalSkills = userSkills.Count,
                LastInitializedAt = userSkills.Any() ? userSkills.Max(us => us.LastUpdatedAt) : null
            }
        };

        // Calculate GPA and subject counts
        var completedSubjects = semesterSubjects.Where(ss => ss.Status == SubjectEnrollmentStatus.Completed).ToList();
        response.CompletedSubjects = completedSubjects.Count;
        response.InProgressSubjects = semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Enrolled);
        response.FailedSubjects = semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Failed);

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

        // Build subject progress list
        response.Subjects = structures.Select(structure =>
        {
            var subject = allSubjects.GetValueOrDefault(structure.SubjectId);
            if (subject == null) return null;

            var semesterSubject = semesterSubjects.FirstOrDefault(ss => ss.SubjectId == subject.Id);
            var quest = quests.FirstOrDefault(q => q.SubjectId == subject.Id);
            var progress = quest != null && questProgress.TryGetValue(quest.Id, out var qp) ? qp : null;

            return new SubjectProgressDto
            {
                SubjectId = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Semester = structure.Semester,
                Status = semesterSubject?.Status.ToString() ?? "NotStarted",
                Grade = semesterSubject?.Grade,
                QuestId = quest?.Id,
                QuestStatus = progress?.Status.ToString()
            };
        })
        .Where(s => s != null)
        .Cast<SubjectProgressDto>()
        .OrderBy(s => s.Semester)
        .ThenBy(s => s.SubjectCode)
        .ToList();

        // Build chapter progress
        response.Chapters = chapters.Select(chapter =>
        {
            var chapterQuests = quests.Where(q =>
            {
                var structure = structures.FirstOrDefault(s => s.SubjectId == q.SubjectId);
                return structure != null && structure.Semester == chapter.Sequence;
            }).ToList();

            var completedInChapter = chapterQuests.Count(q =>
                questProgress.TryGetValue(q.Id, out var qp) && qp.Status == QuestStatus.Completed);

            return new ChapterProgressDto
            {
                ChapterId = chapter.Id,
                Title = chapter.Title,
                Sequence = chapter.Sequence,
                Status = chapter.Status.ToString(),
                TotalQuests = chapterQuests.Count,
                CompletedQuests = completedInChapter
            };
        }).ToList();

        return response;
    }
}