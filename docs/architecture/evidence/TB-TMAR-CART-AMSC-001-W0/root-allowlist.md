# TB-TMAR-CART-AMSC-001-W0 — Root Allowlist

## `Root-Allowlist-State = ENFORCED` (one stale guard entry)

## Declared allowlists — `tmar-module-structure-manifests.json` (`module: "Cart"`)

```json
{
  "module": "Cart",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [
    {
      "projectName": "Tooba.Cart.Application",
      "rootAllowlist": ["GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"],
      "rootAllowlistJustification": "Both are genuinely project-wide shared imports ...",
      "forbiddenRootFiles": ["CartHandlers.cs", "CartDirectoryContracts.cs", "CartPersistenceHours.cs", "CartConversionAdapter.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Cart.Endpoints",
      "rootAllowlist": ["CartEndpointModule.cs"],
      "forbiddenRootFiles": ["CartStorefrontEndpoints.cs", "CartErrorCatalogContributor.cs", "CartErrorResources.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Cart.Infrastructure",
      "rootAllowlist": ["GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"],
      "rootAllowlistJustification": "Both are genuinely project-wide shared imports ...",
      "forbiddenRootFiles": ["CartModule.cs", "CartDirectory.cs", "CartDbContext.cs", "CartEvents.cs", "CartExpiryReconciler.cs", "CartPersistenceHoursSource.cs", "CartCredentialHasher.cs", "CartOutboxRegistration.cs"],
      "forbiddenTopLevelFolders": []
    }
  ]
}
```

## Physical root scan

| Project | Root `.cs` files on disk | Allowlist | Verdict |
| --- | --- | --- | --- |
| `Tooba.Cart.Contracts` | none | (not listed) | OK |
| `Tooba.Cart.Domain` | none | (not listed) | OK |
| `Tooba.Cart.Application` | `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs` | same 2 | **ENFORCED** |
| `Tooba.Cart.Infrastructure` | `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs` | same 2 | **ENFORCED** |
| `Tooba.Cart.Endpoints` | `CartEndpointModule.cs` | same 1 | **ENFORCED** |

No forbidden root file is present. No root dumping.

## Manifest completeness gaps (W2/W3 obligations)

### A. `Tooba.Cart.Contracts` and `Tooba.Cart.Domain` have no manifest entry at all

The manifest lists only 3 of the 5 production projects. `Tooba.Cart.Contracts` and
`Tooba.Cart.Domain` are unguarded by the manifest — a certified module should have an explicit entry
for every production project. `AddressBook`'s manifest entry covers all 5.

**W2/W3 obligation:** add entries for `Tooba.Cart.Contracts` and `Tooba.Cart.Domain` with their real
root allowlists (empty) and forbidden root files.

### B. `forbiddenTopLevelFolders` is empty for every Cart project

`Tooba.Cart.Application.forbiddenTopLevelFolders = []` while the project currently has
`Commands` and `Queries` as top-level technical axes (G5). The manifest therefore does **not**
lock the capability-first shape that W2 will establish.

**W2/W3 obligation:** set `forbiddenTopLevelFolders` for `Tooba.Cart.Application` to
`["Commands", "Queries", "Models", "Ports"]` after W2 establishes `Application/Cart/{Commands,Queries,Validators}`.
`Ports` becomes forbidden as a top-level folder only if W2 moves it under the capability; if W2 keeps
a shared `Ports/` (as `AddressBook` does), it must be excluded from the forbidden list and recorded
in the allowlist justification instead.

### C. `Tooba.Cart.Tests` has no manifest entry

`AddressBook` also omits its test project. **No action** — consistent with the repository convention.

## Stale durable guard (F13 / G8)

`Tooba.Cart.Tests/Architecture/CartArchitectureGuardTests.cs` line 13:

```csharp
private static readonly string[] AllowedContractsFolders = ["Checkout", "Presentation"];
```

`Contracts/Lifetime/` exists on disk but is not in the array, so
`AssertNoRootDump("Tooba.Cart.Contracts", AllowedContractsFolders)` fails:

```text
Assert.Contains() Failure: Item not found in collection
Collection: ["Checkout", "Presentation"]
Not found:  "Lifetime"
   at CartArchitectureGuardTests.AssertNoRootDump(...) line 296
   at CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean() line 49
```

This is **guard staleness**, not an architecture regression: `Lifetime/` is a legitimate capability
folder and its file's namespace (`Tooba.Cart.Contracts.Lifetime`) already passes
`AssertNamespacesAlign`.

**W1/W2 obligation:** repair the allowlist to its documented intent, keeping the Contracts
assertions honest. Do not delete the assertion or weaken the guard.

## Other guard arrays at HEAD (for W2's update)

```csharp
AllowedDomainFolders         = ["Aggregates", "Entities", "ValueObjects", "Events"]
AllowedApplicationFolders    = ["Ports", "Lifetime", "Conversion", "Commands", "Queries", "Models", "Errors", "Presentation", "Validation"]
AllowedContractsFolders      = ["Checkout", "Presentation"]                    // <-- missing "Lifetime"
AllowedInfrastructureFolders = ["Persistence", "Directories", "Messaging", "DependencyInjection", "Events", "Security", "Migrations", "Lifetime"]
AllowedEndpointsFolders      = ["Storefront", "Errors", "Resources"]
```

After W2 the Application array must become capability-first, e.g.
`["Cart", "Composition", "Conversion", "Errors", "Lifetime", "Ports", "Presentation", "Validation"]`
with `Models` removed (tombstone deleted) and `Commands`/`Queries` removed from the **root** set.

## Host root allowlist interaction

No Cart production file exists under `src/backend/Host/Tooba.Host/**`. The Host allowlist is
unaffected. `HOST_FINAL_CLOSURE_REGRESSION = NONE`.
