# TB-TMAR-CART-AMSC-001-W0 — Solution Explorer

## `Solution-Explorer-State = CANONICAL`

`src/backend/Tooba.slnx` groups every Cart project under the canonical Solution Folder
`/Modules/Cart/`:

```xml
  <Folder Name="/Modules/Cart/">
    <Project Path="Modules/Cart/Tooba.Cart.Domain/Tooba.Cart.Domain.csproj" />
    <Project Path="Modules/Cart/Tooba.Cart.Contracts/Tooba.Cart.Contracts.csproj" />
    <Project Path="Modules/Cart/Tooba.Cart.Application/Tooba.Cart.Application.csproj" />
    <Project Path="Modules/Cart/Tooba.Cart.Infrastructure/Tooba.Cart.Infrastructure.csproj" />
    <Project Path="Modules/Cart/Tooba.Cart.Endpoints/Tooba.Cart.Endpoints.csproj" />
    <Project Path="Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj" />
  </Folder>
```

## Verification against disk

| Project on disk | In `.slnx` | Solution Folder |
| --- | --- | --- |
| `Modules/Cart/Tooba.Cart.Contracts/Tooba.Cart.Contracts.csproj` | YES | `/Modules/Cart/` |
| `Modules/Cart/Tooba.Cart.Domain/Tooba.Cart.Domain.csproj` | YES | `/Modules/Cart/` |
| `Modules/Cart/Tooba.Cart.Application/Tooba.Cart.Application.csproj` | YES | `/Modules/Cart/` |
| `Modules/Cart/Tooba.Cart.Infrastructure/Tooba.Cart.Infrastructure.csproj` | YES | `/Modules/Cart/` |
| `Modules/Cart/Tooba.Cart.Endpoints/Tooba.Cart.Endpoints.csproj` | YES | `/Modules/Cart/` |
| `Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj` | YES | `/Modules/Cart/` |

- No Cart project is missing from the solution.
- No stale Cart project entry points at a non-existent path.
- No Cart project is placed under a flat or foreign Solution Folder.
- `Tooba.Cart.Endpoints` **is** present (Structure skill §17 explicitly requires this).

## Project-path / assembly-name preservation

| Project | Path | AssemblyName | RootNamespace | Change in W1–W3 |
| --- | --- | --- | --- | --- |
| `Tooba.Cart.Contracts` | `Modules/Cart/Tooba.Cart.Contracts/` | `Tooba.Cart.Contracts` | `Tooba.Cart.Contracts` | NONE |
| `Tooba.Cart.Domain` | `Modules/Cart/Tooba.Cart.Domain/` | `Tooba.Cart.Domain` | `Tooba.Cart.Domain` | NONE |
| `Tooba.Cart.Application` | `Modules/Cart/Tooba.Cart.Application/` | `Tooba.Cart.Application` | `Tooba.Cart.Application` | NONE |
| `Tooba.Cart.Infrastructure` | `Modules/Cart/Tooba.Cart.Infrastructure/` | `Tooba.Cart.Infrastructure` | `Tooba.Cart.Infrastructure` | NONE |
| `Tooba.Cart.Endpoints` | `Modules/Cart/Tooba.Cart.Endpoints/` | `Tooba.Cart.Endpoints` | `Tooba.Cart.Endpoints` | NONE |
| `Tooba.Cart.Tests` | `Modules/Cart/Tooba.Cart.Tests/` | `Tooba.Cart.Tests` | `Tooba.Cart.Tests` | NONE |

No assembly rename, no `.csproj` relocation, no decorative Solution Folder. Physical moves inside
W1–W3 stay **within** each project directory, so the `.slnx` requires **no edit** in any wave.

## Visual Studio folder rendering after W2

Because W2 moves files only inside existing project directories, Solution Explorer will re-render the
Application project as:

```text
Tooba.Cart.Application
  Cart
    Commands
    Queries
    Validators
  Composition
  Conversion
  Errors
  Lifetime
  Ports
  Presentation
  Validation
  GlobalUsings.Domain.cs
  GlobalUsings.Layout.cs
```

and the Infrastructure project as:

```text
Tooba.Cart.Infrastructure
  DependencyInjection
  Directories
  Events
  Lifetime
  Messaging
  Persistence
    Migrations
  Security
  GlobalUsings.Domain.cs
  GlobalUsings.Layout.cs
```

Both are the professional, capability-first rendering required by the Architect.

## W2 obligation

Re-verify this file after the moves (the guard `CartArchitectureGuardTests` asserts physical layout;
`.slnx` grouping must be re-checked separately per Structure skill §17). No `.slnx` change is
expected.
