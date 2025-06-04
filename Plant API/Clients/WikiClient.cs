using Newtonsoft.Json;
using Plant_API.Models;

namespace Plant_API.Clients;

public class WikiClient(HttpClient httpClient)
{
    public async Task<(string description, string extract)> GetSummary(string scientific, string lang)
    {
        var url = $"https://{lang}.wikipedia.org/api/rest_v1/page/summary/" +
                  $"{Uri.EscapeDataString(scientific)}";
        var resp = await httpClient.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return (string.Empty, string.Empty);


        var body = await resp.Content.ReadAsStringAsync();
        var wiki = JsonConvert.DeserializeObject<WikiResponse>(body);
        string descUpper;
        if (wiki?.Description is null || wiki.Description.Length == 0)
        {
            descUpper = string.Empty;
        }
        else
        {
            var lowercasedInput = wiki?.Description.ToLower();
            descUpper = char.ToUpper(lowercasedInput![0]) + lowercasedInput.Substring(1) + ".";
        }

        var extract = wiki?.Extract ?? string.Empty;
        return (descUpper, extract);
    }


    public async Task<PlantInfo> GetSummaryAsync(string name, string lang)
    {
        var (descUpper, extract) = await GetSummary(name, lang);

        var wikiUrl = $"https://{lang}.wikipedia.org/wiki/{Uri.EscapeDataString(name)}";
        return new PlantInfo
        {
            Plant = name,
            Description = descUpper,
            Summary = extract,
            WikiUrl = wikiUrl
        };
    }
}