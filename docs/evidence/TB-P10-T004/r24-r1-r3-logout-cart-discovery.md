# TB-P10-T004-R24-R1-R3 — Logout cart discovery

| Step | What happens | Deletes DB cart? |
| --- | --- | --- |
| FE `StorefrontAccountMenu.logout` | POST `/api/auth/logout` + `clearCartSession()` | no — local pointer only |
| BFF `/api/auth/logout` | Host `/v1/auth/logout` | no |
| Host `LogoutAsync` | `RevokeSessionAsync` only | no |
| `clearCartSession` | removes sessionStorage cartId/guestSecret | no |
| Header badge | `loadStorefrontCart` after AUTH/CART event; anonymous cannot read account cart | hide, not delete |

No path marks Abandoned, clears CustomerId, or converts on logout.

Anonymous after logout may create a new guest cart; that is a separate cart. Re-login `mergeStorefrontCartAfterLogin` + `/v1/storefront/cart/current` restores the account Active cart.
