using Microsoft.EntityFrameworkCore;

namespace HouseKeeper.Modules.Households.Persistence;

public sealed class HouseholdsDbContext(
    DbContextOptions<HouseholdsDbContext> options) : DbContext(options)
{
    internal const string Schema = "households";

    internal DbSet<Household> Households => Set<Household>();

    internal DbSet<HouseholdMember> Members => Set<HouseholdMember>();

    internal DbSet<Invitation> Invitations => Set<Invitation>();

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
            entity.HasKey(member => member.MemberId);
            entity.Property(member => member.Subject)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(member => member.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(member => member.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(member => member.JoinedAtUtc)
                .IsRequired();
            entity.HasIndex(member => new { member.HouseholdId, member.Subject })
                .IsUnique();
            entity.HasIndex(member => member.Subject);
            entity.HasOne<Household>()
                .WithMany()
                .HasForeignKey(member => member.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations");
            entity.HasKey(invitation => invitation.Id);
            entity.Property(invitation => invitation.TargetEmailDigest)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(invitation => invitation.TokenDigest)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(invitation => invitation.State)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(invitation => invitation.InvitedAtUtc)
                .IsRequired();
            entity.Property(invitation => invitation.ExpiresAtUtc)
                .IsRequired();
            entity.Property(invitation => invitation.UpdatedAtUtc)
                .IsRequired();
            entity.HasIndex(invitation => invitation.TokenDigest)
                .IsUnique();
            entity.HasIndex(invitation => new { invitation.HouseholdId, invitation.State });
            entity.HasOne<Household>()
                .WithMany()
                .HasForeignKey(invitation => invitation.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
