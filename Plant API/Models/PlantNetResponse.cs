using Newtonsoft.Json;

namespace Plant_API.Models;

public class PlantNetResponse
{
    [JsonProperty("results")] public required PlantNetResult[] Results { get; set; }
}

public class PlantNetResult
{
    [JsonProperty("score")] public double Score { get; set; }

    [JsonProperty("species")] public required PlantSpecies Species { get; set; }
}

public class PlantSpecies
{
    [JsonProperty("scientificNameWithoutAuthor")]
    public required string ScientificNameWithoutAuthor { get; set; }

    [JsonProperty("commonNames")] public string[] CommonNames { get; set; } = Array.Empty<string>();
}