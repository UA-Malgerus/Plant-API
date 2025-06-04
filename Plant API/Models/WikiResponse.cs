using Newtonsoft.Json;

namespace Plant_API.Models;

public class WikiResponse
{
    [JsonProperty("description")] public string? Description { get; set; }

    [JsonProperty("extract")] public string? Extract { get; set; }
}