# TB-TMAR-STORECONTEXT-AMSC-001-W2 — Structure

- **Task**: `TB-TMAR-STORECONTEXT-AMSC-001-W2`
- **Mode**: `ARCHITECT_DIRECT_AMSC`
- **Skill**: `tooba-architecture-structure`
- **Target**: `src/backend/Modules/StoreContext/Tooba.StoreContext.*`
- **Lock version**: `ARCH-COMPLETE-002`
- **Starting HEAD**: `d8abe38af8920bb1ac0417270e2de5b8756a104d` (W1)
- **State**: `STRUCTURE_COMPLETE`
- **Verdict**: `STRUCTURE_READY_FOR_CERTIFY`

---

## 1. What StoreContext is (structure constraints that follow from its nature)

| Property | Value | Structural consequence |
| --- | --- | --- |
| `httpApplicability` | `INTERNAL_ONLY` | no `Endpoints` project, no route group, no `MapGroup` |
| `endpointOwnership` | `NOT_APPLICABLE` | no endpoint folders, no endpoint handler leaves |
| `cqrs` | `NOT_APPLICABLE_NO_APPLICATION_USE_CASE` | no `Application` project, no `Commands/`/`Queries/`/`Handlers/`, no MediatR |
| `validatorCoverage` | `NOT_APPLICABLE_INTERNAL_ONLY` | 0 routes, 0 endpoint-reachable requests, 0 required validators (vacuous set-equality gate) |
| `persistence` | none | no `DbContext`, no `Persistence/`, no migrations |
| `purpose` | scoped provider/assigner of `StoreCommerceContext` (Market, DefaultCurrency, SalesChannel) | exactly one capability (`Current`) plus the composition entry |

So the *only* legitimate structure question for this module is: **where does the composition entry live, and is every root truly empty?**

---

## 2. Physical tree — after W2

```text
src/backend/Modules/StoreContext/
  Tooba.StoreContext.Contracts/
    Current/
      StoreCommerceContext.cs
    Tooba.StoreContext.Contracts.csproj
  Tooba.StoreContext.Infrastructure/
    Current/
      StoreCommerceContextAccessor.cs
    DependencyInjection/
      StoreContextModule.cs
    Tooba.StoreContext.Infrastructure.csproj
```

Three production files, two projects, two capability folders, one integration folder. No `bin`/`obj` noise is considered.

## 3. The single structure change

`StoreContextModule.cs` moved:

```text
before: Tooba.StoreContext.Infrastructure/StoreContextModule.cs
        namespace Tooba.StoreContext.Infrastructure
after:  Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs
        namespace Tooba.StoreContext.Infrastructure.DependencyInjection
```

### Why

`ARCH-COMPLETE-002` *permits* a module composition entry at the Infrastructure root, and StoreContext's historical golden evidence
(`docs/evidence/TB-TMAR-STORECONTEXT-GOLDEN-001/store-context-golden.md`) recorded exactly that with
`rootAllowlist: ["StoreContextModule.cs"]`. However every newest certified module converges on
`Infrastructure/DependencyInjection/*Module.cs` (Inventory, CustomerProfile, AddressBook, Pricing, Promotion, Notification…).
Keeping a per-module exception alive through AMSC certification would mean:

1. a non-empty root allowlist that has to be re-justified forever, and
2. a structural divergence between StoreContext and the certified majority that a future reader would have to re-litigate.

Aligning now makes the stronger, self-evident invariant true — **both project roots are empty** — and removes the special case
*before* certification instead of after. W1 explicitly handed this decision to W2
(`structureHandoffDetail` in `storeContextAmsc001W1`).

### Blast radius (all changed atomically)

| File | Change |
| --- | --- |
| `Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs` | moved; namespace line aligned to path; BOM removed |
| `src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs` | `using` directive only: `Tooba.StoreContext.Infrastructure` → `Tooba.StoreContext.Infrastructure.DependencyInjection` |
| `src/backend/Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs` | the two StoreContext root-file facts now assert the file is *absent* at the root and *present* under `DependencyInjection/` |
| `docs/architecture/tmar-module-structure-manifests.json` | Infrastructure `rootAllowlist` `["StoreContextModule.cs"]` → `[]`; `StoreContextModule.cs` added to `forbiddenRootFiles`; `rootAllowlistJustification` added for both projects |

`new StoreContextModule()` in the composition list, its position, and every registration inside `StoreContextModule.cs` are unchanged.

## 4. Behaviour preservation

Code-only hashing (strip every `///` line, every blank line, and the BOM) against the pre-move blob:

| File | Code lines | Pre-move | Post-move |
| --- | --- | --- | --- |
| `Infrastructure/StoreContextModule.cs` | 20 | `B9FBE86326EFF86F` | `AA79F3BDA89EA222` |

The two differing code lines are exactly:

```text
[0] OLD "<BOM>using Microsoft.Extensions.Configuration;"  NEW "using Microsoft.Extensions.Configuration;"   (formatting)
[6] OLD "namespace Tooba.StoreContext.Infrastructure;"    NEW "namespace Tooba.StoreContext.Infrastructure.DependencyInjection;"  (intended alignment)
```

`Contracts/Current/StoreCommerceContext.cs` and `Infrastructure/Current/StoreCommerceContextAccessor.cs` are byte-identical to W1.
No public API, DI lifetime, registration order, project reference edge or schema changed.

## 5. Root allowlist — enforced, and now empty

