import { TRPCError } from "@trpc/server";
import { asc, eq } from "drizzle-orm";
import { z } from "zod";

import { getDb, householdMembers, households } from "@housekeeper/db";

import { householdNameSchema } from "../domain/household-name";
import { protectedProcedure, router } from "../trpc";

const householdSummary = {
  id: households.id,
  name: households.name,
  createdAt: households.createdAt,
};

export const householdsRouter = router({
  list: protectedProcedure.query(async ({ ctx }) => {
    return getDb()
      .select(householdSummary)
      .from(households)
      .innerJoin(
        householdMembers,
        eq(households.id, householdMembers.householdId),
      )
      .where(eq(householdMembers.userId, ctx.userId))
      .orderBy(asc(households.createdAt));
  }),

  create: protectedProcedure
    .input(
      z.object({
        name: householdNameSchema,
      }),
    )
    .mutation(async ({ ctx, input }) => {
      return getDb().transaction(async (tx) => {
        const [household] = await tx
          .insert(households)
          .values({ name: input.name })
          .returning(householdSummary);

        if (!household) {
          throw new TRPCError({
            code: "INTERNAL_SERVER_ERROR",
            message: "Household could not be created.",
          });
        }

        await tx.insert(householdMembers).values({
          householdId: household.id,
          userId: ctx.userId,
          role: "owner",
        });

        return household;
      });
    }),
});
