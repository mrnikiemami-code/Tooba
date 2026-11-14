# Architect review — 265-R1 — 2026-11-14

Decision: PARTIAL / NOT ACCEPTED for the complete A–E repair contract. No automatic next implementation task issued by this review. User visual/functional acceptance remains pending.
Reviewed code commit 906d943c4a3e0df4bbab45e84c7ae06aff44b959 and source at reported main 8ccf2f805a87c1e11841468695bd43171e96dd87 via GitHub. This was source review, not execution of the worker's tests or local runtime.

## Confirmed improvements
Order now persists closure rows; Inventory release uses a transaction and reservation row lock; settlement filters damaged resolutions by source shipment/receipt/line; claim-to-case link recovery exists; successful refund is separated from restock failure; persisted case links and Persian detail mapping were added.

## Remaining concrete issued-scope gaps
1. InventoryReturnObligationRelease.cs: all three replay branches (initial lookup, inside lock, catch/winner) return an inbox result by key without comparing recorded ReservationId or requested quantity. Inbox Quantity stores RELEASED amount, not original requested amount, so it cannot distinguish a clamped request from changed intent. Report's changed-intent conflict claim is unsupported by this code. Final response also labels a Consumed no-op as Released. Keep transaction improvement; finish exact identity and truthful status. Focused changed-reservation/changed-request tests and rollback-window proof are absent from new A–K additions reviewed.

2. ReturnDirectory.cs: ApproveAsync and RejectAsync still load mutable state with no shared row lock, durable approval CAS, or concurrency token in the fetched ReturnsDbContext. Two contexts can both read Requested, then approve/refund versus reject/release the claim. Checking the loaded Status only fixes sequential late rejection. A completed courier case at RefundReleased falls out of the resume branch and calls ordinary request.Approve again, rather than returning its recorded completion. The issued concurrent approval/reject and completed public replay proof is not present. Preserve the improvement separating money success/disposition; finish these exact transition boundaries.

3. CourierReturnSourceDirectory.Plan explicitly throws fulfillment.returns.replacement_partial_claim_unsupported when an existing resolution.Quantity exceeds the selected case quantity. ClaimAsync accepts that partial case, and EnsureCourierOriginClosureAsync closes its Order obligation BEFORE Plan refuses settlement. Thus the required partial-selection path can strand an approved, partially closed case rather than preserve a runnable remainder. Source scoping is improved, but partial source/hold conservation is not implemented. Unique active key also serializes an entire receipt+line+condition, not distinct partial quantities; do not describe it as support for independent non-overlapping partial claims.

## Operator proof
The worker explicitly omitted the issued real create → reload → persisted case → approval navigation and touched-screen mobile verification. Served bundle text and mapping tests establish shipped code, not a working operator handoff. Do not transfer unverified financial approval to the user's acceptance trial.

## Recovery
No scheduling/mobile/notification redesign or broad test sweep. Original 265-R1 A–E contract remains the source of these obligations; no new business stage. One worker at a time, one canonical tooba_alpha, user/screenshot rows read-only, controlled financial ports only. Preserve all accepted 263/264 scope. No Architect ACCEPT, no all-shipping-fixed claim.
