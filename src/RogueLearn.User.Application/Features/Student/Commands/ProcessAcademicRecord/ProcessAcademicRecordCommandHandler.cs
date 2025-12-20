// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordCommandHandler.cs
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models; // For FapRecordData and FapSubjectData
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommandHandler : IRequestHandler<ProcessAcademicRecordCommand, ProcessAcademicRecordResponse>
{
    private static readonly HashSet<string> ExcludedSubjectCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "VOV114", "VOV124", "VOV134", // Vovinam
        "TMI101",                     // Traditional musical instrument
        "OTP101",                     // Orientation
        "TRS601",                      // English 6 (University success), add more codes as needed
        "PEN",
    };
    private readonly IFapExtractionPlugin _fapPlugin;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramRepository _programRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IHtmlCleaningService _htmlCleaningService;
    private readonly ILogger<ProcessAcademicRecordCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ICurriculumImportStorage _storage;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepGenerationService _questStepGenerationService;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IGradeExperienceCalculator _gradeExperienceCalculator;
    private readonly IAcademicAnalysisPlugin _academicAnalysisPlugin;

    public ProcessAcademicRecordCommandHandler(
        IFapExtractionPlugin fapPlugin,
        IStudentEnrollmentRepository enrollmentRepository,
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IUserProfileRepository userProfileRepository,
        IHtmlCleaningService htmlCleaningService,
        ILogger<ProcessAcademicRecordCommandHandler> logger,
        IMediator mediator,
        ICurriculumImportStorage storage,
        IBackgroundJobClient backgroundJobClient,
        IQuestRepository questRepository,
        IQuestStepGenerationService questStepGenerationService,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IGradeExperienceCalculator gradeExperienceCalculator,
        IAcademicAnalysisPlugin academicAnalysisPlugin)
    {
        _fapPlugin = fapPlugin;
        _enrollmentRepository = enrollmentRepository;
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
        _programRepository = programRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _userProfileRepository = userProfileRepository;
        _htmlCleaningService = htmlCleaningService;
        _logger = logger;
        _mediator = mediator;
        _storage = storage;
        _backgroundJobClient = backgroundJobClient;
        _questRepository = questRepository;
        _questStepGenerationService = questStepGenerationService;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _gradeExperienceCalculator = gradeExperienceCalculator;
        _academicAnalysisPlugin = academicAnalysisPlugin;
    }

    public async Task<ProcessAcademicRecordResponse> Handle(ProcessAcademicRecordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[START] Processing academic record for user {AuthUserId}", request.AuthUserId);

        // ========== STEP 1: VALIDATE HTML INPUT ==========
        ValidateHtmlInput(request.FapHtmlContent);

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        if (!userProfile.ClassId.HasValue)
        {
            throw new BadRequestException("User has not selected a specialization class. Onboarding may be incomplete.");
        }

        if (!await _programRepository.ExistsAsync(request.CurriculumProgramId, cancellationToken))
        {
            _logger.LogWarning("Invalid CurriculumProgramId provided: {ProgramId}", request.CurriculumProgramId);
            throw new NotFoundException(nameof(CurriculumProgram), request.CurriculumProgramId);
        }

        var allowedSubjectIds = await BuildAllowedSubjectList(request.CurriculumProgramId, userProfile.ClassId.Value, cancellationToken);
        var rawTextHash = ComputeSha256Hash(request.FapHtmlContent);
        string? extractedJson = await _storage.TryGetByHashJsonAsync("academic-records", rawTextHash, cancellationToken);

        if (!string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogInformation("Cache HIT: Found cached academic record for hash {Hash}. Skipping AI extraction.", rawTextHash);
        }
        else
        {
            _logger.LogInformation("Cache MISS: No cached record found for hash {Hash}. Proceeding with AI extraction.", rawTextHash);
            var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.FapHtmlContent);

            // ========== STEP 2: VALIDATE CLEANED TEXT ==========
            ValidateCleanedText(cleanText);

            extractedJson = await _fapPlugin.ExtractFapRecordJsonAsync(cleanText, cancellationToken);

            // ========== STEP 3: VALIDATE AI EXTRACTION RESULT ==========
            ValidateAiExtractionResult(extractedJson);

            await _storage.SaveLatestAsync(
                bucketName: "academic-records",
                programCode: request.CurriculumProgramId.ToString(),
                versionCode: "fap-sync",
                jsonContent: extractedJson,
                rawTextContent: request.FapHtmlContent,
                rawTextHash: rawTextHash,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Cache WRITE: Saved new academic record to cache for hash {Hash}.", rawTextHash);
        }

        FapRecordData? fapData;
        try
        {
            fapData = JsonSerializer.Deserialize<FapRecordData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI extraction result as JSON. Raw response: {Json}", extractedJson?.Substring(0, Math.Min(500, extractedJson?.Length ?? 0)));
            throw new BadRequestException("The AI failed to extract valid data from the provided content. The HTML may be corrupted or in an unexpected format. Please ensure you're uploading the correct FAP transcript page.");
        }

        // ========== STEP 4: VALIDATE EXTRACTED DATA ==========
        ValidateExtractedData(fapData);

        _logger.LogInformation("Successfully deserialized {SubjectCount} subjects from transcript.", fapData!.Subjects.Count);

        var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == request.AuthUserId, cancellationToken);
        if (enrollment == null)
        {
            _logger.LogInformation("Creating new enrollment for user {AuthUserId}.", request.AuthUserId);
            enrollment = new StudentEnrollment
            {
                AuthUserId = request.AuthUserId,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = EnrollmentStatus.Active
            };
            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        }

        var allAllowedSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => allowedSubjectIds.Contains(s.Id))
            .ToList();

        if (!allAllowedSubjects.Any())
        {
            _logger.LogWarning("Could not find any subjects linked to ProgramId {ProgramId} or ClassId {ClassId}", request.CurriculumProgramId, userProfile.ClassId.Value);
            throw new NotFoundException($"No subjects are associated with program {request.CurriculumProgramId} or class {userProfile.ClassId.Value}");
        }

        var subjectCatalog = allAllowedSubjects.ToDictionary(s => s.SubjectCode);
        _logger.LogInformation("Built combined subject catalog for program and class with {Count} subjects.", subjectCatalog.Count);

        // ========== NEW LOGIC: GENERATE AI ANALYSIS WITH CACHING ==========
        // 1. Try to get existing analysis from storage
        AcademicAnalysisReport? analysisReport = await _storage.GetUserAnalysisAsync(request.AuthUserId, cancellationToken);

        // 2. Only run AI if analysis is missing or if we just parsed new FAP data (cache miss on extraction)
        bool freshDataParsed = string.IsNullOrWhiteSpace(extractedJson); // If we extracted, we should re-analyze

        if (analysisReport == null || freshDataParsed)
        {
            _logger.LogInformation("Generating AI Academic Analysis (Cache Miss or Fresh Data)...");

            var subjectNameMap = new Dictionary<string, string>();
            foreach (var s in fapData.Subjects)
            {
                if (subjectCatalog.TryGetValue(s.SubjectCode, out var subjEntity))
                {
                    subjectNameMap[s.SubjectCode] = subjEntity.SubjectName;
                }
            }

            analysisReport = await _academicAnalysisPlugin.AnalyzePerformanceAsync(
                fapData.Subjects,
                subjectNameMap,
                cancellationToken);

            // 3. Save the new analysis to storage
            await _storage.SaveUserAnalysisAsync(request.AuthUserId, analysisReport, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Using cached Academic Analysis for user {UserId}", request.AuthUserId);
        }

        // Fetch existing records using specialized method to avoid Guid LINQ issues
        var existingSemesterSubjects = (await _semesterSubjectRepository.GetSemesterSubjectsByUserAsync(
            request.AuthUserId, cancellationToken)
            ).ToList();

        int recordsAdded = 0;
        int recordsUpdated = 0;
        int recordsIgnored = 0;

        foreach (var subjectRecord in fapData.Subjects)
        {
            if (IsExcludedSubject(subjectRecord))
            {
                _logger.LogInformation("🚫 Skipping excluded subject: {Code}", subjectRecord.SubjectCode);
                recordsIgnored++;
                continue;
            }
            if (!subjectCatalog.TryGetValue(subjectRecord.SubjectCode, out var subject))
            {
                _logger.LogWarning("Subject {SubjectCode} from transcript not found in user's program/class catalog. Skipping.", subjectRecord.SubjectCode);
                recordsIgnored++;
                continue;
            }

            var existingRecord = existingSemesterSubjects.FirstOrDefault(ss =>
                ss.SubjectId == subject.Id &&
                ss.AcademicYear == subjectRecord.AcademicYear);

            // Updated Mapping Logic:
            // 1. Explicitly map string status
            var parsedStatus = MapFapStatusToEnum(subjectRecord.Status);

            // 2. Safety check: If Mark is missing and Status is empty/ambiguous, verify it's NotStarted
            if (!subjectRecord.Mark.HasValue && parsedStatus == SubjectEnrollmentStatus.NotStarted)
            {
                // Correct behavior: Keep it as NotStarted
            }
            else if (string.IsNullOrWhiteSpace(subjectRecord.Status) && subjectRecord.Mark.HasValue)
            {
                // Fallback: If status missing but grade exists, infer status
                parsedStatus = subjectRecord.Mark >= 5.0 ? SubjectEnrollmentStatus.Passed : SubjectEnrollmentStatus.NotPassed;
            }

            if (existingRecord == null)
            {
                var newSemesterSubject = new StudentSemesterSubject
                {
                    AuthUserId = request.AuthUserId,
                    SubjectId = subject.Id,
                    AcademicYear = subjectRecord.AcademicYear,
                    Status = parsedStatus,
                    Grade = subjectRecord.Mark?.ToString("F1"),
                    CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Passed ? subject.Credits : 0
                };
                await _semesterSubjectRepository.AddAsync(newSemesterSubject, cancellationToken);
                recordsAdded++;
            }
            else if (existingRecord.Status != parsedStatus || existingRecord.Grade != subjectRecord.Mark?.ToString("F1"))
            {
                existingRecord.Status = parsedStatus;
                existingRecord.Grade = subjectRecord.Mark?.ToString("F1");
                existingRecord.CreditsEarned = parsedStatus == SubjectEnrollmentStatus.Passed ? subject.Credits : 0;
                await _semesterSubjectRepository.UpdateAsync(existingRecord, cancellationToken);
                recordsUpdated++;
            }
        }

        _logger.LogInformation(
            "Academic record sync complete. Records Added: {Added}, Records Updated: {Updated}, Records Ignored: {Ignored}",
            recordsAdded, recordsUpdated, recordsIgnored);

        // ========== AWARD XP FOR PASSED SUBJECTS ==========
        var xpAwarded = await AwardXpForPassedSubjectsAsync(
            request.AuthUserId,
            fapData.Subjects,
            subjectCatalog,
            cancellationToken);

        _logger.LogInformation(
            "XP Award Summary: {TotalXp} XP awarded across {SkillCount} skills for user {AuthUserId}",
            xpAwarded.TotalXp, xpAwarded.SkillsAffected, request.AuthUserId);

        _logger.LogInformation("Dispatching GenerateQuestLine command for user {AuthUserId} to create learning path structure.", request.AuthUserId);

        // This command will now perform the JIT difficulty calculation preview and create "NotStarted" attempts
        // It returns the AuthUserId as the virtual LearningPathId

        // MODIFICATION: Pass the AI Analysis Report into the generation command
        var questLineCommand = new GenerateQuestLine
        {
            AuthUserId = request.AuthUserId,
            AiAnalysisReport = analysisReport
        };

        var questLineResponse = await _mediator.Send(questLineCommand, cancellationToken);

        _logger.LogInformation("✅ Academic records processed successfully. Quests updated with difficulty previews.");

        return new ProcessAcademicRecordResponse
        {
            IsSuccess = true,
            Message = "Academic record processed successfully. Your gradebook and learning path have been updated. " +
                      "Recommended quests are ready to explore!",
            LearningPathId = questLineResponse.LearningPathId,
            SubjectsProcessed = fapData.Subjects.Count,
            CalculatedGpa = fapData.Gpa ?? 0.0,
            XpAwarded = xpAwarded.SkillAwards.Any() ? xpAwarded : null,
            AnalysisReport = analysisReport
        };
    }

    private static bool IsExcludedSubject(FapSubjectData subject)
    {
        // Check by code first
        if (ExcludedSubjectCodes.Contains(subject.SubjectCode))
            return true;

        // Check by name (catches variants)
        if (!string.IsNullOrEmpty(subject.SubjectName))
        {
            var nameLower = subject.SubjectName.ToLowerInvariant();
            if (nameLower.Contains("musical instrument")
                || nameLower.Contains("orientation")
                || nameLower.Contains("vovinam"))
                return true;
        }

        return false;
    }

    private async Task<HashSet<Guid>> BuildAllowedSubjectList(Guid programId, Guid classId, CancellationToken cancellationToken)
    {
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == programId, cancellationToken);
        var allowedSet = new HashSet<Guid>(programSubjects.Select(ps => ps.SubjectId));

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(classId, cancellationToken);
        foreach (var subject in classSubjects)
        {
            allowedSet.Add(subject.Id);
        }

        _logger.LogInformation("Built allowed subject list for Program {ProgramId} and Class {ClassId}. Total allowed subjects: {Count}", programId, classId, allowedSet.Count);

        return allowedSet;
    }

    private SubjectEnrollmentStatus MapFapStatusToEnum(string status)
    {
        // Updated to catch "Not Started" specifically
        return status.Trim().ToLowerInvariant() switch
        {
            "passed" => SubjectEnrollmentStatus.Passed,
            "pass" => SubjectEnrollmentStatus.Passed,
            "studying" => SubjectEnrollmentStatus.Studying,
            "in progress" => SubjectEnrollmentStatus.Studying,
            "not started" => SubjectEnrollmentStatus.NotStarted,
            "failed" => SubjectEnrollmentStatus.NotPassed,
            "fail" => SubjectEnrollmentStatus.NotPassed,
            "not passed" => SubjectEnrollmentStatus.NotPassed,
            _ => SubjectEnrollmentStatus.NotStarted
        };
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ========== XP AWARD METHODS ==========

    /// <summary>
    /// Awards XP to skills based on passed subjects using the tiered cap system.
    /// Uses IngestXpEventCommand for idempotency - if XP was already awarded for a subject,
    /// it won't be duplicated.
    /// </summary>
    private async Task<XpAwardSummary> AwardXpForPassedSubjectsAsync(
        Guid authUserId,
        List<FapSubjectData> subjects,
        Dictionary<string, Subject> subjectCatalog,
        CancellationToken cancellationToken)
    {
        var summary = new XpAwardSummary();
        var skillsAffected = new HashSet<Guid>();

        // Get all subject IDs that were passed
        var passedSubjectIds = subjects
            .Where(s => MapFapStatusToEnum(s.Status) == SubjectEnrollmentStatus.Passed && s.Mark.HasValue)
            .Where(s => subjectCatalog.ContainsKey(s.SubjectCode))
            .Select(s => subjectCatalog[s.SubjectCode].Id)
            .ToList();

        if (!passedSubjectIds.Any())
        {
            _logger.LogInformation("No passed subjects found for XP award calculation.");
            return summary;
        }

        // Fetch all skill mappings for passed subjects in one query
        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(
            passedSubjectIds, cancellationToken);

        var mappingsBySubject = skillMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var subjectRecord in subjects)
        {
            // Skip non-passed or subjects without grades
            if (MapFapStatusToEnum(subjectRecord.Status) != SubjectEnrollmentStatus.Passed)
                continue;

            if (!subjectRecord.Mark.HasValue)
                continue;

            if (!subjectCatalog.TryGetValue(subjectRecord.SubjectCode, out var subject))
                continue;

            // Get skill mappings for this subject
            if (!mappingsBySubject.TryGetValue(subject.Id, out var subjectMappings) || !subjectMappings.Any())
            {
                _logger.LogDebug("Subject {SubjectCode} has no skill mappings, skipping XP award.", subject.SubjectCode);
                continue;
            }

            var grade = subjectRecord.Mark.Value;
            var semester = subject.Semester ?? 1;
            var tierInfo = _gradeExperienceCalculator.GetTierInfo(semester);

            _logger.LogInformation(
                "Calculating XP for {SubjectCode} (Sem {Semester}, Tier {Tier}): Grade={Grade}, Skills={SkillCount}",
                subject.SubjectCode, semester, tierInfo.Tier, grade, subjectMappings.Count);

            foreach (var mapping in subjectMappings)
            {
                var xpAmount = _gradeExperienceCalculator.CalculateXpAward(
                    grade,
                    semester,
                    mapping.RelevanceWeight);

                // Use IngestXpEventCommand for idempotency
                // SourceId = SubjectId ensures we don't award XP twice for the same subject
                var response = await _mediator.Send(new IngestXpEventCommand
                {
                    AuthUserId = authUserId,
                    SkillId = mapping.SkillId,
                    Points = xpAmount,
                    SourceService = "AcademicRecord",
                    SourceType = "GradeImport",
                    SourceId = subject.Id,  // Idempotency key
                    Reason = $"Academic grade: {subject.SubjectCode} ({grade:F1}/10.0) - {tierInfo.Description}"
                }, cancellationToken);

                if (response.Processed)
                {
                    summary.TotalXp += xpAmount;
                    skillsAffected.Add(mapping.SkillId);

                    // Add detailed award info for response
                    summary.SkillAwards.Add(new SkillXpAward
                    {
                        SkillId = mapping.SkillId,
                        SkillName = response.SkillName,
                        XpAwarded = xpAmount,
                        NewTotalXp = response.NewExperiencePoints,
                        NewLevel = response.NewLevel,
                        SourceSubjectCode = subject.SubjectCode,
                        Grade = grade.ToString("F1"),
                        TierDescription = tierInfo.Description
                    });

                    _logger.LogInformation(
                        "Awarded {Xp} XP to skill {SkillName} (Level {Level}) from {SubjectCode}",
                        xpAmount, response.SkillName, response.NewLevel, subject.SubjectCode);
                }
                else
                {
                    _logger.LogDebug(
                        "XP already awarded for {SubjectCode} -> skill {SkillId}. Skipping.",
                        subject.SubjectCode, mapping.SkillId);
                }
            }
        }

        summary.SkillsAffected = skillsAffected.Count;
        return summary;
    }

    // ========== VALIDATION METHODS ==========

    /// <summary>
    /// Validates the raw HTML input to ensure it contains expected FAP transcript structure.
    /// </summary>
    private void ValidateHtmlInput(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning("Empty HTML content provided for academic record processing.");
            throw new BadRequestException("The provided HTML content is empty. Please upload your FAP transcript page.");
        }

        // Check minimum length - a valid transcript should have substantial content
        if (htmlContent.Length < 500)
        {
            _logger.LogWarning("HTML content too short ({Length} chars). Likely not a valid transcript.", htmlContent.Length);
            throw new BadRequestException("The provided content is too short. Please upload the complete FAP grade report page.");
        }

        // Check for basic HTML structure
        var hasHtmlStructure = htmlContent.Contains("<table", StringComparison.OrdinalIgnoreCase) ||
                               htmlContent.Contains("<tr", StringComparison.OrdinalIgnoreCase) ||
                               htmlContent.Contains("<td", StringComparison.OrdinalIgnoreCase);

        if (!hasHtmlStructure)
        {
            _logger.LogWarning("HTML content does not contain table structure. Content preview: {Preview}",
                htmlContent.Substring(0, Math.Min(200, htmlContent.Length)));
            throw new BadRequestException("The provided content does not appear to be a valid HTML table. Please upload the FAP transcript page.");
        }

        // Check for FAP-specific indicators (subject codes, grade patterns)
        var hasFapIndicators = ContainsFapIndicators(htmlContent);
        if (!hasFapIndicators)
        {
            _logger.LogWarning("HTML content does not contain FAP-specific indicators (subject codes, grades).");
            throw new BadRequestException("The provided HTML does not appear to be from the FAP academic portal. Please ensure you're uploading the correct transcript page from FAP.");
        }
    }

    /// <summary>
    /// Validates the cleaned text extracted from HTML before sending to AI.
    /// </summary>
    private void ValidateCleanedText(string cleanText)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            _logger.LogWarning("HTML cleaning resulted in empty text.");
            throw new BadRequestException("Failed to extract readable content from the HTML. The file may be corrupted or in an unsupported format.");
        }

        // Check if cleaned text has minimum expected content
        if (cleanText.Length < 100)
        {
            _logger.LogWarning("Cleaned text too short ({Length} chars). HTML may be malformed.", cleanText.Length);
            throw new BadRequestException("The extracted content is too short. The HTML may be corrupted or not contain the expected transcript data.");
        }

        // Check for presence of subject code patterns (e.g., PRO192, CSI104, MAE101)
        var subjectCodePattern = new Regex(@"\b[A-Z]{2,4}\d{3}\b", RegexOptions.IgnoreCase);
        if (!subjectCodePattern.IsMatch(cleanText))
        {
            _logger.LogWarning("No subject codes found in cleaned text. Content may not be a transcript.");
            throw new BadRequestException("No subject codes were found in the content. Please ensure you're uploading your FAP academic transcript that contains subject grades.");
        }
    }

    /// <summary>
    /// Validates the AI extraction result before processing.
    /// </summary>
    private void ValidateAiExtractionResult(string? extractedJson)
    {
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            _logger.LogError("AI extraction returned empty result.");
            throw new BadRequestException("The AI could not extract any data from the provided content. The transcript format may be corrupted or unrecognizable. Please try uploading the page again or contact support.");
        }

        // Check if it looks like valid JSON
        var trimmed = extractedJson.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
        {
            _logger.LogError("AI extraction result is not valid JSON. Result: {Result}",
                extractedJson.Substring(0, Math.Min(200, extractedJson.Length)));
            throw new BadRequestException("The AI returned an invalid response. This may be due to an unusual transcript format. Please ensure you're uploading the standard FAP grade report page.");
        }
    }

    /// <summary>
    /// Validates the deserialized FAP data to ensure it contains required information.
    /// </summary>
    private void ValidateExtractedData(FapRecordData? fapData)
    {
        if (fapData == null)
        {
            _logger.LogError("Deserialized FAP data is null.");
            throw new BadRequestException("Failed to parse the extracted academic data. The transcript format may be incompatible.");
        }

        // Check if subjects list exists and has entries
        if (fapData.Subjects == null || fapData.Subjects.Count == 0)
        {
            _logger.LogWarning("No subjects found in extracted FAP data. GPA: {Gpa}", fapData.Gpa);
            throw new BadRequestException("No subjects were found in the transcript. Please ensure the FAP page contains your grade table with at least one subject entry.");
        }

        // Validate that subjects have required fields
        var invalidSubjects = fapData.Subjects.Where(s => string.IsNullOrWhiteSpace(s.SubjectCode)).ToList();
        if (invalidSubjects.Count == fapData.Subjects.Count)
        {
            _logger.LogWarning("All {Count} subjects have missing subject codes.", invalidSubjects.Count);
            throw new BadRequestException("The extracted subjects are missing subject codes. The transcript format may be corrupted or in an unexpected layout.");
        }

        // Log warning for partial invalid data but continue processing
        if (invalidSubjects.Any())
        {
            _logger.LogWarning("{Count} out of {Total} subjects have missing subject codes and will be skipped.",
                invalidSubjects.Count, fapData.Subjects.Count);
        }

        // Validate subjects have valid status
        var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "passed", "pass", "failed", "fail", "studying", "in progress", "not passed", "not started"
        };
        var subjectsWithInvalidStatus = fapData.Subjects
            .Where(s => !string.IsNullOrWhiteSpace(s.SubjectCode) &&
                       !validStatuses.Contains(s.Status?.Trim() ?? ""))
            .ToList();

        if (subjectsWithInvalidStatus.Count > fapData.Subjects.Count / 2)
        {
            _logger.LogWarning("More than half of subjects ({Count}/{Total}) have invalid status values.",
                subjectsWithInvalidStatus.Count, fapData.Subjects.Count);
            throw new BadRequestException("The extracted data contains too many invalid status values. The transcript format may not be compatible. Please ensure you're uploading the correct FAP grade report.");
        }

        _logger.LogInformation("FAP data validation passed. Subjects: {Count}, GPA: {Gpa}",
            fapData.Subjects.Count, fapData.Gpa?.ToString("F2") ?? "N/A");
    }

    /// <summary>
    /// Checks if the HTML content contains FAP-specific indicators.
    /// </summary>
    private static bool ContainsFapIndicators(string htmlContent)
    {
        var lowerContent = htmlContent.ToLowerInvariant();

        // Check for FAP-specific keywords
        var hasFapKeywords = lowerContent.Contains("fap") ||
                             lowerContent.Contains("fpt") ||
                             lowerContent.Contains("academic") ||
                             lowerContent.Contains("transcript") ||
                             lowerContent.Contains("grade") ||
                             lowerContent.Contains("semester") ||
                             lowerContent.Contains("subject") ||
                             lowerContent.Contains("mark") ||
                             lowerContent.Contains("passed") ||
                             lowerContent.Contains("studying");

        // Check for subject code patterns (e.g., PRO192, CSI104, MAE101)
        var subjectCodePattern = new Regex(@"\b[A-Z]{2,4}\d{3}\b", RegexOptions.IgnoreCase);
        var hasSubjectCodes = subjectCodePattern.IsMatch(htmlContent);

        // Check for grade patterns (numbers like 7.5, 8.0, etc.)
        var gradePattern = new Regex(@"\b\d{1,2}\.\d\b");
        var hasGradePatterns = gradePattern.IsMatch(htmlContent);

        // Return true if we have subject codes AND (FAP keywords OR grade patterns)
        return hasSubjectCodes && (hasFapKeywords || hasGradePatterns);
    }
}