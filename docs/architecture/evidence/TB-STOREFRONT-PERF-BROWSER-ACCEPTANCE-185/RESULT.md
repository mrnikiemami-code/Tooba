# TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185 — isolated real-Next / real-browser acceptance attempt

Parent: TB-STOREFRONT-PERF-PROMOTION-BATCH-184
Base-SHA: `96f9c0876e81bd0c05270aa0ef0d6cac0db31062`
Verdict of this slice: **PARTIAL** — a genuine isolated Next + Host + Postgres stack was built and the Home
route was served end-to-end, but **no rendered browser frame could be obtained**, and the warm Next Home render
itself measured **~90 s** against a **~0.2 s** Host response. Both facts are recorded honestly below.

## 1. Base check

`HEAD == origin/main == 96f9c0876e81bd0c05270aa0ef0d6cac0db31062` after a `--ff-only` merge of the two architect
commits that landed after TB-184 (`e7ef3916d` → `96f9c0876`). Pre-existing dirty files were preserved untouched
(`admin-fulfillment-work-queue-screen.tsx`, TB-075/079 stdout logs, TB-173 evidence edits, task receipts).

## 2. Isolated stack (task-owned)

| Component | Value |
| --- | --- |
| Postgres 16 | container `tooba-tb185-pg`, loopback `127.0.0.1:55506`, db `tooba_185_scratch` + `tooba_185_messaging` |
| Host | `127.0.0.1:5456`, `DOTNET_ENVIRONMENT=Development`, EF level **Warning** (quiet, so wall time is not log-bound) |
| Next | **copy** of `src/frontend` at `%TEMP%\tb185-web` (own `.next`), `next dev -p 3105`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5456` |
| Shared stack | `:3000`, `:5088`, `:5432`, `tooba_alpha`, `store-alpha`, shared `.next` — never touched or restarted |

The Next copy was required precisely to honour "no change to shared `.next`": a second `next dev` inside
`src/frontend` would have written the user's live `.next`. `node_modules` was therefore copied (not junctioned —
a junction produced `Can't resolve './D:/.../next/dist/client/app-next-dev.js'` and killed every page with
`ENOENT .next/fallback-build-manifest.json`).

Isolation guard (`prepare-isolated-target-185.js`) ran before any write and wrote `isolation-guard-185.json`:
scratch ports are not `5432`/shared, Host port is not `5088`/`3000`, and the generated settings contain no
shared connection string.

## 3. Fixture provenance (owner routes only)

* `POST /v1/admin/catalog/demo/seed-only` with the real Development admin actor `X-Tooba-Dev-Actor-User-Id`
  (read from `identity.login_identifiers`) — the app-owned demo bootstrap, not SQL-inserter.
* Publication through the genuine `POST /v1/admin/products/{id}/publish` route only: 40 attempts,
  **7 published** (readiness validators rejected the rest; none bypassed).
* Final fixture: `products=46, published=7, active_offers=46, active_prices=47, stock=47`, tenant/route/locale
  identical to TB-183/184 (`alpha.localhost`, `/v1/storefront/home?locale=fa`).
* Size ratio vs the 3-published baseline: **2.33×** (`idsA=3 → idsB=7`).

## 4. Measured results

### 4.1 Host (authoritative source of the Home payload)

| run | status | wall | payload |
| --- | --- | --- | --- |
| warmup | 200 | 213.9 ms | 41,487 B |
| m1 | 200 | 95.3 ms | 41,487 B |
| m2 | 200 | 153.0 ms | 41,487 B |
| m3 | 200 | 135.1 ms | 41,487 B |
| later re-check | 200 | 209 ms | 38,371 B |

Bodies byte-identical across m1–m3 (`sha D542F215B141…`), i.e. the backend is deterministic and fast at 7
published products. (`docs/…/home-size-a.json`, `home-size-b.json`, `progress-185.log`.)

### 4.2 Next Home route (real Next 15.5.23 app, warm, dev mode)

`GET /fa` on `127.0.0.1:3105`, measured by the Next server itself (`next-dev-server-185.log`) after the route
was already compiled (cold compile was separate: `✓ Compiled / in 15.6s`):

