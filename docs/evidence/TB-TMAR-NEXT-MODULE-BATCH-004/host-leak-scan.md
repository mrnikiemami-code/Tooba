# Host leak scan

Residual Host composition using FulfillmentDbContext / ReturnsDbContext (allowlisted in module guards):

FulfillmentDbContext:
- Program.cs, FulfillmentPanelComposer.cs, FulfillmentEndpoints.cs, AdminFulfillmentWorkQueueQueryEngine.cs, AdminOrderOperationsComposer.cs, AdminOrderOperationsEndpoints.cs, AdminOrderCompletenessComposer.cs, OrderSupplyComposer.cs, StorefrontShippingComposer.cs, ShippingServiceEndpoints.cs, ProductWorkspaceDevelopmentBootstrap.cs, AdminReturnGridQueryEngine.cs
- MigrationRunner ModuleMigrationRegistry.cs

ReturnsDbContext:
- Program.cs, ReturnPanelComposer.cs, ReturnEndpoints.cs, AdminReturnGridQueryEngine.cs, AdminOrderOperationsComposer.cs, AdminOrderCompletenessComposer.cs, AdminPanelComposer.cs, AdminOrdersGridQueryEngine.cs
- MigrationRunner ModuleMigrationRegistry.cs

No Host expansion; migration/bootstrap files remain allowed residual debt.
