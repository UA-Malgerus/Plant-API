using Microsoft.AspNetCore.Mvc;
using Plant_API.DB;

namespace Plant_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavouritesController(PlantDB db) : ControllerBase
{
    [HttpPost("favourite")]
    public async Task<IActionResult> AddFavourite([FromQuery] string plantName,
        [FromHeader(Name = "UserId")] string? userId)

    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");
        if (string.IsNullOrWhiteSpace(plantName))
            return BadRequest("Не вказано рослину.");

        try
        {
            await db.AddFavouriteAsync(plantName, userId);
            return Ok("Рослину додано до улюблених.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("favourites")]
    public async Task<IActionResult> Favourites([FromHeader(Name = "UserId")] string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");

        var list = await db.GetFavouritesAsync(userId);

        return Ok(list);
    }

    [HttpDelete("favourite")]
    public async Task<IActionResult> DeleteFavourite([FromQuery] string? plantName,
        [FromHeader(Name = "UserId")] string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");
        if (string.IsNullOrWhiteSpace(plantName))
            return BadRequest("Не вказано рослину для видалення.");
        try
        {
            await db.DeleteFavouriteAsync(plantName, userId);
            return Ok("Рослину видалено з улюблених.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}