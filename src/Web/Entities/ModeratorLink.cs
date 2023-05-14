namespace Forum.Entities;

// This entity links users to which categories they are can moderate
public class ModeratorLink
{
    public int Id { get; set; }
    public int ApplicationUserId { get; set; }
    public int CategoryId { get; set; }
}