namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;

public class RouteDto
{
    public Guid Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}