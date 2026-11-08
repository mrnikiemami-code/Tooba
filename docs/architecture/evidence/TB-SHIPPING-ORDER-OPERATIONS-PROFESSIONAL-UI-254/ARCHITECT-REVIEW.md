# ARCHITECT-REVIEW — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254 (correction)

Appended `2026-11-08` after the 254 worker result at code `5a11a4b8f`. Correction task:
`TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1`.

## Verdict on 254

**PARTIAL for delivery.** The composition correction itself is accepted: normal cards no longer carry
reuse/quantity inputs, a dedicated group workflow dialog exists, and the seller asides keep only genuinely orphan
parcels. 253-R4/R5/R6 technical recovery is retained and must not be reopened.

## Accepted

- Display-only operational delivery section with one deliberate preparation entry.
- Dedicated dialog owning the reviewed quantities + explicit reuse.
- One display/action owner per group-covered parcel.

## Outstanding (closed by 254-R1)

1. The authorized canonical browser DOM/layout was never checked. A 200 shell plus chunk symbol counts is **not**
   a substitute for a rendered, authorized page. A `loadDenied`/401/shell-only page is PARTIAL, not PASS.
2. Long essential item titles still used `truncate` in `GroupItems` and in the dialog rows. Essential product
   identification must wrap instead of ellipsizing, including at 390 width without page overflow.
3. The NEW dialog file was placed in the already-failing flat `app/admin` directory, worsening `FE-FOLDER-001`
   from **11 → 12** novel files. That extra violation is **task-introduced**, not wholly pre-existing, and must be
   removed by placing the unit in its owning feature structure with a public import surface — without adding an
   allowlist/baseline exemption and without migrating the whole admin folder.

## Correction outcome

See `docs/architecture/evidence/TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1/RESULT.md`. The dialog now lives
in `src/frontend/features/order-operational-groups` with an `index.ts` import surface; `FE-FOLDER-001` is back to
the base-parent count of 11 with this task's entry absent; essential titles wrap; and the authorized canonical order
was rendered in a task-owned sessionless profile through the existing `/v1/admin/dev-context` seam at 1440/1024/390
with measured overflow, title, owner, hit-target and keyboard/focus receipts.

This remains a **technical** PASS within the issued UI scope — not user Visual ACCEPT, not Architect ACCEPT of the
whole shipping chain, and not provider/PSP work. Tipax stays deferred.
