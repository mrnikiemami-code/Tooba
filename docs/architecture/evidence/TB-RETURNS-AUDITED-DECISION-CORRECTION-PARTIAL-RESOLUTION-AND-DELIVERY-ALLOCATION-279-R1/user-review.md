# TB-279-R1 — user review

## What you can look at right now

`http://127.0.0.1:3000/fa/admin/returns` (Next `BUILD_ID 1qgSFICgCehoh6FzH3a0f`) and
`http://127.0.0.1:5088/health` (Host dll `9C5ECED5…`).

The three stored cases are settled history and are shown read-only:

| Case | State | Requested | Approved | Attempts |
| --- | --- | --- | --- | --- |
| `01a1e6b0-f33a-7000-80ee-beffb6344490` | `Completed` | 2 407 839 IRR | — (never frozen) | 1 `Succeeded` |
| `01a1e8fc-c74e-7000-a155-fecfa1a3bf1a` | `Completed` | as stored | — | as stored |
| `01a1e9c5-c729-7000-b614-79daf3d0e812` | `Completed` | as stored | — | as stored |

None of them was mutated: their `updated_at`, amounts, decisions and attempts are exactly what they were before this
task, the four new tables hold 0 rows, and no live POST was made (the wire checks in this task are GETs).

## What is proven but needs an active case to watch live

* The **reviewed split** UI (accepted/rejected per line, reason per item), the **requested vs approved** amounts, the
  **custody remaining** block and the **reopen / publish-resolution / record-custody** actions are mounted and served in
  the chunk, but a settled case offers no action, so their live walkthrough needs a lawful active case.
* The customer modal (shared design-system dialog, per-line quantity with the owning unit, exact remaining ceiling,
  retained uncertain act, one react-toastify toast per command) is proven by the mounted real component; no live
  checkout/session was exercised.

## What was deliberately NOT done

No new return case/order/hold/fixture, no reset or replay of your three cases, no real receipt/decision/reopen/
resolution/custody/refund execution against live data, and no provider/PSP/Wallet/Inventory/notification/waybill call.

## Open limits you should know

Money and stock remain proven with controlled ports (no real provider effect), no committed two-writer race is claimed,
and this is **not** Architect acceptance, not user functional/visual acceptance, and not Marketplace/financial
readiness. Architect review is the next step; there is no automatic follow-on task.
