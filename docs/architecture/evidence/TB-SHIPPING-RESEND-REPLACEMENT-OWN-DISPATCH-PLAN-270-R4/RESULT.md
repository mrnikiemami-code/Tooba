# RESULT — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4

Status: **PASS** within the issued four-defect repair scope (not Architect ACCEPT, not user acceptance).

Decision:
`FIRST_SEND_IS_UNRESOLVED_BEFORE_THE_EXECUTOR_ONE_NEW_PLAN_GATE_COMPOSITION_AFTER_STORED_REPLAY_EXACT_SOURCE_KEY_DECISION_PARITY_FOR_HANDOFF_AND_A_CLOSE_UNMOUNT_RETARGET_RESPONSE_FENCE_WITH_COMPARE_AND_CLEAR`

- Parent-Task: `TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3`
- Mode: `REPAIR_PRE_SEND_DURABILITY_NEW_CONFIRM_GATES_AND_EXACT_HANDOFF`
- Expected-Base-SHA: `1ff02ab8a18c20999b92791f2b51520726bfa2d1`
- 270 stays PARTIAL as the parent; 270-R1 ACCEPT within its read-only seam; 270-R2 / 270-R3 PARTIAL; **271 NOT ISSUED**.

## 1. The four defects and their root cause

| # | Root cause in R2/R3 | Repair in R4 |
| --- | --- | --- |
| A | The reviewed envelope was written with `uncertain:false`, and the load path froze only `uncertain && requestBody`. An interruption between "body stored" and "exception caught" (reload, navigation, unmount, process kill) left a record that looked like history and could be overwritten by a fresh intent. | A durable `unresolved` intent is written (with the exact serialized body) and read back **before** the executor is invoked; from that instant the request may have been sent. `isUnresolvedShipmentPlanAttempt` requires a body and `unresolved !== false`, and TB-270-R3 records (no `unresolved` field) read as unresolved, so they are never assumed never-sent. A failed/absent read-back refuses to POST. |
| B | The confirmation read a stored plan first (correct) but then used the synchronous `RequirePlannable`, which skips the async active-package membership read, and no checkout-cancellation gate existed at all for the mutation. | One composition `RequireNewPlanGatesAsync` = `IsCheckoutCancelled` **and** `RequirePlannableAsync` (active package membership + the existing Created/Paid/group checks), used by BOTH the new confirmation and the new-calendar preview. The stored-plan replay stays ahead of every live gate. |
| C | The resolver fell back to "the only remaining `Created` child" for a resend, and the replacement branch scanned every source parcel's decisions for a key match. | Exact identity only: a resend needs the exact source **and** a non-empty key recorded on the child; a replacement needs the decision to live on the **requested source**, carry the reviewed key, link exactly one replacement id, and that parcel must be that decision's own `Created` child. Missing/mismatched/ambiguous → `null`. |
| D | The epoch effect returned immediately when closed (no cleanup, no bump), and `send.finally` cleared `pending` unconditionally, so a late answer could close a newer surface or clear a newer stored intent (scope-only clearing). | Closing **and** unmount/cleanup bump the presentation epoch; every stale `setState`/`onClose`/`onCompleted` is epoch-guarded (including `finally`); storage is resolved with **compare-and-clear/update** on the exact occurrence + body, never by scope alone. |

## 2. Behavioural guarantees now proven

- **A** — the exact envelope is durable before the executor is called; an unmount with no catch/finally recovers the same
  body/key; a different freshly-offered slot cannot overwrite it; explicit retry re-sends the identical bytes; a failed
  preview still exposes the unresolved intent; a legacy `uncertain:false` record is retried, not rebased.
- **B** — a stored plan answers history/replay without any live eligibility work; a NEW confirmation inside an active
  package or on a cancelled checkout is refused with **zero inserts**; history stays readable after cancellation and a
  different occurrence can never mint a second row; denied actor / cross-checkout remain refused.
- **C** — one `Created` child with the wrong key opens nothing; the right key on another source's decision opens nothing;
  two decisions answering one key are ambiguous and open nothing; exact source+key (+decision link) opens exactly that
  child. No duplicate create, no wizard, no changed creation policy.
- **D** — a late success after unmount/retarget neither closes the replaced surface nor clears a newer stored intent; a
  conflict/authorization answer is NOT treated as proof of no write and keeps the exact intent recoverable, while a
  proven no-write refusal (`selection_expired`, `package_locked`, `order.cancelled.blocks_action`, `shipping.scheduling.*`, …)
  still ends the review and offers a deliberate fresh one with the prior outcome preserved.

