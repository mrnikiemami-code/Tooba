# 16 — Content authorization & edition (TB-P06-T013)

Task: `TB-P06-T013`

## Public

- `GET /v1/content/articles` and `/{slug}` are anonymous/public.
- Only `Published` rows leave the directory for public reads.

## Admin

- All `/v1/admin/content/*` routes call `AdminPanelAccess.RequireAuthorizedAsync` (session + tenant + `IAuthorizationGuard` / SpiceDB ReBAC, with existing Host environment policy).
- Mutations are not available on public routes.

## Edition / tenancy

- Content DbContext uses tenant-scoped Host composition (same pattern as other modules).
- Schema ownership: `content` only — no foreign-schema joins.
- Single-store vs marketplace: Content articles are edition-tenant data; no cross-tenant leak in directory queries.

## Out of scope

- Fine-grained editor roles beyond admin panel gate (author vs publisher RBAC refinement deferred).
