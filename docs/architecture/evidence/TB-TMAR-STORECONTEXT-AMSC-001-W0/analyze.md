# TB-TMAR-STORECONTEXT-AMSC-001 — Wave 0 (Analyze)

- **Skill:** `tooba-architecture-analyze` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/StoreContext/Tooba.StoreContext.*`
- **Starting HEAD:** `e7131dc05a9fe5a310972cee9064cfffbe8c3630`
- **Branch:** `main` (`HEAD == origin/main`)
- **Production code changed in this wave:** NONE (analysis only)
- **Host touched in this wave:** NONE

---

## 1. Target analyzed

| Project | Role | Production `.cs` |
| --- | --- | --- |
| `Tooba.StoreContext.Contracts` | module-boundary contracts (record + three seams) | 1 (`Current/StoreCommerceContext.cs`, 55 LOC) |
| `Tooba.StoreContext.Infrastructure` | module composition + scoped accessor | 2 (`StoreContextModule.cs` 27 LOC, `Current/StoreCommerceContextAccessor.cs` 19 LOC) |

There is no `Domain`, `Application`, `Endpoints` or `Tests` project — by design (see §4/§8).

`Tooba.StoreContext.Infrastructure` is the **only** StoreContext project referenced by Host
(`src/backend/Host/Tooba.Host/Tooba.Host.csproj:36`) and is loaded through
`Tooba.Host.Composition.ToobaModuleComposition.Modules` (`new StoreContextModule()`, line 57).
`Tooba.StoreContext.Contracts` is referenced by `Tooba.Host`, `Tooba.Host.Tests` (transitively via
Host) and `Tooba.Cart.Infrastructure`.

---

## 2. Structured State Fields

| Field | Value |
| --- | --- |
| **Foundation-State** | `FOUNDATION_READY` (module already `structureCertified: true` under ARCH-COMPLETE-002) |
| **Ownership-State** | `correct` (every responsibility belongs to StoreContext; Host owns no StoreContext business authority) |
| **File-Cohesion-State** | `COHESIVE` (3 files, 55/27/19 LOC, one responsibility each) |
| **Oversized/God-File-State** | NONE (max 55 LOC; no baseline entry) |
| **Localization-State** | `CANONICAL` — no user-facing text in the module; failures are typed stable codes owned by consumers |
| **API-Result-Pattern-State** | `CANONICAL` — no HTTP surface; no `Results.*`, no local mapper |
| **Stable-Error-Code-State** | `NOT_APPLICABLE_NO_OWN_CODES` — StoreContext declares/emits zero error codes; consumer modules own `cart.commerce.*` etc. |
| **Logging-State** | `CANONICAL` — zero logging calls in the module (nothing to log) |
| **Sensitive-Logging-State** | `NONE` |
| **OpenTelemetry-State** | `CANONICAL` — no second pipeline, no direct `ActivitySource.StartActivity` |
| **Correlation-Trace-State** | `CANONICAL` — no parallel correlation; Host/workers assign through the canonical seams |
| **CQRS-State** | `NOT_APPLICABLE_NO_APPLICATION_USE_CASE` (accepted lock, see §8) |
| **Validator-Coverage-State** | `NOT_APPLICABLE_INTERNAL_ONLY` (zero endpoint-reachable requests) |
| **Contracts-Boundary-State** | `CLEAN` (boundary semantics only; no Application-internal type leaked) |
| **Cross-Module-Coupling-State** | `LEGAL_CONTRACTS_ONLY` (inbound consumers only; StoreContext itself references no foreign module) |
| **Cross-Module-Join-State** | `NONE` (no persistence at all) |
| **Persistence-Ownership-State** | `NOT_APPLICABLE_NO_PERSISTENCE` |
| **Endpoint-Ownership-State** | `NOT_APPLICABLE` (no HTTP surface; `INTERNAL_ONLY`) |
| **Host-Residue-State** | `ALLOWED_COMPOSITION_ROOT_ONLY` (no `Host/StoreContext` folder exists) |
| **Schema-Migration-State** | `UNCHANGED` (0 migrations) |
| **Behavior-Preservation-Risk** | `LOW` |
| **Canonical-Reference-Used** | `BuildingBlocks` (`SemanticException`/`SemanticError` consumed by Host+Cart, not by the module) + Inventory/CustomerProfile/AddressBook for the `DependencyInjection/` composition-folder precedent + the Persian code-documentation standard (`docs/architecture/32-persian-code-documentation-standard.md`) |
| **Final-Disposition** | `READY_TO_MIGRATE` |

Structural handoff fields (owned by the Structure skill, recorded here for handoff):

| Field | Value |
| --- | --- |
| **Folder-Granularity-State** | `PROFESSIONAL_SHALLOW` |
| **Solution-Explorer-State** | `CANONICAL` (`/Modules/StoreContext/`, 2 projects, `.slnx:179-182`) |
| **Path-Namespace-State** | `EXACT` (0 mismatches over 3 files) |
| **Physical-Copy-State** | `CLEAN` |
| **Root-Allowlist-State** | `ENFORCED` (Contracts root `.cs` = 0; Infrastructure root `.cs` = `StoreContextModule.cs` only) |
| **Single-File-Request-Leaf-State** | `ZERO` (no request folders exist) |
| **Technical-Axis-First-State** | `ZERO` |
| **Structure-Handoff-State** | `REQUIRED` (W2 must decide the Infrastructure composition-folder placement and re-verify allowlists) |

---

## 3. Responsibility map

| Responsibility | Current location | Classification |
| --- | --- | --- |
| Effective storefront commerce context value (`Market`, `DefaultCurrency`, `SalesChannel`) | `Contracts/Current/StoreCommerceContext.cs` | `CONTRACT` |
| Read seam `ICurrentStoreCommerceContext` | `Contracts/Current/StoreCommerceContext.cs` | `CONTRACT` |
| Assignment seam `IStoreCommerceContextAssigner` | `Contracts/Current/StoreCommerceContext.cs` | `CONTRACT` |
| Worker rebuild seam `IWorkerStoreCommerceContextFactory` | `Contracts/Current/StoreCommerceContext.cs` | `CONTRACT` |
| Scoped accessor (read + assign, one DI scope, no `HttpContext`, no `AsyncLocal`, no static state) | `Infrastructure/Current/StoreCommerceContextAccessor.cs` | `PERSISTENCE`-adjacent runtime seam → classified `HOST_COMPOSITION_ROOT` support (module-owned runtime accessor) |
| Module composition / DI registration of the three seams | `Infrastructure/StoreContextModule.cs` | `HOST_COMPOSITION_ROOT` (module-owned composition entry) |

No `MUST_SPLIT` file: each file holds exactly one responsibility and there is no mixed audience.

---

## 4. Ownership map

All responsibilities map to **StoreContext**. Nothing in the module belongs to Host, Cart, Order or
Payment. Verified consumers of the Contracts seams (all legal, all Contracts-only):

| Consumer | Seam consumed | Purpose |
| --- | --- | --- |
| `Host/MultiTenancy/TenantResolutionMiddleware.cs` | `IStoreCommerceContextAssigner` | assigns the effective context on the request path |
| `Host/Outbox/OutboxDispatcher.cs` | `IWorkerStoreCommerceContextFactory` | assigns the effective context per outbox message target |
| `Host/Outbox/WorkerStoreCommerceContextFactory.cs` | `IWorkerStoreCommerceContextFactory` | thin platform adapter over `ControlPlaneRegistry` |
| `Host/Configuration/{ControlPlaneRegistry,TenantRecord,TenantRecordOptions,StoreCommerceOptions,PlatformOptionsValidator}.cs` | `StoreCommerceContext` | control-plane configuration projection + startup fail-fast |
| `Host/Program.cs` | `IWorkerStoreCommerceContextFactory` | composition registration only |
| `Cart/Infrastructure/Lifetime/CartCommerceContextResolver.cs` | `ICurrentStoreCommerceContext` | Cart reads the platform context and fails closed |
| `Cart/Infrastructure/Lifetime/CartExpiryWorker.cs` | `IWorkerStoreCommerceContextFactory` | assigns the context inside the Cart worker scope |

Host references are `ALLOWED_COMPOSITION_ROOT` / `ALLOWED_CONTRACT_CONSUMPTION` only. There is no
`src/backend/Host/Tooba.Host/StoreContext` folder and no `Tooba.Host.StoreContext` namespace.

---

## 5. Current illegal dependencies

**None.** Verified absence of:

- `Tooba.StoreContext.* → foreign .Application / .Infrastructure / .Domain`;
- foreign `DbContext` / `DbSet` reach-through (the module has no persistence);
- cross-module EF/SQL join;
- `TypeForwardedTo`;
- namespace alias hiding placement (no `using X = Y;` in the module);
- StoreContext → Host dependency;
- Host-owned StoreContext business policy.

Project edges (complete inventory):

| From | To | Kind |
| --- | --- | --- |
| `Tooba.StoreContext.Contracts` | `Tooba.BuildingBlocks` | foundation (`ToobaEdition`) |
| `Tooba.StoreContext.Infrastructure` | `Tooba.StoreContext.Contracts` | own module |
| `Tooba.StoreContext.Infrastructure` | `Tooba.ModuleContracts` | foundation (`IToobaModule`) |

`Contracts-Boundary-State = CLEAN` and `Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`.

---

## 6. Cross-module join inventory

`NONE`. The module owns no `DbContext`, no `DbSet`, no schema, no migration and no SQL.

---

## 7. MUST_SPLIT decisions

`NONE`.

- `StoreCommerceContext.cs` declares one record plus three sibling boundary interfaces of the same
  capability (`Current`); that is one cohesive boundary file, not a mixed `*Contracts.cs` dump.
- `StoreContextModule.cs` is the single composition entry (root allowlist).
- `StoreCommerceContextAccessor.cs` implements the read + assignment halves of the same scope-lifetime
  state; splitting them would create two files sharing one field.

---

## 8. CQRS / MediatR gaps

`NONE_APPLICABLE` (accepted lock).

`docs/architecture/tmar-current-state.json → storeContext` records
`httpApplicability: INTERNAL_ONLY`, `endpointOwnership: NOT_APPLICABLE`,
`cqrs: NOT_APPLICABLE_NO_APPLICATION_USE_CASE`, `mediatRState: NOT_APPLICABLE_NO_USE_CASE`,
`endpointsState: NOT_APPLICABLE_INTERNAL_ONLY`. `HostCartResidualGuardTests` additionally asserts that
no `Tooba.StoreContext.Application` / `Tooba.StoreContext.Endpoints` project exists and that no
StoreContext source mentions `MediatR`.

Adding CQRS/Endpoints ceremony here would be a regression against the applicability gate. StoreContext
is a **platform context provider**, not a use-case owner.

---

## 9. Validation classification matrix

| Request | Classification | Reason |
| --- | --- | --- |
| — | — | StoreContext exposes no endpoint-reachable request |

`Validator-Coverage-State = NOT_APPLICABLE_INTERNAL_ONLY` (0 required / 0 no-validator-required).
The set-equality gate (§6a of the certify skill) is satisfied **vacuously**: the derived route set is
empty, the derived `ISender.Send` set is empty, and the classified set is empty — exact equality holds.
A durable guard must keep this true (no StoreContext route, no StoreContext `IRequest`/`ISender`).

---

## 10. Localization findings

- Zero Persian/English user-facing strings in the module.
- Zero error codes declared or emitted by the module.
- Zero `exception.Message` parsing.
- Failures around the context are raised **by consumers** with their own stable codes, e.g.
  `CartErrorCodes.CommerceContextUnavailable` / `CommerceMarketUnconfigured` /
  `CommerceCurrencyUnconfigured` / `CommerceChannelUnconfigured` in
  `Cart/Infrastructure/Lifetime/CartCommerceContextResolver.cs`, and
  `FoundationErrorCodes.PlatformEditionUnconfigured` in `TenantResolutionMiddleware`.
  StoreContext must **not** claim those descriptors — ownership stays with the natural module.

---

## 11. API result / error mapping findings

- `CANONICAL` by construction: no endpoint, no `Results.Json` / `Results.BadRequest` /
  `Results.Problem`, no local `ProblemDetails` builder, no `catch`-and-map, no `ex.Message`
  classification.
- StoreContext defines no `ErrorDescriptor`, no `IErrorCatalogContributor` and no `IErrorResourceSet`.
  This is correct: it owns no error code and must not register descriptors for codes that belong to
  `Foundation` or `Cart`.

---

## 12. Logging / sensitive-data findings

- `Logging-State = CANONICAL`: no `Console.WriteLine`, no `Debug.WriteLine`, no custom logger, no
  second telemetry pipeline.
- `Sensitive-Logging-State = NONE`: no token, secret, `Authorization` header, cookie, connection
  reference or payment payload is logged or passed to telemetry anywhere in the module.

---

## 13. OpenTelemetry / correlation findings

- `OpenTelemetry-State = CANONICAL`; `Correlation-Trace-State = CANONICAL`.
- No direct `ActivitySource.StartActivity`, no manual `traceparent` parsing, no second correlation
  header, no `AsyncLocal` correlation, no `Guid.NewGuid()`-as-correlation inside the module.
- The scoped accessor deliberately avoids `AsyncLocal` and `HttpContext`: lifecycle is exactly one DI
  scope (request or worker cycle), which preserves trace/correlation continuity because assignment
  happens inside the canonical request/worker path.

---

## 14. File cohesion / splitting plan

| File | LOC | Verdict | Action |
| --- | --- | --- | --- |
| `Contracts/Current/StoreCommerceContext.cs` | 55 | `COHESIVE` | keep |
| `Infrastructure/StoreContextModule.cs` | 27 | `COHESIVE` | keep (root allowlist) |
| `Infrastructure/Current/StoreCommerceContextAccessor.cs` | 19 | `COHESIVE` | keep |

No file exceeds any size ceiling and `Baselines/tmar-source-size-baseline.json` has no StoreContext
entry, so no baseline change is required.

---

## 15. Exact target paths / namespaces

Existing folders are already capability-first and shallow:

```text
Tooba.StoreContext.Contracts/
    Current/StoreCommerceContext.cs        namespace Tooba.StoreContext.Contracts.Current
