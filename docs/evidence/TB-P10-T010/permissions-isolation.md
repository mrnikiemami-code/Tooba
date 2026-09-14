# Permissions / isolation

- Admin writes use `AdminPanelAccess.RequireAuthorizedAsync` (session + tenant + guard). No StoreId from client body.
- Catalog DbContext is tenant/store scoped; Store A pages are not in Store B's database.
- Focused test: two in-memory catalogs; published page in A is missing in B; B cannot SetHome to A's PageId (`landing.page.missing`).
- Public `GET /v1/storefront/pages/{slug}` returns Published only; Draft and reserved slugs → 404.
