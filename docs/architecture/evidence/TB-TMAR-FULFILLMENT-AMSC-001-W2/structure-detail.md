# TB-TMAR-FULFILLMENT-AMSC-001 — W2 solution-explorer / path-namespace / root-allowlist

## Solution-Explorer-State: `CANONICAL`

`src/backend/Tooba.slnx` groups all six Fulfillment projects under a single
`/Modules/Fulfillment/` Solution Folder — verified unchanged and consistent with disk:

```xml
<Folder Name="/Modules/Fulfillment/">
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Domain/...csproj" />
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Contracts/...csproj" />
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Application/...csproj" />
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/...csproj" />
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Endpoints/...csproj" />
  <Project Path="Modules/Fulfillment/Tooba.Fulfillment.Tests/...csproj" />
</Folder>
```

No assembly rename, no `.csproj` move, no decorative folder.

## Path-Namespace-State: `EXACT`

Every production `.cs` namespace equals the full path-derived namespace. Examples:

| Path | Namespace |
| --- | --- |
| `Application/Shipping/Commands/CreateShippingServiceCommand.cs` | `Tooba.Fulfillment.Application.Shipping.Commands` |
| `Application/Shipping/Ports/IShippingServiceDirectory.cs` | `Tooba.Fulfillment.Application.Shipping.Ports` |
| `Application/Fulfillments/Models/FulfillmentSnapshot.cs` | `Tooba.Fulfillment.Application.Fulfillments.Models` |
| `Application/WorkQueue/Models/AdminFulfillmentWorkQueueModels.cs` | `Tooba.Fulfillment.Application.WorkQueue.Models` |
| `Application/Checkout/Validators/ListCustomerCheckoutFulfillmentsQueryValidator.cs` | `Tooba.Fulfillment.Application.Checkout.Validators` |
| `Application/Validators/FulfillmentFluentRules.cs` | `Tooba.Fulfillment.Application.Validators` |

This is the Cart/AddressBook canonical style (full path), not a flattened
`...Application.<Capability>` namespace. `AssertNamespacesAlign` in the module guard passes
with the **exact** (not prefix) comparison.

No namespace alias workaround was introduced. No `TypeForwardedTo`.

## Root-Allowlist-State: `ENFORCED`

| Project | Root `.cs` | Manifest |
| --- | --- | --- |
| `Tooba.Fulfillment.Domain` | none | `rootAllowlist: []` |
| `Tooba.Fulfillment.Contracts` | none | `rootAllowlist: []` |
| `Tooba.Fulfillment.Application` | none | `rootAllowlist: []` |
| `Tooba.Fulfillment.Infrastructure` | none | `rootAllowlist: []` |
| `Tooba.Fulfillment.Endpoints` | `FulfillmentEndpointModule.cs` | `rootAllowlist: [FulfillmentEndpointModule.cs]` |

`tmar-module-structure-manifests.json` was tightened for this module:

- `Tooba.Fulfillment.Application.forbiddenTopLevelFolders`:
  `[]` → `["Commands", "Queries", "Models", "Ports"]` — the technical axes can never return
  to the Application root.
- `Tooba.Fulfillment.Application.rootAllowlistJustification` rewritten to describe the
  capability-first tree honestly (it previously documented `Commands/Queries/...` as
  accepted, which *was* the drift).
- `Tooba.Fulfillment.Infrastructure.rootAllowlistJustification` extended to record the
  cohesive-partial split of `FulfillmentDirectory.cs`.

`structureCertified` was **not** flipped — that remains the Certify (W3) verdict.

## Host final closure

Preserved. No Host production folder or file was added or widened:
`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and `HOST_ROOT_FINAL_CERTIFIED` untouched.
The only Host-side edits are `using` directives and one fully-qualified type reference in
existing **test** files plus the existing `Program.cs` CQRS assembly marker.
