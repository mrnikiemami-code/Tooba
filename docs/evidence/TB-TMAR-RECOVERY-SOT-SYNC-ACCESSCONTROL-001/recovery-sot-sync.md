# Evidence — TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001

Track: RECOVERY_SOT_SYNC
Parent: TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001 (commit `4a6074e62fbaf557f57aa2770d76b8d14164dc72`)
Scope: docs-only SoT synchronization

## 1. Stale before-state

`docs/architecture/tmar-current-state.json` still described the **Fulfillment** Host evacuation as live:

| Field | Stale value |
| --- | --- |
| `lastAcceptedTask` | `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001` |
| `lastAcceptedCommit` | `552c928c9d21211698868df73f498d1ac57c0e58` |
| `lastAcceptedSoTStamp` | `552c928c9d21211698868df73f498d1ac57c0e58` |
| `lastAcceptedNote` | Fulfillment Host evacuation note |
| `nextTask` | `TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001` |
| `nextTaskGate` | `HOST_FIRST_FOLDER_BY_FOLDER_AFTER_FULFILLMENT_STRUCTURE_CERTIFICATION` |
| `currentHostEvacuation.activeModule` | `Fulfillment` |
| `currentHostEvacuation.activeTask` | `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001` |
| `currentHostEvacuation.fulfillmentStructureCertification` | `DEFERRED_UNTIL_HOST_EVACUATION_COMPLETE` |
| `currentHostEvacuation.nextHostFolderAfterFulfillment` | `AccessControl` |

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` had no section describing the accepted AccessControl
Host-evacuation progress at all (Fulfillment remained the last described evacuation).

## 2. Corrected last accepted task / commit

- `lastAcceptedTask` = `TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001`
- `lastAcceptedCommit` = `4a6074e62fbaf557f57aa2770d76b8d14164dc72`
- `lastAcceptedSoTStamp` = `4a6074e62fbaf557f57aa2770d76b8d14164dc72`
- `lastAcceptedNote` = AccessControl user-search/effective seam note (Contracts-only enrichment boundary)
- `nextTask` = `TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001`
- `nextTaskGate` = `HOST_FIRST_FOLDER_BY_FOLDER_ACCESSCONTROL_SCOPE_RESOURCES`

## 3. Corrected `currentHostEvacuation`

- `activeModule` = `AccessControl`
- `activeTask` = `TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001`
- `parentAcceptedCommit` = `4a6074e62fbaf557f57aa2770d76b8d14164dc72`
- `workflow` = `HOST_FIRST_FOLDER_BY_FOLDER`
- `accessControlState` = `IN_PROGRESS`
- `accessControlStructureCertification` = `NOT_CERTIFIED_NOT_COMPLETE_REFERENCE_PATTERN`
- `accessControlHostResidue` = `NON_ZERO`
- `accessControlModuleOwnedAccepted` = the nine accepted slices
- `accessControlContractsOnlyUserSearchBoundary` = `IActorContactLookup`, `IActorIdentifierResolver`,
  `IActorDisplayLookup` with the forbidden Application/Domain list
- `accessControlHostResidualRoutes` = the twelve scope-resource routes + `demo-preview`
- `accessControlHostResidualFiles` = `AccessControlEndpoints.cs`, `AccessControlDevelopmentSeed.cs`,
  `AccessControlDemoSnapshot.cs`
- `accessControlHostProgramResidue` = legacy Program mapping/bootstrap residue until final cleanup
- `accessControlStaleTestDebt` = the stale foundation-test assertion, classified
  `TEST_MAINTENANCE_DEBT_NOT_PRODUCTION_REGRESSION`
- `nextHostFolderAfterAccessControl` = `AddressBook`

Historical Fulfillment certification data was **not deleted** — it was preserved under
`currentHostEvacuation.historicalFulfillmentEvacuation` (evacuation COMPLETE, structure CERTIFIED).

## 4. AccessControl residue summary

`AccessControlEndpoints.cs` still owns Admin + Seller `scope-resources` (categories, brands, products,
warehouses/stores/order-segments deferred) and Admin `demo-preview`, plus the still-used helpers
`RequireSellerAsync`, `Trace`, `MapError`. No Production code was changed by this task.

## 5. Pending stale-test debt

`Tooba.Host.Tests/AccessControlFoundationTests.AccessControl_module_boundary_static_checks` asserts Host text
`/v1/admin/sellers/{sellerId:guid}/access-control` and `/me/capabilities`, both evacuated by previously accepted
tasks (AdminSeller family at `b9dda0d7`); it would already have failed at accepted parent `99d59d89`.
Recorded only; **not** repaired in this docs-only task.

## 6. Next task

`TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001` — evacuate the Admin + Seller scope-resources family with a
proper Catalog Contracts/shared-neutral seam instead of moving `ICatalogLookupGateway` from Catalog.Application
into AccessControl.

## 7. Master recovery update

Appended a concise `## TMAR Host Evacuation — Current Live State (AccessControl)` section containing: latest
accepted task + commit, current track/folder, accepted migration summary, Contracts-only user-search boundary,
residual Host routes/files, stale test debt, exact next task, honest `IN_PROGRESS` state, and the preserved
global locks (`BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`, frontend FROZEN, checkout `PAUSED_AT_SAFE_W5_CHECKPOINT`,
certified modules Order/Cart/StoreContext/Offer/Payment/Settlement/Fulfillment). Historical content was kept and
the document was not broadly rewritten.

## 8. Validation (docs-only)

- `tmar-current-state.json` parses successfully (`JSON_OK`).
- `currentHostEvacuation.activeModule` = `AccessControl` (verified).
- `accessControlState` = `IN_PROGRESS`; residue `NON_ZERO` (verified).
- `structureLock.certifiedModules` = `Order,Cart,StoreContext,Offer,Payment,Settlement,Fulfillment`;
  AccessControl **not** present (verified).
- `lastAcceptedTask` / `lastAcceptedCommit` / `nextTask` match the required exact values (verified).
- Production code changed = **NO**.
- Tests changed = **NO**.
- No build, no test run, no solution validation performed (per task instruction).
