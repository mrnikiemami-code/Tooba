# Dataset classification — TB-P07-T041

| Surface | gridId | Classification | Query mode |
|---------|--------|----------------|------------|
| Orders | grid.admin.orders | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/orders/query |
| Fulfillments | grid.admin.fulfillments | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/fulfillments/query |
| Returns | grid.admin.returns | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/returns/query |
| Sellers | grid.admin.sellers | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/sellers/query |
| Customers | grid.admin.customers | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/customers/query |
| Reviews | grid.admin.reviews | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/reviews/query |
| Payouts | grid.admin.payouts | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/settlement/payout-queue/query |
| Content | grid.admin.content | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/content/articles/query |
| Stories (admin) | grid.admin.stories | NON_TRIVIAL_SERVER_QUERY_REQUIRED | POST /v1/admin/stories/query |
| Settlement balances | grid.admin.settlement | SMALL_BOUNDED_CLIENT_SAFE | client executeGridQuery; bounded by seller count |
| Promotions | grid.admin.promotions | SMALL_BOUNDED_CLIENT_SAFE | client; bounded by seller promotion cardinality |
| Attribute definitions | admin-attribute-definitions-grid | SMALL_BOUNDED_CLIENT_SAFE | client; tenant schema defs |
| Category schema | admin-category-schema-grid | SMALL_BOUNDED_CLIENT_SAFE | client; per-category effective schema |
| Gift cards | admin-gift-cards-grid | SMALL_BOUNDED_CLIENT_SAFE | client; admin wallet list bounded |
| Products | grid.admin.products | Already canonical server | unchanged POST /v1/admin/products/query |
