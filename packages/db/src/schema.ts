import {
  foreignKey,
  index,
  pgSchema,
  primaryKey,
  timestamp,
  uuid,
  varchar,
} from "drizzle-orm/pg-core";

// Keep the physical schema compatible with the existing EF Core migration.
// This prevents the TypeScript port from silently creating parallel public
// tables for data that already lives under PostgreSQL schema `households`.
export const householdsSchema = pgSchema("households");

export const households = householdsSchema.table(
  "households",
  {
    id: uuid("Id").notNull(),
    name: varchar("Name", { length: 120 }).notNull(),
    createdAtUtc: timestamp("CreatedAtUtc", {
      withTimezone: true,
      mode: "date",
    }).notNull(),
  },
  (table) => [
    primaryKey({
      name: "PK_households",
      columns: [table.id],
    }),
  ],
);

export const householdMembers = householdsSchema.table(
  "household_members",
  {
    householdId: uuid("HouseholdId").notNull(),
    subject: varchar("Subject", { length: 200 }).notNull(),
    role: varchar("Role", { length: 32 }).notNull(),
    joinedAtUtc: timestamp("JoinedAtUtc", {
      withTimezone: true,
      mode: "date",
    }).notNull(),
  },
  (table) => [
    primaryKey({
      name: "PK_household_members",
      columns: [table.householdId, table.subject],
    }),
    foreignKey({
      name: "FK_household_members_households_HouseholdId",
      columns: [table.householdId],
      foreignColumns: [households.id],
    }).onDelete("cascade"),
    index("IX_household_members_Subject").on(table.subject),
  ],
);
