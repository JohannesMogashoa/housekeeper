using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HouseKeeper.Modules.Households.Persistence.Migrations;

[DbContext(typeof(HouseholdsDbContext))]
[Migration("20260718211500_InitialHouseholds")]
public sealed class InitialHouseholds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: HouseholdsDbContext.Schema);

        migrationBuilder.CreateTable(
            name: "households",
            schema: HouseholdsDbContext.Schema,
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_households", value => value.Id);
            });

        migrationBuilder.CreateTable(
            name: "household_members",
            schema: HouseholdsDbContext.Schema,
            columns: table => new
            {
                HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                Subject = table.Column<string>(
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false),
                Role = table.Column<string>(
                    type: "character varying(32)",
                    maxLength: 32,
                    nullable: false),
                JoinedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_household_members",
                    value => new { value.HouseholdId, value.Subject });
                table.ForeignKey(
                    name: "FK_household_members_households_HouseholdId",
                    column: value => value.HouseholdId,
                    principalSchema: HouseholdsDbContext.Schema,
                    principalTable: "households",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_household_members_Subject",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            column: "Subject");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "household_members",
            schema: HouseholdsDbContext.Schema);

        migrationBuilder.DropTable(
            name: "households",
            schema: HouseholdsDbContext.Schema);
    }
}
