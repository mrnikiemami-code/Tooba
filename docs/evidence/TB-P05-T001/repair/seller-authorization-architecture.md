# TB-P05-T001 REPAIR — Seller authorization architecture

## Flow

```text
HTTP request
  ├─ Actor: Bearer session UserId  OR  (Development only) X-Tooba-Dev-Actor-User-Id
  └─ Context: X-Tooba-Seller-Party-Id   ← request context, NOT authority
        │
        ▼
IAuthorizationGuard.AuthorizeUseCaseAsync
  subject = user:{actorUserId}
  resource = party:{sellerPartyId}
  permission = view   (= member in foundation schema)
        │
        ├─ Allow → SellerPanelComposer (SellerPartyId filter in each module DbContext)
        ├─ Deny  → 403 seller.authorization.denied
        └─ Unavailable → 503 seller.authorization.unavailable
```

## Foundations reused (no second auth system)

- `IAuthorizationGuard` / `IAuthorizationService` / `IAuthorizationTupleWriter`
- Party membership → `party#member` projection (`PartyMembershipProjectionHandler`)
- Development writes the same tuples via `SellerDevActorBootstrap` after Membership persist (InMemory is process-local)
- `CurrentAuthenticatedSession` for real Bearer sessions

## Invariants

| Claim | Status |
| --- | --- |
| Seller Party ≠ authenticated User | Actor UserId ≠ SellerPartyId |
| requested SellerPartyId ≠ authorization authority | header alone never Allow |
| frontend ≠ seller authorization authority | Host guard is authority |
| Catalog Product ≠ Seller Offer | unchanged |

## Development actor seam

- Header: `X-Tooba-Dev-Actor-User-Id`
- Distinct from `X-Tooba-Seller-Party-Id`
- Seeded actors: `seller-actor-a@tooba.local` → آرمان; `seller-actor-b@tooba.local` → دیجی‌استایل
- Exposed for UI via `GET /v1/seller/dev-contexts` (Development only)

## Mode

- `appsettings.Development.json`: `Tooba:Authorization:Mode = InMemory`
- Production default remains fail-closed `Disabled` until SpiceDB is configured
