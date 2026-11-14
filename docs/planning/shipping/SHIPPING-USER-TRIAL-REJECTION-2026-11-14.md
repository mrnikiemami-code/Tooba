# User trial rejected — queue readability and unpaid/physical-state contradiction — 2026-11-14

User reports: queue table unreadable/clipped; displayed PendingPayment order has already shipped/returned parcel. Acceptance REOPENED. Prior source repair review is not end-to-end acceptance. No new worker task issued in this note.

## Source-confirmed fixture provenance
Review checkout 0dabee2e-c347-408e-a058-59876121a472 / fulfillment d6029640-f820-43df-90c5-234e36d9f7e1 is TB265-L. Host test CourierOriginReturns265RelationalTests.CreateReturnedFixtureAsync:
- directly calls FulfillmentUnit.CreateFromPaidOrder and persists it;
- InsertCheckoutAsync uses ExecuteSqlInterpolatedAsync INSERT with no payment confirmation;
- InsertSellerOrderAsync directly inserts status Paid;
- then calls production Fulfillment directory directly to process/pack/create/track/dispatch/non-delivery/return/receive;
- controlled order/payment readers used by tests are not the runtime's actual payment truth.
This explains the inconsistent trial fixture without proving a real unpaid checkout passed the ordinary production payment pipeline. It invalidates presenting this fixture as a coherent end-to-end paid order. Prior worker claims of no raw business SQL / all production-path fixture creation are contradicted by these helpers; append audit correction, do not rewrite old evidence, falsify payment, delete or reset records.

## Actual production boundary requiring bounded verification
FulfillmentDirectory.CreateFromPaidSellerOrdersAsync / EnsureCreatedForPaidCheckoutAsync do check handoff.IsPaid. Existing-unit DispatchShipmentCoreAsync consumes inventory and advances state but does not itself re-read payment; Order ProjectActions gates dispatch by parcel state/tracking/permission/cancelled-order, not payment. ListCoreAsync reads payment but passes none to ProjectActions. This source review is NOT a reproduced ordinary unpaid public dispatch exploit. Verify real public individual/package/bulk paths and owner-supply policy before concluding. Existing inconsistent fixture must not be trusted as payment proof or allowed a new executable supply mutation without lawful authorization.
Online-prepaid fulfillment needs authoritative payment/supply authorization. Preserve any explicitly supported legitimate deferred/COD modes, if they exist; do not invent them or use freight payer (pre/post freight) as merchandise payment permission. Historical return/receipt history must stay readable; don't relabel history or block honest reverse-logistics merely to conceal inconsistency. No duplicate payment truth in UI.

## Readability / locator defects
Screenshot queue shows operational-status badge clipped vertically, tiny multi-line labels, truncated order identity. admin-fulfillment-work-queue-screen status cell stacks badge+scope text while dispatch badge wraps additional text. Check actual grid row-height/density/clipping rather than assume typography alone. Meaningful order number, full readable status and concise scope; selected exact physical-unit locator must resolve. In user's operations screenshot accepted group target is unresolved and falls back to seller items; task factory unit shipping group has no corresponding captured Order group. Do not offer an exact-target action that silently degrades to an unrelated summary. Same design-system grid, RTL, mobile and desktop; existing Toast/MessageBox, no cosmetic framework sweep.

## Next repair acceptance boundary
One bounded task when issued: actual public unpaid-vs-authorized-supply command proof with zero dispatch/consume on refusal, consistent safe review data via owning commands (no SQL-paid fabrication), truthful legacy-fixture display/queue treatment and exact locator, real browser readable rows at390/desktop with no clipped status. No deleting/reseeding/correcting user history, no new database, no provider/refund movement or screenshots required. No scheduling or Returns redesign.
