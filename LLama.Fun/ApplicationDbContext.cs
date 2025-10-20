using Microsoft.EntityFrameworkCore;

namespace LLama.Fun;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    // Default constructor for production use
    public ApplicationDbContext()
    {
    }

    // Constructor for testing with custom options
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured (for testing)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=llama.db");
        }
    }
}
