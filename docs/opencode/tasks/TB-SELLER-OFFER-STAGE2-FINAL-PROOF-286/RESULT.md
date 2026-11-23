# RESULT — TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286

Status: **BLOCKED**
Readiness: **NOT_READY_FOR_ARCHITECT_ACCEPT**
Decision: **BLOCKED_ARCHITECT_DECISION**
Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0
Reference Code SHA: `507d7933a54fe067c240beb6aeb62b63f4b110c0`
Parent evidence SHA: `5dd4d7b47aa64e821b19567d73d8bd39ade8c4a5`
No product code, migration, Cursor, roadmap, OpenSpec spec or live data changed.

## Actual timing

- startedAt: `2026-11-23T03:48:00.1208060+03:30`
- endedAt: `2026-11-23T03:53:55.3494090+03:30`
- elapsedMinutes: **5.92** (5 minutes 55 seconds)
- focused harness: 6.028 seconds (`2026-11-23T00:22:58.073Z` → `00:23:04.101Z`)
- setup/review were not separately instrumented; no predetermined/rounded time is claimed.

## V1 — provenance: PASS

`evidence/focused-286.cjs` writes every observed value directly to `evidence/focused-286.json`; the committed harness SHA256 is `d6468b26589f87a9d22a01499a7cb028c404364ef9a79a992a4d217bf205e7d1`, matching the producer field. `VERIFY.mjs` parses each critical observed value and verifies the producer script hash; its single mutation self-test rejects `secondRowCount=1` with `ok=false`. Raw log: `evidence/focused-286.log`.

## V2 — conflict: PASS

- Seller A: same requestId+intent twice → 201/201, same Offer `172194c0-0f1c-5af7-a511-eeeb29ea7879`; changed SKU with the same requestId → 409 `offer.create.intent_conflict`; original rows 1, changed-SKU rows 0.
- Admin for seller B: same requestId+intent twice → 201/201, same Offer `11c9e867-0690-72f4-a564-848e90f0c017`; changed SKU → 409 `offer.create.intent_conflict`; original rows 1, changed-SKU rows 0.
- Independent SQL before/after each conflict: `pricing.prices` 7→7, `inventory.stock_positions` 7→7, `inventory.reservations` 1→1; price/stock/reservation writes 0. This is measured from owning tables, not inferred from Offer count.

## V3 — concurrency: PASS

Two simultaneous identical seller creates → 201/201, same Offer `34d9aeef-b5b2-6622-8337-f3e5ac263edc`, one Offer row, errors null/null; `pricing.prices` 7→7, `inventory.stock_positions` 7→7, `inventory.reservations` 1→1.

## V4 — late replay after legitimate edit: **FAIL / ARCHITECT DECISION REQUIRED**

Exact sequence on Offer `a839eb1c-8909-2a4c-5867-b2824eecf4ef`:

1. Create with requestId `tb286-v4-late`, SKU `TB286-LATE-A`, status Draft → 201.
2. Authorized admin PATCH SKU to `TB286-LATE-B` → 200; persisted current value becomes B/Draft.
3. Replay the **original** requestId+intent A → **409** `offer.create.intent_conflict`.
4. Replay the same requestId with **changed** intent B → **201**, same OfferId.

This demonstrates that the current comparison uses **mutable current Offer fields**, not the immutable original creation request. A legitimate SKU/status/policy/limit edit therefore makes the original idempotency key reject its own saved intent, while a changed intent that matches the later current state is accepted. That does not satisfy the original “resumes saved intent” contract and can falsely accept a changed intent after edit.

A correct repair needs a durable immutable creation-intent fingerprint (or an architect-approved explicit policy change) and likely a schema/ownership decision. No product code or migration is changed here; per the task this is reported as `BLOCKED_ARCHITECT_DECISION`, not self-repaired.

## Evidence and hashes

- `TASK.md`: `2659b8ab475d778781549aa0abb795f9bc6756f12861a04a759a1e8caa5c2f41`
- `VERIFY.mjs`: `0f950c386f98e473c8254593b722b324b2502e544379f684e76201c2f2af3e8f`
- `evidence/focused-286.cjs`: `d6468b26589f87a9d22a01499a7cb028c404364ef9a79a992a4d217bf205e7d1`
- `evidence/focused-286.json`: `e38ef5e1f4aa3b1bd137f9b26e8f59bc1f5d1b45ddf87dca04c52f256d0f7b98`
- `evidence/focused-286.log`: `f4c64c8e75ed471a1a59ae0f619d0b583651f35e5a6cb0aedb6d37227f9b3097`
- `evidence/verify-286.log`: `ab3c669ff32b71d4ff5d224bc62f3441ebd3a483b335f292c7482a1d9f8ff179`

## Cleanup and scope

- Disposable DB `tooba_offer286_proof` dropped.
- Test Host 5093 stopped; test port closed.
- Canonical 5088 `/health` 200 and 3000 `/fa` 200.
- Product source diff after `507d7933a` is empty; AGENTS/Cursor/roadmap unchanged; no migration.
- Original 285 task/evidence untouched. This task added only task-owned scripts/evidence.

`STOP_FOR_ARCHITECT`; automaticNextImplementationTask: NONE. No Stage 3.
