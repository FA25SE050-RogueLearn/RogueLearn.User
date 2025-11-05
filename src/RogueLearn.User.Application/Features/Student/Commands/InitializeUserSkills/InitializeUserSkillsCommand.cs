using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Features.Student.Commands.InitializeUserSkills
{
    public class InitializeUserSkillsCommand : IRequest<InitializeUserSkillsResponse>
    {
        public Guid AuthUserId { get; set; }
        public Guid CurriculumVersionId { get; set; }
    }
}
