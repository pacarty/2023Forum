using System.ComponentModel.DataAnnotations.Schema;

namespace Forum.Entities;

public class Comment
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public long CreatedTS { get; set; }
    public string Content { get; set; }
    public int PostId { get; set; }
    public int TopicId { get; set; }
    public int CategoryId { get; set; }
    public int ApplicationUserId { get; set; }

    [NotMapped]
    public Post? Post { get; set; }

    [NotMapped]
    public ApplicationUser? ApplicationUser { get; set; }

    [NotMapped]
    public string? HowLongAgo { get; set; }
}