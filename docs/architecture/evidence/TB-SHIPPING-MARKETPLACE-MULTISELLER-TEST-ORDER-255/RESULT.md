# RESULT — TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255

Status: **BLOCKED before creation — environment barrier (no valid Marketplace context exists in the current installation)**
Decision: **MARKETPLACE_OWNER_KIND_UNREACHABLE_IN_CURRENT_SINGLESTORE_CANONICAL_INSTALLATION_ENVIRONMENT_DECISION_REQUIRED**

NOT PASS, NOT PARTIAL creation, NOT Architect/Visual ACCEPT. No new order, cart, checkout, migration, restart, config
change or business-data write was produced. `automaticNextImplementationTask: NONE`; `STOP_FOR_ARCHITECT`.

- Parent: `TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1`
- Expected-Base: `ab4ebd3ee1b28d20250291187695020ae3693729`
- Actual Base / HEAD / `origin/main`: `ab4ebd3ee1b28d20250291187695020ae3693729` (all three equal)
- Code-SHA: **NONE** (this task produced no source change; only the persisted task + this evidence)
- Channel: `tooba-main` · Bridge task row `7c75271b-8dff-40fc-934e-eef17c74b6b2`
- Runtime: edition **SingleStore**, deployment `store-alpha-canonical-tb253r4`, tenant **store-alpha**,
  connection `tenant-alpha` → database **`tooba_alpha`**
- New order/cart IDs: **NONE created** (see §5/§6)
- Review URL: **NONE** — a review URL for a Marketplace-responsibility order does not exist and was not faked

## 1. Preflight (verified, not assumed)

```text
Expected-Base:      ab4ebd3ee1b28d20250291187695020ae3693729
HEAD:               ab4ebd3ee1b28d20250291187695020ae3693729
origin/main:        ab4ebd3ee1b28d20250291187695020ae3693729
Tracked worktree:   clean before this task's own artifacts
```

No intervening task/result commit exists between the expected base and HEAD, so no descendant parity question arises.
The received `TASK.md` was persisted verbatim from the Bridge payload (`docs/ai/tasks/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/TASK.md`, 7260 bytes).
Naming correction recorded: the pipeline ID `TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255` is the only ID used;
the earlier non-envelope draft `TB-MARKETPLACE-MULTISELLER-TEST-ORDER-255` is the same work and was not executed
separately.

## 2. Runtime authority of the current environment

The running Host answered its own Development commerce probe (`GET /v1`-level `/__platform-commerce`, 200):

```json
{"edition":"SingleStore","deploymentId":"store-alpha-canonical-tb253r4","tenantId":"store-alpha",
 "connectionReference":"tenant-alpha","resolvedHost":"127.0.0.1","themeReference":"theme-alpha",
 "defaultMarketReference":"IR"}
```

Verified facts about what is actually running:

| Item | Observed |
| --- | --- |
| Canonical FE | `http://127.0.0.1:3000`, PID 45832, `next start -p 3000` |
| Canonical Host | `http://127.0.0.1:5088`, PID 17344, `artifacts/publish-primary/Tooba.Host.exe`, `/health` 200 `{"status":"ok"}` |
| FE → Host wiring | `src/frontend/.env.local` = `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088` |
| Edition / deployment | `SingleStore` / `store-alpha-canonical-tb253r4` (process-level, from `appsettings.Development.json`) |
| Tenant resolution | `store-alpha` (Active) for host `127.0.0.1`; `tenant-alpha` → `tooba_alpha` |
| Other Tooba processes | **none** — no second Host, no Marketplace process, no alternate port |

`Tooba:Edition` is a process-level value (`ControlPlaneRegistry`, resolved once at startup and read by
`TenantResolutionMiddleware`); there is no per-request, per-Host-header or admin-API way to switch it, and no
endpoint in the Host writes it.

## 3. Existing Marketplace configuration/path — what actually exists

Inspected as instructed before any checkout attempt:

| Location | Content | Usable for this scenario? |
| --- | --- | --- |
| `Tooba:Edition` (Development config of the served build) | `SingleStore` | No — Marketplace owner policy is edition-gated |
| `Tooba:Marketplace` | only `ConnectionReference: "marketplace"`; **no** Marketplace tenants/deployment section | Partial key only |
| `Tooba:SingleStore:Tenants` | `store-alpha` (Active, `tenant-alpha`, hosts incl. `127.0.0.1`), `store-disabled` | This is the live topology |
| `PostgreSQL:ConnectionReferences.marketplace` | `...Database=tooba_marketplace` | DB exists but incomplete (below) |
| `Tooba:DeploymentId` | `store-alpha-canonical-tb253r4` | SingleStore deployment |
| Running Marketplace deployment | **none** | No |

Database inventory (read-only metadata + counts, no business write):

