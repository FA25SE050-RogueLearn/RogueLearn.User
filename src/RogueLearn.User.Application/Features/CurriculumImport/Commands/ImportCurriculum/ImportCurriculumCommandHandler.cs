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

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum
{
    public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
    {
        private readonly ICurriculumProgramRepository _curriculumProgramRepository;

        private readonly ISubjectRepository _subjectRepository;
        private readonly ICurriculumProgramSubjectRepository _programSubjectRepository; // NEW

        private readonly ICurriculumImportStorage _storage;
        private readonly CurriculumImportDataValidator _validator;
        private readonly ILogger<ImportCurriculumCommandHandler> _logger;
        private readonly IFlmExtractionPlugin _flmPlugin;

        public ImportCurriculumCommandHandler(
            ICurriculumProgramRepository curriculumProgramRepository,

            ISubjectRepository subjectRepository,
            ICurriculumProgramSubjectRepository programSubjectRepository, // NEW

            ICurriculumImportStorage curriculumImportStorage,
            CurriculumImportDataValidator validator,
            ILogger<ImportCurriculumCommandHandler> logger,
            IFlmExtractionPlugin flmPlugin)
        {
            _curriculumProgramRepository = curriculumProgramRepository;

            _subjectRepository = subjectRepository;
            _programSubjectRepository = programSubjectRepository; // NEW
            _storage = curriculumImportStorage;
            _validator = validator;
            _logger = logger;
            _flmPlugin = flmPlugin;
        }

        public async Task<ImportCurriculumResponse> Handle(ImportCurriculumCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting curriculum import from text");

            // AI Extraction, Caching, and Validation (logic remains the same)
            // ... (For brevity, assuming this logic correctly produces a validated 'curriculumData' object)
            var extractedJson = await _flmPlugin.ExtractCurriculumJsonAsync(request.RawText, cancellationToken);
            if (string.IsNullOrEmpty(extractedJson))
            {
                throw new Exceptions.BadRequestException("Failed to extract curriculum data from the provided text");
            }
            var curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (curriculumData == null)
            {
                throw new Exceptions.BadRequestException("No curriculum data was extracted");
            }
            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new Exceptions.ValidationException(validationResult.Errors);
            }

            // --- REWRITTEN PERSISTENCE LOGIC ---

            var response = new ImportCurriculumResponse { IsSuccess = true };

            // 1. Create or get CurriculumProgram
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

            // 2. Create or Update all Subjects in a batch
            foreach (var subjectData in curriculumData.Subjects)
            {

                var subject = new Subject
                {
                    SubjectCode = subjectData.SubjectCode,
                    SubjectName = subjectData.SubjectName,
                    Credits = subjectData.Credits,
                    Description = subjectData.Description,
                    Version = curriculumData.Version.VersionCode,
                    Semester = curriculumData.Structure.FirstOrDefault(s => s.SubjectCode == subjectData.SubjectCode)?.TermNumber ?? 0,
                    // Prerequisites will be mapped in a later step after all subjects have IDs
                };
                subjectEntities.Add(subject);
            }

            // For now, we'll add them one-by-one to get their IDs. A bulk upsert would be more efficient in a real scenario.
            var createdSubjects = new List<Subject>();
            foreach (var subjectEntity in subjectEntities)

            {
                var existing = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectEntity.SubjectCode && s.Version == subjectEntity.Version, cancellationToken);
                if (existing != null)
                {
                    existing.SubjectName = subjectEntity.SubjectName;
                    existing.Credits = subjectEntity.Credits;
                    existing.Description = subjectEntity.Description;
                    existing.Semester = subjectEntity.Semester;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
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

            // 3. Update prerequisites now that all subjects have IDs
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


            // 4. Create the mappings in curriculum_program_subjects
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