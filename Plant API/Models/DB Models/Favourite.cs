namespace Plant_API.Models;

public class Favourite
{
    public int Id { get; set; }
    public required string FavPlantName { get; set; }
    public DateTime AddedAt { get; set; }
}