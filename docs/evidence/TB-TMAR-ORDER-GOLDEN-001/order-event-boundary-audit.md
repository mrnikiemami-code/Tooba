# Order event boundary audit

Order integration handling is registered through `IIntegrationEventHandler<PaymentSucceededIntegrationEvent>` and Order-owned outbox registration. Fulfillment, Returns, Payment, and Notification interactions have contract adapters, preserving transport replacement potential.

Full extraction safety is not claimed because synchronous foreign Application dependencies and Host business orchestration remain. Outbox/inbox topology and broker behavior were not redesigned.
