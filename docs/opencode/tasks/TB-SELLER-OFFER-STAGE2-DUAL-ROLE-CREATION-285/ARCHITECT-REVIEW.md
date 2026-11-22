# Architect review — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

Reviewed: 2026-11-22
Decision: **REPAIR_REQUIRED (one consolidated post-matrix repair)**
Code SHA reviewed: `02abb9d4b69f2bb7ebb023c657d62f203ab95475`
Evidence SHA reviewed: `13e74a303ba7d01e49c808d69e343b790f8f3759`
Stage: 2 only. Architect ACCEPT NOT ISSUED; owner visual acceptance NOT CLAIMED.

## Positive audit

Inspected current connected evidence, RESULT, RECEIPTS, OpenCode validator, Impeccable critique, source code of Offer Create, Admin endpoints/authorization, SQL list paging and Catalog variant search. The recorded connected Chromium→Next→Host→isolated PostgreSQL run reports all eight REQs and 23 concrete passing assertions, real Admin create for seller A, A GET and B denial; view-only admin denials, old variant discovered beyond 100, server list paging with >200 offers, mobile FA/EN geometry, OpenSpec strict and Impeccable logs. Source comparison is confined to Stage 2; historical Cursor contracts are untouched. Controlled response substitutions for wrong/lost/error branches are disclosed. Those claims are accepted as recorded evidence, not a fresh independent rerun of the removed test DB.

## Material gap — not covered by the 23 assertions

`src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/CreateOffer/CreateOfferCommand.cs` derives the deterministic Offer ID from `sellerPartyId|catalogVariantId|requestId`. When an existing ID is found, it checks **only** seller and catalog variant and returns `readModels.DetailAsync(existing)`, without matching the first request's other creation-intent values such as `sellerSku`, requested status and return policy. Accordingly a reused requestId with same seller+variant but a **different SKU** can return an apparent success for the original, different intent. The connected REQ03 test uses identical bodies on its two POSTs; the separate differing-intent conflict was never exercised.

This conflicts with the issued task's W2 requirement "**different intent fails closed**" and ARCHITECTURE-PLAN.md section Creation "**a mismatched intent fails closed before any write**", as well as deterministic no-false-success contract. The worker's PASS cannot close that missing negative case.

## Allowed bounded repair in SAME task 285

1. Do not rewrite issued TASK.md/REQUIREMENTS.json or reinterpret 23 old passes as failed. Preserve original evidence identity.
2. Add one explicit connected negative test using a disposable DB, valid seller and published variant: first create with stable requestId/SKU-A succeeds; replay the same requestId and seller/variant with SKU-B (and optionally another differing commercial creation field) MUST yield a typed conflict, no success, no second Offer, zero price/inventory writes; independent SQL+GET confirm original. Test both admin and seller entry when possible; verify a byte-identical retry still returns exactly original OfferId.
3. Fix Offer-owned idempotency/replay contract without changing unrelated Catalog/Pricing/Inventory ownership, and without dropping unique constraints, pretending the changed intent is successful or depending solely on browser localStorage. Document choice of durable intent fingerprint vs deterministic identity, effects of concurrency, no migration policy. If safe fix inherently needs an unapproved migration, **BLOCK for architect** instead of applying it.
4. Include boundary for simultaneous identical creation attempts and verify no duplicate or accidental 500 if the first write wins; do not claim concurrency PASS unless actually tested.
5. Execute ONE final complete 8-REQ connected acceptance matrix on real served Chromium→owning Host→isolated PostgreSQL, plus the new negative(s), strict OpenSpec, focused builds, Impeccable post-check only if UI changed, OpenCode checker and tamper self-tests. Recompute source/build/evidence SHAs, document cleanup and source diff. Original 23 positive assertions remain minimum, not substitute for new negative.
6. No additional repair loop: this is the **one allowed consolidated post-matrix repair**. If the final rerun does not satisfy every mandatory case, report `BLOCKED/NOT_READY`, STOP_FOR_ARCHITECT. No new task and no auto Stage 3.

## Non-blocking known UI observations

Pre-existing 10px AdminShell nav/label and two contrast advisories are documented separately; they are not grounds to expand this repair. Owner visual acceptance remains pending.

This review is an architect instruction to complete **the same Stage-2 task**, not an independent new feature/task. Cursor governance, AGENTS, roadmap and shipping Stage 3 remain untouched.
