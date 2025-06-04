using Microsoft.AspNetCore.Mvc;
using Plant_API.Clients;
using Plant_API.DB;
using Plant_API.Constants;
using static Plant_API.Models.LanguageChoose;

namespace Plant_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WikiController(WikiClient wikiClient, PlantDB db) : ControllerBase
{
    [HttpGet("{plantName}")]
    public async Task<IActionResult> Get(string? plantName, [FromHeader(Name = "UserId")] string? userId,
        [FromQuery] Language lang = Language.uk)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Заголовок UserId не передано.");
        if (string.IsNullOrWhiteSpace(plantName))
            return BadRequest("Не вказано рослину.");

        var lowercasedInput = plantName.ToLower();
        var nameUpper = char.ToUpper(lowercasedInput[0]) + lowercasedInput.Substring(1);

        var info = await wikiClient.GetSummaryAsync(nameUpper, lang.ToString());
        if (!PlantConstants.KeywordRegex.IsMatch(info.Summary ?? ""))
            return BadRequest("Не знайдено інформації про таку рослину.");

        await db.AddSearchHistoryAsync(info.Plant, userId);
        await db.CropSearchHistoryAsync(userId);

        return Ok(info);
    }
}