| Project | `rootAllowlist` | `rootAllowlistJustification` | `forbiddenRootFiles` |
| --- | --- | --- | --- |
| `Tooba.StoreContext.Contracts` | `[]` | boundary semantics only; `Current/` carries the record + the read/assign/worker-factory seams | `StoreCommerceContext.cs` |
| `Tooba.StoreContext.Infrastructure` | `[]` | composition entry aligned to the certified precedent; `Current/` keeps the scoped accessor | `StoreContextModule.cs`, `StoreCommerceContextAccessor.cs` |

Both allowlists were cross-checked against disk (`Directory.GetFiles(..., TopDirectoryOnly)` = ∅) inside the durable guard.

## 6. Path ↔ namespace audit — 0 mismatches

```text
OK  Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs                    -> Tooba.StoreContext.Contracts.Current
OK  Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs       -> Tooba.StoreContext.Infrastructure.Current
OK  Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs     -> Tooba.StoreContext.Infrastructure.DependencyInjection
mismatches: 0
```

No namespace alias workaround (`using X = Y;`) and no `global using` shim exists anywhere in the module.

## 7. Folder granularity — professional shallow, no over-foldering

- capability-first axis only (`Current`, `DependencyInjection`); the technical request axis (`Commands/`, `Queries/`, `Models/`, `Validators/`, `Handlers/`, `Features/`) is **absent by construction** — the module has no application use case;
- no empty capability folder (verified programmatically, ignoring `bin`/`obj`);
- no single-file request leaf and no god file: the largest file is 32 lines including documentation;
- `Current/` is not ceremony: it is the one capability the module owns (the effective context + its three seams).

## 8. Solution Explorer grouping

```xml
<Folder Name="/Modules/StoreContext/">
  <Project Path="Modules/StoreContext/Tooba.StoreContext.Contracts/Tooba.StoreContext.Contracts.csproj" />
  <Project Path="Modules/StoreContext/Tooba.StoreContext.Infrastructure/Tooba.StoreContext.Infrastructure.csproj" />
</Folder>
```

Exactly the two production projects, no test project inside the module folder, no `Host/StoreContext` folder left behind.

## 9. Microservice-extraction readiness (the end goal)

StoreContext must be extractable as an independent service with **zero coupling**. Verified states:

| Axis | State |
| --- | --- |
| foreign module-layer reference (`Tooba.<Other>.{Application,Domain,Infrastructure,Endpoints}`) | `ZERO` |
| `Contracts` project references | foundation only (`Tooba.BuildingBlocks`); no `Infrastructure`/`Application`/`Domain` edge |
| `Infrastructure` project references | `Tooba.StoreContext.Contracts` + `Tooba.ModuleContracts`; no `Tooba.Host` edge |
| cross-module persistence / joins | `NONE` (no persistence at all) |
| namespace alias / type-forwarding workaround | `NONE` |
| inbound coupling | legal `Contracts`-only consumers (Cart and the Host worker adapter) |
| Host residue | composition root line only |

The Host worker adapter (`IWorkerStoreCommerceContextFactory` implementation over the control-plane registry) remains the single
host-side seam; it is a composition concern, not a module dependency — the module never references `Tooba.Host`.

## 10. Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/StoreContextModuleAmsc001W2StructureGuardTests.cs` — 9 facts:

1. stays `INTERNAL_ONLY`: no `Application`/`Domain`/`Endpoints`/`Tests` project, no `Host/StoreContext` folder, no `MediatR`/`IRequest`/`ISender`/`MapGroup`/`DbContext` token, and the historical `storeContext` SoT block is intact;
2. composition entry lives under `DependencyInjection/` with the path-derived namespace and both roots are empty;
3. capability-first shallow tree, exact folder sets, no empty capability folder, exactly 3 production files;
4. path equals namespace for every production file;
5. root allowlists match disk exactly, justifications non-empty, forbidden root files absent, and the `StoreContext` entry is certified (not in `preCertModules`);
6. `/Modules/StoreContext/` solution folder holds exactly the two projects and each `.csproj` exists;
7. Contracts-only boundary + zero foreign module-layer coupling + no alias workaround;
8. no ad-hoc presentation/logging/telemetry mechanism (`Results.*`, `ProblemDetails`, `Console/Debug.WriteLine`, `ILogger`, `ActivitySource`, `ex.Message`);
9. the accepted currency semantics are untouched (exactly `Market`, `DefaultCurrency`, `SalesChannel`; no invented `Currency`/`AllowedCurrencies`/`SettlementCurrency`/`PaymentCurrency`) and the W1 Persian-documentation lock still holds.

## 11. Verification

| Check | Result |
| --- | --- |
| `dotnet build Host/Tooba.Host.Tests` | succeeded — 0 errors (166 pre-existing analyzer warnings, unrelated files) |
| `StoreContextModuleAmsc001W2StructureGuardTests` | 9 / 9 passed |
| `StoreContext*` + `HostCartResidual*` focused filter | 24 / 24 passed |
| path↔namespace audit | 0 mismatches |
| manifest ↔ disk allowlist comparison | exact |

## 12. Handoff to W3 (Certify)

`storeContextAmsc001W2` is recorded additively in `docs/architecture/tmar-current-state.json` with
`startingHead = d8abe38af8920bb1ac0417270e2de5b8756a104d` and `verdict = STRUCTURE_READY_FOR_CERTIFY`.
W3 must: add `certificationNote` to the manifest `StoreContext` entry, keep this durable guard, record the AMSC lineage
`W0 c73545f5 → W1 d8abe38a → W2 <this commit> → W3`, and certify the microservice-extraction readiness claim above.
