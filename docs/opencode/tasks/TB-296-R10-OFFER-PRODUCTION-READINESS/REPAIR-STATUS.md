# R10 consolidated architect repair — status

STATUS: REPAIR_PARTIAL / NOT_READY_FOR_ACCEPTANCE
Source base: 92b4ad74761cfb93112fae3ab8403284f4926962
Source repair commit: ee0cfc68aa81f217c1378c0c52bed86a2232b0e9
Review: docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/ARCHITECT-REVIEW.md

## Repaired and verified
- F01: channel policy accepts only defined named SalesChannel values; numeric/composite/unknown values fail closed. Query/create/duplicate/resolver use StoreCommerce.SalesChannel. Offer tests rebuilt against the real string contract; 130/130 Offer tests pass.
- F02: Inventory-owned locations now have an optional seller owner column (migration 20261126140000_R10InventoryLocationOwner). Active eligibility filters `SellerPartyId == null || SellerPartyId == caller seller`; null remains a store-owned shared location. Provisioning checks the same scope. Owner column migration applied to tooba_alpha.
- F03: Offer.UpdatedAt is the caller-viewed version. Commercial/price/inventory/fulfillment commands accept ExpectedVersion, compare at microsecond precision, and return 409 on stale versions. xmin remains the concurrent-write guard. Runtime proves sequential stale 409 with first value preserved and two concurrent writes produce one 200 and one 409.
- F04 partial: authenticated admin/seller actor id now reaches history entries; history UI localizes kinds and shows actor; admin/seller history load failure is distinct from empty and stale pages are cleared.
- F05: exact seller/offerId/status/channel create reconciliation; initial create carries return/min/max; all local amount/quantity/step/reason inputs are validated before any write; numeric inventory is parsed not localized/coerced; no empty detail cast; selected channel and local inventory persist in the owner/variant-scoped draft.
- F06 partial: registry locale resolves active UrlPrefix/code with active/default fallback; shared grid no longer coerces non-English to Persian.
- F07: admin Offer API/intent moved under app/admin/offers; seller Offer API/intent moved under app/vendor-panel/products. In-scope frontend folder/boundary/size findings are gone; offer-workspace is 797 lines.
- F08 partial: 42 original runtime checks pass; 22 architect-repair checks pass; all 42 exact matrix IDs mapped to executed/source evidence in ACCEPTANCE-MATRIX-R10-REPAIR-MAPPING.json.

## Remaining mandatory findings
- F04 reliability: price/inventory owner commit and Offer history save remain separate. A failure between them can still omit the history row. This needs an owner outbox/projection or equivalent reliable transaction pattern; current code is WAL-free.
- F06 full: global shell locale context remains a static fa/en type; arbitrary registry prefixes are handled at the Offer feature boundary, not the central shell. A DB-backed global provider was not completed.
- F08: architecture guards still report 27 unrelated oversized files plus unrelated flat-folder/admin-api export debt; no Offer file is in those lists. Evidence checker negative-control harness remains incomplete beyond the executed stale/zero-write/invalid-location checks.

## Executed final state
- Host: 5088 ready; PID 21152; published Tooba.Host.dll SHA-256 1A9604BD1889294542854B31A0DECB0D6F285A8788E6028A03B2EE63A32B23C8.
- Next: 3000; BUILD_ID yi2wFQjpulk4m_zC5AVy4.
- Runtime: 42/42 PASS, hash 6B6DE49BBED8F42D44D6DEF4AB6621111B6BB8B29A727AFDCE36E548344B943D.
- Repair counterexamples: 22/22 PASS, hash 6FCF1F16A5B0937409A160B6486CE76B48C818F4B3C0AB904C63EACA6D3589A3.
- Offer backend: 130/130 PASS. Frontend typecheck/build PASS. OpenSpec valid with existing long-requirement warnings.

No PASS claim is made for the unfinished mandatory F04/F06 items.
