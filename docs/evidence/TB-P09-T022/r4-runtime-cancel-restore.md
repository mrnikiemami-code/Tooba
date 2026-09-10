# R4 Runtime — Cancel / Restore (Scenario B)

Host runtime proof via `_r4_runtime.mjs` / `_r4_runtime_raw.json`.

## IDs

| Field | Value |
| --- | --- |
| checkoutId | `01a08b49-87ca-7000-bb9e-05a49db35189` |
| old (paid) reservation | `01a08b49-8728-7000-b4a9-4d3c3180099d` |
| replacement after restore | `01a08b49-8cb8-7000-b9f8-f9ed321bfbbf` |
| original cart ExpiresAt | `2026-09-10 12:28:56.858283+00` |
| cancel / restore HTTP | 200 / 200 |
| dispatch HTTP | 200 |

## Before / after

| Phase | at (UTC) | reservationId | status | ExpiresAt |
| --- | --- | --- | --- | --- |
| beforeCancel | 2026-09-10T12:27:27.441Z | …8728… | Held | null |
| afterCancel | 2026-09-10T12:27:28.233Z | …8728… (same) | **Released** | null |
| afterRestore | 2026-09-10T12:27:29.244Z | …8cb8… (new) | Held | **null** |
| afterExpiry (past original cart TTL) | 2026-09-10T12:29:08.127Z | …8cb8… | Held | null |
| oldAfterExpiry | 2026-09-10T12:29:08.243Z | …8728… | Released | null |
| afterDispatch | 2026-09-10T12:29:09.626Z | …8cb8… | **Consumed** | null |
| oldFinal | 2026-09-10T12:29:09.731Z | …8728… | Released | null |

OrderLine + Fulfillment both rebound to replacement (`aligned: true`). Old Released untouched through expiry + dispatch.

## Verdict

**PASS**