| Database | Shape | Notes |
| --- | --- | --- |
| `tooba_alpha` | 35 schemas, full current install (catalog/offer/cart/order/shipping/…) | The live canonical store; 22 checkouts |
| `tooba_marketplace` | 13 schemas / 74 tables — **no** `catalog`, `offer`, `cart`, `shipping`, `geography`, `fulfillment` | Cannot serve a Marketplace storefront; would need the full migration runner + seeds |
| `tooba_marketplace_tb239` | 34 schemas / 269 tables, 6 old checkouts | A **previous task's** Marketplace trial database (TB239/240-era), not the current installation |

The Marketplace Development bootstrap (`MarketplaceDevelopmentBootstrap.ApplyAsync`) migrates only
party/identity/order/payment/settlement/notification/access_control/content/media/page_composition/story, and its
reference-data and sellable-fixture commissioning is opt-in. A real Marketplace install therefore additionally needs
catalog/offer/shipping/geography migrations plus reference data, sellable fixtures and seller identities.

## 4. Why the requested order cannot be produced in this installation (code authority, not a guess)

The requested scenario requires `accepted_owner_kind = Marketplace` on **every** accepted group and explicitly forbids
"Store relabelled as Marketplace". One Order-owned policy decides that value, and it is edition-gated:

```startLine:130:167:src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedPreview/StorefrontCapturedOwnerPolicy.cs
        return edition switch
        {
            ToobaEdition.SingleStore => ResolveStore(tenant, responsibility.Value),
            ToobaEdition.Marketplace => ResolveMarketplace(tenant, responsibility.Value, groupSellerPartyId),
            _ => Failure(StorefrontCapturedOwnerFailure.OwnerUnavailable),
        };
    }

    private static StorefrontCapturedOwnerResolution ResolveStore(
        TenantContext? tenant,
        OfferFulfillmentResponsibility responsibility)
    {
        // ...
        return responsibility switch
        {
            OfferFulfillmentResponsibility.Platform =>
                Resolved(ShippingCapturedQuoteOwnerKind.Store, tenantId, null),
            // ارسال فروشنده در SingleStore پشتیبانی نمی‌شود و هرگز به Store بازتفسیر نمی‌شود.
            OfferFulfillmentResponsibility.Seller =>
                Failure(StorefrontCapturedOwnerFailure.SellerUnsupportedInSingleStore),
            _ => Failure(StorefrontCapturedOwnerFailure.ResponsibilityInvalid),
        };
    }
```

- `OfferFulfillmentResponsibility` has exactly two values (`Seller = 0`, `Platform = 1`); there is no
  "Marketplace responsibility" an offer could carry.
- In `SingleStore`, `Platform` maps **unconditionally** to `Store + tenantId`; `Seller` is refused. `Marketplace`
  owner kind is produced only in `ToobaEdition.Marketplace`.
- The same edition gate governs the accepted-quote/publication identity, so a Marketplace-owned method cannot even be
  authored or accepted here:

```startLine:43:62:src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingCapturedOwnerScopeResolver.cs
    private static ShippingAuthoringOwnerScope? ResolveStore(
        ShippingCapturedQuoteOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId,
        ICurrentTenant tenant)
    {
        var current = tenant.Current;
        var currentTenantId = current?.TenantId.Value;

        // In SingleStore a residual/inactive/missing tenant context is unusable; nothing is defaulted.
        if (current is null
            || current.Status != TenantStatus.Active
            || string.IsNullOrWhiteSpace(currentTenantId)
            || kind != ShippingCapturedQuoteOwnerKind.Store
            || sellerPartyId is not null
            || !string.Equals(tenantId?.Trim(), currentTenantId.Trim(), StringComparison.Ordinal))
        {
            return null;
        }
```

```startLine:108:114:src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAuthoringOwnerAuthorizer.cs
        var installation = ShippingInstallationContext.Resolve(edition, tenant);
        var owner = installation.Edition == ToobaEdition.SingleStore
            ? ShippingAuthoringOwnerScope.ForStore(installation.TenantId!)
            : ShippingAuthoringOwnerScope.ForMarketplace();
        return (owner, actorUserId);
```

Seller-scoped shipping authoring additionally throws `shipping.authoring.owner_scope_unavailable` (404 capability
failure) before any resolve when the edition is not Marketplace — so no seller-owned alternative exists either.

Architecture also forbids the only shortcut: `docs/architecture/02-edition-tenant-deployment.md` states
"Do **not** interpret the requirement as one runtime serving Marketplace and Single-Store simultaneously" — a
Marketplace responsibility order belongs to a **separate Marketplace deployment** against a marketplace database.

## 5. What this task deliberately did NOT do

