# TB-281 user review notes

Worker status is PARTIAL, not final product acceptance.

What landed for Architect/owner look:

- Admin can create a draft for a chosen shop (server-validated).
- Seller media upload/list/get exists under `/v1/seller/media` with tenant+shop binding.
- Stories refuse empty publish/submit; schedules become visible when start is due without rewriting status.
- Approve-and-publish / approve-and-schedule / seller return-to-draft / stop carry expected version.
- FE transport maps the new snapshot fields and can send the new review bodies.

Still open before sellable shared editor (delivery two + remaining backend):

- Full G1–G7 proof matrix
- Admin grid filter by shop display name
- Stronger CTA/product ownership proofs
- UI editor/list (explicitly out of this task)

No live Story mutation was performed on canonical data during this worker pass.
