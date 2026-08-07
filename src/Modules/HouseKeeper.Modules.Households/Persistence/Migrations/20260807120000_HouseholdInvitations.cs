using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HouseKeeper.Modules.Households.Persistence.Migrations;

[DbContext(typeof(HouseholdsDbContext))]
[Migration("20260807120000_HouseholdInvitations")]
public sealed class HouseholdInvitations : Migration
{
    private static readonly string[] HouseholdSubjectColumns = ["HouseholdId", "Subject"];
    private static readonly string[] HouseholdStateColumns = ["HouseholdId", "State"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_household_members",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.AddColumn<Guid>(
            name: "MemberId",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()");

        migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Active");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RemovedAtUtc",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_household_members",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "IX_household_members_HouseholdId_Subject",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            columns: HouseholdSubjectColumns,
            unique: true);

        migrationBuilder.CreateTable(
            name: "invitations",
            schema: HouseholdsDbContext.Schema,
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                InviterMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetEmailDigest = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                TokenDigest = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                State = table.Column<string>(
                    type: "character varying(32)",
                    maxLength: 32,
                    nullable: false),
                InvitedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                AcceptedByMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                AcceptedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                RevokedAtUtc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invitations", value => value.Id);
                table.ForeignKey(
                    name: "FK_invitations_households_HouseholdId",
                    column: value => value.HouseholdId,
                    principalSchema: HouseholdsDbContext.Schema,
                    principalTable: "households",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_invitations_HouseholdId_State",
            schema: HouseholdsDbContext.Schema,
            table: "invitations",
            columns: HouseholdStateColumns);

        migrationBuilder.CreateIndex(
            name: "IX_invitations_TokenDigest",
            schema: HouseholdsDbContext.Schema,
            table: "invitations",
            column: "TokenDigest",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "invitations",
            schema: HouseholdsDbContext.Schema);

        migrationBuilder.DropIndex(
            name: "IX_household_members_HouseholdId_Subject",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.DropPrimaryKey(
            name: "PK_household_members",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "MemberId",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "RemovedAtUtc",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "Status",
            schema: HouseholdsDbContext.Schema,
            table: "household_members");

        migrationBuilder.AddPrimaryKey(
            name: "PK_household_members",
            schema: HouseholdsDbContext.Schema,
            table: "household_members",
            columns: HouseholdSubjectColumns);
    }
}
