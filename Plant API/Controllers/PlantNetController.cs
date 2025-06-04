using Microsoft.AspNetCore.Mvc;
using Plant_API.Clients;
using Plant_API.DB;
using static Plant_API.Models.LanguageChoose;

namespace Plant_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantNetController(PlantNetClient plantNetClient, PlantDB db) : ControllerBase
{
    [HttpPost("identify")]
    public async Task<IActionResult> Identify(IFormFile image, [FromHeader(Name = "UserId")] string? userId,
        [FromQuery] Language lang = Language.uk)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Файл пустий.");

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");

        await using var stream = image.OpenReadStream();
        var info = await plantNetClient.Identify(stream, lang.ToString());


        if (info == null || string.IsNullOrWhiteSpace(info.Plant))
            return BadRequest("Не вдалося знайти рослину за фото.");

        await db.AddSearchHistoryAsync(info.Plant, userId);
        await db.CropSearchHistoryAsync(userId);

        return Ok(info);
    }

    [HttpPost("identifyMultiple")]
    public async Task<IActionResult> IdentifyMultiple(
        IFormFile image,
        [FromHeader(Name = "UserId")] string? userId,
        [FromQuery] Language lang = Language.uk,
        [FromQuery] int limit = 3)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Файл пустий.");

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");

        if (limit <= 0 || limit > 3) limit = 3;

        await using var stream = image.OpenReadStream();
        var results = await plantNetClient.IdentifyMultipleAsync(stream, lang.ToString(), limit);

        if (!results.Any())
            return BadRequest("Не вдалося знайти рослину за фото.");

        foreach (var info in results) await db.AddSearchHistoryAsync(info.Plant, userId);
        await db.CropSearchHistoryAsync(userId);

        return Ok(results); // JSON-масив PlantInfo
    }
}