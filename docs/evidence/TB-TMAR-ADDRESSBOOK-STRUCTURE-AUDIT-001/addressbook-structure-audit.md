# TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001 — AddressBook pre-certification structure audit

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Task type: AUDIT-ONLY (no refactor, no move, no certification)

## 1. Current accepted checkpoint

| Item | Value |
| --- | --- |
| Architect-accepted parent | `TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001` |
| Accepted parent commit | `8893bdfc530f8f2599b62072a61a2d7b1e2f9489` |
| Recovery-SoT checkpoint | `c49287d574da5143e09ee290d5ff484710f6e32c` |
| Pre-work `HEAD` | `8893bdfc530f8f2599b62072a61a2d7b1e2f9489` |
| Pre-work `origin/main` | `8893bdfc530f8f2599b62072a61a2d7b1e2f9489` |
| Host AddressBook HTTP ownership | ZERO |
| Host AddressBook folder residue | ZERO |
| Module-owned AddressBook routes | 6 |
| AddressBook state | `IN_PROGRESS` / NOT STRUCTURE_CERTIFIED |
| Frontend | FROZEN (`frontendFrozen = true`) |
| Checkout | `PAUSED_AT_SAFE_W5_CHECKPOINT` |

`docs/architecture/tmar-current-state.json` is stale relative to the accepted parent
(`currentHostEvacuation.currentTask` still points at `TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001`
and `hostAddressBookResidue` still lists `AddressBookDevelopmentSeed.cs`). Recorded here as
audit evidence only; this task does not edit that file.

## 2. Physical tree summary (production `.cs`, `obj/`+`bin/` excluded)

### 2.1 `Tooba.AddressBook.Contracts` — 1 file

```text
Tooba.AddressBook.Contracts
└─ CustomerAddressContracts.cs            (ROOT)
```

### 2.2 `Tooba.AddressBook.Domain` — 1 file

```text
Tooba.AddressBook.Domain
└─ CustomerAddress.cs                     (ROOT)
```

### 2.3 `Tooba.AddressBook.Application` — 13 files

```text
Tooba.AddressBook.Application
├─ AddressBookContracts.cs                (ROOT — CustomerAddressWrite + IAddressBookDirectory)
├─ Customer/
│  ├─ Create/CreateCustomerAddressCommand.cs
│  ├─ Delete/DeleteCustomerAddressCommand.cs
│  ├─ Get/GetCustomerAddressQuery.cs
│  ├─ List/ListCustomerAddressesQuery.cs
│  ├─ SetDefault/SetDefaultCustomerAddressCommand.cs
│  └─ Update/UpdateCustomerAddressCommand.cs
└─ Validators/
   ├─ AddressBookFluentRules.cs
   └─ Customer/{Create,Delete,Get,SetDefault,Update}/*Validator.cs
```

### 2.4 `Tooba.AddressBook.Infrastructure` — 8 files

```text
Tooba.AddressBook.Infrastructure
├─ AddressBookDirectory.cs                (ROOT — capability implementation)
├─ AddressBookModule.cs                   (ROOT — composition entry)
├─ Development/AddressBookDevelopmentSeed.cs
├─ Migrations/…                           (TOP-LEVEL, not under Persistence)
│  ├─ 20260825171858_InitialAddressBook.cs
│  ├─ 20260825171858_InitialAddressBook.Designer.cs
│  ├─ 20260913180000_AddRecipientNameParts.cs
│  └─ AddressBookDbContextModelSnapshot.cs
└─ Persistence/AddressBookDbContext.cs
```

### 2.5 `Tooba.AddressBook.Endpoints` — 4 files

```text
Tooba.AddressBook.Endpoints
├─ AddressBookEndpointModule.cs           (ROOT — composition entry)
└─ Customer/
   ├─ AddressBookCustomerActorResolver.cs
   ├─ AddressBookCustomerReadEndpoints.cs
   └─ AddressBookCustomerWriteEndpoints.cs
```

Total production `.cs` files audited: **27** (Contracts 1, Domain 1, Application 13, Infrastructure 8, Endpoints 4).

