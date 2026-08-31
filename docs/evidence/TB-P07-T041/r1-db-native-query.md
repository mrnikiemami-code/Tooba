# TB-P07-T041-R1 — DB-native admin list query

## Goal

Replace Host-memory paging (`InMemoryGridQueryEngine` / full materialization then Skip/Take) for NON_TRIVIAL_SERVER_QUERY_REQUIRED admin lists with module-owned EF `IQueryable` filter → sort → `CountAsync` → `Skip`/`Take` → enrich page only.

## Mechanism

- Shared helpers: `AdminEfGridQuery` (text/enum/number/date/search + `PageAsync`)
- Per-list engines under `Tooba.Host/Grid/Admin*GridQueryEngine.cs`
- Composers call `AdminListGridPolicies.*.Normalize` then `engine.QueryAsync`
- Former in-memory engine renamed `BoundedListGridQueryEngine` (tests / truly bounded only)

## Non-trivial lists covered

Orders, Sellers, Customers, Fulfillments, Returns, Reviews, Payouts, Content, Stories

## Explicitly not using in-memory production path

Grep evidence: composers no longer call `AdminListGridPolicies.*.Execute` for these nine lists.
