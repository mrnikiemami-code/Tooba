# 11 — Action capability projection

Task: TB-P06-T024-R1

## Completed in this repair

| Area | Mechanism | Status |
|------|-----------|--------|
| **Navigation hide** | `hasViewCapability` in admin/vendor shells | LIVE |
| **Access Control mutations** | Backend `AccessControlDirectory` + UI `canManage` / `disabledByCeiling` / system role guards | LIVE (backend authority) |
| **Scope editor enable** | `canManage && !disabledByCeiling && role.isMutable` | LIVE |
| **Order handle** | `FulfillmentEndpoints.EnsureSellerOrderHandleScopeAsync` | LIVE |
| **Order view/detail** | `SellerPanelComposer` scope resolution | LIVE |
| **Effective access preview** | `GetEffectiveAccessAsync` with ceiling filter | LIVE |

## Story module (pre-existing pattern)

`StoryManagementScreen` + `ADMIN_STORY_CAPABILITIES` / `SELLER_STORY_CAPABILITIES` — static capability matrix per mode (view/create/publish/review). Tests: `story-capabilities.test.ts`.

## Remaining partial (honest)

| Module | Nav projection | In-page manage actions |
|--------|----------------|------------------------|
| Products | `product.view` gates nav | Workspace pages — backend enforces; FE not fully driven by `me/capabilities` for create/edit/publish buttons |
| Orders | `order.view` gates nav | List/detail from scoped API; handle via fulfillment backend |
| Promotions | `promotion.view` gates nav | Page-level manage flags not unified on capabilities payload |
| Reviews | `review.view` gates nav | Same |
| Payments / Settlement | partial nav | Backend authority |
| Access Control | `accesscontrol.view` / manage via backend | ACC pages pass `canManage` true in dev pages — production should derive from `accesscontrol.manage` in effective access |

## Backend remains authority

All mutation endpoints retain server-side permission checks (SpiceDB / AccessControlDirectory). UI projection gaps do not bypass authorization.

## Task acceptance framing

Navigation projection for live modules: **no longer PARTIAL** at shell level. Fine-grained per-button projection across all commercial modules: **partial** except Access Control center + Stories pattern.
