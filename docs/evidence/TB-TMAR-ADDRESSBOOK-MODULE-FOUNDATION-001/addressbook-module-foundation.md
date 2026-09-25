# AddressBook Module Foundation Shell

Task: `TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001`
Parent: `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001` at `b22e73fd12182556c712af234555c90383e0a542`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — foundation shell only.
Recovery honesty: AddressBook remains **IN_PROGRESS**; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. New Endpoints project

Created `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj`:

- `TargetFramework` = `net8.0`, `ImplicitUsings` = `enable`, `Nullable` = `enable`, `NoWarn` = `$(NoWarn);1591` (identical style to `Tooba.Payment.Endpoints`).
- `RootNamespace` / `AssemblyName` = `Tooba.AddressBook.Endpoints`.
- `FrameworkReference Include="Microsoft.AspNetCore.App"`.
- Project references — exactly two, nothing else:
  - `..\Tooba.AddressBook.Application\Tooba.AddressBook.Application.csproj`
  - `..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj`

Boundary proof (verified by grep over the new project):

| Edge | Result |
| --- | --- |
| `Tooba.AddressBook.Endpoints` → `Tooba.Host` | ZERO |
| `Tooba.AddressBook.Endpoints` → `Tooba.AddressBook.Infrastructure` | ZERO |
| `Tooba.AddressBook.Endpoints` → foreign `Application`/`Domain`/`Infrastructure` | ZERO |
| `Tooba.AddressBook.Endpoints` → `Tooba.AddressBook.Application` | allowed |
| `Tooba.AddressBook.Endpoints` → `Tooba.BuildingBlocks` | allowed (referenced; no symbol consumed yet) |

## 2. Endpoints root state

Root `.cs` files (exactly one):

- `AddressBookEndpointModule.cs` — namespace `Tooba.AddressBook.Endpoints`.

`public static class AddressBookEndpointModule` exposes the standard module composition signatures needed by the later slice, both documented as temporary `FOUNDATION_ONLY_NO_ROUTES`:

- `MapAddressBookModuleEndpoints(this IEndpointRouteBuilder app)` → null-guard, returns `app`, **maps no routes**.
- `AddAddressBookEndpointPresentation(this IServiceCollection services)` → null-guard, registers **nothing**.

`Program.cs` does **not** call either extension in this task.

## 3. New endpoint route count

`ZERO`. Grep for `MapGet|MapPost|MapPut|MapDelete|MapGroup|MapMethods` across the new project returns no matches. No Customer endpoint file, no request DTO, no authorizer/actor seam, no handler, no validator was created.

## 4. Program CQRS foundation registration

`src/backend/Host/Tooba.Host/Program.cs` existing `AddToobaCqrsFoundation(...)` call received one additional assembly argument:

```csharp
typeof(Tooba.AddressBook.Application.IAddressBookDirectory).Assembly
```

placed immediately after the AccessControl Application assembly argument. `IAddressBookDirectory` is the stable current AddressBook Application type and matches the existing style (every other argument is a stable Application type from the owning module).

## 5. Duplicate pipeline proof

- No `AddMediatR` call was added anywhere.
- No `AddValidatorsFromAssembly` call was added anywhere.
- No `IPipelineBehavior` registration was added anywhere.
- `Tooba.BuildingBlocks/TmarFoundation.cs` `ToobaCqrsRegistration.AddToobaCqrsFoundation` remains the single shared registration point: it calls `AddValidatorsFromAssembly(typeof(FoundationPingCommand).Assembly)` once, `AddMediatR` once (registering the foundation assembly plus each supplied module assembly), then loops the supplied assemblies once for `AddValidatorsFromAssembly`, then registers the three pipeline behaviors once.
- The only change is one extra assembly in the existing `params Assembly[]` list, so AddressBook Application becomes discoverable through the already-shared foundation — a second pipeline was **not** introduced.

## 6. Host route ownership unchanged

- `app.MapAddressBookEndpoints();` remains at `Program.cs:506`, still calling the Host-owned `Tooba.Host.AddressBook.AddressBookEndpoints`.
- All six routes remain Host-owned: `GET /v1/customer/addresses`, `GET /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses`, `PUT /v1/customer/addresses/{addressId:guid}`, `DELETE /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses/{addressId:guid}/default`.
- No routing behavior was modified.

## 7. Host AddressBook files unchanged

`git diff -- src/backend/Host/Tooba.Host/AddressBook/` is empty. Both files are byte-identical to the parent commit:

- `AddressBookEndpoints.cs` — untouched (route behavior, `ResolveActor`, dev header, guest fallback, DTOs all intact).
- `AddressBookDevelopmentSeed.cs` — untouched.

Also untouched: `ProductWorkspaceDevelopmentBootstrap` AddressBook seed calls, the `Order.Application` guest-actor dependency, AddressBook Application/Domain/Infrastructure root structure, DB schema and migrations.

## 8. Solution inclusion

`src/backend/Tooba.slnx` received the minimum required entry next to the other AddressBook projects:

```xml
<Project Path="Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj" />
```

No other solution or project-list change was made.

## 9. Focused build results

1. `dotnet restore src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj` — succeeded (1.7 s; new project assets generated; 4 of 5 projects already up to date).
2. `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` — **Build succeeded**, 0 warnings, 0 errors.
3. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` — **Build succeeded**, **0 errors**, 11 warnings (all pre-existing: Catalog/Promotion XML-comment warnings, `Program.cs` duplicate `using`, `ProductWorkspaceComposer` nullable warning — none introduced by this task).

Focused tests: **NONE run**. No existing project-registration guard failed compilation, so no test was updated (per task instruction).

## 10. Scope statement

- **Production-Code-Scope:** exactly three touched files — new `Tooba.AddressBook.Endpoints.csproj`, new `AddressBookEndpointModule.cs`, one added line in `Program.cs`, one added line in `Tooba.slnx`. No AddressBook route, handler, validator, DTO, authorizer, schema or migration change.
- **Test-Code-Scope:** NONE — no test file added or modified.
- **Checkout-State:** unchanged; checkout still consumes `IAddressBookCheckoutLookup`, and the `Program.cs:188` checkout-lookup alias is untouched.
- **Frontend-Production-Changes:** NONE.
- **Residual-Defects:** none introduced. Pre-existing and intentionally untouched: `Tooba.AddressBook.Endpoints` consumes `Tooba.BuildingBlocks` without using any symbol yet; the `FOUNDATION_ONLY_NO_ROUTES` extensions are deliberate placeholders; AddressBook Application/CQRS/validators/capability structure and Host `AddressBook` folder still require the later slices.

## 11. Exact recommended next slice

**`TB-TMAR-ADDRESSBOOK-CQRS-REQUEST-SLICE-001`** — smallest safe follow-up, one bounded slice:

1. Add the six AddressBook CQRS requests + handlers in `Tooba.AddressBook.Application` (Commands/Queries/Errors/Models) wrapping the existing `IAddressBookDirectory` operations, preserving ownership rules in `AddressBookDirectory`.
2. Add the four/six `VALIDATOR_REQUIRED` vs `NO_VALIDATOR_REQUIRED` classification for the six endpoint-reachable requests.
3. Add the Customer endpoint file(s) in the new `Tooba.AddressBook.Endpoints` project with a Host-independent actor seam based on `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` plus a Host-registered dev/testing actor provider, using `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` for the guest value.

Still no Host deletion in that slice — Host endpoint evacuation remains the following slice so route ownership can be switched and parity-proven separately.
