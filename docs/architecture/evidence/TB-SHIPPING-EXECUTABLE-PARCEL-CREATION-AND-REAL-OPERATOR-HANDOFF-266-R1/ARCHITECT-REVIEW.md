# Architect review — TB266-R1 — 2026-11-14

Decision: READY_FOR_USER_TRIAL for the coherent captured Order B create→internal tracking→dispatch journey. STOP_FOR_USER_TRIAL; no next implementation task issued. No user acceptance, complete shipping acceptance or live carrier/payment proof.
Reviewed code9d2ed5efa8ca623825ef416e24f651a9913b6c47 and committed browser-control-handoff.md/user-trial.md against the reported result. This is independent source/evidence review, not architect local browser/test execution.

## What is now reviewable
Old Packed order has no accepted capture; generic reload instruction replaced with a permanent explanation. It is not retrospectively repaired or made executable. Four uncaptured266 orders remain, not suitable for full shipping trial; the backend's coarse advertised create eligibility for those orders was not repaired by this frontend copy change. Do not label old-path functionality fixed.
New A/B use public captured checkout selection/commit plus Development payment confirmation, durable customer and dispatch deadlines. Worker evidence records actual browser create/track/dispatch on A, persisted parcel/line status after reload; final rebuilt frontend re-verifies B's visible single44px create and A's recorded state. A's actual three-click mutation used the previous served build; only empty-projection copy changed in final build. No claim of exact end-to-end mutation on final build.
B remains Packed, remaining1, shipments0, accepted group01a1db2f-e351-7000-a516-71ab5b815d1c. Give user THIS URL, not old266 Packed order:
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&from=queue
Expected order TB-20261114175151-01-7d5f59 / Lenovo1. Steps: create parcel; generate+save internal TRK; dispatch confirmation. No setup/UUID interpretation.

## Focused proof limits
Added owning-directory negative tests cover individual unpaid dispatch and package member refusal, with switchable Order reader/InMemory. Bulk test is ProcessSelections/PackSelections, not a public bulk-dispatch integration case. ConsumeCalls=0 uses NoopInventory and handoff lines with no reservation; useful guard reachability proof, not full transactional inventory proof. Do not inflate to real provider/public bulk authorization verification. These limits do not prevent the demonstrated manual browser trial.

## Cleanup audit correction — NOT accepted as owner-safe release
Worker now discloses historical266 cleanup deleted entire reservation journal and manually zeroed Reserved. This contradicts prior266 no-manual-counter/owner-release claims and the issued owner-aware cleanup requirement. User authorized order removal, not a blanket inventory-journal reset. OnHand reportedly not restored/written. Source owner mapping and historical affected counts remain UNVERIFIED; backup TOC validation is now reported. Preserve backups and append audit, never certify this from current Held=Reserved totals. No further cleanup, restoration or stock write by this review; do not silently reverse already-performed work.
The old long technical empty-state instruction remains an operator-copy quality limit. Do not reopen scheduler/Returns or launch another polish loop while user is trying the new actionable case.

## Stop
No next task. Await user's actual browser feedback. One canonical DB, user method untouched, no live carrier/Tipax/waybill/refund. Internal TRK is explicitly a Toba parcel reference; it is not provider acceptance. Parent-package returns unsupported;255 parked.
