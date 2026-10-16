# SHIPPING ONE-LEVEL UI — TB-142

**Task:** `TB-SHIPPING-ONE-LEVEL-TABS-142`
**Mode:** `BOUNDED_ISOLATED_MARKETPLACE_SETTLEMENT_PENDING_LEDGER_ACCEPTANCE`
**Parent:** `TB-SHIPPING-ONE-LEVEL-TABS-141`
**Base SHA:** `0a771866e85057470a2f7d8fee4764a6f84093a9` (verified `HEAD == origin/main` before work)
**Result:** **PARTIAL** (Mission A = PASS; Mission B = BLOCKED on an Architect decision, with the exact prerequisite below)
**Stop-State:** `STOP_FOR_ARCHITECT`

---

## 1. What TB-142 asked for

The Architect chose Option 1 of TB-141: keep Settlement schema activation **explicit/CLI-owned**; do **not** add
Settlement to the Development startup migrator chain, do **not** add a migration-order constant, do **not**
change shared/production module activation. Instead, authorize an isolated, disposable **Marketplace-edition**
overlay **only** if the canonical Marketplace connection-reference authority and fail-closed tenant binding can
be established **without fabricated production settings**, and then:

- **Mission A** — prove the Settlement schema is reachable and applied through the owner-owned mechanism, never
  by adding automatic Host startup registration.
- **Mission B** — with a *real sanctioned scratch fixture* on a *genuine customer/canonical API path*, attempt
  exactly one same-seller two-lines/two-methods checkout + fake sandbox payment completion, and read back
  **exactly two** `settlement.shipping_group_fees` rows (`PendingAllocation`, `SUM=380000`).

## 2. Mission A — SAFE MARKETPLACE ACTIVATION: **PASS**

### 2.1 Edition / connection authority actually used

| Dimension | Value |
| --- | --- |
| Edition | `Marketplace` (`Tooba:Edition`) |
| Commerce binding | `Tooba:Marketplace:ConnectionReference = marketplace-142-scratch` |
| Tenant list | **empty** (`Tooba:SingleStore:Tenants = []`) |
| Reference → DB | `marketplace-142-scratch` → `tooba_142_marketplace` on `127.0.0.1:55463` (task-owned container `tooba-tb142-pg`) |
| Messaging reference | `messaging-142-scratch` → `tooba_142_messaging` on the same disposable container |
| Authorization | `Mode=InMemory`, `ApplySchemaOnStartup=false` |
| Payment | `Mode=Sandbox`, `DefaultProvider=fake`, PSP endpoints/secrets **empty** |
| Legacy SingleStore seeds | `CatalogDemo:RunLegacyBootstraps=false`, `CatalogDemo:AllowResetAndSeed=false` |

`Tooba:Marketplace:ConnectionReference` is the **only** authority used: it is the same field that
`PlatformOptionsValidator.BuildRegistry` requires (`Marketplace edition requires Tooba:Marketplace:ConnectionReference.`),
that `TenantResolutionMiddleware` resolves for every non-health request, and that `HostReadinessEvaluator`
validates against `Tooba:PostgreSQL:ConnectionReferences`. No marketplace setting was invented; the overlay is
scratch connection plumbing around the canonical authority only.

### 2.2 Fail-closed tenant binding

A Marketplace registry carries **no** tenant map by construction (`BuildRegistry` populates `hosts`/`tenants`
only for `SingleStore`). The guard therefore asserts the *stronger* property:

- `tenantCount == 0` **and** `marketplaceReferenceConfigured == true` **and** the marketplace reference
  **resolves** in `Tooba:PostgreSQL.ConnectionReferences`.

A request can only ever bind to that one reference: if the reference were blank, `BuildRegistry` throws and the
process refuses to start; if it were unresolvable, `TenantResolutionMiddleware` throws
`platform.connection.unconfigured` per request. There is no per-request tenant to spoof, so there is no
cross-tenant leak surface in this overlay. See `isolation-guard.json` (`failClosedTenantBinding: true`).

### 2.3 Isolation

Every isolation dimension passed (`isolation-guard.json`, `failClosed: true`):

