# Behavior parity — W15

| Concern | State |
|---|---|
| Route / method | `GET /v1/admin/products/{productId:guid}/history` |
| Auth | Admin panel via ICatalogAdminAuthorizer |
| View scope | GET allowed; no AllowsCatalogEdit gate |
| JSON page | Items, TotalCount, Skip, Take |
| JSON item | HistoryId, EventType, Section, SectionLabelFa, SummaryFa, BeforeSummary, AfterSummary, ActorDisplayName, OccurredAt |
| Actor fallback | blank → ProductHistoryRules.ActorSystemFa |
| Section labels | ProductHistoryRules.SectionLabelFa |
| Missing product | workspace.product.missing 404 |
| Paging/filter/order | preserved (see paging-filter-parity.md) |
| Aggregate Activity/Audit | Host BuildHistoryShellListsAsync unchanged behaviorally |
| Schema / frontend | unchanged |
