---
applyTo: "src/HouseKeeper.Web/**,tests/HouseKeeper.Web.Tests/**,tests/HouseKeeper.EndToEndTests/**"
---

# PWA review instructions

When reviewing matching files, verify the following in addition to repository-wide instructions.

## Trust and API boundaries

- The PWA contains no secret, privileged credential, storage account key, signing key, database connection, or trusted authorization rule.
- API requests use typed/versioned contracts and preserve authentication/token failure behavior without exposing tokens in logs or browser storage.
- Client-side visibility checks are user experience only; protected behavior remains enforced by the API.

## Offline and persistence

- Service-worker asset caching is never presented as full offline business-data synchronization.
- Browser-persisted data has an explicit schema/version strategy, bounded retention, and user/household isolation.
- Pending actions use the server idempotency protocol and do not persist access or refresh tokens.
- Sign-out, account switch, household switch, membership loss, payload-version mismatch, and browser storage failure have safe outcomes.
- Multiple tabs cannot corrupt queue state; server idempotency remains the correctness boundary.

## User experience and accessibility

- Components expose clear loading, empty, offline, pending, retrying, conflict, authorization, validation, and terminal failure states.
- Forms preserve labels, validation summaries, focus management, keyboard operation, and useful error association.
- Interactive targets are touch-friendly and mobile-first with no avoidable horizontal overflow.
- Safe-area insets, orientation changes, reduced motion, contrast, and screen-reader semantics are considered where relevant.
- UI does not reveal household names, record existence, or sensitive detail to unauthorized callers through error differences.

## Service worker and installability

- Manifest, icon, cache-version, update, and offline-fallback changes are reviewed as deployment-sensitive behavior.
- New service-worker caching avoids stale authenticated API responses and does not cache secrets or private business responses unintentionally.
- Browser-specific assumptions, especially iOS install/permission behavior, are documented.
- Published output is validated, including static-asset fingerprint transformation.

## Tests

- Use bUnit for deterministic component state and accessibility-sensitive rendering behavior.
- Use Playwright against published artifacts for sign-in, household scope, offline/reconnect, browser reload, service-worker, file input, and critical mobile journeys as applicable.
- Test negative states and recovery, not only the successful click path.
- Treat missing user isolation, offline replay, accessibility, or browser-restart coverage as material findings when affected.