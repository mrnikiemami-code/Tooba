# 55 — Admin Product Workspace

Status: IN_PROGRESS (TB-P04-T005 / awaiting Architect visual ACCEPT)

Product Workspace is a multi-domain UI composition. Backend/module boundary != UI boundary.

## Route

- `/admin/products` list entry using the accepted DataGrid
- `/admin/products/[productId]` workspace

## View-model composition

Host `ProductWorkspaceComposer` queries Catalog, Offer, Pricing, Inventory, and Tax DbContexts separately and composes in memory. No SQL JOIN across module schemas. No frontend-direct DB.

UI view-model != domain aggregate. Product has no Price and no Stock fields.

The Next shell rewrites `/v1/*` to Host. Production Admin routes (`/admin/products`, `/admin/products/[productId]`) read that Host composition by default. If Host is unreachable, the UI shows an error/retry state; fixture JSON is not substituted on those routes. Development Host may insert representative Catalog/Offer/Price/Tax/Inventory rows through module directories, then the same HTTP APIs are read back.

## Admin chrome

Admin routes use a persistent operations shell (sidebar placeholders for future modules, header context). Header appearance controls change direction and color scheme for the operator; they are not debug chrome. Debug stale-save / fixture-scope toggles are not part of the production Admin route. Theme and direction follow the Design System html contract.

List and workspace copy is operator-facing Persian. Seller and location labels come from Party/Inventory display names composed in Host memory, not raw UUIDs as primary labels.

## Sections

Overview, Variants, Media, Commercial, Inventory, SEO & Content, Publication, History.

Commercial composes multi-seller Offers + authored prices (tax exclusive) + tax classification. Inventory is offer-scoped and multi-location.

## Permissions and concurrency

Generic UI components do not call SpiceDB. Host header `X-Tooba-Workspace-Scope: view` forces read-only flags. Catalog title PATCH uses `UpdatedAt` optimistic concurrency (`workspace.catalog.stale`). A 409 is shown as a concurrent-edit operator state, not a debug control.

## Known gaps

- Media binary upload, promotion write, and full content studio are explicit unsupported mutations.
- Visual ACCEPT is pending Architect review. Cursor PASS is not visual ACCEPT.
- Grid virtualization remains DEFERRED_NON_BLOCKING.
