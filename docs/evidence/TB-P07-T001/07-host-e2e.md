# TB-P07-T001 — Host E2E (no DB mutation beyond seed)

## Admin
| Step | Result |
| --- | --- |
| GET `/v1/admin/catalog/attribute-definitions` | 200 — includes color/storage/ram/screen_size |
| GET `/v1/admin/catalog/categories/{mobile}/attribute-schema/effective` | 200 — ordered Color→Storage→RAM→Screen; screen required |
| GET `/v1/admin/products/{seeded}` | 200 — attributes + 2 variants |
| PUT attributes / variant-axes | Covered by CatalogAttributeSchemaTests + UI |

## Seller
| Step | Result |
| --- | --- |
| PUT `/v1/seller/products/{productId}/attributes/{definitionId}` | Endpoint live (SellerPanelEndpoints) |
| PUT `/v1/seller/products/{productId}/variant-axes` | Endpoint live |
| Redefine Attribute Definitions | Forbidden — no seller schema authoring routes |

## Storefront
| Step | Result |
| --- | --- |
| Existing sellable PDP | 200 (e.g. کتاب دمو) |
| Seeded `schema-mobile-demo-phone` | 200 after commercial seed binding; variants=2 |
| FULL_VARIANT_MATRIX | DEFERRED — only seeded combinations |

Actor: admin `01a036c2-970e-7000-8eb7-94bf5cc2d8db`
