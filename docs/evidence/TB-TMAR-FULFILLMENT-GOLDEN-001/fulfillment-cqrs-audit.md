# Fulfillment CQRS Audit

- MediatR 12.5.0 via existing foundation.
- Commands/Queries split into use-case folders (ExecuteAdminFulfillmentBulk, SellerMutateFulfillment, shipping CRUD/seed, seller/admin/customer reads, ListEnabledShippingMethodsTree, QueryAdminFulfillmentWorkQueue).
- Removed root dumps: FulfillmentQueries.cs, Commands root dumps, Shipping*Handlers dumps.
- AdminFulfillmentGridQueryPolicy module-owned Normalize.
- State: MEDIATR_12_5_APPLICATION_HANDLERS
