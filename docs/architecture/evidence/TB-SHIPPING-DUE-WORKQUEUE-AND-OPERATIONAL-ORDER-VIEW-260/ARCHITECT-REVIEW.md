# Architect review260 — PARTIAL: incorrect physical row identity affects deadlines and targets
2026-11-10; independent source review main a5de61258f71ce5cceef30729a35ffac0751a972.
Accepted direction: pure explicit-clock accepted-plan timing, full-filter-set counts before paging, shared finance-free operational panel. No independent Windows test execution claimed.

BLOCKING concrete source defect:
AdminFulfillmentWorkQueueQueryEngine.MapPageAsync returns rows.Select(unit=>...), ONE row per FulfillmentUnit/seller. It passes ALL plansBySellerOrder to Classify while pending is computed across the whole unit, and Earliest(plans).ShippingGroupId becomes the target. It never scopes the plans to the still-pending group/parcel or accounts for common-package membership.
Repro: same seller group A due today already dispatched, group B tomorrow still pending. pending=true due to B, but Classify chooses A's earliest plan -> false overdue/today and wrong group locator. Two Created separate parcels become one seller row; a common package across sellers becomes multiple rows. Claimed one physical unit per row is not implemented.
Additionally MapPageAsync reconstructs ShipmentSnapshot without ShippingGroupId/resend lineage; do not lose257/258 semantic fields when fixing row ownership.
Route target summary also computes readiness only from remaining packed quantity, calling a group with an existing Created parcel and tracking/dispatch work 'کار فیزیکی باقی نمانده'. Resolve target status from actual parcel/package/capability facts, not fresh allocation quantity alone.

Issue5 PARTIAL; issues1–4 remain accepted (4/5). Only260-R1, no261. Preserve accepted UI/timing design; fix exact row ownership/plan/locator/status and focused cases, no broad loops. Same canonical DB, no provider or user business mutation.
