# inventory-audit

## Structure
Domain/Application/Contracts/Infrastructure present. No Endpoints project (seller writes via Offer gateway). Tests project added with Golden guards.

## Repairs
- Host production no longer opens InventoryDbContext; uses IInventoryQueryGateway + IInventorySchemaMigrator
- Availability DTOs/gateway moved to Inventory.Contracts
- InventoryDirectory: IClock/IIdGenerator/IModuleCallTracer; Domain factories take explicit ids
- SetInventoryAsync remains Task<Result> with QuantityInvalid / Offer NotFound failures
- MigrationRunner uses InventoryModuleMigration adapter
- Domain invariant messages scrubbed from Persian prose to stable English codes

## Result adoption
Seller write: Task<Result>. Expected quantity/offer/seller failures => Result.Failure. Success => Result.Success. No catch-all Exception→Result.

## Residuals
Cart.Application still references Inventory.Application for IInventoryDirectory release APIs (pre-existing App→App; availability via Contracts). No owned Inventory HTTP surface requiring ApiResponseFactory.