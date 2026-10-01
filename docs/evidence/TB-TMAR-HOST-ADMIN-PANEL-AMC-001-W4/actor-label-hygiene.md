# actor-label-hygiene — W4

Before:
- `AdminDevActorBootstrap` set `actorLabel` to hard-coded Persian prose `"مدیر نمونهٔ توبا"`.

After:
- `actorLabel` = existing machine identity `AdminEmail` (`admin-actor@tooba.local`).
- Response field shape unchanged: `{ actorUserId, actorLabel, tenantId }`.
- Bootstrap password remains out of response (unchanged out-of-scope behavior).
- No new hard-coded English display sentence added.
