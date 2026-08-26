"use client";

import { useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { trpc } from "@/lib/trpc/provider";

export function HouseholdPanel() {
  const [name, setName] = useState("");
  const utils = trpc.useUtils();
  const households = trpc.households.list.useQuery();
  const createHousehold = trpc.households.create.useMutation({
    onSuccess: async () => {
      setName("");
      await utils.households.list.invalidate();
    },
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createHousehold.mutate({ name });
  }

  return (
    <section className="grid gap-4 lg:grid-cols-[1.1fr_0.9fr]" aria-labelledby="households-heading">
      <Card>
        <CardHeader>
          <p className="text-sm font-medium text-primary">Your household</p>
          <CardTitle id="households-heading">Household foundation</CardTitle>
          <p className="text-sm leading-6 text-muted-foreground">
            The first migrated vertical slice is membership-scoped and persisted in PostgreSQL.
          </p>
        </CardHeader>
        <CardContent>
          {households.isLoading ? (
            <p className="text-sm text-muted-foreground">Loading households…</p>
          ) : households.error ? (
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm">
              <p className="font-medium">The household service is not ready.</p>
              <p className="mt-1 text-muted-foreground">
                Start PostgreSQL and run pnpm db:push, then retry this page.
              </p>
            </div>
          ) : households.data.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No household yet. Create the first one to establish the owner membership.
            </p>
          ) : (
            <ul className="space-y-3">
              {households.data.map((household) => (
                <li key={household.id} className="rounded-lg border p-4">
                  <p className="font-medium">{household.name}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Created {new Date(household.createdAt).toLocaleDateString()}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-xl">Create a household</CardTitle>
          <p className="text-sm text-muted-foreground">
            Names are trimmed and constrained to 2–120 characters.
          </p>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-2">
              <label htmlFor="household-name" className="text-sm font-medium">
                Household name
              </label>
              <Input
                id="household-name"
                name="householdName"
                minLength={2}
                maxLength={120}
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Mogashoa Home"
                autoComplete="off"
                required
              />
            </div>

            {createHousehold.error ? (
              <p className="text-sm text-destructive">{createHousehold.error.message}</p>
            ) : null}

            <Button className="w-full" type="submit" disabled={createHousehold.isPending}>
              {createHousehold.isPending ? "Creating…" : "Create household"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </section>
  );
}
