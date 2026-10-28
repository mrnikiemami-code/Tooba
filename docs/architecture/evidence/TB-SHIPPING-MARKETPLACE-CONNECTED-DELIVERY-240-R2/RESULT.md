# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2 — worker execution evidence

Status: **BLOCKED** (two real repairs landed + measured; the required mixed-owner persisted order is blocked by an
existing DB unique index that this task forbids changing).
Base: `4f95bdd9b7dcabecc9950c9b1485c0b046b509e0` (issue commit, fast-forward only).

Task Host: `D:\temp\tb240r2-build\out`, Development / Marketplace / Tenant NULL / `tb239-marketplace`,
`tooba_marketplace_tb239`, `http://127.0.0.1:5190` (PID verified before each restart; no other host touched).

## A — SELLER AUTHORIZATION: FIXED, both real sellers, existing authority only

Root cause confirmed by source and by the live 403 in 240-R1:
`AccessControlDirectory.EnsureBootstrapAsync` creates the seller-owner role, its delegable catalog permissions and
the platform seller ceilings per seller scope, but never assigns that role to the seller's real identity user.

Narrow, AccessControl-owned implementation (no new permission engine, no public endpoint, no direct SQL):

- `Tooba.AccessControl.Contracts/Development/AccessControlDevelopmentSeedContracts.cs` — one new prelude member
  `EnsureMarketplaceSellerAccessAsync(Guid? platformAdminUserId, CancellationToken)` plus the neutral result records
  `AccessControlMarketplaceSellerAccess` / `AccessControlMarketplaceSellerScope`. No Application/Domain/
  Infrastructure type crosses the boundary.
- `AccessControlDevelopmentSeedPrelude` — resolves the REAL pairs through the existing
  `ISellerDevContextStore.EnsureAsync` (Identity user + Party membership; no fixed/default actor GUID and no invented
  business tenant), calls the existing `EnsureBootstrapAsync` with `tenantId: null` for both real seller ids, then
  for each seller scope reads the EXISTING `seller-owner` role via `ListRolesAsync`, checks
  `ListAssignmentsAsync` and assigns ONLY if missing through `AssignRoleAsync` (which syncs capability tuples);
  `Publish(ActorA, ActorB)` runs LAST so the legacy synthetic Marketplace pair cannot stay published.
  Idempotent by construction; no role/permission/ceiling duplication; no Platform grant to a seller.
- `Host/Configuration/MarketplaceSellerAccessOptions.cs` + `Program.cs` registration +
  `MarketplaceDevelopmentBootstrap.EnsureMarketplaceSellerAccessAsync` — guarded by REAL Development + explicit local
  opt-in (`Tooba:Development:MarketplaceSellerAccess:Enabled`, checked-in default false). Audit actor resolved from
  the same real admin identifier seam; never a fixed id.

Live receipts (Host5190, after restart with the opt-in):

```
access_control.user_role_assignments
 platform-admin | owner_scope 1 | user 01a1818b-5257-7000-bada-84233a2b21fb
 seller-owner   | owner_scope 2 | 01a181b0-5c87-7000-976e-ae5ce19864f6 (SellerA) <- 01a18178-67bc-7000-9662-fb5cd519fd58
 seller-owner   | owner_scope 2 | 01a181b0-5c9a-7000-bf9b-8c8e1c47da72 (SellerB) <- 01a18178-6820-7000-a04e-01f81bcca3a4
roles=3 seller_roles=2   (one owner role per real seller, no duplicate)
```

```
POST /v1/seller/shipping-services/authoring-drafts  (SellerA token + X-Tooba-Seller-Party-Id=SellerA) -> 201
   draft 01a181dd-6bcb-7000-81eb-576c1f587eef owner {"ownerKind":"Seller","sellerPartyId":"01a181b0-5c87-..."}
POST /v1/seller/shipping-services/authoring-drafts  (SellerB token + header=SellerA)              -> 403 seller.authorization.denied
POST /v1/admin/shipping-services/authoring-drafts   (SellerA token)                              -> 403 admin.authorization.denied
```

Both SELLER captures were then authored and PUBLISHED through their own normal seller APIs on the SAME existing
binding/type as Platform (`post`/`standard`, type `heavy` 01a181c7-7863-7000-925f-b6f64ed4694b), each with own
label/row/fees/dates/hours (identical weekly schedule, own freight/packaging):

