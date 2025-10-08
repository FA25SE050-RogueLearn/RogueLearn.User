// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Api/Controllers/QuestLinesController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.Quests.Application.Features.QuestLines.Commands;

namespace RogueLearn.Quests.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protect all endpoints in this controller
public class QuestLinesController : ControllerBase
{
	private readonly IMediator _mediator;

	public QuestLinesController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpPost("generate-from-curriculum")]
	[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> GenerateFromCurriculum([FromBody] GenerateFromCurriculumCommand command)
	{
		var questLineId = await _mediator.Send(command);
		return CreatedAtAction(nameof(GenerateFromCurriculum), new { id = questLineId }, questLineId);
	}
}