using G2CCRMPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Data;

public class G2CCrmDbContext : DbContext
{
    public G2CCrmDbContext(DbContextOptions<G2CCrmDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<IssueRequest> IssueRequests => Set<IssueRequest>();
    public DbSet<TrackRequest> TrackRequests => Set<TrackRequest>();
    public DbSet<CitizenFeedback> CitizenFeedbacks => Set<CitizenFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User → raised issues
        modelBuilder.Entity<User>()
            .HasMany(u => u.RaisedIssues)
            .WithOne(i => i.Citizen)
            .HasForeignKey(i => i.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        // User → assigned issues
        modelBuilder.Entity<User>()
            .HasMany(u => u.AssignedIssues)
            .WithOne(i => i.AssignedTo)
            .HasForeignKey(i => i.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        // One feedback per issue (unique index)
        modelBuilder.Entity<CitizenFeedback>()
            .HasIndex(f => f.IssueRequestId)
            .IsUnique();

        // Default values handled by DB — tell EF they are computed
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.Status)
            .HasDefaultValue("Submitted");

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.Priority)
            .HasDefaultValue("Medium");

        modelBuilder.Entity<IssueRequest>()
            .Property(i => i.IsBreached)
            .HasDefaultValue(false);

        modelBuilder.Entity<TrackRequest>()
            .Property(t => t.ChangedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<CitizenFeedback>()
            .Property(f => f.SubmittedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}