```
SellerA draft 01a181dd-6bcb-7000-81eb-576c1f587eef rev6 -> Selected snapshot 01a181de-0c80-7000-9c01-ca51a0da05f4
        area group 01a181dd-6bcc-7000-823b-ee56a67b5736  rate row 01a181de-0c26-7000-891b-a803cf897db6  freight 62000 + 3000
SellerB draft 01a181de-0d04-7000-a79f-1bf723510020 rev6 -> Selected snapshot 01a181de-0e59-7000-9127-f5142617340e
        area group 01a181de-0d04-7000-9d2b-f3ffede2fde9  rate row 01a181de-0e2d-7000-83ef-06d8623b713a  freight 47000 + 2500
Platform (unchanged) draft 01a181ca-e730-7000-bfaf-a5d9e5778dee snapshot 01a181ca-e8f8-7000-840c-1a09cbb105e5
        area group 01a181ca-e731-7000-9037-0a1d0f7354e5  rate row 01a181ca-e8a1-7000-b24b-ab819db7a043  freight 95000 + 5000
```

Mixed 3-offer cart through normal buyer APIs (`POST /v1/storefront/cart`, 3× `POST .../lines`), then the real
captured projection:

```
POST /v1/storefront/shipping/projection -> 200 cartVersion 7, 3 PRICED owner groups, no reasons:
 cap|platform|marketplace|01a181b05c877000976eae5ce19864f6|01a181c778637000925fb6f64ed4694b  seller 01a181b0-5c87-...  goods 2016500  method post/standard payable 100000.00 (95000+5000) date 2026-10-31
 cap|seller|01a181b05c877000976eae5ce19864f6|01a181c778637000925fb6f64ed4694b                 seller 01a181b0-5c87-...  goods 1580500  payable 65000.00 (62000+3000)  date 2026-10-29
 cap|seller|01a181b05c9a7000bf9b8c8e1c47da72|01a181c778637000925fb6f64ed4694b                 seller 01a181b0-5c9a-...  goods 1951100  payable 49500.00 (47000+2500)  date 2026-10-31
```

## B — MIXED-OWNER SELECTION: two real Order defects found and repaired, then BLOCKED by an index

1. `CartShippingGroupPlanner` (`Order.Domain`) refused the lawful cart with
   `shipping.group.duplicate_seller_method`: its collapse key was `seller|methodCode` only, so the SAME seller's
   Platform-owned commitment and its OWN Seller commitment (same catalog method, disjoint lines) collided.
   Repaired in place: the collapse key is now the full commitment `seller|method|ordered line membership`; a repeated
   LINE is still refused by the existing `DuplicateLineMembership` guard, so a genuine duplicate can never pass.
2. Selection then failed `storefront.captured_selection.unavailable`. Cause: `ResolveMethod` resolves an eligible row
   by seller+method only, so both of that seller's commitments received the FIRST row's price, and
   `CartShippingDraftGroup.ReplaceAcceptedEvidence` correctly refused the payable mismatch. Repaired with an optional
   trailing `ObligationKey` on `CartShippingGroupRequest`/`CartShippingGroupEligibleMethod` (legacy callers unaffected,
   default null): the captured writer stamps the stable server group key on both sides and the planner prefers that
   exact match before any seller+method fallback.
3. With both repairs the request now reaches the atomic writer and fails at persistence:

```
PUT /v1/storefront/shipping/selection -> 500 platform.unexpected
Microsoft.EntityFrameworkCore.DbUpdateException -> Npgsql.PostgresException 23505:
duplicate key value violates unique constraint "ux_cart_shipping_groups_cart_seller_method"
(OrderDbContext, Tooba.Order.Infrastructure.Persistence)
```

**Blocker (exact).** The persisted `order.cart_shipping_groups` carries a UNIQUE index on
(cart, seller, method) — `ux_cart_shipping_groups_cart_seller_method`. The required Marketplace scenario puts the SAME
real seller in TWO groups on ONE cart with the SAME one-level catalog method (`post:standard`), one Platform-fulfilled
and one Seller-fulfilled. That state is structurally rejected by the existing index. Making it lawful needs a
schema/migration change (or a new per-group owner discriminator column), which this task explicitly forbids
("No schema/migration/history/baseline edit"). The write is therefore blocked before any reservation/order exists;
nothing was half-written (the failure is a single SaveChanges inside the existing atomic boundary).

