# Architect final review — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

Date: 2026-11-23 (architect review of worker report 2026-11-22)
Decision: **BLOCKED_EVIDENCE / NOT_READY_FOR_ARCHITECT_ACCEPT**
Stage: **2 only**. The one consolidated post-matrix repair has been used. **No automatic second repair, no Stage 3**.
Reviewed code SHA: `507d7933a54fe067c240beb6aeb62b63f4b110c0`.
Reviewed evidence SHA: `5dd4d7b47aa64e821b19567d73d8bd39ade8c4a5`; final HEAD `45cfa5dcb6e8f6471d8071e7017857cb425cbd73`.

## Verified improvements

- Read `CreateOfferCommand.cs`: identical-key replay now compares seller, catalog variant, channel, normalized SKU, current status, return policy/window and min/max quantities and emits `OfferErrorCodes.CreateIntentConflict` on differences.
- Read `OfferStore.cs`: PostgreSQL unique-key race `23505/pk_offers` detaches losing EF entry, rereads winner; handler checks intent rather than blindly accepting the winner.
- The committed `request-id-285.log` records seller and admin same-key replays 201/201, changed-SKU 409 `offer.create.intent_conflict`, and two concurrent identical creates 201/201, one row. Full browser evidence records 23 connected assertions, plus four new claims in the manifest; strict OpenSpec, builds, cleanup and Impeccable are recorded.
- Compared base architect review `3e7cdf3...` through worker final HEAD: product changes are Offer-owned and two UI error-message changes. No changes listed to AGENTS, Cursor contract or migrations; unrelated intervening OpenCode governance commits are present but not Stage-2 product scope.
- The repaired code plausibly fixes the originally identified immediate changed-SKU replay bug. This review does not claim a fresh independently run PostgreSQL/browser test, since the disposable DB was already dropped.

## Mandatory proof/provenance defects

1. **Committed test source and claimed JSON facts do not match.** `evidence/request-id-285.cjs` writes `sellerDifferent` with only `status,error,rows`, and likewise `adminDifferent` with only `status,error,rows`. It does **not** output `originalId`, `originalRows`, or `secondRowCount`. Yet `evidence/request-id-285.json` contains these additional fields and reports `sellerDifferent.originalRows=1,secondRowCount=0` and `adminDifferent.originalRows=1,secondRowCount=0`; `request-id-285.log` does not contain those fields. The source-to-result provenance is unreconciled. JSON hashes alone prove byte integrity after commit, **not** that a committed script measured the asserted values.
2. **Zero Pricing/Inventory writes after a conflicting request are asserted but not independently measured in the new focused script.** It selects counts solely from `offer.offers` by SKU pattern; it does not query the Pricing or Inventory tables before/after the conflicting request. A 409 and one Offer row support the immediate conflict behavior, but do not independently establish the broader "zero price/inventory writes" assertion required by the prior architect review.
3. **Verifier does not independently validate focused new observed facts.** `VERIFY-OPEN-CODE.mjs` adds four assertion *names* to the 23 browser names, checks the JSON artifact SHA, and prints 27; it does not parse `request-id-285.json` and assert its 409 status, one-row/unchanged state, or concurrent outcomes. The extra four claimed PASS values can therefore pass the checker without verifying their content.
4. **Durable original-intent semantics are not fully established.** `MatchesCreationIntent` compares replay request against **current mutable** Offer fields, not an immutable original request fingerprint. After legitimate Offer-owned SKU/status/policy/limit edits, replay of the original creation intent could receive 409; conversely a key with modified terms matching the later current state could receive success. This is a source-identified limitation for historical/late replay, not evidence of failure in the tested immediate retries. Requires architect decision whether this is inside the original "saved intent resumes" promise; no migration or new product task is preauthorized.

## Decision under bounded repair governance

**Do not issue ARCHITECT_ACCEPT** on the strength of the current `PASS 27/27` claim. The one permitted post-matrix repair and final rerun already occurred. Do not silently request another worker repair cycle or broaden 285. Status **BLOCKED_EVIDENCE / STOP_FOR_ARCHITECT** pending an explicit owner/architect decision on whether to authorize a narrowly scoped verification-only exception or defer unresolved proof/late-replay behavior in a separate future stage. If authorization is given, the next action must be narrowly scoped and versioned; preserve immutable original TASK, requirements and historic evidence.

The owner has not visually accepted the UI. Shipping Stage 3 remains not authorized.