Tooba.StoreContext.Infrastructure/
    Current/StoreCommerceContextAccessor.cs namespace Tooba.StoreContext.Infrastructure.Current
    StoreContextModule.cs                   namespace Tooba.StoreContext.Infrastructure
```

The only structural question handed to W2 is the placement of the module composition entry. The
repository standard states: *"Root is the module composition entry only"*, and the newest certified
modules (Offer, Inventory, Pricing, Tax, Payment, Fulfillment, Promotion, Returns, Settlement,
Notification, Support, AddressBook, CustomerProfile) place `*Module.cs` under
`Infrastructure/DependencyInjection/`. W2 must decide between:

- **(a)** keep `StoreContextModule.cs` at the Infrastructure root (explicitly permitted by the standard
  and pinned by two facts in `HostCartResidualGuardTests`), or
- **(b)** align with the newest certified precedent and move it to `Infrastructure/DependencyInjection/`
  (then update the allowlist, both `HostCartResidualGuardTests` facts and the manifest).

W0 records this as a Structure-owned decision and does **not** pick it here.

W1 adds no new production files; the only W1 change is documentation quality inside the existing three
files (see §17).

---

## 16. Behavior-preservation checklist

Must remain exactly equivalent:

- **Public API:** every public type, member name, signature, parameter name and namespace of
  `Tooba.StoreContext.Contracts.Current` and `Tooba.StoreContext.Infrastructure` unchanged;
  `StoreCommerceContext(string? Market, string? DefaultCurrency, string? SalesChannel)` positional
  shape and `record` equality semantics unchanged.
- **DI:** `StoreCommerceContextAccessor` stays scoped; `ICurrentStoreCommerceContext` and
  `IStoreCommerceContextAssigner` keep resolving to the same scoped instance; no lifetime change; no
  new/removed registration; `StoreContextModule.Name == "StoreContext"` and its position as the first
  module in `ToobaModuleComposition.Modules` unchanged.
- **Assignment semantics:** last `Assign` wins within the scope; `Assign(null)` still throws
  `ArgumentNullException`; `Current` is `null` until assigned (fail-closed consumers unchanged).
- **Routes:** none.
- **Schema/migrations:** none.
- **Config keys:** `StoreCommerce:DefaultCurrency` and
  `SingleStore:Tenants:<n>:StoreCommerce:DefaultCurrency` unchanged (Host-owned, not touched).
- **Localization keys:** none owned.
- **Telemetry event names / correlation:** unchanged (no telemetry emitted here).
- **Dependency edges:** unchanged (Contracts → BuildingBlocks; Infrastructure → Contracts +
  ModuleContracts).

---

## 17. W1 migration order (the only substantive work available in a behavior-preserving migrate wave)

StoreContext has **no ownership, coupling, cohesion, CQRS, validation, localization, API-result,
logging, telemetry or schema debt**. The one real, non-cosmetic finding is a documentation-standard
gap:

`docs/architecture/32-persian-code-documentation-standard.md` (Architect-accepted, `COMPLETE`)
requires strong professional Persian documentation on Tooba-owned public members, and its review
checklist says *"Public C# types/members have strong Persian XML, not name-echo."* The three
StoreContext production files currently ship **English** XML summaries and English `<param>` docs:

| File | Members with English XML |
| --- | --- |
| `Contracts/Current/StoreCommerceContext.cs` | `StoreCommerceContext` (+3 `<param>`), `ICurrentStoreCommerceContext`, `.Current`, `IStoreCommerceContextAssigner`, `.Assign` (+`<param>`), `IWorkerStoreCommerceContextFactory`, `.FromTarget` (+2 `<param>`) |
| `Infrastructure/StoreContextModule.cs` | `StoreContextModule`, `Name`, `AddServices` |
| `Infrastructure/Current/StoreCommerceContextAccessor.cs` | `StoreCommerceContextAccessor`, `Current`, `Assign` |

W1 therefore performs a **documentation-only, behavior-preserving** migrate:

1. rewrite every XML summary/`<param>`/`<returns>`/`<inheritdoc />` context in the three files in
   professional Persian, following the module's existing Persian precedent
   (`Host/Outbox/WorkerStoreCommerceContextFactory.cs`, `Host/MultiTenancy/TenantResolutionMiddleware.cs`,
   `Host/Configuration/ControlPlaneRegistry.cs`, `Cart/Infrastructure/Lifetime/CartCommerceContextResolver.cs`);
2. preserve every invariant sentence's meaning — in particular the two architectural invariants that
   must stay explicit: **`DefaultCurrency` is a default-selection input only, never a transaction/
   line/order/settlement/payment-group currency and never a single-currency Cart/Order invariant**, and
   **`null` means unresolved and consumers must fail closed rather than inventing
   Market/DefaultCurrency/SalesChannel**;
3. keep technical identifiers in English inside Persian prose (`StoreCommerceContext`,
   `DefaultCurrency`, `SalesChannel`, `HttpContext`, `AsyncLocal`, `DI scope`, `Marketplace`);
4. change **no** code, signature, attribute, registration, namespace or using directive;
5. keep CS1591 green (`GenerateDocumentationFile` + `WarningsAsErrors=CS1591` on non-test projects) and
   keep `HostCartResidualGuardTests.StoreContext_is_golden_certified_with_default_currency_semantics`
   green — it asserts the literal `string? DefaultCurrency` token and the absence of `string? Currency`,
   `AllowedCurrencies`, `SettlementCurrency`, `PaymentCurrency`.

No other W1 change is authorized; any ownership/coupling "fix" would be a fabricated defect.

---

## 18. Verification plan

| Wave | Focused validation |
| --- | --- |
| W0 | none (analysis only); no production change; `git status` free of production modifications |
| W1 | `dotnet build src/backend/Tooba.slnx` (0 errors; CS1591 stays enforced); `HostCartResidualGuardTests` (both StoreContext facts); `CartModuleAmsc001W3CertGuardTests` + `FulfillmentModuleAmsc001W3CertGuardTests` (their foreign-layer lists contain `Tooba.StoreContext.*`); `HostConfigurationAmcCertGuardTests`; `ErrorCatalogUniqueCodeGuardTests` |
| W2 | new `StoreContextModuleAmsc001W2StructureGuardTests`; `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`; `.slnx` `/Modules/StoreContext/` grouping check |
| W3 | new `StoreContextModuleAmsc001W3CertGuardTests`; `TmarCompleteReferenceStructureGateTests`; `HostCartResidualGuardTests`; `ErrorCatalogUniqueCodeGuardTests` |

Bounded rule: one deterministic repair per failing focused check, then stop and report.

---

## 19. Certification blockers

Blockers that **must** be closed before StoreContext can claim a current `TB-TMAR-STORECONTEXT-AMSC-001`
certification:

1. **Documentation-standard gap** — every public StoreContext member carries English XML while
   `32-persian-code-documentation-standard.md` (Architect-accepted) requires professional Persian
   documentation. W1 closes this.
2. **Structure justifications absent from the manifest** — the StoreContext manifest entry has empty
   `rootAllowlistJustification` on both projects and carries **no `certificationNote`** recording the
   module's AMSC lineage / `INTERNAL_ONLY` rationale, unlike every other certified module. W2/W3 close
   this.
3. **Composition-entry placement undecided** — the `DependencyInjection/` precedent question in §15 is
   still open; W2 must decide and, if it moves the file, update the allowlist, the two
   `HostCartResidualGuardTests` facts and the manifest atomically.
4. **No AMSC SoT record / Master Recovery checkpoint / AMSC evidence tree** — StoreContext currently has
   only the historical `storeContext` SoT block and the golden evidence under `docs/evidence/`.
   W0→W3 add the AMSC records.
5. **No StoreContext-scoped durable AMSC guard** — the existing protections live in shared Host guard
   files (`HostCartResidualGuardTests`, `HostConfigurationAmc*`). W2/W3 add module-scoped guards.

Non-blocking observations (recorded, no action required):

- `HostCartResidualGuardTests` pins the Infrastructure root file set twice (lines 265 and 338) plus the
  exact namespace strings (lines 343/346). Any structural move in W2 must update all four sites in one
  change.
- `HostDevelopmentMigrationSeamGuardTests` lists 28 module composition roots and asserts exactly 28
  `AddModuleSchemaMigrator` registrations. StoreContext has no persistence and correctly appears in
  neither the list nor the count — it must stay out of that guard.
- The historical `storeContext` SoT block references `docs/evidence/TB-TMAR-STORECONTEXT-GOLDEN-001/`
  (a different evidence root from `docs/architecture/evidence/`). That historical record is preserved
  unrewritten; the AMSC lineage is recorded additively.

---

## 20. Disposition

`READY_TO_MIGRATE`

`Structure-Handoff-State = REQUIRED` (W1 documents; W2 owns the composition-folder decision and the
final structural verdict).
