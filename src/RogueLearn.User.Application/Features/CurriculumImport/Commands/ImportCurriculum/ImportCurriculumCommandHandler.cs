// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumImport/Commands/ImportCurriculum/ImportCurriculumCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum
{
    public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
    {
        private readonly ICurriculumProgramRepository _curriculumProgramRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
        private readonly CurriculumImportDataValidator _validator;
        private readonly ILogger<ImportCurriculumCommandHandler> _logger;
        private readonly ICurriculumExtractionPlugin _curriculumPlugin;
        private readonly IHtmlCleaningService _htmlCleaningService;

        public ImportCurriculumCommandHandler(
            ICurriculumProgramRepository curriculumProgramRepository,
            ISubjectRepository subjectRepository,
            ICurriculumProgramSubjectRepository programSubjectRepository,
            CurriculumImportDataValidator validator,
            ILogger<ImportCurriculumCommandHandler> logger,
            ICurriculumExtractionPlugin curriculumPlugin,
            IHtmlCleaningService htmlCleaningService)
        {
            _curriculumProgramRepository = curriculumProgramRepository;
            _subjectRepository = subjectRepository;
            _programSubjectRepository = programSubjectRepository;
            _validator = validator;
            _logger = logger;
            _curriculumPlugin = curriculumPlugin;
            _htmlCleaningService = htmlCleaningService;
        }

        public async Task<ImportCurriculumResponse> Handle(ImportCurriculumCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting curriculum import from text");

            // 1. Extract clean text from HTML
            var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
            if (string.IsNullOrWhiteSpace(cleanText))
            {
                _logger.LogError("HTML cleaning service failed to extract meaningful text from the provided HTML.");
                throw new Exceptions.BadRequestException("Failed to extract meaningful text from the provided HTML.");
            }

            // 2. Use Plugin to extract structured JSON (now local HTML parser)
            var extractedJson = await _curriculumPlugin.ExtractCurriculumJsonAsync(request.RawText, cancellationToken);
            if (string.IsNullOrEmpty(extractedJson))
            {
                _logger.LogError("Plugin failed to extract curriculum JSON.");
                throw new Exceptions.BadRequestException("Failed to extract curriculum data.");
            }

            // 3. Deserialize JSON
            CurriculumImportData? curriculumData;
            try
            {
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson, serializerOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize extracted JSON: {Json}", extractedJson);
                throw new Exceptions.BadRequestException("Extraction returned invalid JSON format.");
            }

            if (curriculumData == null)
            {
                throw new Exceptions.BadRequestException("No curriculum data was extracted");
            }

            // 4. Validate Data
            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new Exceptions.ValidationException(validationResult.Errors);
            }

            var response = new ImportCurriculumResponse { IsSuccess = true };

            // 5. Get or Create Curriculum Program
            var program = await _curriculumProgramRepository.FirstOrDefaultAsync(p => p.ProgramCode == curriculumData.Program.ProgramCode, cancellationToken);
            if (program == null)
            {
                program = new CurriculumProgram
                {
                    ProgramName = curriculumData.Program.ProgramName,
                    ProgramCode = curriculumData.Program.ProgramCode,
                    Description = curriculumData.Program.Description,
                    DegreeLevel = curriculumData.Program.DegreeLevel,
                    TotalCredits = curriculumData.Program.TotalCredits,
                    DurationYears = (int?)curriculumData.Program.DurationYears
                };
                program = await _curriculumProgramRepository.AddAsync(program, cancellationToken);
                _logger.LogInformation("Created new curriculum program with ID {ProgramId}", program.Id);
            }
            else
            {
                // Optional: Update existing program details
                program.ProgramName = curriculumData.Program.ProgramName;
                program.Description = curriculumData.Program.Description;
                program.TotalCredits = curriculumData.Program.TotalCredits;
                await _curriculumProgramRepository.UpdateAsync(program, cancellationToken);
            }
            response.CurriculumProgramId = program.Id;

            // 6. Process Subjects (LINK EXISTING ONLY)
            // Get all subject codes from the import data
            var extractedCodes = curriculumData.Subjects
                .Where(s => !s.IsPlaceholder)
                .Select(s => s.SubjectCode)
                .Distinct()
                .ToList();

            // Bulk fetch only subjects that already exist in the DB
            var existingSubjects = (await _subjectRepository.GetByCodesAsync(extractedCodes, cancellationToken)).ToList();
            var existingSubjectsMap = existingSubjects.ToDictionary(s => s.SubjectCode, StringComparer.OrdinalIgnoreCase);

            var processedSubjects = new List<Subject>();
            DateTimeOffset effectiveDate = new DateTimeOffset(curriculumData.Version.EffectiveYear, 1, 1, 0, 0, 0, TimeSpan.Zero);

            foreach (var subjectData in curriculumData.Subjects.Where(s => s.IsPlaceholder == false))
            {
                if (existingSubjectsMap.TryGetValue(subjectData.SubjectCode, out var existingSubject))
                {
                    // Update existing subject with latest info from curriculum
                    existingSubject.SubjectName = subjectData.SubjectName;
                    existingSubject.Credits = subjectData.Credits;
                    existingSubject.Description = subjectData.Description;

                    // Update semester from structure if available (contextual to this curriculum view, but stored on subject for now)
                    var structureInfo = curriculumData.Structure.FirstOrDefault(s => s.SubjectCode == subjectData.SubjectCode);
                    if (structureInfo != null)
                    {
                        existingSubject.Semester = structureInfo.TermNumber;
                    }

                    existingSubject.UpdatedAt = effectiveDate;

                    var updated = await _subjectRepository.UpdateAsync(existingSubject, cancellationToken);
                    processedSubjects.Add(updated);
                    _logger.LogInformation("Updated existing subject: {Code}", updated.SubjectCode);
                }
                else
                {
                    // SKIP creation as requested
                    _logger.LogWarning("Subject {Code} not found in database. Skipping import/link as per policy.", subjectData.SubjectCode);
                }
            }

            // 7. Handle Prerequisites (Second pass for linking, only if both exist)
            var subjectMapByCode = processedSubjects.ToDictionary(s => s.SubjectCode, s => s);
            foreach (var subject in processedSubjects)
            {
                var structureData = curriculumData.Structure.FirstOrDefault(s => s.SubjectCode == subject.SubjectCode);
                if (structureData?.PrerequisiteSubjectCodes?.Any() == true)
                {
                    var prereqIds = new List<Guid>();
                    foreach (var prereqCode in structureData.PrerequisiteSubjectCodes)
                    {
                        // We check against processedSubjects which only contains existing ones
                        if (subjectMapByCode.TryGetValue(prereqCode, out var prereqSubject))
                        {
                            prereqIds.Add(prereqSubject.Id);
                        }
                    }

                    if (subject.PrerequisiteSubjectIds == null || !subject.PrerequisiteSubjectIds.SequenceEqual(prereqIds))
                    {
                        subject.PrerequisiteSubjectIds = prereqIds.ToArray();
                        await _subjectRepository.UpdateAsync(subject, cancellationToken);
                    }
                }
            }

            response.SubjectIds.AddRange(processedSubjects.Select(s => s.Id));

            // 8. Link Subjects to Program (CurriculumProgramSubject)
            int linksCreated = 0;
            foreach (var subject in processedSubjects)
            {
                // Idempotency check
                var mappingExists = await _programSubjectRepository.AnyAsync(
                    ps => ps.ProgramId == program.Id && ps.SubjectId == subject.Id,
                    cancellationToken);

                if (!mappingExists)
                {
                    var programSubject = new CurriculumProgramSubject
                    {
                        ProgramId = program.Id,
                        SubjectId = subject.Id
                    };
                    await _programSubjectRepository.AddAsync(programSubject, cancellationToken);
                    linksCreated++;
                }
            }

            _logger.LogInformation("Processed {SubjectCount} existing subjects. Created {LinkCount} new program-subject links.",
                processedSubjects.Count, linksCreated);

            response.Message = $"Curriculum imported successfully. {processedSubjects.Count} existing subjects linked to program.";
            return response;
        }
    }
}