# TB-SHIPPING-CORE-RATES-048

Milestone **3 of 6** (Shipping MVP): the module-owned, **non-runtime** Shipping rate-authority
foundation. A new `src/backend/Modules/Shipping` capability now owns commercial rate and coverage
*policy*; it builds, it is covered by 34 focused tests, and **it changes no running behaviour**.

- Parent task: `TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047`
- Expected base SHA: `fcf91e30e21675c94d40bd7eab6df55127e52f6f`
- Status: **PASS** (worker). Worker PASS != Architect ACCEPT.
- Stop-State: `STOP_FOR_ARCHITECT`
- Scope ledger: see §5. Persistence is **mapped but not wired and not migrated**.

## 1. Ownership — what moved, what deliberately did not

| Thing | Owner before | Owner after this task |
| --- | --- | --- |
| Service / option **identity + display** (codes, provider kind, icon, colour, sort, translations) | Fulfillment (unchanged) | Fulfillment (unchanged) |
| Service **catalog CRUD** and `/admin/shipping-services` | Fulfillment CRUD + existing Admin surface (unchanged) | unchanged |
| Commercial **base charge, preparation/lead days, free-above threshold, coverage grant** | implicit in `Tooba:ShippingMethods` configuration read by the Order calculator | **Shipping** (new, non-runtime) |
| Customer-facing **promise** (frozen snapshot) | Order (unchanged) | Order (unchanged) |

No second Admin shipping-services page was created, no existing CRUD was moved or duplicated, and the
legacy configuration path that checkout reads today is untouched. Shipping's aggregates carry **no**
display name, icon, colour, sort order or translation — a test pins that
(`Shipping_does_not_duplicate_the_fulfillment_catalog_identity`).

`ServiceCode` in Shipping is an **opaque compatibility key**: exactly the codes Fulfillment already
publishes. `ServiceCodeCompatibilityTests` proves Shipping can express a policy for **every** code in
`ShippingMethodRegistry.All` **and** every code in the default `ShippingMethodsOptions.Rates`, by reading
Fulfillment **Contracts from the test project only** — so if Fulfillment ever publishes a new code, the
test fails and a human decides, rather than Shipping silently drifting.

## 2. What was built

```text
src/backend/Modules/Shipping/
  Tooba.Shipping.Contracts/        Errors/ShippingErrorCodes.cs
                                   Rates/ShippingRatePolicyContracts.cs
  Tooba.Shipping.Domain/           Aggregates/ShippingRatePolicy.cs
                                   Aggregates/ShippingCoverageGrant.cs
                                   Rates/ShippingRatePolicyEngine.cs
  Tooba.Shipping.Infrastructure/   Persistence/ShippingDbContext.cs
                                   Rates/ShippingRateQuoteLookup.cs
                                   DependencyInjection/ShippingModule.cs
  Tooba.Shipping.Tests/            Rates/, Compatibility/
```

**Rate-policy identity and parameters** (`ShippingRatePolicy`): stable `RatePolicyId`, `ServiceCode`,
destination scope, `ChargeAmount`, `CurrencyCode` (3 letters), **`PreparationDays` and `LeadDays` as two
distinct concepts**, optional `FreeAboveThreshold`, `IsActive`, timestamps. `Create` rejects a negative or
absurd charge, a non-3-letter currency, negative days, a negative threshold and an incoherent
half-scope (a service area with no owning city).

**Coverage** (`ShippingCoverageGrant`): an explicit, reviewable row meaning "this service is allowed in
this destination subtree". Provinces/districts are never invented; a parent-level grant covers a subtree
by design, but the *existence* of the row is the decision.

**Bounded eligibility result** (`ShippingRatePolicyEngine`, pure and deterministic): a missing/unknown
coverage grant yields `NotCovered`, never unconditional serviceability. Outcomes are
`Quoted | NotCovered | UnknownService | ScopeConflict | CurrencyMismatch`, each with a stable
language-independent `ReasonCode`. Two equally specific competing policies are reported as
`ScopeConflict` instead of "first row wins". The deepest matching scope wins; code matching ignores case
and whitespace.

**Preserved legacy semantics.** The live route's historic meaning — an **empty** `AllowedProvinces` =
"unrestricted" — was *not* imported into the new authority and the route itself was not touched. A test
pins the difference (`Legacy_empty_allowed_provinces_semantics_are_not_reused_by_this_module`), because
copying that meaning into the new model would silently grant nationwide coverage.

## 3. Persistence — mapped, not wired, not migrated

