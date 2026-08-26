# GitHub automation guidance

HouseKeeper is a pnpm/Turborepo TypeScript monorepo.

- CI must run against Node.js 24 and pnpm 11.
- Prefer root scripts (`pnpm typecheck`, `pnpm test`, `pnpm build`) so Turbo applies workspace ordering.
- Do not add .NET SDK, NuGet, MSBuild, or CDK-specific validation back into application CI.
- The Codex review-request workflow is intentionally stack-agnostic and remains separate from CI.
- Deployment workflows should be introduced only once the TypeScript deployment target is selected and documented.
- Never place secrets or database credentials directly in workflow YAML.
