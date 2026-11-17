# TB-277-R1 — owning unit precision, exact scaled client quantities, durable unknown-intent retention

Status: **PASS within the issued A–C repair scope**
Decision:
`EXACT_OWNING_UNIT_QUANTUM_ENFORCEMENT_AT_THE_RECEIPT_WRITE_BOUNDARY_ONE_EXACT_INTEGER_MILLIONTH_QUANTITY_AUTHORITY_SHARED_BY_THE_DIALOG_AND_ITS_CONFIRM_GATE_AND_A_BODY_ONLY_PROVEN_NO_EFFECT_REFUSAL_SET_THAT_KEEPS_EVERY_POSSIBLY_SENT_INTENT_UNRESOLVED_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ZERO_LIVE_BUSINESS_WRITE`

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT any provider/financial/stock/UI-completeness claim.

- Base: `42e2df1f328e16b1bd855cc57edde168071a600f` (== Expected-Base-SHA; worktree started clean on `main`).
- Parent TB-277 stays reviewed-PARTIAL; its modal work, decision-only approval and explicit first-refund route are
  **retained unchanged**. This repair closes the three source-confirmed defects only.
- Single worker, one task, no delegation, no historical agent resumed, no screenshots, no automatic next task.

| Area | State | What this means |
| --- | --- | --- |
| **A owning unit at the write** | enforced before any row | `ReturnDirectory.Receipts.cs` reads the case's own Order return context (`_orders.GetReturnContextAsync(request.SellerOrderId)`) and each return item's `OrderLineId`, then validates every submitted line with `OwningUnitQuantity.IsAlignedTo(quantity, places)`. `0.5` on a `pcs`/0-decimal line and `0.0001` on a 3-decimal gram line are refused with the typed `return.receipt.unit_quantity_invalid` (400 Business), **zero writes**. A missing/foreign order context, an empty `OrderLineId`, an absent precision or an out-of-range precision is the same truthful refusal — never a six-decimal default. The exact stored occurrence replay is answered **before** this gate, so an already-recorded receipt still replays when remaining is `0` or metadata later disappears. Value equality is preserved (`1` == `1.000000`). |
| **A contract** | additive only | `OrderReturnLineSnapshot.QuantityDecimalPlacesSnapshot` (trailing optional `int?`, default `null`) is mapped in `OrderReturnBridge` from the same owning accepted-line snapshot the admin detail read publishes. Live GET now carries `unitDecimalPlaces: 0` for the exercised case line. |
| **B exact client arithmetic** | integer millionths | `app/returns/return-quantity.ts` carries every quantity as an exact integer count of `numeric(18,6)` millionths. `0.1 + 0.2` is exactly `0.3`; `requested − received` is exactly `0.2`, not `0.19999999999999998`; `1` == `1.000000`; Persian/Arabic digits and `٫` normalize; `1.0000001` is refused rather than rounded; `isAlignedToUnit` refuses `0.5` for a piece unit and accepts `0.125` for a 3-decimal gram unit. |
| **B one bounded dialog authority** | one pure module | `app/returns/return-receipt-quantities.ts` (no React/DOM) owns `evaluateQuantityEntry` (`empty/malformed/unit_unknown/ceiling_unknown/precision/over_ceiling`) and `collectReceiptItems`, which sums usable + damaged as exact units against ONE `ceilingUnits`. The workspace's confirm-button disabled state and its confirm handler read the same functions, so an invalid line can no longer be "reported after send". The draft ceiling is exact integer subtraction, and a line whose owning precision is unknown is BLOCKED with an explicit Persian explanation instead of an assumed scale. |
| **C refusal set** | body-dictated only | `PROVEN_NO_EFFECT_REFUSAL_CODES` now holds only refusals determined by the frozen reviewed body or an immutable identity: `return.quantity_invalid`, `return.quantity_precision`, `return.duplicate_line`, `return.idempotency_required`, `return.rejection_reason_required`, `return.receipt.occurrence_required`, `return.receipt.line_invalid`, `return.receipt.unit_quantity_invalid`, `return.line_missing`, `return.receipt.origin_invalid`. Every authorization denial, missing actor, operation denial, generic validation failure, stale/already/conflict/state code, `refund.*` code, all `return.recovery.*` codes (including `return.refund.first_not_lawful`) were removed: they describe THIS attempt and can never prove an earlier possibly-sent command did not write. |
| **localization** | bilingual | `admin-error-map.ts` gained the Persian/English copy for `return.receipt.unit_quantity_invalid`, so the localized-refusal test again covers every retained token. |
| **proof** | focused | Returns module **139/139** (includes `ReturnsReceiptUnitPrecision277R1Tests` 10 facts and the retained `ReturnsExplicitRefund277Tests`); Host `ReturnsModuleAmsc001*` **34/34**; Host `ReturnsCustomerReceipts274*` **10/10** on real `tooba_alpha` with no skips; FE `return-quantity.test.ts` **7/7**, `admin-returns-work-queue.test.ts` **26/26**, `admin-error-map.test.ts` **8/8**, mounted `admin-return-workspace-277.mount.test.ts` **8/8**; `tsc --noEmit` **0**; `npm run build` **0**; `dotnet build Tooba.slnx -c Release` **0 errors**; Host publish **0 errors**. |
| **schema / data** | untouched | zero migration/DDL/seed/backfill/reset/cleanup/restore; one canonical `tooba_alpha`; the exercised case `01a1e6b0-f33a-7000-80ee-beffb6344490` and its checkout were only read; no receipt/decision/refund/retry/recovery/notification/stock/provider/waybill/money write. |