## 3. Application structure classification

| File | Namespace | Capability | ARCH-COMPLETE-002 verdict |
| --- | --- | --- | --- |
| `AddressBookContracts.cs` | `Tooba.AddressBook.Application` | none (root) | **ILLEGAL root capability file** (write model + port dumped at root) |
| `Customer/Create/…Command.cs` | `.Application.Customer.Create` | Customer/Create | valid |
| `Customer/Delete/…Command.cs` | `.Application.Customer.Delete` | Customer/Delete | valid |
| `Customer/Get/…Query.cs` | `.Application.Customer.Get` | Customer/Get | valid |
| `Customer/List/…Query.cs` | `.Application.Customer.List` | Customer/List | valid |
| `Customer/SetDefault/…Command.cs` | `.Application.Customer.SetDefault` | Customer/SetDefault | valid |
| `Customer/Update/…Command.cs` | `.Application.Customer.Update` | Customer/Update | valid |
| `Validators/AddressBookFluentRules.cs` | `.Application.Validators` | shared Validation capability | valid |
| `Validators/Customer/<Cap>/*Validator.cs` (×5) | `.Application.Validators.Customer.<Cap>` | shared Validation capability | valid |

Root `.cs` files: `AddressBookContracts.cs` (1).

Root allowlist candidate: **`[]`** (empty), i.e. `CustomerAddressWrite` → a `Customer/Models/` (or `Customer/Ports/`)
capability path and `IAddressBookDirectory` → a `Customer/Ports/` capability path.

Reference comparison — `Application` root in certified modules:
`Order []`, `Payment []`, `Offer []`, `AccessControl []`, `Cart/Settlement [GlobalUsings.*]` only.

## 4. Endpoints structure classification

| File | Namespace | Verdict |
| --- | --- | --- |
| `AddressBookEndpointModule.cs` | `Tooba.AddressBook.Endpoints` | valid — composition entry at root is explicitly allowed |
| `Customer/AddressBookCustomerActorResolver.cs` | `.Endpoints.Customer` | valid |
| `Customer/AddressBookCustomerReadEndpoints.cs` | `.Endpoints.Customer` | valid |
| `Customer/AddressBookCustomerWriteEndpoints.cs` | `.Endpoints.Customer` | valid |

- Capability grouping is coherent and visible (`Customer/`).
- No `*Endpoints.cs` file is dumped at project root.
- `AddressBookEndpointModule` is the single composition entry:
  `MapAddressBookModuleEndpoints` creates the `/v1/customer/addresses` group once and delegates to
  `MapReads` + `MapWrites`.
- Every module route mapped exactly once: `MapGroup("/v1/customer/addresses")` appears exactly once in
  production source (`AddressBookEndpointModule.cs` L21). Host legacy map call is retired.
- No duplicate Host ownership: Host contains only `app.MapAddressBookModuleEndpoints();` (Program.cs L508).
- Namespaces match physical paths exactly.
- No alias workaround.

Root `.cs` files: `AddressBookEndpointModule.cs` (1). Root allowlist candidate: **`[AddressBookEndpointModule.cs]`**.

## 5. Infrastructure structure classification

| File | Namespace | Responsibility | Verdict |
| --- | --- | --- | --- |
| `AddressBookModule.cs` | `.Infrastructure` | module composition (+ `AddressBookOutboxRegistration`) | valid root entry |
| `AddressBookDirectory.cs` | `.Infrastructure` | directory/service implementation | **ILLEGAL root implementation file** |
| `Development/AddressBookDevelopmentSeed.cs` | `.Infrastructure.Development` | dev seed | valid capability folder (moved by accepted parent) |
| `Migrations/*.cs` (×4) | `.Infrastructure.Migrations` | EF migrations | **deviant location** — standard requires `Persistence/Migrations` |
| `Persistence/AddressBookDbContext.cs` | `.Infrastructure.Persistence` | persistence | valid |

- Moved development seed is correctly placed at `Tooba.AddressBook.Infrastructure/Development`.
- Foreign adapters: none for AddressBook (no `Integrations/` needed).
- Competing duplicated top-level folders: none beyond the `Migrations` deviation.

