# Codex instructions for HouseKeeper PWA component tests

These instructions apply to `tests/HouseKeeper.Web.Tests/` in addition to the repository root `AGENTS.md`.

Tests in this tree verify the PWA trust boundary and user-visible state model. Review test-only changes against the corresponding production behavior in `src/HouseKeeper.Web/`.

## Required coverage

- Use bUnit for deterministic component behavior, rendering, form validation, focus management, keyboard operation, and accessibility-sensitive states.
- Cover loading, empty, offline, pending, retrying, conflict, authorization, validation, and terminal failure states when affected.
- Verify that client-side visibility never substitutes for API authorization and that errors do not reveal household names, record existence, or sensitive details.
- Exercise authenticated-user and household isolation for browser-persisted state, including sign-out, account switch, household switch, and membership loss.
- Cover offline queue schema/version handling, bounded retention, idempotency identifiers, storage failures, payload-version mismatches, and safe recovery.
- Treat missing negative-state, isolation, accessibility, offline replay, or browser-restart evidence as a material finding when the changed behavior depends on it.

Do not accept tests that only assert successful click paths while omitting the failure and recovery semantics required by `src/HouseKeeper.Web/AGENTS.md`.
