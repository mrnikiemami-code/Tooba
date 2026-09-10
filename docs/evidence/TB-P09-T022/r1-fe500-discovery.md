# TB-P09-T022-R1 — FE Admin HTML 500 discovery

## Symptom (T022 smoke)

FE Admin Order Detail HTML shell returned **HTTP 500** during T022 visual smoke.

## Reproduction (R1)

Observed when the Next.js FE process was **down or cold-restarting**:

| Item | Value |
| --- | --- |
| Route | `http://127.0.0.1:3000/fa/admin/orders/{checkoutId}` |
| HTTP | **500** |
| Body | `Internal Server Error` |
| Next log | `Failed to proxy ... ECONNRESET` toward `localhost:3000` (dead / restarting Next worker) |
| Host | `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088` (correct; Host API reachable when FE was Ready) |

## Classification

| Candidate | Result |
| --- | --- |
| Consolidated Package SSR / DTO bug | **Ruled out** — Admin route healthy once Next reports Ready |
| Auth / fixture / package projection | **Ruled out** — same routes 200 after FE Ready |
| FE process unavailable / cold-restart connection reset | **Root cause** |

Failure is infrastructure/runtime availability of the Next worker, not Admin Consolidated Package render logic.

## Evidence continuity

T022 `visual-smoke.md` recorded the 500 and correctly withheld browser visual acceptance. R1 re-ran smoke with FE Ready (see `r1-admin-runtime.md`, `r1-visual-smoke.md`).
