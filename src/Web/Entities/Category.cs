using System.ComponentModel.DataAnnotations.Schema;

namespace Forum.Entities;

public class Category
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public long CreatedTS { get; set; }
    public string Name { get; set; }

    [NotMapped]
    public List<Topic> Topics { get; set; }
}