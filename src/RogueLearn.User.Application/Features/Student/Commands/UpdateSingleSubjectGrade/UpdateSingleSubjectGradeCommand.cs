using MediatR;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Student.Commands.UpdateSingleSubjectGrade;

public class UpdateSingleSubjectGradeCommand : IRequest<UpdateSingleSubjectGradeResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    public Guid SubjectId { get; set; }

    public double Grade { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubjectEnrollmentStatus Status { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
}