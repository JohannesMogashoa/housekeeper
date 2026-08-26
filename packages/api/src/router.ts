import { householdsRouter } from "./routers/households";
import { router } from "./trpc";

export const appRouter = router({
  households: householdsRouter,
});

export type AppRouter = typeof appRouter;
