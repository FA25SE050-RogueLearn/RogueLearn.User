using FluentValidation;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class CurriculumImportDataValidator : AbstractValidator<CurriculumImportData>
{
    public CurriculumImportDataValidator()
    {
        RuleFor(x => x.Program)
            .NotNull()
            .WithMessage("Program information is required.")
            .SetValidator(new CurriculumProgramDataValidator());

        RuleFor(x => x.Version)
            .NotNull()
            .WithMessage("Version information is required.")
            .SetValidator(new CurriculumVersionDataValidator());

        RuleFor(x => x.Subjects)
            .NotEmpty()
            .WithMessage("At least one subject is required.")
            .Must(subjects => subjects.Count <= 100)
            .WithMessage("Cannot have more than 100 subjects.");

        RuleForEach(x => x.Subjects)
            .SetValidator(new SubjectDataValidator());

        RuleFor(x => x.Structure)
            .NotEmpty()
            .WithMessage("Curriculum structure is required.");

        RuleForEach(x => x.Structure)
            .SetValidator(new CurriculumStructureDataValidator());

        RuleForEach(x => x.Syllabi)
            .SetValidator(new SyllabusDataValidator())
            .When(x => x.Syllabi != null);
    }
}

public class CurriculumProgramDataValidator : AbstractValidator<CurriculumProgramData>
{
    public CurriculumProgramDataValidator()
    {
        RuleFor(x => x.ProgramName)
            .NotEmpty()
            .WithMessage("Program name is required.")
            .MaximumLength(255)
            .WithMessage("Program name cannot exceed 255 characters.");

        RuleFor(x => x.ProgramCode)
            .NotEmpty()
            .WithMessage("Program code is required.")
            .MaximumLength(50)
            .WithMessage("Program code cannot exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.DurationYears)
            .GreaterThan(0)
            .WithMessage("Duration years must be greater than 0.")
            .LessThanOrEqualTo(10)
            .WithMessage("Duration years cannot exceed 10.")
            .When(x => x.DurationYears.HasValue);

        RuleFor(x => x.TotalCredits)
            .GreaterThan(0)
            .WithMessage("Total credits must be greater than 0.")
            .LessThanOrEqualTo(300)
            .WithMessage("Total credits cannot exceed 300.")
            .When(x => x.TotalCredits.HasValue);
    }
}

public class CurriculumVersionDataValidator : AbstractValidator<CurriculumVersionData>
{
    public CurriculumVersionDataValidator()
    {
        RuleFor(x => x.VersionCode)
            .NotEmpty()
            .WithMessage("Version code is required.")
            .MaximumLength(50)
            .WithMessage("Version code cannot exceed 50 characters.");

        RuleFor(x => x.EffectiveYear)
            .GreaterThan(2000)
            .WithMessage("Effective year must be greater than 2000.")
            .LessThanOrEqualTo(DateTime.Now.Year + 5)
            .WithMessage("Effective year cannot be more than 5 years in the future.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class SubjectDataValidator : AbstractValidator<SubjectData>
{
    public SubjectDataValidator()
    {
        RuleFor(x => x.SubjectCode)
            .NotEmpty()
            .WithMessage("Subject code is required.")
            .MaximumLength(50)
            .WithMessage("Subject code cannot exceed 50 characters.");

        RuleFor(x => x.SubjectName)
            .NotEmpty()
            .WithMessage("Subject name is required.")
            .MaximumLength(255)
            .WithMessage("Subject name cannot exceed 255 characters.");

        RuleFor(x => x.Credits)
            .GreaterThan(0)
            .WithMessage("Credits must be greater than 0.")
            .LessThanOrEqualTo(10)
            .WithMessage("Credits cannot exceed 10.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class CurriculumStructureDataValidator : AbstractValidator<CurriculumStructureData>
{
    public CurriculumStructureDataValidator()
    {
        RuleFor(x => x.SubjectCode)
            .NotEmpty()
            .WithMessage("Subject code is required.")
            .MaximumLength(50)
            .WithMessage("Subject code cannot exceed 50 characters.");

        RuleFor(x => x.TermNumber)
            .GreaterThan(0)
            .WithMessage("Term number must be greater than 0.")
            .LessThanOrEqualTo(12)
            .WithMessage("Term number cannot exceed 12.");

        RuleFor(x => x.PrerequisiteSubjectCodes)
            .Must(prerequisites => prerequisites == null || prerequisites.Count <= 10)
            .WithMessage("Cannot have more than 10 prerequisites.")
            .When(x => x.PrerequisiteSubjectCodes != null);

        RuleFor(x => x.PrerequisitesText)
            .MaximumLength(500)
            .WithMessage("Prerequisites text cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.PrerequisitesText));
    }
}