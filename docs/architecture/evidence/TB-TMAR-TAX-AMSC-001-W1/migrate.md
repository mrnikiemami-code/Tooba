# TB-TMAR-TAX-AMSC-001-W1 — Migrate (`tooba-architecture-migrate`)

```text
TASK:          TB-TMAR-TAX-AMSC-001-W1
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-migrate (V2)
TARGET:        src/backend/Modules/Tax/Tooba.Tax.*
PARENT:        TB-TMAR-TAX-AMSC-001-W0 (Analyze) @ 36d243cc
LOCK VERSION:  ARCH-COMPLETE-002
STATE:         MIGRATE_COMPLETE
VERDICT:       READY_TO_STRUCTURE
```

## Scope

`src/backend/Modules/Tax/Tooba.Tax.*` only. No foreign module was touched (Tax has **zero** inbound
illegal edges and zero outbound foreign internals; W0 recorded `Cross-Module-Coupling-State = NONE`).
W1 consumes the W0 verdict `READY_TO_MIGRATE` (commit `36d243cc`) and closes the three W0 certification
blockers that belong to the migrate wave. Tax has no prior ARCH-COMPLETE-002 certification and is not a
member of `structureLock.certifiedModules`, so there is no prior accepted baseline to preserve.

Behavior is preserved: every stable code value, the `tax` schema, the single migration
`20260823190000_InitialTax`, the outbox event names, the calculation semantics and the DI lifetimes are
byte-identical after the wave.

---

## Repair 1 — Single canonical stable-code home (Contracts boundary)

- **Created** `Tooba.Tax.Contracts/Errors/TaxErrorCodes.cs` with `namespace Tooba.Tax.Contracts.Errors`
  and the certified declared-code guard:
  - `private static readonly HashSet<string> KnownCodes` (ordinal) seeded from the declared constants;
  - `public static bool IsKnown(string? code)` — null/whitespace false, exact ordinal membership only;
  - **11** `public const string` members keeping their exact wire values (`tax.rule.id_required`,
    `tax.jurisdiction.required`, `tax.market.required`, `tax.validity.inverted`, `tax.rate.out_of_range`,
    `tax.rate.not_applicable`, `tax.rate.kind_mismatch`, `tax.category.id_required`,
    `tax.category.code_required`, `tax.category.missing`, `tax.outbox.unmapped_event_type`).
- The twelve pre-existing machine-shaped literals are **not renamed** — they are the machine identity and
  were already correct; W1 only moves them from inline string literals into the single declared home.
- The one English user-facing prose fault (`TaxOutboxRegistration.cs:76`,
  `"Unmapped Tax integration event type."`) becomes the new stable code `tax.outbox.unmapped_event_type`,
  mirroring the certified `InventoryErrorCodes.OutboxUnmappedEventType`.
- `Tooba.Tax.Domain.csproj` now references its **own** `Tooba.Tax.Contracts` so the Domain consumes the
  single canonical home. This is own-module layering (the accepted `Domain -> own Contracts/Errors`
  precedent, identical to certified Pricing), never a foreign `Contracts` reference; the Tax architecture
  guard was tightened so the only permitted `Contracts` reference on the Domain is `Tooba.Tax.Contracts`.
- `Tooba.Tax.Infrastructure.csproj` gained the explicit `Tooba.Tax.Contracts` reference (it previously
  reached Contracts transitively through Application).

## Repair 2 — Canonical typed-fault seam

- **Created** `Tooba.Tax.Application/Composition/TaxOperation.cs` mirroring the certified
  Inventory/Pricing/Media/Party/Payment `*Operation` shape exactly:
  - `ExecuteAsync<T>(Func<Task<T>>)` and value-less `ExecuteAsync(Func<Task>)`;
  - `ArgumentNullException.ThrowIfNull`;
  - `catch (ContractOperationException ex) when (TaxErrorCodes.IsKnown(ex.Code))` →
    `Result.Failure<T>(new SemanticError(ex.Code))`;
  - `catch (SemanticException ex)` → `Result.Failure<T>(ex.Error)`;
  - a contract fault whose code is not a declared Tax code, and every unknown exception, propagates
    untouched to the canonical global exception boundary.
- Classification is by typed code only — never by message/prose heuristics. The seam returns
  `Result`/`Result<T>` (never `IResult`), so the boundary contract is unchanged.
- Tax owns **zero** endpoint-reachable requests (verified by W0), so the seam is deliberately a dormant
  boundary; no CQRS/MediatR/validator ceremony was invented for an `INTERNAL_ONLY` module.

## Repair 3 — Domain/Infrastructure fault typing (13 raw faults retired)