## 3. Focused checks actually run (once, after the final code change)

| Evidence class | Suite | Result |
| --- | --- | --- |
| Order command (real orchestrator boundary, controlled ports) | `AdminShipmentPlan270R2Tests` | **28/28** (25 retained + 3 new Repair-B cases) |
| FE helper (pure resolver) — run explicitly | `features/order-operational-groups/created-parcel.test.ts` | **13/13** |
| FE mounted plan surface (deterministic deferred promises) | `app/admin/admin-shipment-plan-270r2.mount.test.ts` | **14/14** (10 retained + 4 new A/D cases) |
| FE mounted connected panel handoff | `app/admin/admin-order-operational-delivery.mount.test.ts` | **75/75** (74 retained + 1 new three-case negative) |
| Typecheck | `npx tsc --noEmit` | 0 errors |
| Backend build | `dotnet build src/backend/Tooba.slnx -c Release` | 0 errors (332 pre-existing warnings) |
| Backend publish | `dotnet publish Host/Tooba.Host -c Release -o artifacts/publish-primary` | 0 errors |
| Frontend build | `npm run build` | 0 errors, `BUILD_ID cz4aReIm9Nx33hKljkmfr` |

The three-case connected negative exercises the real panel: (1) the only `Created` resend child with a different
`shippingDispatchKey`, (2) a replacement decision of another source parcel, (3) two decisions answering one reviewed key —
each produces **0** plan modals and **0** plan requests, while the retained positive connected cases
(`TB-270-R3 mounted: a returned-source resend create …` and `… a replacement with real stock …`) still open exactly the
created child.

Not rerun (untouched, declared): `AdminShipmentDispatchPlan270R2RelationalTests` (store/index only — no orchestrator
gate path), `AdminFulfillmentOwnPlanProjection270R2Tests`, `OrderShipmentDispatchPlanStoreFilter270R3Tests`,
`DispatchPlanGuard270R2Tests`, `ReplacementReadiness270R3Tests`, `AdminFulfillmentBulkTests`,
`ShippingAttemptSchedulingLookupTests`. No unfiltered suite, baseline/stash comparison or speculative hardening was run.

## 4. Deploy / runtime

- Code: `src/backend` + `src/frontend` build outputs rebuilt; Host graceful stop (PID 43312) → publish → restart
  (launcher 48388, listener **PID 48388**) on `http://127.0.0.1:5088`.
  `GET /health` **200** `{"status":"ok"}`; `GET /health/ready` **200**
  `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","messaging":"Healthy"}}`.
- FE: previous Next server (PID 620) stopped, `npx next start -p 3000` (listener **PID 31184**), `GET /fa` **200**,
  `/_next/static/cz4aReIm9Nx33hKljkmfr/_buildManifest.js` **200** (6,612 B) and the served chunk
  `/_next/static/chunks/9424-c99e9070dc020045.js` **200** (191,506 B) carrying the plan-modal copy
  («تلاش دوباره با همین درخواست»).
- One canonical `tooba_alpha` (127.0.0.1:5432, SingleStore `store-alpha`); no migration, no DDL, no reset/reseed/backfill,
  no business SQL, no cleanup, no restore, no provider/waybill/money/notification action, no new fixture, no live plan row.
  R2's post-apply backup is preserved; the missing pre-apply backup remains declared as an irreversible audit deviation.

## 5. Limits / non-claims

- Worker PASS is not Architect ACCEPT and not user visual/functional acceptance; no carrier/provider/waybill/consent/
  financial claim.
- Provenance: this worktree already carried an uncommitted, incomplete edit set for exactly these four defects when the
  R4 task arrived (code only — no command-gate fixtures, no A/D mounted cases, and the test fixture did not compile). It
  was reviewed against the issued base, completed, and verified from `1ff02ab8a…`; nothing was stashed, discarded, reset or
  rebased, and the pre-existing untracked scratch scripts were left untouched.
- True two-writer concurrency stays **UNPROVEN** here (no new concurrency harness was added, by instruction).
- The mounted tests prove presentation/durability semantics with deterministic deferred promises, not a browser session.
- The existing review URL is completed history only (no eligible live `Created`/unplanned linked parcel exists), so no new
  live plan click-through was produced and none is claimed.
- `tailwind.config.ts` still does not scan `features/**`; this repair adds no new feature-level utility.
- Untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files are pre-existing and are not part of this
  change set.
