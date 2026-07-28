using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class DataContext: DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }
    
    public DbSet<Values> ValuesRecord { get; set; }
    public DbSet<Results> ResultsRecord { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Values>()
            .HasOne(v => v.Results)
            .WithMany(r => r.Values)
            .HasForeignKey(v => v.ResultsId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Results>()
            .HasIndex(r => r.FileName)
            .IsUnique();
    }
}