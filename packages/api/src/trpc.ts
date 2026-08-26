import { TRPCError, initTRPC } from "@trpc/server";

export interface TRPCContext {
  userId: string | null;
}

export function createTRPCContext(input: TRPCContext): TRPCContext {
  return input;
}

const t = initTRPC.context<TRPCContext>().create();

export const router = t.router;
export const publicProcedure = t.procedure;

export const protectedProcedure = t.procedure.use(({ ctx, next }) => {
  if (!ctx.userId) {
    throw new TRPCError({ code: "UNAUTHORIZED" });
  }

  return next({
    ctx: {
      ...ctx,
      userId: ctx.userId,
    },
  });
});
