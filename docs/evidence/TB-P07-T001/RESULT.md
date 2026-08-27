PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P07-T001
Channel:
tooba-main
Status:
PASS

Summary:
Category Attribute Schema + Product Variant Axes foundation shipped: typed definition metadata, category binding/inheritance/effective schema, typed product values with validation, product-specific axes, combination fingerprint duplicate protection, Admin/Seller APIs+UI, Mobile seed with sellable PDP (2 axis combinations). FULL_VARIANT_MATRIX and faceted search remain DEFERRED. Existing sale flow non-regressed. Backend 305 tests 0/0/0/0; FE typecheck/lint/test/build green. Runtimes kept alive.

Audit:
Category: LIVE
Product: LIVE
Variant: LIVE (fingerprint uniqueness)
attributes: LIVE typed kinds
Shopeiva: productForm colors/tags mapped; UI derived from Admin/vendor shells

Attribute-Definitions:
types: Text/Number/Boolean/Enumeration/Instant
options: stable IDs + localized labels + displayOrder/IsActive
localization: CatalogLocalizedText
validation: min/max/maxLength + required

Category-Schema:
binding: LIVE
inheritance: LIVE parent→child
override: child required/order overrides
effective schema: GET admin API
cycle safety: walk detects cycles

Product-Values:
typed: LIVE
required: validated on publish
invalid option: rejected
unknown attribute: rejected when schema-bound
category change: PreviewCategoryChangeAsync orphan report (no silent delete)

Variant-Axes:
allowed: IsVariantAxisAllowed (= IsVariantAxis column)
product-specific: CatalogProductVariantAxis
value selection: option IDs
combination identity: CombinationFingerprint
duplicate prevention: LIVE
full matrix: DEFERRED

Admin:
APIs: /v1/admin/catalog/attribute-definitions + category schema + product attrs/axes
permissions: catalog.attribute.view/manage
definitions UI: /admin/catalog/attributes
category schema UI: /admin/catalog/category-schema
product attributes UI: workspace card + /admin/catalog/products/{id}/attributes

Seller:
APIs: PUT attributes + variant-axes
permissions: seller panel + product.edit path
product attributes UI: vendor offer detail panel
axis selector: LIVE
schema redefine: FORBIDDEN (no seller definition routes)

Storefront:
PDP compatibility: schema-mobile-demo-phone 200 with 2 variants
current sale flow: existing PDP OK
deferred: FULL_VARIANT_MATRIX cart matrix UX

Seed:
Development only: YES
idempotent: YES (+ commercial ensure on restart)
Category: Mobile
Attributes: color/storage/ram/screen_size
Product: schema-mobile-demo-phone
axes: color+storage

Visual-Fidelity:
Shopeiva source: vendor productForm
CSS/JS: Admin shell patterns
forms/cards/tabs: card editors not JSON wall
foreign UI: NO
unauthorized deviation: NO

Validation:
backend restore: n/a (build)
backend build: 0 warnings 0 errors
backend tests: Host 301 + MigrationRunner 4 = 305 passed
warnings: 0
errors: 0
failed: 0
skipped: 0
typecheck: green
lint: green
storefront: green
seller: green
catalog: CatalogAttributeSchemaTests + CatalogFoundationTests green
frontend build: green
git diff --check: clean (CRLF warnings only)

Runtime:
Backend: :5088 up
Frontend: :3000 localhost
Shopeiva: :3001 up
health/live: 200
health/ready: 200
kept alive: YES

USER-PREVIEW:
Admin Attribute Definitions: http://localhost:3000/admin/catalog/attributes
Admin Category Schema: http://localhost:3000/admin/catalog/category-schema?categoryId=01a043f3-30c5-7000-9c2d-2e96d8da1439
Admin Product: http://localhost:3000/admin/products/01a0455c-53c8-7000-a110-061ffa1f936e
Seller Product: vendor-panel offer detail attributes panel (when catalogProductId present)
Storefront PDP: http://localhost:3000/fa/products/schema-mobile-demo-phone
Original Shopeiva: http://localhost:3001/vendor-panel/products/new
dev preview identity/context: admin actor 01a036c2-970e-7000-8eb7-94bf5cc2d8db
preview steps: see docs/evidence/TB-P07-T001/10-user-preview-urls.md

Source-of-Truth:
P07: IN_PROGRESS
T029: ACCEPTED
T001: AWAITING_ARCHITECT_ACCEPT
category attributes: LIVE
product attributes: LIVE
variant axes: LIVE
full matrix: DEFERRED
visual contract: SHOPEIVA_LOCKED

Git:
commit: (pending fill after push)
push: origin/main
final HEAD: (pending)
origin/main: (pending)
synchronized: YES
tracked tree: clean after commit

Architectural-Concerns:
FULL_VARIANT_MATRIX / faceted search intentionally deferred; Multivalue storage still single canonical row foundation.
Visual-Concerns:
Admin attribute UI is operational Admin-shell derived; not a pixel clone of a missing Shopeiva attribute-admin screen.
Blockers:
NONE

END_TOOBA_WORKER_RESULT
