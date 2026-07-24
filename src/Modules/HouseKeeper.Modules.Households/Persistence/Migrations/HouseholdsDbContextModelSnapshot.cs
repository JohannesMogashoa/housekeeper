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

            entity.HasKey("HouseholdId", "Subject");
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
    }
}
