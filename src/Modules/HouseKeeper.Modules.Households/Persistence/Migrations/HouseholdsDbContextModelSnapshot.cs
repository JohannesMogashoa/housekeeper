using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HouseKeeper.Modules.Households.Persistence.Migrations;

[DbContext(typeof(HouseholdsDbContext))]
public sealed class HouseholdsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema(HouseholdsDbContext.Schema)
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("HouseKeeper.Modules.Households.Persistence.Household", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entity.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(120)
                .HasColumnType("character varying(120)");

            entity.HasKey("Id");
            entity.ToTable("households", HouseholdsDbContext.Schema);
        });

        modelBuilder.Entity("HouseKeeper.Modules.Households.Persistence.HouseholdMember", entity =>
        {
            entity.Property<Guid>("MemberId")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entity.Property<Guid>("HouseholdId")
                .HasColumnType("uuid");

            entity.Property<string>("Subject")
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            entity.Property<DateTimeOffset>("JoinedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("Role")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.Property<DateTimeOffset?>("RemovedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.HasKey("MemberId");
            entity.HasIndex("HouseholdId", "Subject")
                .IsUnique();
            entity.HasIndex("Subject");
            entity.ToTable("household_members", HouseholdsDbContext.Schema);
        });

        modelBuilder.Entity("HouseKeeper.Modules.Households.Persistence.HouseholdMember", entity =>
        {
            entity.HasOne("HouseKeeper.Modules.Households.Persistence.Household", null)
                .WithMany()
                .HasForeignKey("HouseholdId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("HouseKeeper.Modules.Households.Persistence.Invitation", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entity.Property<Guid?>("AcceptedByMemberId")
                .HasColumnType("uuid");

            entity.Property<DateTimeOffset?>("AcceptedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<DateTimeOffset>("ExpiresAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<Guid>("HouseholdId")
                .HasColumnType("uuid");

            entity.Property<Guid>("InviterMemberId")
                .HasColumnType("uuid");

            entity.Property<DateTimeOffset>("InvitedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<DateTimeOffset?>("RevokedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("State")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.Property<string>("TargetEmailDigest")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<string>("TokenDigest")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<DateTimeOffset>("UpdatedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.HasKey("Id");
            entity.HasIndex("HouseholdId", "State");
            entity.HasIndex("TokenDigest").IsUnique();
            entity.ToTable("invitations", HouseholdsDbContext.Schema);
        });

        modelBuilder.Entity("HouseKeeper.Modules.Households.Persistence.Invitation", entity =>
        {
            entity.HasOne("HouseKeeper.Modules.Households.Persistence.Household", null)
                .WithMany()
                .HasForeignKey("HouseholdId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
