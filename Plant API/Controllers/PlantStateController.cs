using Microsoft.AspNetCore.Mvc;
using Plant_API.Clients;
using static Plant_API.Models.LanguageChoose;

namespace Plant_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantStateController(PlantHealthAnalyzer plantHealthAnalyzer) : ControllerBase
{
    [HttpPost("analyzePlantHealth")]
    public async Task<IActionResult> AnalyzePlantHealthWithMscc(IFormFile? image,
        [FromQuery] Language lang = Language.uk)
    {
        if (image == null || image.Length == 0) return BadRequest("Файл зображення не надано.");

        try
        {
            await using var imageStream = image.OpenReadStream();
            var analysisResult =
                await plantHealthAnalyzer.AnalyzePlantHealthAsync(imageStream, lang.ToString());

            return Ok(analysisResult);
        }
        catch (ArgumentNullException ex) when (ex.ParamName == "apiKey")
        {
            Console.WriteLine($"Помилка конфігурації: {ex.Message}");
            return BadRequest("Помилка конфігурації сервера: API ключ не знайдено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Загальна помилка в AnalyzePlantHealthWithMscc: {ex.Message}");
            return BadRequest(ex.Message);
        }
    }
}