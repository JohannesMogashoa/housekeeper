import { drizzle } from "drizzle-orm/postgres-js";
import postgres from "postgres";

import * as schema from "./schema";

function createDatabase() {
  const databaseUrl = process.env.DATABASE_URL;

  if (!databaseUrl) {
    throw new Error("DATABASE_URL is required to access PostgreSQL.");
  }

  const client = postgres(databaseUrl, {
    max: process.env.NODE_ENV === "production" ? 10 : 1,
  });

  return drizzle(client, { schema });
}

type Database = ReturnType<typeof createDatabase>;

let cachedDatabase: Database | undefined;

export function getDb(): Database {
  cachedDatabase ??= createDatabase();
  return cachedDatabase;
}
