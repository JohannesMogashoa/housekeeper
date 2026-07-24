# Codex instructions for HouseKeeper PWA end-to-end tests

These instructions apply to `tests/HouseKeeper.EndToEndTests/` in addition to the repository root `AGENTS.md`.

Tests in this tree prove browser and published-artifact behavior for `src/HouseKeeper.Web/`. Review test-only changes against the PWA rules in `src/HouseKeeper.Web/AGENTS.md`.

## Required coverage

- Run Playwright against published artifacts rather than development-only output for critical journeys.
- Cover authentication and household isolation, including wrong-household, removed-member, sign-out, account switch, and household switch behavior.
- Exercise offline/reconnect transitions, pending-action replay, duplicate submission, browser reload/restart, storage failure, and safe recovery where affected.
- Validate service-worker update behavior, cache-version changes, offline fallbacks, and the absence of unintended authenticated API or private-data caching.
- Verify static-asset fingerprint transformation and installability assumptions affected by manifest, icon, cache, or deployment changes.
- Include keyboard, focus, screen-reader semantics, touch targets, mobile viewport, orientation, safe-area, reduced-motion, and horizontal-overflow checks when relevant.
- Treat missing isolation, offline replay, accessibility, service-worker, published-output, or browser-restart evidence as a material finding when the changed behavior depends on it.

Successful happy-path navigation alone is insufficient evidence for PWA correctness.
