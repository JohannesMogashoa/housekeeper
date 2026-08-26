import { HouseholdPanel } from "@/components/household-panel";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const signals = [
  { label: "Today", value: "Clear", detail: "No overdue household actions" },
  { label: "Maintenance", value: "Next", detail: "Track recurring home care here" },
  { label: "Shopping", value: "Shared", detail: "One household list, coming next" },
];

export default function HomePage() {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-6xl flex-col gap-10 px-5 py-8 sm:px-8 lg:py-12">
      <header className="flex flex-col gap-3 border-b pb-8">
        <span className="text-sm font-medium text-primary">HouseKeeper</span>
        <h1 className="max-w-3xl text-4xl font-semibold tracking-tight sm:text-5xl">
          Know what your home needs today.
        </h1>
        <p className="max-w-2xl text-base leading-7 text-muted-foreground sm:text-lg">
          A focused home command centre for household responsibilities, maintenance,
          supplies, and the people sharing them.
        </p>
      </header>

      <section className="grid gap-4 md:grid-cols-3" aria-label="Home health overview">
        {signals.map((signal) => (
          <Card key={signal.label}>
            <CardHeader className="pb-2">
              <p className="text-sm text-muted-foreground">{signal.label}</p>
              <CardTitle>{signal.value}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-6 text-muted-foreground">{signal.detail}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <HouseholdPanel />
    </main>
  );
}
