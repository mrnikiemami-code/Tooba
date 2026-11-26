# R10 root-cause and recurrence prevention

Authority: docs/opencode/ARCHITECT-QUALITY-GATE.md owner directive 2026-11-26.
This file records why the original R10 candidate missed the architect findings. It does not replace the independent review.

| Finding | Expected control | Actual evidence | Failure mechanism | Classification | Corrective action | Prevention / verification |
|---|---|---|---|---|---|---|
| F01 | Channel policy and tests use one current StoreCommerce contract | Candidate policy took a string while OfferChannelPolicyTests still called ToobaEdition; tests were not recompiled after the type change | Test project was executed with `--no-build`; stale assembly hid a compile-incompatible test contract | Worker noncompliance + stale build/evidence | Named-only parsing against StoreCommerce; tests rewritten and rebuilt | Offer test build is mandatory before test execution; 130/130 current tests |
| F02 | Inventory location eligibility is owner-scoped or explicitly store-owned | InventoryLocation had no owner field; active tenant filter was accepted as eligibility | Ownership was never modelled; active was treated as eligible | Spec coverage gap + architect issuance ambiguity | Nullable seller owner migration; null = store-owned; filter and provisioning both scope owner | R10 repair runtime source check and migration applied |
| F03 | Writes compare caller-viewed version and concurrent owner state | Only xmin existed, which protects overlapping tracked writes but not a later stale request | Concurrency was reduced to EF row version, not request version | Worker noncompliance | ExpectedVersion on commercial/price/inventory/fulfillment; 409 stale; xmin remains concurrent guard | Sequential stale and two-session runtime checks |
| F04 | Owner change and its history are reliably linked | Price/Inventory owner commit happens before Offer history save; actor was null | Cross-module history was treated as ordinary second write | Spec/contract coverage gap (no outbox projection required) | Actor propagated; history UI/error semantics repaired; reliability remains unfinished | Remaining: owner outbox/projection or equivalent; explicitly not PASS |
| F05 | Create/apply and edit verify exact committed state before retry | Matcher accepted any nonempty seller; initial create omitted conditions; local Number() coercion | Loose reconciliation and partial write ordering | Worker noncompliance | Exact seller/offerId/status/channel matcher; conditions in create; pre-write validation; scoped draft | 42 runtime checks plus create/edit counterexamples |
| F06 | One DB-backed locale authority for shell, grid, controls and API headers | Registry helper was feature-local; global shell type remained fa/en; grid coerced non-English to fa | Feature workaround instead of central provider | Architect issuance/spec gap + worker noncompliance | Active UrlPrefix/code and grid fallback repaired | Remaining: global shell provider; explicitly not PASS |
| F07 | Offer files live in canonical feature folders; no flat dumping ground | admin-offer-api/intent and seller transport were flat | Guards reported failures but were reclassified as unrelated | Worker noncompliance | Files moved to app/admin/offers and app/vendor-panel/products; imports/typecheck/build pass | FE-BOUNDARY pass; no Offer file in FOLDER/SIZE failures; unrelated debt remains |
| F08 | Exact 42 IDs, independent evidence, no recycled build | Descriptive check names, no ID mapping, stale test assembly, hash discrepancy | Evidence generation was not tied to matrix IDs or exact build | Worker noncompliance + stale build/evidence | 42 runtime + 22 repair checks; 42-ID mapping; hashes/build IDs pinned | Architecture guard log and mapping recorded; checker negative-control harness still incomplete |

## Unresolved control gaps
1. F04 reliable cross-module history still needs an executable owner-outbox/projection control. A prose rule alone is insufficient.
2. F06 needs a central DB-backed locale provider test covering shell, grid, controls and Accept-Language, not only Offer route fallback.
3. F08 needs a negative-control harness that rejects missing/dropped/duplicated 42 IDs and mismatched evidence hashes.

## Hypotheses vs proven
Proven from source/logs: stale test assembly, missing owner field, xmin-only concurrency, separate history write, loose matcher, flat paths, descriptive evidence names.
Not claimed: hidden model reasoning or unobserved local intent. Unrun checks remain explicit in ARCHITECT-REVIEW.md and this status.
