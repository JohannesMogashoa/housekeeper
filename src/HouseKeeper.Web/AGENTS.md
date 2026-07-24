# Codex instructions for the HouseKeeper PWA

These instructions apply to `src/HouseKeeper.Web/` in addition to the root and `src/AGENTS.md` rules.

## Trust and API boundaries

- The PWA contains no secret, privileged credential, database connection, signing key, storage key, or trusted authorization rule.
- API requests use typed/versioned contracts and preserve authentication/token failure behavior without exposing tokens in logs or durable browser queues.
- Client-side visibility checks are user experience only; the API enforces every protected operation.

## Offline and browser persistence

- Service-worker asset caching is never described as full offline business-data synchronization.
- Browser-persisted data has an explicit schema/version strategy, bounded retention, and authenticated-user/household isolation.
- Pending actions use the server idempotency protocol and do not persist access or refresh tokens.
- Sign-out, account switch, household switch, membership loss, payload-version mismatch, and browser storage failure have safe outcomes.
- Multiple tabs must not corrupt queue state; server idempotency remains the correctness boundary.

## User experience and accessibility

- Components provide clear loading, empty, offline, pending, retrying, conflict, authorization, validation, and terminal failure states where relevant.
- Forms preserve labels, validation summaries, focus management, keyboard operation, and useful error association.
- Interactive targets are touch-friendly and mobile-first with no avoidable horizontal overflow.
- Review safe-area insets, orientation changes, reduced motion, contrast, and screen-reader semantics when affected.
- Error differences must not reveal household names, record existence, or sensitive details to unauthorized callers.

## Service worker and installability

- Treat manifest, icon, cache-version, update, and offline-fallback changes as deployment-sensitive behavior.
- Do not cache authenticated API responses, secrets, or private business data unintentionally.
- Document browser-specific assumptions, especially iOS installation and permission behavior.
- Validate published output, including static-asset fingerprint transformation.

## Tests

- Use bUnit for deterministic component states and accessibility-sensitive rendering.
- Use Playwright against published artifacts for authentication, household scope, offline/reconnect, browser reload, service-worker, file input, and critical mobile journeys as applicable.
- Test negative states and recovery, not only successful click paths.
- Treat missing user isolation, offline replay, accessibility, or browser-restart coverage as material when affected.
