# TB-279-R2 — RESULT

Task: `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2`
Parent: `…-279-R1` · Channel `tooba-main` · Worker `tooba-worker-01` · Agent `cursor`
Expected-Base-SHA `6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb` (ancestor of HEAD, verified)
Reviewed-Code-SHA `3f8b670cd4a16db9ef8e1c4766704b53a075d364`
Claim commits: `ff69838cf` (docs-only issuance) → `22a0ec148` (containment) → `3b1ce75d9` (code)

Status **PASS within the issued A–G scope**, decision
`ONE_CONNECTED_REVIEWED_WIRE_CONTRACT_ONE_PORTION_CAPACITY_AUTHORITY_AND_ONE_EXACT_ORIGINATING_CUSTOMER_INTENT_WITH_TARGETED_NEGATIVE_CONTROLS_AND_PRIVATE_LOCAL_KEY_PRESERVATION`,
NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT Marketplace/provider/financial readiness, NOT a claim that
the historical key exposure is fully remediated.

| Area | State | What this means |
| --- | --- | --- |
| A containment | untracked, preserved, still live | the plaintext Data Protection key XML, 20 task scratch scripts and `RESULT.bridge.txt` are out of the Git index and covered by scoped ignore rules (`22a0ec148`); the files were privately copied and hash-verified under `D:\temp\tb279r2-containment` BEFORE untracking, the active runtime key file was not deleted, and `key-containment.md` records `Security-Exposure-State=CONTAINED_PENDING_REVIEWED_RETIREMENT` with the exact removal/rotation procedure and its impact on existing cookies/protected payloads |
| B wire contract | one frozen shape per action | `start_refund` carries the reviewed decision revision as a route/query parameter only, `retry` carries the failed-attempt id plus the reviewed decision revision, approve/reject carry both reviewed revisions, the resolution dialog sends the FIVE explicit per-condition portions and the custody dialog sends `proofKind=operator_attestation` with its proof reference; the modal freezes the revisions of the snapshot it loaded, and the same values travel real modal → `return-api` → route/model binding → CQRS → Directory without a fresh GET |
| C portion capacity | accepted + owed, never owed alone | `ReadOccupancyAsync` reads live allocations for EVERY case of the seller order, so a rejected-only seller cannot gain free capacity; custody trims the line to `accepted + outstanding custody of that condition`; the required counterexample is a stored fact: delivered/requested/received 3, accepted 2, rejected-received 1, handback 0.5 ⇒ one live allocation of **2.5**, remaining eligibility **0.5** |
| D reviewed versions + unit quantum | refused before any row | a NEW approval/rejection over a published plan refuses a missing revision with `return.resolution.review_version_required` and a stale one with `return.correction.version_stale`; resolution and custody portions must align to the owning line quantum, so `0.0001` on a 3-decimal line (and `0.5` on a whole-piece line) is `return.resolution.portion_invalid` / `return.custody.quantity_invalid` with zero rows |
| E evidenced custody | no unproven claim | `carrier_delivered_back` is recognized and refused with `return.custody.proof_kind_unavailable` (no owning carrier reader exists in this increment) instead of being stored as verified delivery; legacy ambiguity now considers expired AND live slices, so an unbound legacy case is never guessed onto a slice |
| F customer intent | epoch fenced on every path | the modal invalidates its epoch on unmount/close/scope change, fences before EVERY child send, treats legacy `v1`/tb278 evidence as unresolved and blocking instead of "nothing pending", and `recordChildOutcome` compares the exact originating envelope and frozen child before touching state |
| G proof | connected, owning, focused | Returns **177/177** (15 in the 279 directory suite, including the three R2 counterexamples), Host **37/37** (34 AMSC guards + 3 rollback-only relational facts, `committedRows=0`), FE unit **29/29** and mounted **34/34** (11 customer modal + 23 admin workspace), `tsc --noEmit` **0**, `next build` **0** (`BUILD_ID IdLHLUerRPBB-OzXddPwk`), `dotnet build Tooba.slnx -c Release` **0 errors**, Host publish **0 errors** |
| served | refreshed | Host `5088` PID `7808`, dll SHA256 `7B8AAB852972C1CDCED3A12AA2A057700183FA16AE1A2FA89A60D209CF499977` (`/health` 200, `/health/ready` 200); Next `3000` PID `9904`, `BUILD_ID IdLHLUerRPBB-OzXddPwk`, `/fa/admin/returns` 200 |
| schema/data | untouched | R2 adds NO migration and no DDL; the single R1 migration remains the only Returns schema change and was applied once after a verified pre-apply backup. Zero reset/reseed/backfill/cleanup/restore, zero live business/provider/money/stock write in this task |

## What was actually repaired

1. **Wire shape** — the three admin refund commands, the seller decision commands and the Order operations adapter now
   carry the reviewed decision/resolution revisions; the resolution command carries the five explicit portions; custody
   carries the reviewed proof kind. Missing values are typed refusals, never defaults.
2. **Portion capacity** — one authority (`accepted + owed custody`) drives custody trimming and eligibility; held
   allocations are read regardless of the active-status set.
3. **Version/quantum fences** — approve/reject re-read and compare the reviewed plan revision and the frozen amount under
   the existing locked transition; resolution/custody portions are validated against the owning line quantum before any
   insert.
4. **Positive evidence** — the carrier proof kind is refused, legacy attribution considers every plausible historical
   slice, and a reopen re-acquires its exact previously owned slice.
5. **Customer act** — unmount/close epoch invalidation, per-send fencing, legacy evidence recognition and exact
   originating-envelope comparison on child outcomes.

## Known limits

- Money/stock are proven with controlled ports; no real PSP/Wallet/Inventory effect was exercised (none is authorized).
- The 2.5 occupancy fact is in-memory EF over the real `ReturnEligibilityEvaluator`/`ReturnDirectory`; relational
  occupancy was not re-run for this counterexample.
- Customer active-dialog behaviour rests on the mounted jsdom suite (behaviour, not geometry); there is no live active
  case to open, so the served check is read-only history plus the typed refusal paths.
- `carrier_delivered_back` is refused rather than implemented; there is no carrier return reader in this increment.
- Key exposure is contained in Git only: the material exists in history and on disk, so retirement/rotation still needs
  an explicit reviewed decision.
- The whole-repository frontend `vitest` sweep fails at collection (`jsx to preserve`); the repository's supported
  runners are `node --experimental-strip-types --test` and `tsx --tsconfig tsconfig.mount-tests.json`, which are the
  commands reported above.

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
