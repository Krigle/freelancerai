using Microsoft.EntityFrameworkCore;
using FreelanceFinderAI.Models;

namespace FreelanceFinderAI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalText).IsRequired();
            entity.Property(e => e.ExtractedJson).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}

