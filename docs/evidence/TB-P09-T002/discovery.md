# Discovery

Inspected Order detail (`admin-order-detail-screen.tsx`), AdminOrderOperationsComposer, AdminPanelComposer, PaymentOperationalSnapshot, FulfillmentSnapshot, ReturnsDbContext pattern, Settlement ListEntriesBySellerOrderIdsAsync.

No existing invoice/receipt generator or Order internal-notes entity; Support notes are ticket-scoped (not reused).

Reused AdminPanelAccess + order.view / order.handle effective-access pattern from ops composer. Timeline composed in Host (no new event store).
