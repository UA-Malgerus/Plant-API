namespace Plant_API.Constants;

public class DefaultConstants
{
    public const string PlantNetUrl = "https://my-api.plantnet.org/";
    public static readonly string? PlantNetApiKey = Environment.GetEnvironmentVariable("PLANT_API_KEY");
    public static readonly string? DefaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
    public static readonly string? GoogleAIKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
}