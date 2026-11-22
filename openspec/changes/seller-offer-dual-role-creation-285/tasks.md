# Tasks

## 1. Contracts and authorization

- [ ] Add admin offer create/read/edit/query endpoints gated by existing admin panel and product permissions.
- [ ] Add seller create/edit capability checks and keep seller party scope.
- [ ] Add stable request identity to Offer creation with no migration.

## 2. Server paging

- [ ] Add published Catalog variant search/page above 100 products.
- [ ] Add seller and admin Offer list search/sort/page with bounded SQL count and page.
- [ ] Add frontend clients and server query adapters for both shells.

## 3. Creation UX

- [ ] Rework seller create to Draft-first, resumable, with searchable/paged picker and honest partial state.
- [ ] Add admin offer list, create and detail routes in AdminShell with seller and Catalog selection.
- [ ] Document Impeccable critique, decisions and post-check; verify fa/en and 360x640.

## 4. Acceptance

- [ ] Run connected Chromium/Next/Host/PostgreSQL for all 8 REQs with independent GET/SQL.
- [ ] Run negative authority, lost-create, malformed-response, cancel and Stage-1 safety checks.
- [ ] Validate OpenSpec strict, OpenCode verifier negative self-tests, builds, cleanup and diff boundary.
