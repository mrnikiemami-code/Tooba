# Operational-history parity matrix

| Baseline kinds | Source owner | New contract | Implementation path | Test | State |
|---|---|---|---|---|---|
| order_created, order_cancelled | Order | Order-owned store | Order Infrastructure | existing completeness tests | partial |
| inventory_released, order_restored | Order | Order-owned store | not implemented | none | missing |
| payment_created, payment_pending, payment_deposit_restored, payment_succeeded, payment_failed, payment_deposit_rejected, payment_cancelled, payment_expired, payment_refund_pending, payment_refunded, payment_refund_failed | Payment | `IPaymentAdminGateway` | not projected | none | missing |
| fulfillment_processing, fulfillment_packed, shipment_created, tracking_assigned, tracking_corrected, shipment_cancelled, allocation_released, shipment_dispatched, shipment_delivered | Fulfillment | missing history contract | not implemented | none | missing |
| consolidated_package_created, consolidated_package_member_added, consolidated_package_cancelled, consolidated_package_members_released, consolidated_package_dispatched, consolidated_package_members_dispatched, consolidated_package_delivered, consolidated_package_members_delivered | Fulfillment | missing history contract | not implemented | none | missing |
| return_requested, return_approved, return_rejected, refund_completed, refund_failed, refund_retried | Returns | missing history contract | not implemented | none | missing |
| settlement_cancel_adjustment, settlement_adjustment, settlement_accrual | Settlement | missing history contract | not implemented | none | missing |
| inventory_recovery_requested, inventory_recovery_succeeded, inventory_recovery_failed_insufficient, inventory_recovery_manual_review | Order legacy compatibility | missing parser | not implemented | none | missing |
| operational_note | Order | `ICheckoutDirectory` | Order Infrastructure | existing completeness tests | partial |

Seller labels, product titles, actor labels, and quantity-aware summaries remain missing. Sorting is timestamp-only and therefore lacks the baseline kind tie-breaker; paging is applied after the currently incomplete merge.
