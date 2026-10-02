# TB-TMAR-ACCESSCONTROL-AMC-001-W3 — Result&lt;T&gt; / ApiResponseFactory pipeline

Mode: MIGRATE  
Slice: RESULT_PIPELINE  
Parent: TB-TMAR-ACCESSCONTROL-AMC-001-W2

## Changes

- `AccessControlOperation` maps `AccessControlException` → `Result` / `Result&lt;T&gt;` by stable Code; `NotFoundIfNull` for missing role
- All CQRS commands/queries return `Result` or `Result&lt;T&gt;` (Unit retired); handlers wrap directory calls via `ExecuteAsync`
- Admin / AdminSeller / Seller endpoints: `api.From(await sender.Send(...))` — **zero** `catch (AccessControlException)` and **zero** `Results.Json(await sender.Send`
- W2 durable guard relaxed: `AccessControlHttpErrors` class retained; endpoints no longer require catch-path mapping

## Deferred

- Domain Aggregates/Enums foldering; Application Models dump rename; Endpoints→Domain ZERO (W4)
- SellerDevContexts Development endpoint still uses ad-hoc `Results.Json` (dev-only)

## Microservice

Presentation success/failure now flows through BuildingBlocks `ApiResponseFactory` + Contracts error codes — no Host-specific exception choreography on AccessControl routes.
