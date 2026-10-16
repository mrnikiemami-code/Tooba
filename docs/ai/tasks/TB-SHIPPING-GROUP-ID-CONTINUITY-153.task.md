PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GROUP-ID-CONTINUITY-153
Parent-Task: TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 9c581914c79fd1d55b60cf9d7361c7584ec48bdb
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_FIX_PERSISTED_SHIPPING_GROUP_ID_CONTINUITY
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; by minute 35 finish implementation/test and reserve remaining 10 for evidence, commit, push and report. Do not loop/restart shared services.

ARCHITECTURAL AUTHORITIES — READ FIRST

docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-IMPLEMENTATION-145.md
docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152/RESULT.md (or actual 152 evidence summary)
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md

ROOT CAUSE / USER-VISIBLE OBJECTIVE
TB-152 proved actual authenticated two-seller SAME-method shipping schedules, each with own fee/date/window, plus payment draft HTTP 200 and real isolated browser screenshots. BUT on every accepted PUT /v1/storefront/shipping/selection, BOTH previously persisted ShippingGroupIds change, even when user edits only group A or submits an identical request. This violates the LOCKED stable ShippingGroupId invariant. Fix this narrowly, without losing group-specific selection/policy validation or creating orders/payment during Shipping navigation.

DELIVERABLE — REAL CODE, NOT ANOTHER DESIGN OR REPRO TASK

Inspect the existing authoritative CartShippingDraft.ShippingGroups (with its line membership) and CartShippingGroupPlanner.Plan/newGroupId callsite in StorefrontShippingService.SaveSelectionAsync and persistence EF mapping.
On a repeat save, reuse each existing ShippingGroupId ONLY if it represents the same compatible logical obligation: same trusted cart, tenant/seller, canonical method/service+option, compatible origin/promise identity and matching authoritative line membership as defined by the existing supported grouping rules. For pure date/time change or idempotent retry, the id must stay stable. Explicitly decide and cover membership change semantics (preserve if valid same obligation, else allocate a NEW id); do not accidentally retain an id for a different seller/method or stale cart version.
Ensure no duplicate ids, one-to-one line membership, old-group removal as appropriate, and no EF tracked-entity identity conflicts or accidental cascades. The existing group line join rows must reconcile accurately. No client-submitted ID can become authoritative; derive reuse from trusted persisted draft only.
Must preserve per-group independent selected dates and windows, price snapshots, seller eligibility, current validation faults, and safe single-group legacy shorthand. No global cart-wide dates or window intersection. Case D (same seller+same method incompatible origin) remains BLOCKED; DO NOT remove the unique index or fabricate origin authority.
Keep accepted Order/Payment/Fulfillment/Settlement group linkage compatible and existing live order snapshots unchanged; no schema changes unless proven strictly necessary (prefer none). No arbitrary deterministic GUID/hash replacing actual persisted identity.
Avoid unrelated Admin/financial-policy, tax, pricing, frontend visual, storefront N+1 or shared logging edits.

MANDATORY FOCUSED TESTS (SOURCE AND PERSISTENCE)

First save creates two different ShippingGroupIds for two sellers both using tipax:standard; each has own valid schedule/fee and one line.
Identical subsequent save returns EXACT same ids (idempotent).
Change only seller A's selected time-window and save: A and B BOTH retain their respective ids; B's data remains byte-identical; A's schedule updates.
A genuine switch of seller/method or incompatible membership cannot reuse another group's id; no duplicate line membership, no orphaned join rows and no cross-seller data reuse.
Fail-closed invalid date/window or invalid method never corrupts persisted group ids/schedules.
Test through actual SaveSelectionAsync + EF repository in a safe isolated DB if feasible; if that handler cannot be driven inside the timebox, create an owner-correct focused persistence/transaction-level test and disclose the missing end-to-end proof. Do NOT call partial coverage PASS.
Single-group legacy read/write compatibility remains unchanged.

ACCEPTANCE / SAFETY

Use only task-owned disposable scratch services if a live database is needed; never write to shared :3000/:5088, shared :5432, store-alpha/tooba_alpha, or touch existing process. No OTP, payment, PSP, SMS, settlement payout or real carrier API.
Reuse and adapt TB-152 journey scripts if they exist and are safe; DO NOT rerun costly broad acceptance or build an entire new frontend just for this task. Minimum targeted tests and only one controlled scratch run if feasible. Persist a sanitized before/after id matrix showing stable first/second/edited save and matching DB joins; redact tokens/credentials.
No destructive migration, unrelated file changes, rebase, stash, reset or force push. Preserve pre-existing local Admin and TB-075/079 evidence dirt intact. Verify expected base or STOP and report upstream divergence.
PASS only after exact-id continuity and join reconciliation are proven by focused tests and at least one trustworthy persistence-backed test. Otherwise PARTIAL with precise blocker, but commit narrowly valid code with evidence.
Push implementation and concise evidence to origin/main, verify HEAD==origin/main; include precise files, SHA, tests, environmental safety, clear PASS/PARTIAL/BLOCKED, STOP_FOR_ARCHITECT.

NEXT PROGRAM STEP (NOT AUTOMATIC): after PASS, architect proceeds to remaining Shipping acceptance gap: demonstrate disabled->enabled Continue gating in isolated browser and confirm independently persistent group schedules across return/reload; then tackle outstanding Admin financial/create/write-roundtrip and release blockers. Do not issue or start next task yourself.
END_TOOBA_TASK