`ShippingDbContext` maps an owned `shipping` schema with two tables (`rate_policies`, `coverage_grants`)
plus the conventional module outbox, unique indexes on `(service, scope)` and a lookup index on
`(service, country, is_active)`. `ShippingRateQuoteLookup` implements the single read-only Contracts seam
`IShippingRateQuoteLookup` and delegates every commercial decision to the same pure engine.

**Deliberately inert:**

- **No migration was generated and none was applied** — there is no `Migrations/` folder and no DDL ever
  reached a database.
- **`Tooba.Host` has no reference to this assembly** and `ToobaModuleComposition`'s explicit module list
  was not edited, so no running process loads it.
- `ShippingModule` registers **no** `IModuleSchemaMigrator`. Registering one would make the Development
  Host emit `CREATE SCHEMA/TABLE` DDL at startup (the hazard documented in
  `SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044`), and the canonical order source
  `ModuleSchemaMigrationOrder` is outside this task's allowed paths. The hook is prepared-only.
- **No endpoint, no DI integration into Host, no service switch, no checkout behaviour change, no
  read/write to any database.**

## 4. Validation

`log-build.txt` — `dotnet build` of the Shipping test project (Contracts + Domain + Infrastructure):
**Build succeeded, 0 Warnings, 0 Errors**.

`log-tests.txt` — focused run, Shipping module only (no broad suite):

```text
Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 25 ms
```

Covered: amount non-negativity / zero-charge allowed / absurd-charge rejection; currency shape; separate
negative preparation vs lead days; half-scope rejection in both aggregates; deactivation preserving
stable identity; fail-closed on missing **and** null coverage; inactive policy and inactive grant;
deepest-scope-wins; another-area and another-country non-coverage; equally-specific conflict reported not
picked; narrowed grant not covering a sibling area; free-above threshold on both sides; currency mismatch
reported not converted; preparation/lead returned separately; case/whitespace-insensitive code matching;
and the Fulfillment compatibility proofs.

## 5. Scope ledger + runtime non-actions

Implemented: Contracts (scope/outcome/quote/reason codes/error codes/one read port), Domain (two
aggregates + pure engine), Infrastructure (owned DbContext mapping, one read adapter, prepared-only
composition root), focused tests, solution entries, this report and evidence.

Deferred on purpose: EF migration generation/application; Host composition and DI; any endpoint or Admin
surface; Order/Checkout binding; carrier payables; international polygon coverage; full scheduling;
subsidy/ledger; service-area distribution data; the legacy `AllowedProvinces` bridge.

`log-runtime-non-action.txt` records: Host csproj has **no** Shipping reference, **0** generated
migration files, and the `Tooba.slnx` diff is **additive entries only** (one new `/Modules/Shipping/`
folder, four project lines). The five pre-existing dirty tracked files (mtimes 10/12–10/13, before this
task's claim at 13:29:12Z) were preserved and none of them was staged or edited by this task; the one path
`git diff` reports under the frontend admin tree is that pre-existing dirty file, not a Shipping change.

## 6. Risk and honest notes

- **The new authority is not yet the truth anyone reads.** Until a later milestone binds Order to
  `IShippingRateQuoteLookup`, checkout still prices from `Tooba:ShippingMethods`. Nothing in this task
  claims runtime pricing is DB-backed — it is not.
- **Two rate sources will coexist during transition** (configuration-derived today, catalog-backed later).
  That is intentional and must be reconciled by explicit tasks, not by a silent switch.
- **Coverage is fail-closed, so a future cutover with no coverage rows would deny everything.** The
  milestone-4/5 tasks must seed explicit coverage before any binding is enabled — the "empty means
  unrestricted" shortcut must stay dead.
- **Elapsed exceeded the 30-minute timebox** (claim 13:29:12Z → result ≈14:15Z, ~46 min). Reason: the
  acceptance gate is a *compiling* foundation with focused evidence, and producing the module plus a real
  build/test cycle cost ~10 minutes beyond the box. I chose to finish a coherent, verifiable slice rather
  than stop with an uncompiled tree; nothing beyond the authorized paths was touched. This is stated
  rather than hidden.
- No `MigrationRunner` run, no database access, no Host restart, no payment/fulfillment action.

## 7. Recommended next bounded task (one)

`TB-SHIPPING-CHECKOUT-RATE-BINDING-DESIGN-049` — design (without switching) the milestone-4/6 binding:
prove how `IShippingRateQuoteLookup` replaces the configuration-derived rate path for the storefront
shipping projection, define the explicit coverage-grant seeding required by fail-closed eligibility, and
specify the shadow-compare evidence that every destination resolvable today still quotes the same
amount — with the legacy route and its guard untouched until an authorized, reviewed switch.