```text
GET /fa 200 in 132027ms
GET /fa 200 in  90008ms
GET /fa 200 in  89986ms
GET /fa     (client aborted at 24,992 ms)
```

**This is the headline finding of this slice.** With the backend at ~0.1–0.2 s and a fixed 41 KB payload, the
Next Home render costs **~90 s warm** — two to three orders of magnitude above the Host. It is *not* cold
compile (the route was already compiled) and *not* the promotion/query work proved fixed in TB-183/184.
So the user-visible "Home stays loading" symptom is **not** explained by the backend any more; it must be
attacked on the Next render/data-fetch path before PERF-CRIT-001 can be closed.

### 4.3 Browser

Three separate mechanisms were attempted against the isolated Next origin:

1. `Page.navigate` over a direct page-target CDP socket — never returned (45 s cap);
2. `Page.captureScreenshot` on a tab created via `PUT /json/new` — never returned (15–45 s caps);
3. one-shot `chrome --headless=new --screenshot --dump-dom --virtual-time-budget=25000` — produced no file
   and no DOM within 125 s.

`Runtime.evaluate` on the same tabs *did* answer, and the tabs reported `document.body.innerText.length == 0`,
i.e. the renderer never produced content while the Next server was still grinding on the ~90 s response.
Conclusion: with Home taking ~90 s to respond, a rendered-frame capture inside the timebox was not achievable
without altering the isolated stack. **No screenshot is claimed and no browser visual proof is asserted** —
`home-cold.png` / `home-warm.png` were never produced and are not committed.

## 5. What this slice does and does not certify

* Certified: an isolated Next app *does* serve the real Home route through the real Host (HTTP 200), the Home
  payload is byte-stable and fast at the API boundary, promotion-owned batching (TB-184) is intact, and the
  fixture is genuine and published through validators.
* NOT certified: any pixel-level render, DOM card integrity, browser DCL/load timings, or browser-side network
  waterfall. Therefore **the user's browser responsiveness verdict remains open**, and PERF-CRIT-001
  **must not be closed**.
* The measured `~90 s` Next render is a dev-mode isolated-copy observation; it must be re-verified on the
  user's own dev server before being treated as the official root cause, but it is a real, reproducible signal
  in this environment and is the strongest lead for the remaining symptom.

## 6. Reproducible user steps (unchanged gate)

1. Start the normal dev stack the project already uses (`:5088` Host + `:3000` Next).
2. Open `http://localhost:3000/fa` in the real browser, hard-reload, and note time-to-cards.
3. Compare with `http://127.0.0.1:5088/v1/storefront/home?locale=fa` (should be fast).
4. Report: (a) time until product cards are visible, (b) whether the spinner persists, (c) console errors.
   If the API is fast but the page is slow, the remaining defect is on the Next render path, not the backend.

## 7. Cleanup

Task-owned only: `tooba-tb185-pg` container removed, `%TEMP%\tb185-web` copy and its `.next`, temp Chrome
profiles and Next log removed, Host process stopped, `.tmp-185-*` removed. Shared `:3000`/`:5088`/`:5432`,
`tooba_alpha`, `store-alpha`, Redis, RabbitMQ and the shared `.next` were never written to. No credentials are
committed (scratch password was environment-only).

## 8. Commands actually run

```text
git merge --ff-only origin/main                     -> 96f9c0876 (expected base)
docker run postgres:16-alpine (55506)               -> scratch db ready
dotnet publish Tooba.Host + run on 5456              -> /health 200
POST /v1/admin/catalog/demo/seed-only (dev actor)    -> products 5 -> 46
POST /v1/admin/products/{id}/publish (40 attempts)   -> 7 published, 33 readiness-rejected
GET  /v1/storefront/home?locale=fa (x4)              -> 200, 95-214 ms, 41,487 B
next dev -p 3105 (isolated copy)                     -> compiled / in 15.6 s
GET  http://127.0.0.1:3105/fa (x4, warm)             -> 200 in 132027 / 90008 / 89986 / aborted
chrome headless CDP / one-shot screenshot            -> no frame produced
```
