# TB-279-R2 — USER REVIEW

## What to look at

| Surface | URL | What is honest about it |
| --- | --- | --- |
| Returns queue | `http://127.0.0.1:3000/fa/admin/returns` | read-only list; the served instance is the current build |
| A settled case | `http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` | this user-exercised case stays `Completed` history; a settled case offers no action, which is why the new dialogs cannot be opened against live data |
| Customer act | storefront return modal on a delivered order | no live checkout exists in this environment, so the modal can only be reviewed as a component/test fact |

## What changed that a reviewer can verify in the UI

- Each of reopen, approve, reject, resolution, custody and first-refund opens ONE design-system dialog; nothing is sent
  before confirmation, and every command reports through the application's single react-toastify container.
- The resolution dialog shows all five per-condition quantities per item (accepted usable, accepted damaged, rejected
  received usable, rejected received damaged, rejected unreceived) with the real product, unit and ceiling.
- The custody dialog requires a proof reference, sends `operator_attestation`, and cannot exceed the rejected-RECEIVED
  remainder of the chosen condition.
- The customer modal keeps a possibly-sent act visible across unknown/conflict/lost answers and never sends a second
  child after the surface closes.

## What this task does NOT claim

- Not Architect ACCEPT, not user functional/visual acceptance, not Marketplace/provider/financial readiness.
- No live money, stock, provider, notification or database write was performed; the served data is unchanged history.
- The historical Data Protection key exposure is contained in Git only. Retiring/rotating it needs an explicit
  decision (see `key-containment.md`); it was not revoked or rotated here.
- `carrier_delivered_back` custody is refused as unproven rather than implemented.

## Suggested next review (Architect)

1. Read `closure-matrix.md` against the R2 TASK A–G sections.
2. Spot-check the three new directory facts in
   `Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsDecisionCorrection279DirectoryTests.cs`.
3. Decide whether the key ring retirement procedure should become its own reviewed task.
