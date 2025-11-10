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
using HtmlAgilityPack;
using System.Text;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum
{
    public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
    {
        private readonly ICurriculumProgramRepository _curriculumProgramRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
        private readonly ICurriculumImportStorage _storage;
        private readonly CurriculumImportDataValidator _validator;
        private readonly ILogger<ImportCurriculumCommandHandler> _logger;
        private readonly ICurriculumExtractionPlugin _curriculumPlugin;
        private readonly IHtmlCleaningService _htmlCleaningService;

        public ImportCurriculumCommandHandler(
            ICurriculumProgramRepository curriculumProgramRepository,
            ISubjectRepository subjectRepository,
            ICurriculumProgramSubjectRepository programSubjectRepository,
            ICurriculumImportStorage storage,
            CurriculumImportDataValidator validator,
            ILogger<ImportCurriculumCommandHandler> logger,
            ICurriculumExtractionPlugin curriculumPlugin,
            IHtmlCleaningService htmlCleaningService)
        {
            _curriculumProgramRepository = curriculumProgramRepository;
            _subjectRepository = subjectRepository;
            _programSubjectRepository = programSubjectRepository;
            _storage = storage;
            _validator = validator;
            _logger = logger;
            _curriculumPlugin = curriculumPlugin;
            _htmlCleaningService = htmlCleaningService;
        }

        public async Task<ImportCurriculumResponse> Handle(ImportCurriculumCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting curriculum import from text");

            var cleanText = _htmlCleaningService.ExtractCleanTextFromHtml(request.RawText);
            if (string.IsNullOrWhiteSpace(cleanText))
            {
                _logger.LogError("HTML cleaning service failed to extract meaningful text from the provided HTML.");
                throw new Exceptions.BadRequestException("Failed to extract meaningful text from the provided HTML.");
            }

            var extractedJson = await _curriculumPlugin.ExtractCurriculumJsonAsync(cleanText, cancellationToken);
            if (string.IsNullOrEmpty(extractedJson))
            {
                _logger.LogError("AI plugin failed to extract curriculum JSON from the cleaned text.");
                throw new Exceptions.BadRequestException("Failed to extract curriculum data from the provided text");
            }

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
                throw new Exceptions.BadRequestException("AI returned invalid JSON format.");
            }

            if (curriculumData == null)
            {
                throw new Exceptions.BadRequestException("No curriculum data was extracted");
            }

            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new Exceptions.ValidationException(validationResult.Errors);
            }

            var response = new ImportCurriculumResponse { IsSuccess = true };

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
            response.CurriculumProgramId = program.Id;

            var subjectEntities = new List<Subject>();

            // Construct an effective date from the version info to use for UpdatedAt
            DateTimeOffset effectiveDate = new DateTimeOffset(curriculumData.Version.EffectiveYear, 1, 1, 0, 0, 0, TimeSpan.Zero);

            foreach (var subjectData in curriculumData.Subjects.Where(s => s.IsPlaceholder == false))
            {
                var subject = new Subject
                {
                    SubjectCode = subjectData.SubjectCode,
                    SubjectName = subjectData.SubjectName,
                    Credits = subjectData.Credits,
                    Description = subjectData.Description,
                    Semester = curriculumData.Structure.FirstOrDefault(s => s.SubjectCode == subjectData.SubjectCode)?.TermNumber ?? 0,
                    Content = null,
                    // MODIFIED: Set UpdatedAt to the effective date of the curriculum import.
                    UpdatedAt = effectiveDate
                };
                subjectEntities.Add(subject);
            }

            var createdSubjects = new List<Subject>();
            foreach (var subjectEntity in subjectEntities)
            {
                // MODIFIED: Lookup is now only by SubjectCode.
                var existing = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectEntity.SubjectCode, cancellationToken);
                if (existing != null)
                {
                    existing.SubjectName = subjectEntity.SubjectName;
                    existing.Credits = subjectEntity.Credits;
                    existing.Description = subjectEntity.Description;
                    existing.Semester = subjectEntity.Semester;
                    existing.UpdatedAt = subjectEntity.UpdatedAt;
                    var updated = await _subjectRepository.UpdateAsync(existing, cancellationToken);
                    createdSubjects.Add(updated);
                }
                else
                {
                    var created = await _subjectRepository.AddAsync(subjectEntity, cancellationToken);
                    createdSubjects.Add(created);
                }
            }
            var subjectMapByCode = createdSubjects.ToDictionary(s => s.SubjectCode, s => s);

            foreach (var createdSubject in createdSubjects)
            {
                var structureData = curriculumData.Structure.FirstOrDefault(s => s.SubjectCode == createdSubject.SubjectCode);
                if (structureData?.PrerequisiteSubjectCodes?.Any() == true)
                {
                    var prereqIds = new List<Guid>();

                    foreach (var prereqCode in structureData.PrerequisiteSubjectCodes)
                    {
                        if (subjectMapByCode.TryGetValue(prereqCode, out var prereqSubject))
                        {
                            prereqIds.Add(prereqSubject.Id);
                        }
                    }
                    createdSubject.PrerequisiteSubjectIds = prereqIds.ToArray();
                    await _subjectRepository.UpdateAsync(createdSubject, cancellationToken);
                }
            }
            response.SubjectIds.AddRange(createdSubjects.Select(s => s.Id));


            foreach (var subject in createdSubjects)
            {
                var mappingExists = await _programSubjectRepository.AnyAsync(ps => ps.ProgramId == program.Id && ps.SubjectId == subject.Id, cancellationToken);
                if (!mappingExists)
                {
                    var programSubject = new CurriculumProgramSubject
                    {
                        ProgramId = program.Id,
                        SubjectId = subject.Id
                    };
                    await _programSubjectRepository.AddAsync(programSubject, cancellationToken);
                }
            }
            _logger.LogInformation("Created {Count} program-subject mappings.", createdSubjects.Count);


            response.Message = "Curriculum imported successfully";
            return response;
        }
    }
}