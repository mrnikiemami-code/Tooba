# P1-B ENTRY_VERIFY result

Status: BLOCKED — entry provider completion is partial; no P1-B product edits.
Source: 3aac14d1ab5bc31959b5a32853ebc5f0be43c5fe; product entry db7bb1bd3 (P1-A product f7f8b38 unchanged from D2).

## PASS / reused exact-source evidence
- Offer.Tests build PASS; complete suite 139/139; focused responsibility + error localization 14/14 (entry-offer-* logs; source unchanged since prior entry).
- Real Order line commitment: inserted one actual `"order".order_lines` row for a fresh Offer, then admin responsibility Seller→Platform returned 409 `offer.fulfillment.change_protected`; raw entry-order-line-denial.log. This exercises the real Order bridge (`Any(OfferId)`), covering paid/restorable-recorded line denial at the reader contract.
- Prior D2 success reused only because P1-A product is unchanged: no-commit assign/reassign/clear, Held/Consumed/zero-allocation denial, and actual Inventory lifecycle harness.
- Architecture baseline/guard and Offer suite prior evidence remain applicable; no source change since 9a1/D2 product f7f8.

## NOT_RUN (mandatory remaining)
- Concurrent new hold + responsibility assignment.
- Concurrent allocation + responsibility assignment.
- Stale owner physical mutation.
- Concurrent checkout captured-owner/reused-hold + assignment.
- Partial release/consume.
- First-allocation concurrency.
- Full order-service paid recovery/replacement.
- Repeated HTTP admin/seller capability and history target/location matrix.

No receipt exists for P1-A; this remains entry evidence only. No product/test/guard/matrix changes. Fixtures cleaned and baseline SingleStore Host restored.