- no `tooba_alpha` string, no `tenant-alpha` reference, no `Port=5432`, no prior scratch port (`55447–55462`);
- task-owned scratch port `55463` only;
- messaging on its own disposable DB (`tooba_142_messaging`, schema `transport`, 10 tables);
- shared `:5088`, `:3000` and the shared Postgres `:5432` listeners were **not** addressed; the shared
  `tooba_alpha` database/container was **not** touched.

### 2.4 Single launch, no loops

One launch of the task-owned isolated Host (`launch-isolated-host-142.ps1`, PID `29488`, `LAUNCH_COUNT=1`):

```
HEALTH_200_OBSERVED=True   HEALTH_BODY={"status":"ok"}    STARTUP_ELAPSED_SECONDS=4
READY_STATUS=200  READY_BODY={"status":"ready","checks":{"edition":"Marketplace","postgresql":"configured",
                  "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
```

`/ready` reporting `edition=Marketplace` is the runtime proof that the canonical Marketplace edition is legally
bootable with **no** source change at all.

### 2.5 Settlement schema is applied by the canonical Marketplace owner seam

This is the new, decisive TB-142 finding and it **supersedes the TB-141 blocker framing**:

`MarketplaceDevelopmentBootstrap` — the pre-existing, edition-owned Marketplace composition seam invoked from
`Program.cs` whenever `Environment.IsDevelopment() && Edition == Marketplace` — explicitly migrates
`SettlementDbContext` (line 54), alongside Party/Identity/Order/Payment/Notification/AccessControl/Content/Media/
PageComposition/Story.

Consequences, all observed at runtime (read-only readback `scratch-readback-142-marketplace.txt`):

- `settlement.__ef_migrations_history` contains **both** migrations:
  `20260827030000_InitialSettlement`, `20261016140000_AddSettlementShippingGroupFees`;
- `settlement.shipping_group_fees` exists with **26 columns** and **6 indexes** (`pk_`, `idempotency_key`,
  `payment_group`, `payment_group_charge`, `seller_party_id`, `shipping_group_id`);
- the payment-only columns (`allocation_policy_version/reference`, `carrier_payable_amount`,
  `seller_share_amount`, `marketplace_share_amount`, `shipping_discount_amount`, `shipping_tax_amount`,
  `funder_adjustment_amount`, `funder_reference`, `reversal_of_shipping_group_fee_id`) remain **nullable** —
  i.e. the "amounts unknown / never revenue" shape is preserved at schema level in this edition too;
- `settlement.shipping_group_fees` row count = **0**.

**No source was modified.** No `IModuleSchemaMigrator` registration was added, no `ModuleSchemaMigrationOrder`
constant was added, and `SettlementModule` still registers zero module migrators (TB-141 finding unchanged).
The schema arrives through the Marketplace seam that already existed; the SingleStore Development startup
migrator chain is untouched.

### 2.6 Marketplace handler registration (fresh focused evidence)

`Settlement_handlers_are_marketplace_gated_in_module_registration` → **Passed** (1/1). Marketplace registers
`SettlementPaymentSucceededHandler` + `SettlementRefundSucceededHandler`; SingleStore does not. The runtime gate
is `SettlementModule.IsMarketplaceEdition(configuration)` reading `Tooba:Edition`.

So after TB-142 the two halves are provable separately:

| Half | Edition needed | TB-142 status |
| --- | --- | --- |
| Settlement schema applied by an owner seam | `Marketplace` | **proved at runtime** |
| Settlement payment/refund handlers registered | `Marketplace` | **proved by focused test** |

## 3. Mission B — one paid event acceptance: **BLOCKED (prerequisite named)**

### 3.1 First owner-correct blocker, observed at runtime

The checkout/payment path requires commerce-module schemas that the canonical Marketplace Development seam does
**not** provision. `MarketplaceDevelopmentBootstrap` migrates exactly **11** contexts; after it ran, the scratch
marketplace database contained exactly these schemas:

```
access_control, content, identity, media, notification, order, page_composition, party, payment, settlement, story  (+ public)
```

**Absent:** `cart`, `catalog`, `offer`, `pricing`, `inventory`, `tax`, `promotion`, `shipping`/`shipping_*`,
`fulfillment`, `returns`, `geography`.

