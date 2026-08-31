# Server GridQuery endpoints — TB-P07-T041

Shared infrastructure: `InMemoryGridQueryEngine`, `AdminListGridQueryPolicy`, `AdminListGridPolicies`, `AdminGridQueryEndpoint`.

| Endpoint | Composer |
|----------|----------|
| POST /v1/admin/orders/query | AdminPanelComposer.QueryOrdersGridAsync |
| POST /v1/admin/sellers/query | AdminPanelComposer.QuerySellersGridAsync |
| POST /v1/admin/customers/query | AdminPanelComposer.QueryCustomersGridAsync |
| POST /v1/admin/fulfillments/query | FulfillmentPanelComposer.QueryGridAsync |
| POST /v1/admin/returns/query | ReturnPanelComposer.QueryGridAsync |
| POST /v1/admin/settlement/payout-queue/query | SettlementPanelComposer.QueryPayoutGridAsync |
| POST /v1/admin/reviews/query | ReviewPanelComposer.QueryPendingGridAsync |
| POST /v1/admin/content/articles/query | ContentPanelComposer.QueryGridAsync |
| POST /v1/admin/stories/query | StoryPanelComposer.QueryAdminGridAsync |

Response shape: `GridPageResponse<T>` → `{ items, page, pageSize, totalCount }`.