| File | Before | After |
| --- | --- | --- |
| `Domain/Aggregates/TaxRule.cs` (8 sites) | `throw new InvalidOperationException("tax.*")` | `throw new SemanticException(new SemanticError(TaxErrorCodes.*))` |
| `Domain/Aggregates/TaxCategory.cs` (2 sites) | `throw new InvalidOperationException("tax.category.*")` | `throw new SemanticException(new SemanticError(TaxErrorCodes.*))` |
| `Infrastructure/Adapters/TaxDirectory.cs` (2 sites) | `throw new InvalidOperationException("tax.category.missing")` | `throw new ContractOperationException(TaxErrorCodes.CategoryMissing)` |
| `Infrastructure/Outbox/TaxOutboxRegistration.cs` (1 site) | `throw new InvalidOperationException("Unmapped Tax integration event type.")` | `throw new ContractOperationException(TaxErrorCodes.OutboxUnmappedEventType)` |

Zero raw `InvalidOperationException("<literal>")` remains anywhere in the Tax production surface.
`TaxRuleInvariantTests.Create_rejects_percentage_rate_above_one` was repointed from
`Assert.Throws<InvalidOperationException>` to `Assert.Throws<SemanticException>` **plus** an explicit
`Assert.Equal(TaxErrorCodes.RateOutOfRange, error.Error.Code)` — the intent is preserved and strengthened
(no test was weakened).

## Repair 4 — Localization surface (catalog + bilingual resources)

- `Tooba.Tax.Contracts/Errors/TaxErrorCatalogContributor.cs` — **11** `ErrorDescriptor`s
  (`LocalizationKey = code`, explicit HTTP status, `ErrorSeverity.Warning`, safe English fallback):
  - Validation `400` — `RuleIdRequired`, `JurisdictionRequired`, `MarketRequired`, `ValidityInverted`,
    `RateOutOfRange`, `RateNotApplicable`, `CategoryIdRequired`, `CategoryCodeRequired`;
  - Conflict `409` — `RateKindMismatch`;
  - NotFound `404` — `CategoryMissing`;
  - Platform `500` — `OutboxUnmappedEventType`.
- `Tooba.Tax.Contracts/Errors/TaxErrorResourceSet.cs` — `TaxErrorResources` `ResourceManager` marker plus
  `TaxErrorResourceSet : IErrorResourceSet` owning the `tax.` keyspace.
- `Tooba.Tax.Contracts/Resources/TaxErrors.resx` and `TaxErrors.fa.resx` — **11 EN + 11 FA** keys with
  distinct, non-empty values, embedded with explicit logical names
  (`Tooba.Tax.Contracts.Resources.TaxErrors[.fa].resources`).
- `checkout.tax.unavailable` is **deliberately absent**: it is Order-owned
  (`StorefrontOrderErrors.CheckoutTaxUnavailable`, raised by `Order.Infrastructure.Checkout.Persistence`
  `CheckoutDirectory.Reservations.cs`). Duplicate *usage* is allowed; duplicate *descriptor ownership* is
  not. Tax does not claim it and does not re-register it.
- Registration happens **exactly once**, in the Infrastructure composition root
  (`TaxModule.AddServices`), matching the certified `INTERNAL_ONLY` Inventory/Pricing precedent
  (`services.AddSingleton<IErrorCatalogContributor, TaxErrorCatalogContributor>()` +
  `services.AddSingleton<IErrorResourceSet, TaxErrorResourceSet>()`). No Endpoints project is involved.

## Repair 5 — Stale guard/assertion correction (behavior-neutral)

- `Tooba.Host.Tests/TaxFoundationTests.cs:74` read the deleted
  `Modules/Tax/Tooba.Tax.Domain/TaxDomain.cs` (split into cohesive aggregate files in commit `edecccce`)
  and threw `FileNotFoundException` at the W0 baseline. The assertion now reads the real
  `Tooba.Tax.Domain/Aggregates/*.cs` files while preserving its **exact intent** (no `0.09` rate and no
  `1405` date hard-coded in the Tax Domain).
- `Tooba.Tax.Tests/Architecture/TaxArchitectureGuardTests.Tax_golden_boundaries_remain_clean` previously
  asserted that the Domain has **no** `Tooba.Tax.Contracts` reference. The canonical own-module
  `Domain -> own Contracts/Errors` edge (the Pricing precedent) is now required, so the guard was
  corrected to permit **only** `Tooba.Tax.Contracts` while still forbidding any foreign module
  `Contracts` reference on the Domain. Nothing was weakened.

## Repair 6 — No further repair required

Logging, sensitive logging, telemetry, correlation, persistence, outbox and schema were verified
canonical by W0 and are untouched by W1: zero `ILogger<T>`/`Console.WriteLine`/`Debug.WriteLine`,
zero second `ActivitySource`/`Meter`, zero raw `StartActivity(`, zero manual `traceparent` parsing, zero
competing correlation id, zero cross-module join, zero foreign `DbContext`/`DbSet`. No migration,
snapshot or `TaxDbContext` file was touched. Tax issues no cross-module call (it is the *callee*), so no
`IModuleCallTracer` decoration is required.

