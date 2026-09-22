# Behavior Preservation Audit

Preserved:
- customer/seller paging (`skip`/`take`)
- locale fallback `fa`
- unread count
- mark one read / mark all count
- dismiss/soft-delete
- 204 / 404 outcomes
- recipient scoping (customer actor / seller party)
- customer ownership + seller party scope
- Development/Testing actor header + guest fallback
- production unauthorized (`customer.session.required`)
- rendering/copy + event/outbox (Infrastructure untouched)

Validated by `Notification.Tests` (18): architecture ownership/CQRS/host, CQRS HTTP contract, prior characterization + projector parity.

## Verdict
Notification-Behavior-Preservation: VERIFIED
