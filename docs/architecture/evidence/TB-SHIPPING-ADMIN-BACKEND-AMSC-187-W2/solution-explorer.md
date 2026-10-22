# Solution Explorer (filesystem vs `Tooba.slnx`)

State: **`CANONICAL`** — verified separately from disk, unchanged by W2.

`src/backend/Tooba.slnx` lines 252–258:

```xml
<Folder Name="/Modules/Shipping/">
  <Project Path="Modules/Shipping/Tooba.Shipping.Contracts/Tooba.Shipping.Contracts.csproj" />
  <Project Path="Modules/Shipping/Tooba.Shipping.Application/Tooba.Shipping.Application.csproj" />
  <Project Path="Modules/Shipping/Tooba.Shipping.Domain/Tooba.Shipping.Domain.csproj" />
  <Project Path="Modules/Shipping/Tooba.Shipping.Infrastructure/Tooba.Shipping.Infrastructure.csproj" />
  <Project Path="Modules/Shipping/Tooba.Shipping.Endpoints/Tooba.Shipping.Endpoints.csproj" />
  <Project Path="Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj" />
</Folder>
```

Checks performed:

* All six `.csproj` files exist on disk at exactly those paths.
* Each of the six project paths occurs **exactly once** in the whole solution file (no stale or
  duplicate entry, no second group).
* No Shipping project exists under a flat/wrong group; no decorative solution folder.
* No assembly rename and no `.csproj` move was needed or performed — W2 moved one `.cs` file only.

Durable guard: `ShippingModuleStructureGuardTests.Solution_membership_is_canonical_and_unique`
asserts the group body equals the six expected paths and that each path count is 1.
