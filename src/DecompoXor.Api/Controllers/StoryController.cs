using Microsoft.AspNetCore.Mvc;
using DecompoXor.Domain.Entities;
using DecompoXor.Application.Features.StoryDecomposition;

namespace DecompoXor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoryController : ControllerBase
{
    private readonly StoryDecompositionService _decompositionService;

    public StoryController(StoryDecompositionService decompositionService)
    {
        _decompositionService = decompositionService;
    }

    [HttpPost("decompose")]
    public async Task<IActionResult> DecomposeStoryAsync([FromBody] Story story, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _decompositionService.DecomposeStoryAsync(story, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway, title: "The configured LLM returned an invalid story analysis.");
        }
    }
}
