# HouseKeeper Codex pull-request review

HouseKeeper uses OpenAI Codex code review for repository-aware pull-request analysis. Codex supplements—not replaces—the author's verification, the complete CI pipeline, and deliberate human approval.

## Why Codex

Codex can review a pull request against its stated intent, inspect the broader repository rather than only isolated diff fragments, and run or reason about tests and dependencies. It can review automatically when enabled for the repository or on demand through a pull-request mention.

HouseKeeper does not require GitHub Copilot for this workflow.

## Repository instruction model

Codex reads `AGENTS.md` guidance from the repository. The root file defines universal architecture and review requirements, while deeper files add rules for their directory trees.

| File | Responsibility |
|---|---|
| `AGENTS.md` | Root architecture, security, reliability, testing, and review-output contract |
| `src/AGENTS.md` | .NET, module boundaries, domain/application behavior, EF Core, PostgreSQL, and worker rules |
| `src/HouseKeeper.Web/AGENTS.md` | PWA trust boundary, offline storage, accessibility, service worker, installability, and browser tests |
| `tests/HouseKeeper.Web.Tests/AGENTS.md` | PWA component-test isolation, offline states, accessibility, and recovery coverage |
| `tests/HouseKeeper.EndToEndTests/AGENTS.md` | Published-PWA browser, service-worker, restart, mobile, and household-isolation coverage |
| `deploy/AGENTS.md` | AWS identity, CDK, environments, migrations, deployment, rollback, recovery, and cost rules |
| `scripts/AGENTS.md` | Local/CI orchestration isolation, disposable infrastructure, health/readiness, smoke, and persistence checks |
| `.github/AGENTS.md` | Workflow permissions, CI integrity, deployment safety, PR evidence, and Codex-review usage |
| `.github/pull_request_template.md` | Author evidence contract for linked work, risks, migrations, tests, UX, operations, and reviewer focus |

The nearest applicable `AGENTS.md` adds specificity; it does not cancel the root invariants.

## On-demand GitHub review

After the repository is connected to Codex, comment on a pull request:

```text
@codex review
```

Add a focused request when the change has a dominant risk:

```text
@codex review for household authorization, PostgreSQL migration safety, deployment permissions, and API restart recovery
```

Codex posts its analysis to the pull request. If it proposes a correction, keep the discussion in the review thread and either implement the change deliberately or ask Codex to prepare a patch for human review.

Request another review after material changes unless automatic review of new pushes is enabled.

## Automatic GitHub review

The repository's `codex-review-request.yml` workflow runs on
`pull_request_target` for every pull request targeting `development` and posts
one idempotent Codex request. It asks Codex to review the change and generate a
concise description in the `housekeeper-codex-description` block in the PR
body. It has only `contents: read` and `pull-requests: write`; it never checks
out, builds, or executes pull-request code. `pull_request_target` is safe here
because the workflow performs only a fixed GitHub API comment operation through
a reviewed workflow action. It must not be expanded to run tests, inspect
files, or assume AWS credentials.

The repository template is tracker-neutral: link a GitHub issue, Notion page,
Linear task, another planning system, or no external item. If the connected
Codex integration cannot write the PR body, it returns the generated text in
its response and the author can paste it between the template markers.

The comment is a request, not a synthetic check result. Codex review findings,
the reusable CI checks, and human approval are separate evidence streams:

- Codex reviews repository intent, changed-code risk, surrounding behavior and
  the `AGENTS.md` contract. Authors resolve or explicitly disposition material
  findings.
- `ci.yml` and `validate-development-pr.yml` provide deterministic
  development-PR build, test, migration-backed smoke evidence. `ci.yml` pushes
  and `ci-release.yml` release/master pull requests use `validate.yml` for the
  full CDK, browser, restart, coverage, and artifact evidence.
- A human reviewer owns the final authorization, migration, deployment,
  rollback, cost, and residual-risk decision.

Automatic review is also enabled in Codex, not through GitHub Copilot files or a repository ruleset.

1. Open Codex and connect the GitHub account or organization that owns `JohannesMogashoa/housekeeper`.
2. Ensure the ChatGPT GitHub connector is authorized for the repository.
3. Create or select the Codex environment for HouseKeeper.
4. Open the Codex code-review settings.
5. Enable automatic review for the HouseKeeper repository or the applicable team/personal pull requests.
6. Choose whether reviews should run only when a draft becomes ready or also after subsequent pushes.
7. Open a small test pull request and verify that Codex posts a review using the repository `AGENTS.md` policy.

Exact labels can evolve with the Codex interface. The invariant is that the repository is enabled for Codex code review through Codex settings; no Copilot reviewer or `.github/agents/*.agent.md` profile is required.

## Review contract

Codex prioritizes:

1. cross-household isolation, authorization, and secret safety;
2. data integrity, migration safety, concurrency, idempotency, and restart recovery;
3. modular-monolith dependency and schema ownership;
4. offline/PWA correctness, accessibility, and browser storage isolation;
5. external-provider failure handling and durable worker semantics;
6. test adequacy, observability, deployment safety, rollback, and documentation drift;
7. maintainability issues with a concrete failure or change-cost impact.

Every material finding should include:

- native priority: `[P0]`, `[P1]`, `[P2]`, or `[P3]`;
- changed file and line/range;
- violated invariant or issue requirement;
- realistic failure mode;
- smallest safe correction;
- test or evidence needed to prove the correction.

Priority meanings are defined in the root `AGENTS.md`: `[P0]` is stop-ship, `[P1]` urgent, `[P2]` material and normally pre-merge, and `[P3]` narrowly scoped and non-urgent.

Questions and non-blocking suggestions remain separate from defects. Codex should avoid speculative abstractions, unrelated refactors, personal style preferences, vague suggestions, and praise-only noise.

## Author workflow

Before requesting review:

1. Link the relevant GitHub issue, Notion page, Linear task, another tracker item, or explain why there is none.
2. Complete the concise pull-request template; leave conditional sections as `N/A` when they do not apply.
3. Run the risk-appropriate tests and link the exact CI run.
4. Include migration, rollout, compatibility, and rollback notes for data changes.
5. Include screenshots or recordings for visible PWA changes.
6. State residual manual validation gaps and link deferred work.
7. Identify the highest-risk behavior in the reviewer-focus field.
8. Comment `@codex review` or confirm that automatic Codex review has run.
9. Resolve or explicitly disposition material findings before human approval.

## Local review with Codex

Codex CLI or the Codex app can also review local or branch changes before a pull request is opened. Run in a review/suggest mode and ask it to compare the branch with `master` using the same `AGENTS.md` contract. This is useful for early feedback but does not replace the GitHub review attached to the final PR head.

## Limitations

- AI review is non-deterministic and can miss defects or raise false positives.
- Codex requires repository authorization and an enabled Codex environment/review configuration.
- Automatic review is a Codex account/workspace setting; repository files only define the review policy.
- Network, secret, and environment access for Codex tasks must remain intentionally configured and least-privileged.
- Green CI and a clean Codex review do not replace explicit human approval.

## Primary OpenAI documentation

- [Introducing upgrades to Codex](https://openai.com/index/introducing-upgrades-to-codex/)
- [Using Codex with your ChatGPT plan](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan)
- [Introducing Codex and the AGENTS.md instruction model](https://openai.com/index/introducing-codex/)
- [Codex documentation](https://developers.openai.com/codex)
