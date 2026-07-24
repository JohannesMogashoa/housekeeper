using Microsoft.EntityFrameworkCore;

namespace HouseKeeper.Modules.Households.Persistence;

public sealed class HouseholdsDbContext(
    DbContextOptions<HouseholdsDbContext> options) : DbContext(options)
{
    internal const string Schema = "households";

    internal DbSet<Household> Households => Set<Household>();

    internal DbSet<HouseholdMember> Members => Set<HouseholdMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Household>(entity =>
        {
            entity.ToTable("households");
            entity.HasKey(household => household.Id);
            entity.Property(household => household.Name)
                .HasMaxLength(Domain.HouseholdName.MaxLength)
                .IsRequired();
            entity.Property(household => household.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.ToTable("household_members");
            entity.HasKey(member => new { member.HouseholdId, member.Subject });
            entity.Property(member => member.Subject)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(member => member.Role)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(member => member.JoinedAtUtc)
                .IsRequired();
            entity.HasIndex(member => member.Subject);
            entity.HasOne<Household>()
                .WithMany()
                .HasForeignKey(member => member.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
