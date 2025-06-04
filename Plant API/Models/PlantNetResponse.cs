using Newtonsoft.Json;

namespace Plant_API.Models;

public class PlantNetResponse
{
    [JsonProperty("results")] public PlantNetResult[]? Results { get; set; }
}

public class PlantNetResult
{   
    [JsonProperty("score")] public double Score { get; set; }

    [JsonProperty("species")] public PlantSpecies? Species { get; set; }
}

public class PlantSpecies
{
    [JsonProperty("scientificNameWithoutAuthor")]
    public string? ScientificNameWithoutAuthor { get; set; }

    [JsonProperty("commonNames")] public string[] CommonNames { get; set; } = Array.Empty<string>();
}