---

## Verification

- `dotnet build src/backend/Modules/Tax/Tooba.Tax.Tests` — succeeded (0 warnings, 0 errors).
- `Tooba.Tax.Tests` — **12 passed / 0 failed** (the W0 baseline had 1 stale-guard failure: the
  `Tax_golden_boundaries_remain_clean` Domain-Contracts assertion; now corrected and green).
- New durable guard `TaxModuleAmsc001W1MigrateGuardTests`
  (`src/backend/Host/Tooba.Host.Tests/Architecture/`) — **8 passed / 0 failed**, pinning the single
  canonical Contracts home + `IsKnown` declared count/set equality, the `TaxOperation` shape, the
  exactly-once composition-root registration, the 11-descriptor catalog and 11+11 bilingual keys, the
  no-re-inlined-literal rule, the no-message-text-classification rule, the Contracts-only boundary and
  the zero-raw-fault rule.
- Focused `Tooba.Host.Tests` (`TaxModuleAmsc001W1MigrateGuardTests | TaxFoundationTests |
  ErrorCatalogUniqueCodeGuardTests | CheckoutOrderFoundationTests`) — **14 passed / 2 skipped / 0 failed**.
- Wider focused `Tooba.Host.Tests` (`FullyQualifiedName~Tax`) — **24 passed / 5 skipped / 1 failed**; the
  single failure is the pre-existing, unrelated
  `HostAdminAmcW30PwTaxonomyGuardTests.Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT`
  (expects 15 `Host/Admin` files, repository has 17; unchanged by W1, identical at the W0 baseline).
- Also observed in the wider focused run and **pre-existing / out of scope** (not caused by W1, not
  repaired by W1): `HostDevelopmentEnricherClosureGuardTests.New_development_gateways_live_in_owning_module_contracts`
  (reads `Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` while the file has lived at
  `Party/Tooba.Party.Contracts/Ports/…` since commit `00c0b9bf`).
- No migration, snapshot, route, descriptor code value, resource key or DI lifetime changed.

---

## State

```text
Foundation-State            : FOUNDATION_PARTIAL (unchanged; Endpoints retirement is W2)
Stable-Error-Code-State     : UNREGISTERED_CODES_13
                              -> CONTRACTS_OWNED_11_DECLARED_11_REGISTERED_SINGLE_HOME_ISKNOWN_GUARDED
Localization-State          : MISSING_INFRASTRUCTURE_USE
                              -> CANONICAL (11 descriptors + bilingual pair + single IErrorResourceSet)
Typed-Fault-State           : MISSING -> CANONICAL (Application/Composition/TaxOperation.cs)
Raw-Fault-State             : 13_RAW_FAULTS -> ZERO
Contracts-Boundary-State    : VIOLATION_NAMESPACE_PATH_DRIFT_ONLY (semantics now clean; the path<->namespace
                              repair remains a W2 Structure obligation and is NOT claimed here)
Cross-Module-Coupling-State : NONE (unchanged, correct)
Cross-Module-Join-State     : NONE (unchanged, correct)
API-Result-Pattern-State    : CANONICAL (unchanged)
Logging-State               : CANONICAL_ZERO_CALL_SITES (unchanged)
Sensitive-Logging-State     : NONE (unchanged)
OpenTelemetry-State         : CANONICAL (unchanged)
Correlation-Trace-State     : CANONICAL (unchanged)
CQRS-State                  : NOT_APPLICABLE_INTERNAL_ONLY (unchanged, correct - no ceremony invented)
Validator-Coverage-State    : EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED (unchanged, correct)
Persistence-Ownership-State : CORRECT_OWN_TAX_SCHEMA_OWN_OUTBOX (unchanged)
Endpoint-Ownership-State    : MODULE_OWNED_ZERO_ROUTES_HOST_ZERO (ceremonial Endpoints project retires in W2)
Host-Residue-State          : ALLOWED_COMPOSITION_ROOT (unchanged; zero Host production files touched)
Schema-Migration-State      : UNCHANGED (0 migration files touched)
Behavior-Preservation       : PRESERVED
microserviceExtractable     : TARGET_TRUE_ALREADY_LEGAL_CONTRACTS_ONLY
                              -> TRUE_CONTRACTS_ONLY_AND_SELF_CONTAINED_ERROR_SURFACE_PATH_NAMESPACE_PENDING_W2
Structure-Handoff-State     : REQUIRED
Final-Disposition           : READY_TO_STRUCTURE
```

Production code changed; schema unchanged; guards strengthened; zero guards weakened; zero unrelated
files touched.