Root `.cs` files: `AddressBookDirectory.cs`, `AddressBookModule.cs` (2).
Root allowlist candidate: **`[AddressBookModule.cs]`** (after `AddressBookDirectory.cs` → `Directories/`).

Reference comparison — `Infrastructure` root in certified modules:
`Order [OrderModule.cs]`, `AccessControl [AccessControlModule.cs]`, `Payment []`, `Fulfillment []`,
`Settlement [GlobalUsings.*]`, `Cart [GlobalUsings.*]`. All certified modules place the directory
implementation under `Directories/` (e.g. `AccessControl.Directories.AccessControlDirectory`), and all
certified modules place migrations under `Persistence/Migrations`.

## 6. Contracts / Domain structure classification

| File | Namespace | Verdict |
| --- | --- | --- |
| `Contracts/CustomerAddressContracts.cs` | `Tooba.AddressBook.Contracts` | root dumping of a DTO+port pair; certified Contracts projects have empty roots |
| `Domain/CustomerAddress.cs` | `Tooba.AddressBook.Domain` | root domain aggregate — accepted by precedent (`Order.Domain`, `AccessControl.Domain`) |

- No foreign `Application`/`Infrastructure`/`Host` dependency in Contracts or Domain.
- Contracts declares no namespace alias and no error-code dumping beyond the checkout lookup + record.
- Capability ownership ambiguity: `CustomerAddressRecord` + `IAddressBookCheckoutLookup` are both
  checkout-facing contracts, so a coherent home exists (`Contracts/Customer/`); no ceremonial folder
  is required beyond that.

## 7. Exact path↔namespace mismatch list

Generated migrations/snapshots included for completeness. **All 27 AddressBook production files match their
physical path exactly. Mismatch count = 0.**

| # | File | Physical path-derived namespace | Declared namespace | State |
| --- | --- | --- | --- | --- |
| 1 | `Contracts/CustomerAddressContracts.cs` | `Tooba.AddressBook.Contracts` | `Tooba.AddressBook.Contracts` | MATCH |
| 2 | `Domain/CustomerAddress.cs` | `Tooba.AddressBook.Domain` | `Tooba.AddressBook.Domain` | MATCH |
| 3 | `Application/AddressBookContracts.cs` | `Tooba.AddressBook.Application` | `Tooba.AddressBook.Application` | MATCH (root) |
| 4–9 | `Application/Customer/<Cap>/*.cs` | `…Application.Customer.<Cap>` | same | MATCH |
| 10 | `Application/Validators/AddressBookFluentRules.cs` | `…Application.Validators` | same | MATCH |
| 11–15 | `Application/Validators/Customer/<Cap>/*Validator.cs` | `…Application.Validators.Customer.<Cap>` | same | MATCH |
| 16 | `Infrastructure/AddressBookDirectory.cs` | `Tooba.AddressBook.Infrastructure` | same | MATCH (root) |
| 17 | `Infrastructure/AddressBookModule.cs` | `Tooba.AddressBook.Infrastructure` | same | MATCH (root) |
| 18 | `Infrastructure/Development/AddressBookDevelopmentSeed.cs` | `…Infrastructure.Development` | same | MATCH |
| 19–22 | `Infrastructure/Migrations/*.cs` | `…Infrastructure.Migrations` | same | MATCH (location deviant) |
| 23 | `Infrastructure/Persistence/AddressBookDbContext.cs` | `…Infrastructure.Persistence` | same | MATCH |
| 24 | `Endpoints/AddressBookEndpointModule.cs` | `Tooba.AddressBook.Endpoints` | same | MATCH (root) |
| 25–27 | `Endpoints/Customer/*.cs` | `…Endpoints.Customer` | same | MATCH |

Legitimate root-namespace files (path-derived namespace == project namespace by definition):
`AddressBookContracts.cs`, `AddressBookDirectory.cs`, `AddressBookModule.cs`, `AddressBookEndpointModule.cs`,
`CustomerAddressContracts.cs`, `CustomerAddress.cs`.

