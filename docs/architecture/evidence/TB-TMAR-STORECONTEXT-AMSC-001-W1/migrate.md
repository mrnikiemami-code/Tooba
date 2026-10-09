# TB-TMAR-STORECONTEXT-AMSC-001 — Wave 1 (Migrate)

- **Skill:** `tooba-architecture-migrate` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/StoreContext/Tooba.StoreContext.*`
- **Starting HEAD:** `c73545f547d970745cbcb0e5c9fc634aff3c96ae` (W0)
- **Branch:** `main` (`HEAD == origin/main`)
- **Ownership moves:** NONE (no responsibility was misplaced)
- **Behavior change:** NONE (documentation-only migration)

---

## 1. Source target

Three production files:

| File | LOC | Role |
| --- | --- | --- |
| `Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs` | 55 | boundary record + 3 seams |
| `Tooba.StoreContext.Infrastructure/StoreContextModule.cs` | 27 | module composition entry (root allowlist) |
| `Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs` | 19 | scoped read+assign accessor |

## 2. Destination module

**StoreContext only.** No file changed project, namespace, folder or module.

## 3. Foundation readiness

`FOUNDATION_READY` — `structureCertified: true` under ARCH-COMPLETE-002 in
`docs/architecture/tmar-module-structure-manifests.json`; `/Modules/StoreContext/` solution grouping
present (`src/backend/Tooba.slnx:179-182`). No foundation was created or extended.

## 4. Foundation created/extended

NONE. Creating `Application`/`Domain`/`Endpoints` here would violate the applicability gate
(`INTERNAL_ONLY`, `cqrs: NOT_APPLICABLE_NO_APPLICATION_USE_CASE`) and is explicitly forbidden by the
accepted `storeContext` lock in `tmar-current-state.json`.

## 5. Responsibility split

NONE. W0 §7 established `MUST_SPLIT = NONE`; each file already holds exactly one responsibility.

## 6. Ownership map

Every responsibility remains StoreContext-owned: the boundary record and the three seams (`CONTRACT`),
the scoped accessor (module-owned runtime seam), the composition entry (`HOST_COMPOSITION_ROOT`,
module-owned). Host and Cart remain consumers only.

## 7. Files moved / created / deleted

| Action | File |
| --- | --- |
| modified (XML docs only) | `Contracts/Current/StoreCommerceContext.cs` |
| modified (XML docs only) | `Infrastructure/StoreContextModule.cs` |
| modified (XML docs only) | `Infrastructure/Current/StoreCommerceContextAccessor.cs` |
| created | `docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W1/migrate.md` |
| created | `docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W1/patch-sot.js` |

Nothing moved, nothing deleted.

## 8. Old → new path map

`UNCHANGED` for all three production files.

## 9. Namespace changes

`UNCHANGED`:

```text
Tooba.StoreContext.Contracts.Current
Tooba.StoreContext.Infrastructure
Tooba.StoreContext.Infrastructure.Current
```

## 10. Contracts reused / created

Reused only. No new Contracts type, no changed member, no changed signature. The Contracts boundary was
already `CLEAN` and semantically correct: `StoreCommerceContext` and the three seams are genuine
module-boundary semantics (consumed by Host and Cart), so they correctly live in `*.Contracts` rather
than a would-be `Application`.

## 11. Illegal references removed

NONE existed (W0 §5). Re-verified after the change: no foreign `.Application` / `.Infrastructure` /
`.Domain` edge, no foreign `DbContext`/`DbSet`, no `TypeForwardedTo`, no `using X = Y;` alias, no
StoreContext → Host edge.

## 12. Cross-module joins removed

NONE existed. The module has no persistence at all.

## 13. Replacement communication mechanism

Not applicable — no coupling required replacement. The existing Contracts seams remain the only
cross-module mechanism, and they are inbound-consumed only.

## 14. CQRS / MediatR state

`NOT_APPLICABLE_NO_APPLICATION_USE_CASE` (unchanged). No `IRequest`, no handler, no `ISender`, no
MediatR reference, no ceremonial pipeline. `HostCartResidualGuardTests` continues to assert that no
StoreContext source mentions `MediatR`.

## 15. Validation matrix

`NOT_APPLICABLE_INTERNAL_ONLY` — zero endpoint-reachable requests. The certify §6a set-equality gate
holds vacuously: derived route set = ∅, derived `ISender.Send` set = ∅, classified set = ∅.

## 16. Endpoint ownership

`NOT_APPLICABLE` — no `Tooba.StoreContext.Endpoints` project exists and none may be created.
`HostCartResidualGuardTests` asserts its absence.

## 17. Localization state

`CANONICAL` (unchanged, nothing to migrate). The module declares and emits zero error codes, so there is
nothing to catalogue, no `IErrorCatalogContributor`, no `IErrorResourceSet` and no `.resx` pair. This is
the correct state: registering descriptors for `cart.commerce.*` or `platform.*` codes here would create
duplicate descriptor ownership, which the canonical rule forbids. The consumer-owned codes stay with
their natural owners (Cart, Foundation).

## 18. API result / error mapping state

`CANONICAL` (unchanged). No HTTP surface, no `Results.*`, no local mapper, no `ex.Message`
classification.

## 19. Logging / telemetry state

`CANONICAL` (unchanged). No `ILogger`, no `Console.WriteLine`, no second telemetry pipeline, no
`ActivitySource`, no correlation mechanism. `Sensitive-Logging-State = NONE`.

## 20. Correlation / trace continuity state

`CANONICAL` (unchanged). The accessor still avoids `HttpContext` and `AsyncLocal`, so assignment stays
inside the canonical request/worker path and cannot fork correlation.

## 21. File cohesion / decomposition performed

No decomposition. The migration was a **documentation-standard repair** against
`docs/architecture/32-persian-code-documentation-standard.md` (Architect-accepted, `COMPLETE`), whose
review checklist requires strong professional Persian XML on public Tooba-owned members and explicitly
rejects name-echo comments.

Before: every public member in all three files carried **English** XML (`StoreCommerceContext` + 3
`<param>`, `ICurrentStoreCommerceContext`, `.Current`, `IStoreCommerceContextAssigner`, `.Assign` +
`<param>`, `IWorkerStoreCommerceContextFactory`, `.FromTarget` + 2 `<param>`, `StoreContextModule`,
`Name`, `AddServices`, `StoreCommerceContextAccessor`, `.Current`, `.Assign`).

After: every one of those members carries professional Persian XML that states responsibility,
contract and invariant. Two architectural invariants are preserved verbatim in meaning and kept
explicit, because a weak translation would silently destroy them:

1. **`DefaultCurrency` is a default-selection input only** — never the currency of a
   transaction/line/order/settlement/payment-group, and never a single-currency Cart/Order invariant;
   consumer transaction lines may carry their own currency.
2. **`null` means unresolved** — the consumer must fail closed instead of inventing
   Market/DefaultCurrency/SalesChannel.

Technical identifiers stay in English inside the Persian prose (`StoreCommerceContext`,
`DefaultCurrency`, `SalesChannel`, `HttpContext`, `AsyncLocal`, `scope`, `DI`, `Marketplace`,
`control plane`, `edition`, `tenant`), exactly as the standard requires.

## 22. Host residue / authority

`ALLOWED_COMPOSITION_ROOT_ONLY` (unchanged). No `Host/Tooba.Host/StoreContext` folder, no
`Tooba.Host.StoreContext` namespace. Host touches StoreContext only through the canonical seams
(`IStoreCommerceContextAssigner` in `TenantResolutionMiddleware`, `IWorkerStoreCommerceContextFactory`
in `OutboxDispatcher`/`WorkerStoreCommerceContextFactory`) and through control-plane configuration
projection. W1 changed no Host file.

## 23. Persistence / schema state

`NOT_APPLICABLE` / `UNCHANGED`. Zero migrations, zero `DbContext`, zero schema.

## 24. DI / composition updates

NONE. Registrations are byte-identical: scoped `StoreCommerceContextAccessor`, plus
`ICurrentStoreCommerceContext` and `IStoreCommerceContextAssigner` resolving to that same scoped
instance. `StoreContextModule.Name == "StoreContext"` and its position as the first entry of
`ToobaModuleComposition.Modules` are unchanged. No lifetime, no registration, no ordering change.

## 25. Behavior-preservation evidence

**Code-token hash proof.** For each of the three files, every `///` comment line and every blank line
was stripped from both the pre-change blob (`git show HEAD:<path>`) and the post-change file, and the
remaining code-only text was SHA-256 hashed:

