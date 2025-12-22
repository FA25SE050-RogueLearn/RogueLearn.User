using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitCodingActivity;

public class SubmitCodingActivityCommand : IRequest<SubmitCodingActivityResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    [JsonIgnore]
    public Guid QuestId { get; set; }
    [JsonIgnore]
    public Guid StepId { get; set; }
    [JsonIgnore]
    public Guid ActivityId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public class SubmitCodingActivityResponse
{
    public Guid SubmissionId { get; set; }
    public bool IsPassed { get; set; }
    public int Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
}