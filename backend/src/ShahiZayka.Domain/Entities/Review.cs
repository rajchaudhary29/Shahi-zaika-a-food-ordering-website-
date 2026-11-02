namespace ShahiZayka.Domain.Entities;

public class Review : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public int Rating { get; set; } = 5; // 1 to 5
    public string Comment { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = "images/profile1.jpeg";
    public bool IsApproved { get; set; } = true;
}
