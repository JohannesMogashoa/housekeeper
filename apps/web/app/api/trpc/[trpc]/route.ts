import { appRouter, createTRPCContext } from "@housekeeper/api";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";

function handler(request: Request) {
  return fetchRequestHandler({
    endpoint: "/api/trpc",
    req: request,
    router: appRouter,
    createContext: () => {
      const developmentUserId =
        process.env.NODE_ENV === "production"
          ? null
          : (request.headers.get("x-housekeeper-user-id") ?? "development-user");

      return createTRPCContext({ userId: developmentUserId });
    },
  });
}

export { handler as GET, handler as POST };
