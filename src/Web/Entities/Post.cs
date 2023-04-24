using System.ComponentModel.DataAnnotations.Schema;

namespace Whirl1.Entities;

public class Post
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public long CreatedTS { get; set; }
    public int? FirstCommentId { get; set; }
    public string Title { get; set; }
    public int? MostRecentCommentId { get; set; }
    public long? MostRecentCommentTS { get; set; }
    public int TopicId { get; set; }
    public int CategoryId { get; set; }
    public int ApplicationUserId { get; set; }

    [NotMapped]
    public Comment? MostRecentComment { get; set; }

    [NotMapped]
    public int? LastPage { get; set; }

    [NotMapped]
    public Topic? Topic { get; set; }

    [NotMapped]
    public ApplicationUser? ApplicationUser { get; set; }

    [NotMapped]
    public List<Comment> Comments { get; set; }
}