## Served artifacts

- Host `5088` PID `21060` loading `artifacts\publish-primary\Tooba.Host.dll` SHA256
  `8AA71DD82E6D737F9698A93D0F99EE7A77EB07E02916E4185DE9C271890E7948`;
  `/health` 200, `/health/ready` 200 (`SingleStore`, PostgreSQL configured, messaging `Healthy`),
  `/__platform-commerce` → `store-alpha`.
- Next `3000` PID `7864`, `BUILD_ID 0RrDo7CGZ60FPsYrWxQ1a`, `_buildManifest.js` 200.
- Served chunk evidence at 127.0.0.1:3000: `1323-979bf525b6515616.js` 200 (unit-quantity refusal copy) and
  `7952-040a265ac5604fbd.js` 200 (explicit `start-refund` route + retained bounded-ceiling copy).
- Pages: `/fa/admin/returns` 200, `/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` 200.
- Live read-only wire (GET only): case detail 200 with `status=Completed`, `unitLabel=pcs`,
  `unitDecimalPlaces=0`, item disposition `SaleableRestock`, `saleableRestockedQuantity=1.000000`, and the retained
  succeeded attempt — the user's exercised history is unchanged.

## Declared deviations and limits

1. **Two stale characterizations repaired.** `ReturnsCharacterizationTests.Create_approve_refund_*` and
   `…Wallet_destination_credits_wallet_without_psp_refund` still asserted that approving completes the case and moves
   money — the behavior TB-277 deliberately ended. They failed at the issued base (approve now answers `Approved`).
   They are updated to pin the accepted behavior (approve = decision only, zero gateway/Wallet/Inventory effect; the
   separate first-refund command performs the money). Nothing was weakened to make them pass.
2. **AMSC guard matrices extended.** TB-277 added a route, a command and an error code without updating the certification
   pins (9 failures at base). The matrices now carry the 14th route, the 7th transport validator and 45 reachable + 1
   platform codes; the new validator's presence is asserted through the real CQRS container.
3. **Relational harness.** The receipt relational facts previously used an order reader that answered no line precision;
   they now use the Returns-backed order reader so the owning-unit gate has real metadata. Both suites pass without
   skipping on `tooba_alpha`.
4. **Envelope provenance.** The Bridge returned an empty `content` for the claimed row, so `TASK.md` is a repair record
   reconstructed from the Architect review and the task identity — declared, not presented as verbatim text.
5. **Pre-existing FE source-size guard.** 25 baseline violations remain; `admin-return-workspace.tsx` is still listed as an
   oversized file but **shrank** (1386 → 1301 lines) because the quantity authority moved into two small pure modules.
6. Money/stock remain proven with controlled ports only; no real PSP/Wallet/Inventory effect, no committed two-writer
   race. The mounted suite is jsdom. The live case is settled history, so no live modal was opened against it and the
   live check is HTTP/GET + served-chunk text only (no screenshots). Units other than the exercised `pcs` line are
   covered by focused tests, not by a second live case.
STOP_FOR_ARCHITECT
