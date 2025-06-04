using Plant_API.Constants;
using Mscc.GenerativeAI;
using Plant_API.Models;

namespace Plant_API.Clients;

public class PlantHealthAnalyzer
{
    private readonly GenerativeModel _geminiModel;

    public PlantHealthAnalyzer()
    {
        var googleAI = new GoogleAI(DefaultConstants.GoogleAIKey);
        _geminiModel = googleAI.GenerativeModel(Model.Gemini15Flash);
    }

    public async Task<PlantInfo> AnalyzePlantHealthAsync(Stream imageStream, string language)
    {
        var plantInfoResult = new PlantInfo();
        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await imageStream.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        var base64Image = Convert.ToBase64String(imageBytes);

        string selectedPrompt;
        switch (language.ToLowerInvariant())
        {
            case "en":
                selectedPrompt = PromptConstants.PlantHealthPromptEn;
                break;
            default:
                selectedPrompt = PromptConstants.PlantHealthPromptUk;
                break;
        }

        var parts = new List<IPart>
        {
            new TextData { Text = selectedPrompt }, new InlineData { MimeType = "image/jpeg", Data = base64Image }
        };

        try
        {
            var response = await _geminiModel.GenerateContent(parts);

            string? resultText = null;
            if (!string.IsNullOrEmpty(response.Text))
                resultText = response.Text.TrimEnd();
            else if (response.Candidates != null && response.Candidates.Any())
                resultText = response.Candidates.FirstOrDefault()?
                    .Content?.Parts.FirstOrDefault()?
                    .Text.TrimEnd();

            if (string.IsNullOrEmpty(resultText))
            {
                Console.WriteLine($"{resultText} is Null or Empty.");
                plantInfoResult.PlantHealthAnalysis = "Не вдалося отримати аналіз стану рослини.";
            }
            else
            {
                plantInfoResult.PlantHealthAnalysis = resultText;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка під час аналізу зображення: {ex.Message}");
            plantInfoResult.PlantHealthAnalysis = $"Помилка під час аналізу зображення.";
        }

        return plantInfoResult;
    }
}