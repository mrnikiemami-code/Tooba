# TB-TMAR-ORDER-AMC-001-W5-R1 — Manifest ↔ disk reconciliation

## Defect being repaired

The Order entry in `docs/architecture/tmar-module-structure-manifests.json` described only **3** of the **5**
certified production projects and carried stale root allowlists. W5's certification evidence therefore claimed
"exact root allowlists" that were not yet durable in the manifest.

W5 production implementation is **ARCHITECT-ACCEPTED** and was **not** reopened. R1 is certification-package,
documentation and guard reconciliation only.

## 1. Exactly one Order manifest entry

```text
orderEntryCount = 1
```

No duplicate Order module entry exists.

## 2. Exactly five production project entries

Before (W4/W5) → After (W5-R1):

| # | Before | After |
|---|---|---|
| 1 | `Tooba.Order.Application` | `Tooba.Order.Domain` |
| 2 | `Tooba.Order.Endpoints` | `Tooba.Order.Contracts` |
| 3 | `Tooba.Order.Infrastructure` | `Tooba.Order.Application` |
| 4 | *(missing)* | `Tooba.Order.Endpoints` |
| 5 | *(missing)* | `Tooba.Order.Infrastructure` |

```text
orderProjectCount = 5
```

The certified production surface is the five Order production projects. `Tooba.Order.Tests` is a test project
and is correctly excluded.

## 3. Expected vs actual root `.cs` equality (all five)

Measured directly from disk at
`src/backend/Modules/Order/<project>/*.cs` (`SearchOption.TopDirectoryOnly`):

| Project | Disk root `.cs` (actual) | Manifest `rootAllowlist` (declared) | State |
|---|---|---|---|
| `Tooba.Order.Domain` | `GlobalUsings.cs` | `["GlobalUsings.cs"]` | `EXACT` |
| `Tooba.Order.Contracts` | *(none)* | `[]` | `EXACT` |
| `Tooba.Order.Application` | `GlobalUsings.cs` | `["GlobalUsings.cs"]` | `EXACT` |
| `Tooba.Order.Endpoints` | `OrderEndpointModule.cs` | `["OrderEndpointModule.cs"]` | `EXACT` |
| `Tooba.Order.Infrastructure` | `GlobalUsings.cs`, `OrderModule.cs` | `["GlobalUsings.cs", "OrderModule.cs"]` | `EXACT` |

Corrections applied:

- `Tooba.Order.Application.rootAllowlist`: `[]` → `["GlobalUsings.cs"]` (was the defect W5 flagged).
- `Tooba.Order.Infrastructure.rootAllowlist`: `["OrderModule.cs"]` → `["GlobalUsings.cs", "OrderModule.cs"]`.
- `Tooba.Order.Domain` added with `rootAllowlist: ["GlobalUsings.cs"]`.
- `Tooba.Order.Contracts` added with `rootAllowlist: []`.

### Newly represented Domain / Contracts locks

Only minimal, truthful structure locks derived from current disk and the W4 structure evidence were added.

- `Tooba.Order.Domain` → `forbiddenRootFiles` lists the capability files that must **not** return to the Domain
  root (they live under `Aggregates/`, `Checkout/`, `Checkout/Abuse/`, `Enums/`, `Events/`, `PendingPayment/`,
  `Reservation/`, `Rules/`). `forbiddenTopLevelFolders: []` — every present top-level folder is a legitimate
  capability axis, and W3's de-godding did **not** create any forbidden sink folder, so none was invented.
- `Tooba.Order.Contracts` → `forbiddenRootFiles: []` and `forbiddenTopLevelFolders: []` because the Contracts
  root is genuinely empty (all contracts live under capability folders: `Admin/`, `Customer/`, `Fulfillment/`,
  `Notifications/`, `Payments/`, `PurchaseVerification/`, `Reservation/`, `Returns/`, `Storefront/`).

No cosmetic restriction unrelated to the certified surface was invented.

## 4. Preserved fields

| Field | Value |
|---|---|
| `structureCertified` | `true` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `Tooba.Order.Application.forbiddenRootFiles` | preserved (12 entries) |
| `Tooba.Order.Endpoints.forbiddenRootFiles` | preserved (9 entries) |
| `Tooba.Order.Infrastructure.forbiddenRootFiles` | preserved (16 entries) |
| `Tooba.Order.Infrastructure.forbiddenTopLevelFolders` | preserved (`CheckoutAbuse`, `Payments`, `Fulfillment`) |

No existing restriction was weakened or removed.

## 5. W5 production files unchanged

```text
production .cs changes in W5-R1 = ZERO
```

`git status` reports no modified/added/deleted production `.cs` file under `Modules/Order` (nor under
Fulfillment/Payment/Host production). The only new file in this wave is the Order test-project guard.

## 6. Durable guard

New: `src/backend/Modules/Order/Tooba.Order.Tests/Architecture/OrderManifestDiskReconciliationGuardTests.cs`

| Fact | Fails when |
|---|---|
| `Order_manifest_has_exactly_one_entry_certified_under_arch_complete_002` | Order entry count ≠ 1, `structureCertified != true`, or `lockVersion != ARCH-COMPLETE-002` |
| `Order_manifest_represents_exactly_the_five_production_projects` | a production project is missing from the manifest, an extra one is represented, or the declared set ≠ the real on-disk `Tooba.Order.*` directories (excluding `*.Tests`) |
| `Order_manifest_root_allowlists_equal_real_disk_root_cs_files` | any project's `rootAllowlist` ≠ the actual top-level production `.cs` files in that project directory |
| `Order_production_surface_has_no_message_text_failure_classification` | production Order code reintroduces `catch (...) when (...Message...)`, `.Message.StartsWith` or `.Message.Contains` |

The guard reads the **real** manifest JSON and the **real** project directories. It contains no hard-coded PASS
independent of disk state, and it is defence-in-depth for the W5 message-classification closure.

## 7. Certification reassertion rationale

| Gate | State | Why |
|---|---|---|
| W5 typed-fault implementation | `PRESERVED` | zero production change in R1; W5 remains the accepted implementation commit `e648f32e` |
| `MANIFEST_DISK_EXACT` | `ACHIEVED` | one entry, five projects, all five allowlists equal disk |
| `messageClassificationState` | `ZERO` | re-proved by scan and by the new durable guard |
| `foreignAppInfraDomainCoupling` | `ZERO` | unchanged by a docs/manifest/test-guard-only wave |
| `Structure-State` | `READY_FOR_CERTIFY` | no file added/moved/renamed on the production surface |
| `Host final closure` | `PRESERVED` | zero Host production change |
| `microserviceExtractable` | `true` | Contracts-only boundary and typed contract faults unchanged |

Therefore the final verdict is reasserted:

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
```