Read-only, bounded API probes against the running isolated Host (2 GET requests, no writes) show the canonical
storefront path failing at the module boundary rather than producing a checkout:

```
GET /v1/storefront/products?locale=fa  -> 500 (platform.unexpected)
GET /v1/storefront/home?locale=fa      -> 500 (platform.unexpected)
```

and the Host's own per-handler error logs name the owner-correct blocker:

```
Npgsql.PostgresException 42P01: relation "catalog.categories" does not exist
   -> ExceptionHandlerMiddleware: unhandled exception while executing the request (the 500 above)
Npgsql.PostgresException 42P01: relation "cart.carts" does not exist
   -> CartExpiryWorker: "Cart expiry failed for one tenant."
Npgsql.PostgresException 42P01: relation "catalog.outbox_messages" does not exist
   -> "Outbox claim failed for one module. Schema=catalog"
Npgsql.PostgresException 42P01: relation "offer.outbox_messages" does not exist
   -> "Outbox claim failed for one module. Schema=offer"
```

There is therefore **no cart, no catalog product, no offer, no price, no stock, no tax rule, no shipping
method and no fulfillment table** in the isolated Marketplace runtime — so a genuine customer cannot even browse,
let alone check out, and no paid event can ever reach `SettlementPaymentSucceededHandler`.

### 3.2 Why I did not work around it (scope discipline)

Every available workaround is explicitly forbidden by TB-142, and each would have produced a fake green:

1. **Register the commerce modules in the Marketplace startup seam** — i.e. add module migration registration to
   `MarketplaceDevelopmentBootstrap`. TB-142 forbids: *"No startup migration registration"*. It is also the exact
   class of change the Architect rejected in TB-141.
2. **Apply the other modules' migrations myself into the scratch DB** — a step the canonical Marketplace seam
   deliberately omits (it is not the approved mechanism for those modules), and it would still leave the runtime
   with **nothing to sell**.
3. **Seed a commerce fixture under Marketplace** — no canonical Marketplace seeder exists. The single sanctioned
   Development commerce dataset (`WorkspaceDemoSeed` → `WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync`:
   demo sellers/offers/prices/stock/companion offer) is reachable **only** through
   `DevelopmentSchemaMigrator.ApplyAsync`, which requires an **Active `store-alpha` SingleStore tenant** and is
   not invoked on the Marketplace branch of `Program.cs`. Reaching it from Marketplace would mean fabricating a
   tenant/seed path.
