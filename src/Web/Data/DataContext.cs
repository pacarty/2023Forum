using Whirl1.Entities;

using Microsoft.EntityFrameworkCore;


namespace Whirl1.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<ModeratorLink> ModeratorLinks { get; set; }
    public DbSet<BannedLink> BannedLinks { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
}