## 8. Endpoint route → request → handler → validator matrix

| # | Route | Capability file | Request (MediatR) | Handler | Validator | Classification |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/addresses` | `Customer/AddressBookCustomerReadEndpoints.cs` | `ListCustomerAddressesQuery` | `ListCustomerAddressesQueryHandler` | — | `NO_VALIDATOR_REQUIRED` (no transport input; actor is server trust) |
| 2 | `GET /v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerReadEndpoints.cs` | `GetCustomerAddressQuery` | `GetCustomerAddressQueryHandler` | `GetCustomerAddressQueryValidator` | `VALIDATOR_REQUIRED` / PRESENT |
| 3 | `POST /v1/customer/addresses` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `CreateCustomerAddressCommand` | `CreateCustomerAddressCommandHandler` | `CreateCustomerAddressCommandValidator` | `VALIDATOR_REQUIRED` / PRESENT |
| 4 | `PUT /v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `UpdateCustomerAddressCommand` | `UpdateCustomerAddressCommandHandler` | `UpdateCustomerAddressCommandValidator` | `VALIDATOR_REQUIRED` / PRESENT |
| 5 | `DELETE /v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `DeleteCustomerAddressCommand` | `DeleteCustomerAddressCommandHandler` | `DeleteCustomerAddressCommandValidator` | `VALIDATOR_REQUIRED` / PRESENT |
| 6 | `POST /v1/customer/addresses/{addressId:guid}/default` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `SetDefaultCustomerAddressCommand` | `SetDefaultCustomerAddressCommandHandler` | `SetDefaultCustomerAddressCommandValidator` | `VALIDATOR_REQUIRED` / PRESENT |

### Exact validator coverage counts

| Metric | Expected (task) | Observed | State |
| --- | --- | --- | --- |
| Endpoint-reachable requests | 6 | 6 | MATCH |
| `VALIDATOR_REQUIRED` | 5 | 5 | MATCH |
| Validators present | 5 | 5 | MATCH |
| `NO_VALIDATOR_REQUIRED` | 1 | 1 | MATCH |
| Missing validators | — | 0 | MATCH |

Discovery path: Host `AddToobaCqrsFoundation(typeof(Tooba.AddressBook.Application.IAddressBookDirectory).Assembly)`
→ `Tooba.BuildingBlocks.TmarFoundation` `AddValidatorsFromAssembly(assembly)` (assembly-level
`FluentValidation` scan) + `ValidationBehavior<,>` pipeline. Validators carry only transport-shape rules
(`RequireNonBlank` / `RequireId`); ownership/existence business rules stay in Domain/Directory.

## 9. MediatR / CQRS check (all 6 HTTP use cases)

| Property | Result |
| --- | --- |
| All requests `IRequest`-based | YES (5 × `IRequest<T>`, 1 × `IRequest<Unit>`) |
| Dispatched via `ISender` | YES — read + write endpoint files declare `ISender sender` and never call directories |
| Real MediatR handlers | YES — one `IRequestHandler<,>` per request, injecting `IAddressBookDirectory` only |
| Endpoints touch Infrastructure/Directory directly | NO |
| Legacy dispatcher abstractions reintroduced | NO (zero `IDispatcher` / `IUseCase` / `Mediator.` hits) |
| CQRS bypassed through Host | NO — Host only maps the module composition entry |

MediatR version remains the approved `12.5.0` via `Tooba.BuildingBlocks` (unchanged by this task).

## 10. Host → AddressBook reference inventory and classification