4. **Inject a payment event / call `SettlementDirectory` / synthesize ledger rows** — explicitly forbidden
   ("Do not inject payment events, call SettlementDirectory manually, synthesize ledger rows or claim
   end-to-end success from isolated unit tests").

### 3.3 The real architectural conflict (decision-ready)

The recorder and the fixture live in **mutually exclusive editions**, and that is not a test-environment artefact
— it is a deployment-model contradiction:

- **Edition `SingleStore`** owns the runnable storefront commerce fixture (per-tenant `store-alpha`, catalog
  demo, cart/checkout/fulfillment) — and, by design, **checkout can be on the marketplace sales channel**
  (`StoreCommerce.SalesChannel = Marketplace`, as used by the sanctioned `alpha.localhost` dev store and by
  `WorkspaceDemoMarketplaceSeed`, which prices offers with `SalesChannel.Marketplace`).
- **Edition `Marketplace`** registers the Settlement payment/refund handlers — and has no commerce fixture at all.

So a real single-store deployment whose storefront sells on the Marketplace **sales channel** submits a genuine
paid checkout that the Settlement module is **not wired to observe**, because the gate is the deployment
**edition** (`Tooba:Edition == "Marketplace"`), not the **sales channel** of the sale.

### 3.4 Exact prerequisite for a future PASS

One Architect decision, then the pending-ledger acceptance becomes executable:

- **Option A (edition-gate correction).** Re-gate `SettlementPaymentSucceededHandler` /
  `SettlementRefundSucceededHandler` on the **sale's sales channel / deployment capability**
  (`SalesChannel.Marketplace` seen through `IStoreCommerceContext` or an explicit settlement capability flag)
  instead of `Tooba:Edition`. Then the already-sanctioned **SingleStore** isolated fixture (TB-138/TB-140
  lineage: one Checkout, one SellerOrder, two shipping groups, two payment allocations, one FulfillmentUnit with
  two group ids) can produce the two pending rows in an environment where the fixture legitimately exists.
  *Risk/impact:* changes which deployments write settlement ledger rows → commercial/architectural decision.
- **Option B (Marketplace runtime fixture).** Authorize the Marketplace composition seam to provision the
  commerce-module schema set **and** a Development commerce fixture under the marketplace connection reference,
  so a Marketplace runtime can host the existing demo dataset. *Risk/impact:* source change to a startup
  migration seam (the thing TB-141/TB-142 deliberately kept closed).

Until one of these is authorized, the two pending rows cannot be produced by any canonical path — and TB-142
must not claim them.

## 4. What remains proven (unchanged, re-verified in TB-142)

`ShippingGroupFeeAllocationTests` → **Passed 9/9** (`focused-test-settlement-pending-ledger-142.txt`):
two groups of one seller record distinct known charges with no seller revenue; same method / different sellers
never merge; **recording and reversing are idempotent and create no duplicate row** (bounded targeted proof that
repeated deliveries cannot duplicate charge rows — no extra customer payments were made); pending allocation can
never become seller revenue or a payout; reversal is scoped to one group and never rewrites the original row.

Domain/persistence-level guarantees therefore remain: `PendingAllocation` only, financial columns NULL, no
shipping seller credit and no payout.

## 5. Explicit non-claims

- **No** `settlement.shipping_group_fees` row was created in this task; the marketplace-path readback is **0 rows**.
  Consequently the runtime NULL-shape of a *recorded* row is **not** asserted here — it is asserted at the
  domain/persistence level only (§4).
- **No** end-to-end paid-journey PASS is claimed, in either edition.
- **No** final commercial settlement is claimed: signed seller/platform portions, carrier cost, tax and funding
  remain a business decision (`PendingAllocation` must never be paid out as-is).
- **No** production/`tooba_alpha`/shared-port configuration was used, copied or derived.

## 6. Changes to source

**None.** TB-142 is observation/proof only; no proven owner-correct blocker was fixable inside scope, and both
available fixes are forbidden by the task (Option A is a commercial gate decision, Option B is startup migration
registration). The task's requirement *"avoid modifying architecture to force a green result"* is honoured.

## 7. Rollback

Nothing to roll back: no production/shared resource was touched, no source changed. The only runtime resources
were the task-owned container `tooba-tb142-pg` (removed) and the task-owned Host PID `29488` (path-verified then
killed); `.tmp-142-run/` and the scratch password file were deleted. Rollback of this commit only removes
documentation and evidence.

## 8. Evidence index

`docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-142/`

| File | Content |
| --- | --- |
| `isolation-guard.json` | edition, marketplace reference, empty tenant list, fail-closed binding, isolation booleans |
| `prepare-isolated-marketplace-target-142.js`, `prepare-output-142.txt` | guarded overlay builder + `PREPARE=PASS` |
| `launch-isolated-host-142.ps1`, `host-launch-142.txt` | single launch, PID `29488`, health/ready bodies |
| `readback-142-marketplace.sql`, `scratch-readback-142-marketplace.txt` | schemas, migration history, `shipping_group_fees` DDL/rowcount |
| `bounded-commerce-probe-142.txt` | 2 read-only storefront GETs → 500, correlated `traceId`s |
| `host-commerce-blocker-log-142.txt` | owner-correct `42P01` module blockers from the Host's own logs |
| `focused-test-settlement-marketplace-gate.txt` | Marketplace handler registration → Passed 1/1 |
| `focused-test-settlement-pending-ledger-142.txt` | `ShippingGroupFeeAllocationTests` → Passed 9/9 |
| `cleanup-142.ps1`, `cleanup-142.txt` | PID-verified teardown; shared listeners untouched |
| `post-result.js`, `RESULT.bridge.txt` | Bridge Result payload |

## 9. Timebox

Single bounded activation attempt + single bounded paid-path feasibility probe; cleanup and report inside the
45-minute box. No loops, no repeated startup attempts, no extra customer payments, no broad test suites.
