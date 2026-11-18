# TB-279-R1 — RESULT

Status: **PASS within the issued A–G consolidated scope**, decision

`ONE_REVIEWED_PORTION_AUTHORITY_FOR_MONEY_STOCK_AND_CUSTODY_ONE_ATOMIC_VERSION_FENCED_DECISION_TRANSITION_ONE_SHARED_RETURNS_PAYMENT_RESERVATION_PROTOCOL_FOR_EVERY_NEW_ATTEMPT_AND_ONE_VIEWER_SCOPED_EXACT_DURABLE_CUSTOMER_INTENT_WITH_NO_GUESSED_LEGACY_ALLOCATION_ON_THE_EXISTING_RETURNS_MODULE`

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT provider/financial/Marketplace readiness.

| Item | Value |
| --- | --- |
| Task | `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1` |
| Parent | `…-279` (stays PARTIAL until this repair is independently reviewed) |
| Expected-Base-SHA | `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32` |
| Base-SHA at claim | `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32` — docs-only issuance delta, zero source/config/schema drift |
| Reviewed-Implementation-SHA (parent) | `9c0885a5943e7f1a4e1a09a17fac417d23037aa6` |
| Code-SHA / Evidence-SHA / Final-HEAD | ONE combined commit (see `git log -1` and the Bridge result) |
| Envelope executed | Bridge-delivered `Content` 24 251 bytes, SHA256 `A5FABF0BC43546F4775F14018679813F8DF67B8374300A4F14A89C2F131494AF` |
| Envelope issued | committed `…279-R1/TASK.md` 24 446 bytes, SHA256 `8913a28534abf2516b03cae41fde4cd3334c922aed5b217b46be46042b0435c9`; identical scope, differing only in `##`-heading/bullet markdown formatting (checked line-by-line, no requirement differs) |
| Schema | one additive Returns migration `20261118023434_Tb279R1PerConditionPortionsAndCustodyProof`, applied once after a verified fresh backup |
| Live business write | ZERO |
| Provider/money/stock effect | ZERO |
| Architect accept | NO |
| User acceptance | NO |

## What changed

**A (F1)** `ReturnDirectory.Reservations.cs` is the one reservation protocol for the first refund and every NEW retry
child: owning payment facts are read before any lock, the seller-order → case → payment order is taken, the effective
decision revision and frozen amount are re-read after the lock, the payment's whole Returns budget
(Pending+Succeeded of EVERY case) is counted under one stable advisory key, and the attempt + case state commit before
the external port. A missing/foreign/non-successful payment fails closed; a duplicate answers its stored identity.

**B (F2)** `ReturnResolutionPolicy` is the single reviewed-portion authority: accepted per condition, rejected split
into received/unreceived, exact six-decimal units, refuse-never-clamp, money = accepted × STORED unit price with the
requested amount immutable and the approved amount frozen before the first attempt. Settlement restocks only accepted
usable, needs each accepted portion's own durable completion, and an unbound accepted-usable portion is a typed PENDING
blocker instead of a false completion.

**C (F3)** Rejected received goods keep their delivery-allocation occupancy until an evidenced handback; the custody
command validates case/item/condition/quantum/ceiling/recipient/proof and compares the whole immutable intent on replay;
whole-case rejection exposes the same handback; custody is zero money and cannot reopen effects.

**D (F4)** Reviewed decision and resolution versions travel in the frozen command; a NEW effectful command refuses a
missing or stale version; the decision revision/baseline, case status, frozen approved amount and capacity change are
persisted inside ONE locked transaction; occurrence identities are cryptographic digests of the canonical intent.

**E (F5)** A legacy case without positively provable slices publishes `return.allocation.legacy_ambiguous` and writes
nothing instead of guessing the oldest slice; new claims keep oldest-eligible-deadline-first, expired-slice refusal,
same-instant merge and zero-row insufficient capacity.

**F/S1–S5** Customer act is now viewer-scoped (`v2`), storage-probed, exact-byte re-verified, epoch-fenced and
non-overwriting; the modal uses the shared design-system `Dialog`; command outcomes use the single existing
react-toastify container; unknown unit precision has one honest read/write policy; the workspace shows human locator,
product/unit, requested vs approved money, accepted vs restocked and outstanding custody, never "all refunded" for a
zero-owed/rejected case; the stale 276 mounted fixture was updated to the current wire contract without weakening
assertions.

## Proof summary

* Returns project **172/172**; AMSC guards **34/34**; **real-PG rollback-only 3/3** (`committedRows=0`);
  `dotnet build` 0 errors; Host publish 0 errors.
* Frontend: `tsc --noEmit` 0; returns bundle **44/44**; `npm run build` success
  (`BUILD_ID 1qgSFICgCehoh6FzH3a0f`).
* Served: Host `5088` PID `28300`, dll SHA256 `9C5ECED5…63F62D`, `/health` + `/health/ready` `200` SingleStore &
  messaging Healthy, `/__platform-commerce` → `store-alpha`; Next `3000` PID `29448` serving
  `chunk 7944-5791a99ccf276c21.js` with the new resolution/custody/reopen tokens.
* Read-only live wire: unauthenticated case GET → `401 admin.actor.missing`; privileged GET → `200` `Completed`,
  `approvedAmount null`, `custody 0`, `decisionHistory 0`, `attempts 1`, recovery `Settled`, `updated_at` unchanged;
  unknown case → `404 return.missing`.
* Data: zero reset/reseed/backfill/cleanup; three stored cases remain `Completed` with `approved_amount NULL` and
  original `updated_at`; the four new tables hold 0 rows; no live POST and no provider/money/stock call.

## Evidence limits (declared)

Money/stock/messaging are proven with controlled ports — no real PSP/Wallet/Inventory/notification effect and no
committed two-writer race. The relational facts prove the shared budget lock, the cross-case budget read and the atomic
version-fenced approval, but the effectful first-refund commit path itself is refused under a caller transaction and is
covered by controlled ports. The mounted FE suites are jsdom (behaviour, not geometry). No live customer session,
checkout or active return case exists, so the active operator/customer flow has no live walkthrough; all three stored
cases are settled history and were read only.

## Next

`Recovery-Next-Task: NONE` · `Next-Recommended-Task: NONE` · `automaticNextImplementationTask: NONE` ·
`STOP_FOR_ARCHITECT`.
