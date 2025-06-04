using Microsoft.AspNetCore.Mvc;
using Plant_API.DB;

namespace Plant_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController(PlantDB db) : ControllerBase
{
    [HttpGet("history")]
    public async Task<IActionResult> History([FromHeader(Name = "UserId")] string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");

        var list = await db.GetSearchHistoryAsync(userId);

        return Ok(list);
    }
}