namespace Plant_API.Models;

public class SearchHistory
{
    public int Id { get; set; }
    public required string PlantName { get; set; }
    public DateTime CreatedAt { get; set; }
}