# 11 — Authorization / isolation gate (TB-P05-T026)

Architecture: SpiceDB / `IAuthorizationGuard` decision boundary; Identity ≠ AuthZ; request headers are **not** privileged authority.

Prior accepted seams: TB-P05-T001 (seller isolation + repair auth architecture) · customer isolation from TB-P05-T022 · Admin Host-scoped ops from T024.

## Actor / context seams (Development)

| Seam | Role | Authority? |
|---|---|---|
| Bearer session `UserId` | Authenticated actor | Subject for authz |
| `X-Tooba-Dev-Actor-User-Id` | **Development-only** actor substitute | Subject seam only — not production authority |
| `X-Tooba-Seller-Party-Id` | Seller **context** (Party) | **Not** authorization authority by itself |
| Frontend panel choice | UX routing | **Not** authorization authority |

Invariant: Actor UserId ≠ SellerPartyId. Guard checks `user:{actor}` on `party:{sellerPartyId}` (member/view). Header alone never Allow.

## Isolation checks

| Check | Result | Basis |
|---|---|---|
| Customer isolation | **PASS** | Customer panel data scoped to authenticated/customer actor; no cross-customer invent |
| Seller isolation | **PASS** | Host `AuthorizeUseCaseAsync` + SellerPartyId filter in module contexts; cross-seller denied (T001) |
| Admin authorization | **PASS** | Admin routes/composers Host-authorized; workspace scope flags preserved |
| No request-supplied privileged actor authority | **PASS** | Seller Party header ≠ Allow; Dev actor distinct and Development-only |
| SpiceDB / use-case boundary preserved | **PASS** | Modules consume `IAuthorizationGuard`; no second auth matrix; Mode InMemory in Dev, production fail-closed until SpiceDB configured |

**Authorization / isolation gate: PASS**