| File | code-only lines | old hash | new hash | equal |
| --- | --- | --- | --- | --- |
| `Contracts/Current/StoreCommerceContext.cs` | 18 | `B15192DF3A112D1D` | `B15192DF3A112D1D` | **TRUE** |
| `Infrastructure/StoreContextModule.cs` | 20 | `28D4269CD646CAF1` | `28D4269CD646CAF1` | **TRUE** |
| `Infrastructure/Current/StoreCommerceContextAccessor.cs` | 12 | `616D2C4DC528EB45` | `616D2C4DC528EB45` | **TRUE** |

Additionally, the unified diff of `src/backend/Modules/StoreContext` contains **no** added or removed
line other than `///` documentation lines and blank lines.

Preserved exactly:

- public API shape, member names, parameter names, namespaces, `record` positional shape and equality;
- `Assign(null)` → `ArgumentNullException` (`ArgumentNullException.ThrowIfNull`);
- `Current` is `null` until assigned; last `Assign` wins inside one scope;
- DI lifetimes and registration identities;
- project edges (`Contracts → BuildingBlocks`; `Infrastructure → Contracts + ModuleContracts`);
- zero routes, zero schema, zero migration, zero telemetry, zero localization keys.

## 26. Focused builds / tests

| Check | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | **0 errors, 0 warnings** (CS1591 stays a build error on non-test projects) |
| Focused filter: `HostCartResidualGuardTests`, `HostConfigurationAmcCertGuardTests`, `HostConfigurationAmcW1GuardTests`, `CartModuleAmsc001W3CertGuardTests`, `FulfillmentModuleAmsc001W3CertGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, `HostMultiTenancyAmcCertGuardTests`, `HostOutboxAmcCertGuardTests` | **56 passed / 0 failed / 0 skipped** |

The `HostCartResidualGuardTests.StoreContext_is_golden_certified_with_default_currency_semantics` fact
stays green, including its literal `string? DefaultCurrency` token assertion and its assertions that
`string? Currency`, `AllowedCurrencies`, `SettlementCurrency` and `PaymentCurrency` are absent.

## 27. Guards added / updated

NONE weakened, NONE added in this wave. The existing Host-level protections were used as evidence, not
modified. The module-scoped durable AMSC guards are added in W2/W3.

## 28. Residual debt

`ZERO` ownership/coupling/cohesion/quality debt inside the module. The only open items are the
Structure-owned decisions handed to W2 (composition-entry placement) plus the missing manifest
justifications and AMSC SoT lineage — all recorded in W0 §19 and closed in W2/W3.

## 29. Certification readiness

`READY_FOR_CERTIFICATION` from the migrate skill's perspective, with
`Structure-Handoff-State = REQUIRED` — the final physical verdict belongs to W2
(`tooba-architecture-structure`). W2 must decide the `Infrastructure/DependencyInjection/`
composition-entry question, record the root-allowlist justifications, and add the module-scoped
structure guard.