| # | Location | Reference | Classification |
| --- | --- | --- | --- |
| 1 | `Program.cs` L33 | `using Tooba.AddressBook.Endpoints;` | `ALLOWED_COMPOSITION_ROOT` |
| 2 | `Program.cs` L87 | `builder.Services.AddAddressBookEndpointPresentation();` | `ALLOWED_COMPOSITION_ROOT` |
| 3 | `Program.cs` L158 | `typeof(Tooba.AddressBook.Application.IAddressBookDirectory).Assembly` inside `AddToobaCqrsFoundation` | `ALLOWED_COMPOSITION_ROOT` (assembly handler/validator registration) |
| 4 | `Program.cs` L190 | `AddScoped<Tooba.AddressBook.Contracts.IAddressBookCheckoutLookup>(sp => IAddressBookDirectory)` | `ALLOWED_CONTRACT_CONSUMPTION` (checkout lookup seam) |
| 5 | `Program.cs` L508 | `app.MapAddressBookModuleEndpoints();` | `ALLOWED_COMPOSITION_ROOT` |
| 6 | `Composition/ToobaModuleComposition.cs` L23, L72 | `using Tooba.AddressBook.Infrastructure;` + `new AddressBookModule()` | `ALLOWED_COMPOSITION_ROOT` |
| 7 | `Admin/ProductWorkspaceDevelopmentBootstrap.cs` L43, L47, L134, L161, L287 | `AddressBook.Infrastructure.Development` + `Persistence` (migrate + seed) | `ALLOWED_COMPOSITION_ROOT` (development-only bootstrap, explicitly allowlisted) |
| 8 | `Customer/CustomerPanelComposer.cs` L1, L16, L25; `Customer/CustomerPanelModels.cs` L17–18 | Host composer injects `Tooba.AddressBook.Application.IAddressBookDirectory` for `CountAsync` + dashboard `AddressBookCount` | `STRUCTURAL_DEBT_ONLY` (pre-existing Host consumer of a module Application port; no business write, no endpoint ownership) |

Explicit confirmations:

- `AddressBook.Application` assembly registration in Host is legitimate composition — **YES**.
- `AddressBook.Infrastructure` registration in Host is legitimate composition — **YES** (`AddToobaModules`).
- `AddressBook.Contracts` checkout lookup/reference is legitimate cross-module contract consumption — **YES**.
- Any Host implementation/business logic for AddressBook still exists — **NO** (Host AddressBook folder is deleted,
  `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` absent from `Tooba.Host/AddressBook/`).
- No legitimate composition edge was removed merely to reach a zero count.

## 11. Cross-module project/reference inventory

### AddressBook project references

| Project | References | Verdict |
| --- | --- | --- |
| `Tooba.AddressBook.Contracts` | (none) | CLEAN |
| `Tooba.AddressBook.Domain` | `Tooba.BuildingBlocks` | CLEAN |
| `Tooba.AddressBook.Application` | `BuildingBlocks`, `AddressBook.Domain`, `AddressBook.Contracts` | CLEAN |
| `Tooba.AddressBook.Endpoints` | `AddressBook.Application`, `BuildingBlocks`, `Tooba.Order.Contracts` | Contracts-only foreign edge |
| `Tooba.AddressBook.Infrastructure` | `AddressBook.Application`, `Tooba.Order.Contracts`, `ModuleContracts`, `Tooba.Persistence` | Contracts-only foreign edge |

### AddressBook → other modules (source-level `using Tooba.`)

```text
Tooba.BuildingBlocks / Tooba.BuildingBlocks.Security / Tooba.ModuleContracts / Tooba.Persistence
Tooba.Order.Contracts.Fulfillment        ← the only foreign module namespace
```

| Foreign dependency | Kind | Verdict |
| --- | --- | --- |
| `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor` | Contracts | ALLOWED (stable actor authority contract) |
| `Tooba.Order.Application` | — | ABSENT |
| `Tooba.Order.Infrastructure` | — | ABSENT |
| `Tooba.Order.Domain` | — | ABSENT |
| Host types | — | ABSENT |

**Order-Contracts-only dependency state: CONFIRMED.** The only Order edge is
`Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`, consumed in
`Infrastructure/Development/AddressBookDevelopmentSeed.cs` and
`Endpoints/Customer/AddressBookCustomerActorResolver.cs`. No `Order.Application` dependency remains and no
guest-actor literal is duplicated.

## 12. Alias workaround audit

- Namespace-alias declarations (`using X = …`) in AddressBook production source: **NONE**.
- Global-namespace pollution / foreign global aliases: **NONE**.
- `using` directives are all direct namespace imports.
- `Alias-Workaround-State = NO_ALIAS_NO_FOREIGN_GLOBAL_ALIAS`.

