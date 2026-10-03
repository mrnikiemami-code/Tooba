# TB-TMAR-ORDER-AMC-001-W5-R1 — Validation

## Starting state

```text
Starting HEAD = e648f32e521d5a6450346643d743cd99e719ea29
origin/main   = e648f32e521d5a6450346643d743cd99e719ea29
```

## Focused validation performed

### 1. JSON parse

```text
docs/architecture/tmar-module-structure-manifests.json  → OK (modules = 22)
docs/architecture/tmar-current-state.json               → OK (orderAmc001.state = W5_CERTIFY_COMPLETE_REFERENCE_PATTERN)
```

### 2. Manifest reconciliation counts

```text
orderEntryCount    = 1
orderProjectCount  = 5
  Tooba.Order.Domain          allowlist = [GlobalUsings.cs]
  Tooba.Order.Contracts       allowlist = []
  Tooba.Order.Application     allowlist = [GlobalUsings.cs]
  Tooba.Order.Endpoints       allowlist = [OrderEndpointModule.cs]
  Tooba.Order.Infrastructure  allowlist = [GlobalUsings.cs, OrderModule.cs]
```

### 3. New durable guard + Order structure/certification guards

```text
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Debug
Passed! - Failed: 0, Passed: 143, Skipped: 0, Total: 143
```

The project previously held 139 tests. The wave adds **4** new manifest↔disk guard facts (143 = 139 + 4).
No existing guard was weakened, skipped, or removed, and no baseline was widened.

This run also covers the existing focused Order structure/certification guards, proving no structural
regression from the reconciliation:

- `OrderApplicationOrganizationGuardTests` (root allowlist + namespace alignment)
- `OrderApplicationFolderGranularityGuardTests` (no single-file Command/Query leaf, no technical-axis-first root)
- `OrderEndpointOrganizationGuardTests`, `OrderInfrastructureOrganizationGuardTests`
- `OrderAdminOperationsArchitectureGuardTests`, `OrderStorefrontArchitectureGuardTests`
- `OrderInfrastructureForeignLayerBoundaryGuardTests`
- `OrderManifestDiskReconciliationGuardTests` (new)

### 4. Build

```text
dotnet test .../Tooba.Order.Tests.csproj -c Debug   # compiles Tooba.Order.Tests
Build succeeded — 0 Error(s)
```

Build was required because the new guard is compiled into `Tooba.Order.Tests`. No full solution build was run.

### 5. Search proof

```text
Order manifest entries        = exactly 1
Order manifest project entries = exactly 5
production .cs changes (W5-R1) = ZERO
message-based classification in Modules/Order production = ZERO
```

Message-classification scan command shape (production only; `bin`/`obj`/`Migrations`/tests excluded):

```text
Select-String -Pattern 'catch \([^)]*\) when \(.*\.Message|\.Message\.StartsWith|\.Message\.Contains|\.Message =='
→ hits = 0
```

## Not run (deliberately, per task scope)

- No full repository test suite.
- No Host / Fulfillment / Payment production or test change.
- No unrelated pre-existing Host failure was repaired.

## Final state

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
```
