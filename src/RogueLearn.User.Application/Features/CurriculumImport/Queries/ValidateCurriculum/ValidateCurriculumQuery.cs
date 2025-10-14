using MediatR;
using System.ComponentModel.DataAnnotations;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQuery : IRequest<ValidateCurriculumResponse>
{
    [Required(ErrorMessage = "The rawText field is required.")]
    public string RawText { get; set; } = string.Empty;
}