Consequence for the acceptance ledger: no 3-owner persisted order, no manual Pending for it, no Order-only read of
mixed evidence, no coupon-on-one-group fact, and no usable end-to-end payment page can be delivered in this envelope.

## C — PAYMENT DUPLICATE / JSON WIRE: repaired, measured

Source trace confirmed: `StorefrontStartPaymentComposer` derives `start-payment:payment:<raw key>` and forwards it to
Payment; `PaymentDirectory.InitiateAsync` already replays a persisted key, and Payment keeps idempotency ownership.

Measured defect (real, concurrent): two SIMULTANEOUS same-key `POST /v1/storefront/cart/{id}/start-payment` calls —
one returned `200` with payment `01a181df-0071-...`, the other returned
`500 platform.unexpected / DbUpdateException` (unique index on `idempotency_key`). The loser surfaced a raw
persistence error as a platform failure.

Repair in the owning adapter only (`Payment.Infrastructure/Directories/PaymentDirectory.cs`): the create-path
`SaveChangesAsync` is wrapped for the EXISTING idempotency unique index (SQLSTATE 23505 + constraint name contains
`idempotency_key`); on that conflict it detaches exactly this attempt's payment/allocation/attempt graph and returns
the WINNER — same `PaymentId` AND same `AttemptId` — after re-checking checkout + provider identity; if the winner's row
is not yet visible (still open transaction) it fails closed with the typed `payment.idempotency.conflict`. No random
fallback key, no in-memory lock, no new table, no swallowed exception, no new response.

After the repair (same concurrency shape, fresh key `tb240r2-sim-62ba5760-...`):

```
J1: 200 paymentId 01a181e1-83c5-7000-a58f-465131be142e attemptId 01a181e1-83c8-7000-a583-376584e48fee status Pending amount 2116500.0000
J2: 200 paymentId 01a181e1-83c5-7000-a58f-465131be142e attemptId 01a181e1-83c8-7000-a583-376584e48fee   (identical)
DB: 1 payment row for that key, 1 attempt; no key with more than one payment row.
Sequential same-key repeats: identical PaymentId + AttemptId on every call.
```

JSON wire claim could NOT be reproduced on the served binary: the browser-shaped camelCase body
(`expectedCartVersion`, `idempotencyKey`, `providerCode`, `expectedSelectionRevision`) bound correctly — the created
payment's persisted key is exactly `start-payment:payment:<the camelCase key sent>`, the provider persisted as
`manual`, and the checkout/amount were the real ones. No serializer change was made, because none is justified.
`ExpectedSelectionRevision` remained TEXT end-to-end in that call (`"1"`).

## D — What was explicitly NOT delivered

- No mixed-owner Order/manual-Pending (blocked by the index above), no 3-group payment-draft/StartPayment.
- No coupon fact (the group context they would be measured on never persisted).
- No Order-only historical read of mixed evidence, no foreign-buyer denial fact for it.
- No FE3102 build, no browser/desktop/390/360 screenshots and **no usable URL handoff**: the shipping page can render
  the three priced owner cards, but its save would hit the same 500, so handing off a payment window would be
  misleading. No API-only handoff is claimed as a usable trial.
- Geography outcome handling: implemented (non-Completed commissioning now throws at startup and the seed validates
  the existing shape BEFORE staging, so nothing stays eligible for a later SaveChanges) — verified by build + the
  existing seam tests only; NOT re-exercised against a contradictory live row.
- The `checkoutReplayed` flag still reports `false` on a replayed checkout; not repaired (no defect proven against a
  double-payment/duplicate-order side effect).

## E — Preservation

No migration/schema/baseline/history change; no reseed/reset; no product/stock seed; no direct business SQL; no
foreign DbContext; no new permission catalogue; no guard weakening. Task Host5190 only (PID verified each restart).
USER watched `:3000`/`:5088`, handoff `:3100`/`:5188` + `D:\temp\tb238-handoff-chrome` and the user's review window were
never enumerated, read, automated, reloaded or closed. Checked-in gates stay OFF; local task-only toggles unchanged
(`StorefrontCapturedPreview` preview/selection/checkout true, `AvailableDateCount=7`). TB185 paused/default-OFF/
PendingAllocation. 231/232 shared NOT APPLIED and the stale EF snapshot gap retained. Known 238-unknown-22001,
230 empty-key refresh/in-flight, cross-owner label/decoder, Offer API proof, 227 mobile empty-note and the unrecovered
225-R1 user LOCAL tab edits remain disclosed and untouched.
