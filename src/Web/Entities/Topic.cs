using System.ComponentModel.DataAnnotations.Schema;

namespace Whirl1.Entities;

public class Topic
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public long CreatedTS { get; set; }
    public string Name { get; set; }
    public int? MostRecentCommentId { get; set; }
    public int CategoryId { get; set; }

    [NotMapped]
    public Category? Category { get; set; }

    [NotMapped]
    public List<Post> Posts { get; set; }

    [NotMapped]
    public Post? MostRecentCommentedPost { get; set; }
}