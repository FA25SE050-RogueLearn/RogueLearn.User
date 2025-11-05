using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusQuery : IRequest<GetAcademicStatusResponse?>
{
    public Guid AuthUserId { get; set; }
    public Guid? CurriculumVersionId { get; set; } // Optional - get for specific curriculum or default
}
