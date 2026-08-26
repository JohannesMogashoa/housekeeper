import { randomUUID } from "node:crypto";

import { TRPCError } from "@trpc/server";
import { asc, eq } from "drizzle-orm";
import { z } from "zod";

import { getDb, householdMembers, households } from "@housekeeper/db";

import { householdNameSchema } from "../domain/household-name";
import { protectedProcedure, router } from "../trpc";

const householdSelection = {
  id: households.id,
  name: households.name,
  createdAtUtc: households.createdAtUtc,
};

function toHouseholdSummary(household: {
  id: string;
  name: string;
  createdAtUtc: Date;
}) {
  return {
    id: household.id,
    name: household.name,
    createdAt: household.createdAtUtc.toISOString(),
  };
}

export const householdsRouter = router({
  list: protectedProcedure.query(async ({ ctx }) => {
    const rows = await getDb()
      .select(householdSelection)
      .from(households)
      .innerJoin(
        householdMembers,
        eq(households.id, householdMembers.householdId),
      )
      .where(eq(householdMembers.subject, ctx.userId))
      .orderBy(asc(households.createdAtUtc));

    return rows.map(toHouseholdSummary);
  }),

  create: protectedProcedure
    .input(
      z.object({
        name: householdNameSchema,
      }),
    )
    .mutation(async ({ ctx, input }) => {
      return getDb().transaction(async (tx) => {
        const id = randomUUID();
        const now = new Date();
        const [household] = await tx
          .insert(households)
          .values({
            id,
            name: input.name,
            createdAtUtc: now,
          })
          .returning(householdSelection);

        if (!household) {
          throw new TRPCError({
            code: "INTERNAL_SERVER_ERROR",
            message: "Household could not be created.",
          });
        }

        await tx.insert(householdMembers).values({
          householdId: household.id,
          subject: ctx.userId,
          role: "Owner",
          joinedAtUtc: now,
        });

        return toHouseholdSummary(household);
      });
    }),
});
