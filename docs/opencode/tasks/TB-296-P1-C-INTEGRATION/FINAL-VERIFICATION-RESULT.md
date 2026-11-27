# TB-296 P1-C final verification result

Status: BOUNDED VERIFICATION COMPLETE / READY_FOR_ARCHITECT — not final acceptance.
Source: 84f3372a443119fd26eb8ca6e0e533da279c946f.
Served Host DLL SHA-256: 5669d7b08199b4cc8b39d886d496399441cacb7b3d19da251569460a9a767976.
Next BUILD_ID: hMU29Ej5cfQxp1-pC9yp6. DB: tooba_alpha.
Restorable backup: C:\Users\User\AppData\Local\Temp\opencode\p1c-final-20261127173245.dump, SHA-256 589f8c2a0cb525dca1238deee8c05e81bfaf71cab8a19b0862f4b5af74e68166.

## Build/test gates
- Host build PASS; Offer.Tests build PASS; complete Offer suite 144/144 PASS; focused endpoint/error tests 19/19 PASS.
- Pricing 13/13 PASS; Inventory 53/53 PASS.
- Frontend typecheck/build PASS; registry/routing tests PASS; mounted workspace 27/27 PASS.
- Frontend architecture exit 1 with 27 pre-existing baseline violations, no new/Offer-owned violation.
- Strict OpenSpec exit 0 with existing long-requirement warnings.
- No product edits; no screenshots.

## Repaired-candidate verification
- R10-08-N PASS: authenticated admin and synthetic seller explicit null and omitted price returned exact 400 `offer.price.amount_required`; persisted price/revision/facts unchanged; unauthorized seller fulfillment denied without write.
- R10-08-B PASS: explicit zero price persisted as policy allows; negative price denied without overwrite; assignment clear/orphan type behavior exact.
- R10-09-P PASS: actual FA/EN/RU registry metadata, shared in-app language control, direction and draft preservation on create/edit/list.
- R10-09-N PASS: invalid public prefix real 404; unknown panel prefix canonicalized to active default with zero writes.
- R10-09-B PASS: isolated changed default/code-differs-from-prefix registry fixture observed through shared resolver.
- R10-13-B PASS: real 360/768/1440 long-label form/grid geometry, usable controls, no unintended horizontal overflow.
- OWN-09 PASS: real EF commit interceptor, owner-history query fault, fresh-context replay and actual Host history event ID/actor/time/delta matched durable owner fact. First run used a stale temp harness binary; current repo harness was rebuilt and the corrected oracle passed. This was test-preparation correction, not a product failure.

## Consolidated ledger
PASS 58, FAIL 0, NOT_RUN 2, total 60.
- NOT_RUN: R10-01-B (requires two independently allowed trusted channels; DEV SingleStore exposes only configured Marketplace) and OWN-10 (Marketplace capability/seller browser clauses unavailable in SingleStore).
- Historical/reused observations remain unchanged for unaffected IDs.
- A05 scope limitations remain: D2 paths are actual owner/contract/provider oracles but not full order-service certification; R10-13-P omits some original campaign/cart/export/saved-view/sort clauses; no full architect-review-gate receipt.

No final ACCEPT or owner UAT is claimed.
