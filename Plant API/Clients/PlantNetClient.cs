using System.Net.Http.Headers;
using Newtonsoft.Json;
using Plant_API.Constants;
using Plant_API.Models;

namespace Plant_API.Clients;

public class PlantNetClient(HttpClient httpClient, WikiClient wikiClient)
{
    public async Task<PlantInfo> Identify(Stream imageStream, string lang)
    {
        var content = new MultipartFormDataContent();
        var img = new StreamContent(imageStream);
        img.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(img, "images", "photo.jpg");

        var url = $"{DefaultConstants.PlantNetUrl}v2/identify/all" +
                  $"?api-key={DefaultConstants.PlantNetApiKey}" +
                  $"&lang={lang}&nb-results=1";

        var resp = await httpClient.PostAsync(url, content);
        var body = await resp.Content.ReadAsStringAsync();

        var pn = JsonConvert.DeserializeObject<PlantNetResponse>(body);
        var best = pn?.Results?.OrderByDescending(r => r.Score).FirstOrDefault();
        if (best is null) return null!;

        var sci = best.Species.ScientificNameWithoutAuthor;
        if (string.IsNullOrWhiteSpace(sci)) return null!;

        var common = best.Species.CommonNames?.FirstOrDefault() ?? sci;
        var (descUpper, extract) = await wikiClient.GetSummary(sci, lang);

        var wikiUrl = $"https://{lang}.wikipedia.org/wiki/{Uri.EscapeDataString(sci)}";


        return new PlantInfo
        {
            Plant = common,
            Description = descUpper,
            Summary = extract,
            WikiUrl = wikiUrl
        };
    }

    public async Task<List<PlantInfo>> IdentifyMultipleAsync(Stream imageStream, string lang, int maxResults = 3)
    {
        using var content = new MultipartFormDataContent();
        var img = new StreamContent(imageStream);
        img.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(img, "images", "photo.jpg");

        var url = $"{DefaultConstants.PlantNetUrl}v2/identify/all" +
                  $"?api-key={DefaultConstants.PlantNetApiKey}" +
                  $"&lang={lang}&nb-results={maxResults}";

        var resp = await httpClient.PostAsync(url, content);
        var body = await resp.Content.ReadAsStringAsync();

        var pn = JsonConvert.DeserializeObject<PlantNetResponse>(body);
        var ordered = pn?.Results?.OrderByDescending(r => r.Score).Take(maxResults).ToList() ??
                      new List<PlantNetResult>();
        if (!ordered.Any()) return new List<PlantInfo>();

        var list = new List<PlantInfo>();
        foreach (var res in ordered)
        {
            var sci = res.Species.ScientificNameWithoutAuthor;
            var common = res.Species.CommonNames.FirstOrDefault() ?? sci;
            var (descUpper, extract) = await wikiClient.GetSummary(sci, lang);
            var wikiUrl = $"https://{lang}.wikipedia.org/wiki/{Uri.EscapeDataString(sci)}";


            list.Add(new PlantInfo
            {
                Plant = common,
                Description = descUpper,
                Summary = extract,
                WikiUrl = wikiUrl
            });
        }

        return list;
    }
}