## 13. FINAL GAP CLASSIFICATION

### A. CERTIFICATION_BLOCKER

| ID | Exact file(s) | Exact problem | Exact required fix | Production behavior | Repair grouping |
| --- | --- | --- | --- | --- | --- |
| AB-B1 | `Tooba.AddressBook.Application/AddressBookContracts.cs` | Illegal project-root capability file; `CustomerAddressWrite` (model) and `IAddressBookDirectory` (port) dumped at Application root. Violates `APPLICATION_CAPABILITY_FOLDERS` + `ROOT_ALLOWLIST` ("no miscellaneous `*Contracts.cs` dumping at root"). | Split into capability folders, e.g. `Application/Customer/Models/CustomerAddressWrite.cs` and `Application/Customer/Ports/IAddressBookDirectory.cs`; update namespaces to `…Application.Customer.Models` / `…Application.Customer.Ports`; update consumers (handlers, Infrastructure directory, Host `AddToobaCqrsFoundation` type token). | UNCHANGED (pure relocation + namespace) | G-A Application capability relocation |
| AB-B2 | `Tooba.AddressBook.Infrastructure/AddressBookDirectory.cs` | Capability implementation/directory file at Infrastructure root. Violates `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS` ("capability implementation/bridge/service files must not live at root; root is the module composition entry only"). Every certified module keeps its directory under `Directories/`. | Move to `Infrastructure/Directories/AddressBookDirectory.cs`, namespace `Tooba.AddressBook.Infrastructure.Directories`; update the `IAddressBookDirectory` registration in `AddressBookModule` and any consumers. | UNCHANGED | G-B Infrastructure capability relocation |
| AB-B3 | `Tooba.AddressBook.Infrastructure/Migrations/*.cs` (4 files) | Migrations live in a **top-level `Migrations` folder** instead of `Persistence/Migrations`. Violates `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS` ("persistence under `Persistence`; migrations under `Persistence/Migrations`"). All 6 certified modules use `Persistence/Migrations`. | Move the 4 files to `Infrastructure/Persistence/Migrations/`; change namespaces to `Tooba.AddressBook.Infrastructure.Persistence.Migrations`. EF discovers migrations via the `DbContext` assembly, so no migration regeneration is required and schema names are unaffected. | UNCHANGED | G-B Infrastructure capability relocation |
| AB-B4 | `Tooba.AddressBook.Contracts/CustomerAddressContracts.cs` | Root dumping of a DTO + checkout-lookup port pair in Contracts. Certified Contracts projects (`Order`, `Cart`, `Offer`, `Payment`, `Settlement`, `Fulfillment`) all have empty roots. Violates `ROOT_ALLOWLIST` root-dumping principle. | Move to a coherent capability path, e.g. `Contracts/Customer/CustomerAddressContracts.cs`, namespace `Tooba.AddressBook.Contracts.Customer`; update consumers (Application commands/handlers, Infrastructure directory, Host checkout-lookup registration, tests). | UNCHANGED | G-C Contracts capability relocation |
| AB-B5 | `docs/architecture/tmar-module-structure-manifests.json`, `docs/architecture/tmar-current-state.json`, AddressBook guard test | No AddressBook manifest entry and no durable AddressBook endpoint-reachable validator-coverage guard exist. Certification step 5 requires every endpoint-reachable request classified `VALIDATOR_REQUIRED` / `NO_VALIDATOR_REQUIRED` in a durable manifest/manifest-equivalent guard; AddressBook's accepted expectation (`5 REQUIRED / 5 PRESENT / 1 NO_VALIDATOR_REQUIRED`) is currently only in accepted task prose. | At certification time: add the AddressBook manifest entry (roots from this audit), add an AddressBook validator-coverage guard asserting the exact 6-request / 5+1 classification and `NO_VALIDATOR_REQUIRED` reason for `ListCustomerAddressesQuery`, and record the coverage string in `tmar-current-state.json`. Guard may live in `Tooba.Host.Tests` like AccessControl (AddressBook has no dedicated Tests project). | UNCHANGED (guard/SoT only) | G-D Guard + manifest + SoT |

