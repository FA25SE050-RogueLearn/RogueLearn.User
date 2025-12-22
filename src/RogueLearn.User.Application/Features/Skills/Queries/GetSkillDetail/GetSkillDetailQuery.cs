using MediatR;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

public class GetSkillDetailQuery : IRequest<SkillDetailDto?>
{
    public Guid AuthUserId { get; set; }
    public Guid SkillId { get; set; }
}