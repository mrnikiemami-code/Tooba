# 06 — Event consumers (TB-P06-T023)

Handlers registered in `NotificationModule` as `IIntegrationEventHandler<T>` (Host MassTransit SQL transport → dispatcher). No RabbitMQ.

| Event | Customer | Seller |
|---|---|---|
| payment.succeeded.v1 | yes | yes (each SellerOrder) |
| payment.failed.v1 | yes | no |
| fulfillment.created.v1 | yes | yes |
| shipment.dispatched.v1 | yes | yes |
| return.requested.v1 | yes | yes |
| return.approved.v1 | yes | yes |
| refund.succeeded.v1 | yes | yes |
| story.* | skipped | skipped |

Recipients resolved via `IOrderNotificationReader` (Order Application bridge).