`structureCertified` MUST remain `false` in this audit task.

### B. NON_BLOCKING_DEBT

| ID | Item | Note |
| --- | --- | --- |
| AB-N1 | No `Tooba.AddressBook.Tests` project; AddressBook behavior/structure tests live in `Tooba.Host.Tests` (`AddressBookFoundationTests`). | Precedent: `AccessControl` also has no module Tests project. Acceptable, but a dedicated project would align AddressBook with Order/Cart/Offer/Payment/Settlement/Fulfillment. |
| AB-N2 | Host `Customer/CustomerPanelComposer.cs` injects `Tooba.AddressBook.Application.IAddressBookDirectory` directly for `CountAsync`. | Pre-existing Host consumer composition; not an HTTP endpoint, no business write. Should eventually consume a Contracts read port, but it is not required for AddressBook structure certification. |
| AB-N3 | `Tooba.AddressBook.Infrastructure/Development/` has no certified-module precedent (only `Support/Seeds/`, `Content/…` root seeds exist). | The folder is coherent and capability-named; keeping it is acceptable, but the choice is recorded so the certification task can justify it in the manifest. |
| AB-N4 | `docs/architecture/tmar-current-state.json#currentHostEvacuation` is stale (still lists `AddressBookDevelopmentSeed.cs` residue and the parent task as `currentTask`). | SoT refresh belongs to the certification/closure task, not to this audit. |

### C. ACCEPTABLE_BY_STANDARD

| ID | Item | Justification |
| --- | --- | --- |
| AB-A1 | `Tooba.AddressBook.Domain/CustomerAddress.cs` at Domain root. | Precedent: `Order.Domain` (10 root files) and `AccessControl.Domain` (`AccessControlDomain.cs`) are certified with root domain files allowlisted. |
| AB-A2 | `Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs` at Endpoints root. | Standard explicitly allows the composition entry at root; identical to every certified Endpoints project. |
| AB-A3 | `Tooba.AddressBook.Infrastructure/AddressBookModule.cs` at Infrastructure root (also holds `AddressBookOutboxRegistration`). | Certified precedent (`OrderModule.cs`, `AccessControlModule.cs`); `AddressBookOutboxRegistration` is a tiny module-scoped registration collocated with the composition entry, consistent with the certified AccessControl shape. |
| AB-A4 | `Validators/` shared validation capability with `Validators/Customer/<Cap>/` nesting. | Matches Settlement (`Validators/Seller`, `Validators/Admin`) and Fulfillment (`Validators/<capability>`) certified shape. |
| AB-A5 | `Tooba.Order.Contracts`-only foreign dependency. | ARCH-CONTRACT-001 compliant; `StorefrontGuestActor` is the stable actor authority contract. |
| AB-A6 | Actor-authority seam inside `Endpoints/Customer` (`IAddressBookCustomerActorResolver`) instead of a Host security adapter. | Module-owned neutral seam consuming `ICurrentAuthenticatedUser`; no Host type leakage. |
| AB-A7 | No `Errors/` or `Resources/` folder in Endpoints. | Optional per standard ("may contain"); AddressBook error codes are inline semantic results consistent with accepted behavior. |

## 14. Exact proposed manifest values (`structureCertified: false`)

