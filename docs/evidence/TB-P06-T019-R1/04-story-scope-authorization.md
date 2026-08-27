# 04 — Story scope & authorization (TB-P06-T019-R1)

## Seller (own-only)

| Operation | Enforcement |
|---|---|
| List / Get / Mutate / Submit | `SellerPartyId` + `Origin == Seller` filter in `StoryDirectory` |
| Ownership change | Not exposed; seller create binds `SellerPartyId` from authorized context |
| Foreign seller data | `SellerGet` → null; submit/mutate → fail |
| Publish / Approve / Activate | No seller routes; domain blocks Activate before Approved |

Seller routes: `/v1/seller/stories*` via `SellerPanelAccess.RequireAuthorizedAsync`.

## Admin review

| Operation | Enforcement |
|---|---|
| List (incl. pending) | `/v1/admin/stories`; filter `ReviewStatus=Submitted` |
| Approve / Reject | `AdminApproveAsync` / `AdminRejectAsync` + `AdminPanelAccess` |
| Schedule / Enable / Disable | Existing admin story lifecycle routes |

## SpiceDB / seller panel headers

`SellerPanelAccess` (`Tooba.Host/Seller/SellerPanelAccess.cs`):

- `X-Tooba-Seller-Party-Id` — request context only; **not** authority.
- Actor from Bearer session, or Dev-only `X-Tooba-Dev-Actor-User-Id`.
- SpiceDB check: actor membership/view on seller Party; missing actor → 401; cross-seller → 403.
- Fail-closed on DENY / Unavailable.

UI capabilities are convenience only; backend remains source of truth.