Per §3 of the received task ("If a valid Marketplace context is unavailable without such a change, STOP before
creating another nonconforming order"), the checkout was stopped **before** creating anything:

- no new cart, no new checkout/order, no reservation, no payment attempt;
- no edition, tenant-resolution, connection-mapping, deployment-id or port change;
- no parallel database, no replacement runtime, no alternate final port;
- no Marketplace semantics added to SingleStore;
- no direct business SQL, DDL, reset/reseed, or fabricated origin/payment facts;
- no relabel/rewrite of any existing accepted Store evidence;
- no Host rebuild/restart, DataProtection untouched, local CapturedPreview gates untouched (checked-in OFF).

All database access in this task was read-only metadata/count verification.

## 6. Retained prior evidence (unchanged, not relabelled)

The earlier multi-seller order created through the normal storefront flow is **untouched** and still shows what this
installation can lawfully produce:

`01a1bd30-7efe-7000-9abc-3bcc111d967c` (`OnlinePurchase` / channel `Marketplace` / IRR / shipping 375000 / submitted)

| Seller | Product split | Method identity (`method_code`) | Day / window | Accepted owner kind | Owner tenant | Fee |
| --- | --- | --- | --- | --- | --- | --- |
| `01a030d1-40cb-7000-8abe-6d31739956c5` (A) | A1 + A2 (one method) | `post\x1F` (service `post`, option NULL) | 2026-11-16 / 09:00-10:00 | `0` = **Store** | `store-alpha` | 125000 |
| `01a030d1-40cb-7000-8abe-6d31739956c5` (A) | A3 (second method) | `post\x1Fstandard` (service `post`, option `standard`) | 2026-11-11 / 09:00-17:00 | `0` = **Store** | `store-alpha` | 125000 |
| `01a0ffff-0001-7000-8000-000000000001` (B) | at least one line | `post\x1F` | 2026-11-16 / 09:00-10:00 | `0` = **Store** | `store-alpha` | 125000 |

This proves the seller/product/2+1-method/day/window half of the scenario is reachable in the current environment; the
**responsibility half is not**. It was not modified, and no second Store-owned substitute order was created.

## 7. The one concrete environment decision required

> **Authorize a Marketplace deployment for this test, because `SingleStore` + `store-alpha` + `tooba_alpha`
> cannot emit `accepted_owner_kind = Marketplace` by design.**

Concretely, the decision must cover (single decision, one place):

| Aspect | Required change if approved |
| --- | --- |
| Edition | process `Tooba:Edition = Marketplace` (currently `SingleStore`) |
| Deployment | a distinct `Tooba:DeploymentId` for the Marketplace deployment |
| Connection | `Tooba:Marketplace:ConnectionReference = marketplace` (already present) resolved to **one complete Marketplace database** |
| Database | either fully migrate `tooba_marketplace` (all modules + reference data + sellable fixtures + seller identities) or explicitly adopt `tooba_marketplace_tb239` |
| Runtime | edition is process-level ⇒ either the canonical 5088 process switches edition, or an approved second Marketplace process/port exists (currently prohibited) |

Expected access/data impact, stated honestly:

- **No** `tooba_alpha` row is written, deleted or relabelled by this decision. The 22 existing checkouts (including
  `01a1bd30-…`) stay exactly as they are.
- A Marketplace run does **not** serve the `store-alpha` storefront: tenant resolution becomes `Tenant = null` and all
  storefront/cart/order reads move to the marketplace database. During a switched test, `3000/5088` would no longer
  show the `tooba_alpha` store; a switch back would be required afterwards.
- The test order, its cart, its sellers and its accepted groups would live in the marketplace database, not in
  `tooba_alpha` — which conflicts with the earlier "same environment and same `tooba_alpha`" instruction and is
  precisely why an explicit decision (not a worker guess) is required.
- `tooba_marketplace` as it stands today cannot be used without a full migration + seeding pass; using it silently
  would fabricate a half-installed storefront.

If instead the decision is to keep `SingleStore`/`tooba_alpha`, then the only lawful outcome for this scenario is
**Store** responsibility (already demonstrated by `01a1bd30-…`), and no code path may relabel it as Marketplace.

## 8. Boundaries preserved

- Base/HEAD/`origin/main` parity verified; no reset, amend, rebase, force or stash.
- No backend, frontend, contract, migration or DI change; no test-suite loop, no disposable database, no screenshots.
- No live Tipax call, no credential request, no provider acceptance claim.
- Prior technical-vs-visual acceptance boundaries, Tipax deferral, TB185 paused/default-OFF, and retained unrelated
  issues are all unchanged.

## 9. Worker result envelope

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255
Parent-Task: TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Status: BLOCKED
Decision: MARKETPLACE_OWNER_KIND_UNREACHABLE_IN_CURRENT_SINGLESTORE_CANONICAL_INSTALLATION_ENVIRONMENT_DECISION_REQUIRED
Base-SHA: ab4ebd3ee1b28d20250291187695020ae3693729
Code-SHA: NONE
Evidence-SHA: c79f23bd90d3426deb18c9808ab6a31a23ce0655
Runtime-Edition: SingleStore
Runtime-Deployment: store-alpha-canonical-tb253r4
Runtime-Tenant: store-alpha
Runtime-Connection: tenant-alpha
Runtime-Database: tooba_alpha
New-Order-Id: NONE
New-Cart-Id: NONE
Review-URL: NONE
Order-State: no order created (stopped before a nonconforming Marketplace-substitute order)
Payment-State: unchanged; no payment attempt
END_TOOBA_WORKER_RESULT
```

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
