# Inventory HTTP Applicability Audit

Verdict: `INTERNAL_ONLY`.

Production searches covered `/inventory`, `/stock`, `/availability`, `MapInventory`, `InventoryEndpoints`, Inventory Application/Infrastructure use, and `InventoryDbContext`.

- `INVENTORY_OWNED_HTTP`: none.
- `FOREIGN_MODULE_HTTP_USING_INVENTORY_CONTRACT`: Offer seller route `/offers/{offerId:guid}/inventory`; Offer endpoint sends `SetOfferInventoryCommand`, whose handler calls `ISellerOfferInventoryGateway`.
- `INTERNAL_APPLICATION_FLOW`: checkout, order, availability, fulfillment, and returns adapters/directories.
- `MESSAGE_CONSUMER`: Inventory messaging handlers and return-restock inbox.
- `BOOTSTRAP/DI_ONLY`: Host composition, migration gateway, development/demo coordinators.
- `TEST_ONLY`: Host and module foundations instantiate Inventory infrastructure only in tests.

There is no Inventory Endpoints project, Host Inventory endpoint directory, `MapInventoryEndpoints`, or Inventory-owned route.
