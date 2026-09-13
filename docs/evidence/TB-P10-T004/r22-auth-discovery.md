# TB-P10-T004-R22 — Auth Discovery

| Route/Action | Anonymous allowed? | Auth required? | Existing behavior | Required behavior |
| --- | --- | --- | --- | --- |
| Home / PLP / PDP | yes | no | Public storefront | keep |
| AddToCart | yes | no | Guest cart + secret | keep |
| Cart page `/cart` | yes | no | Guest session | keep |
| Shipping `/shipping` | yes | no | Guest shipping form; saved address needs session | gate under AuthenticatedOnly |
| Checkout `/checkout` | yes | no | Guest checkout; comment says guest path open | gate |
| Payment `/payment` | yes | no | Guest payment with committed proof | gate |
| `POST /v1/storefront/checkout` | yes | no | `ResolvePlacementActor` allows guest (+ Dev actor header) | backend reject if AuthenticatedOnly |
| Login UI | n/a | n/a | **No page.** BFF `POST /api/auth/login` password-only | reuse existing Login page — **missing** |
| OTP login | n/a | n/a | OTP is identifier-verification, not login | Dev 09111111111 / 123456 via existing login — **no such flow** |
| Cart merge on login | n/a | n/a | **None** in Cart module | required backend merge |
| Return URL | n/a | n/a | No `/login` route; no `returnTo` | required |
| Admin store settings | operator | yes | Profile / locale / quantity / holds tabs | add هویت مشتری در فرایند خرید |
| Pending Orders on login | n/a | n/a | Authenticated list by `PlacedByUserId`; guest by proof | must not merge pending into cart |

Login/session today: Host `POST /v1/auth/register` + `POST /v1/auth/login` (identifier + password) → Bearer; FE BFF sets HttpOnly `tooba_session`. No `type=password` input exists in any frontend TSX.

Checkout identity policy setting: **absent**.
Guest checkout: default path today.
No TB-P10-T005 created.
