using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Specialization.Commands.SetUserSpecialization;

public class SetUserSpecializationCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    public Guid ClassId { get; set; }
}