```json
{
  "module": "AddressBook",
  "structureCertified": false,
  "lockVersion": "ARCH-COMPLETE-002-PENDING",
  "projects": [
    {
      "projectName": "Tooba.AddressBook.Contracts",
      "rootAllowlist": [],
      "rootAllowlistJustification": "After AB-B4 repair the checkout record + lookup port live under Contracts/Customer/; no root .cs remains.",
      "forbiddenRootFiles": ["CustomerAddressContracts.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.AddressBook.Domain",
      "rootAllowlist": ["CustomerAddress.cs"],
      "rootAllowlistJustification": "Single small aggregate at Domain root; certified precedent Order.Domain/AccessControl.Domain.",
      "forbiddenRootFiles": [],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.AddressBook.Application",
      "rootAllowlist": [],
      "rootAllowlistJustification": "After AB-B1 repair no Application root .cs remains; all commands/queries/models/ports/validators are capability-owned.",
      "forbiddenRootFiles": ["AddressBookContracts.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.AddressBook.Endpoints",
      "rootAllowlist": ["AddressBookEndpointModule.cs"],
      "rootAllowlistJustification": "Composition entry only; Customer/ carries read/write endpoint files and the actor seam.",
      "forbiddenRootFiles": ["AddressBookCustomerReadEndpoints.cs", "AddressBookCustomerWriteEndpoints.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.AddressBook.Infrastructure",
      "rootAllowlist": ["AddressBookModule.cs"],
      "rootAllowlistJustification": "After AB-B2/AB-B3 repair root holds only the module composition entry; the directory lives under Directories/ and migrations under Persistence/Migrations.",
      "forbiddenRootFiles": ["AddressBookDirectory.cs"],
      "forbiddenTopLevelFolders": ["Migrations"]
    }
  ]
}
```

Manifest-adjacent fields proposed:

| Field | Proposed value |
| --- | --- |
| `httpApplicability` | `HTTP_OWNING` |
| `endpointOwnership` | `MODULE_OWNED_6_OF_6` |
| `hostOwnedRouteCount` | `0` |
| `cqrs` | `COMPLETE_6_USECASES_ISENDER_MEDIATR_12_5_0` |
| `validatorCoverage` | `COMPLETE_5_OF_5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED` |
| `validatorRequiredCount` | `5` |
| `validatorsPresentCount` | `5` |
| `validatorsMissingCount` | `0` |
| `noValidatorRequiredCount` | `1` |
| `noValidatorRequiredRequests` | `ListCustomerAddressesQuery` (`NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`) |
| `pathNamespaceState` | `EXACT` |
| `aliasWorkaroundState` | `NO_ALIAS_NO_FOREIGN_GLOBAL_ALIAS` |
| `hostResidueState` | `ZERO` |
| `hostBusinessAuthorityState` | `NONE` |
| `crossModuleState` | `ORDER_CONTRACTS_ONLY` |
| `structureCertified` | `false` |

`tmar-current-state.json` must NOT be edited by this task; the values above are proposals for the
certification task.

## 15. Focused validation results

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore` | Build succeeded — 0 warnings, 0 errors |
| `dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore` | Build succeeded — 0 warnings, 0 errors |
| `dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj --no-restore` | Build succeeded — 0 warnings, 0 errors |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests` | Passed — Failed 0, Passed 8, Skipped 0, Total 8 |

No solution build, no broad integration/architecture suite.

## 16. Explicit untouched list

- Host AddressBook HTTP ownership = ZERO (unchanged).
- Host AddressBook residue = ZERO (unchanged).
- All 6 AddressBook routes remain module-owned via `ISender`/MediatR.
- AddressBook behavior, actor seam behavior, seed semantics unchanged.
- `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor` authority unchanged.
- Checkout `PAUSED_AT_SAFE_W5_CHECKPOINT` unchanged.
- Frontend FROZEN — no frontend file touched.
- Existing certified modules and their manifests unchanged.
- `docs/architecture/tmar-module-structure-manifests.json` and `docs/architecture/tmar-current-state.json` NOT edited.
- No AddressBook production `.cs`, `.csproj`, Host production `.cs`, frontend, checkout, lock file, unrelated test or baseline edited.
- AddressBook remains `IN_PROGRESS` and NOT STRUCTURE_CERTIFIED; no `COMPLETE_REFERENCE_PATTERN` claim made.

## 17. Exact next recommended task

`Certification-Blocker-Count: 5` → the audit found certification blockers, so the next task must be ONE bounded
repair task based on the exact blocker set:

**`TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001`** — perform repair groupings G-A → G-D
(Application capability relocation, Infrastructure capability relocation, Contracts capability relocation,
then guard + manifest + SoT), behaviour-preserving, followed by
`TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001` for certification.

Do not start either automatically.
