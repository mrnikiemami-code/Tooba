# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — manifest-sot

## Manifest state

`docs/architecture/tmar-module-structure-manifests.json` — module `AddressBook`:

| Field | Value |
| --- | --- |
| `module` | `AddressBook` |
| `structureCertified` | `true` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `certificationNote` | initial certification + Offer-style realign + post-realign repair + `AMSC-001 W1` (migrate) + `AMSC-001 W2` (structure) dispositions |
| projects | 5 |
| certified entries for `AddressBook` | **exactly 1** |

### Per-project allowlists (final certified state)

| Project | `rootAllowlist` | `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | `[]` | `CustomerAddressContracts.cs` | `[]` |
| `Tooba.AddressBook.Domain` | `[]` | `[]` | `[]` |
| `Tooba.AddressBook.Application` | `[]` | `AddressBookContracts.cs` | `[]` |
| `Tooba.AddressBook.Endpoints` | `["AddressBookEndpointModule.cs"]` | 3 capability endpoint files | `[]` |
| `Tooba.AddressBook.Infrastructure` | `[]` | `AddressBookModule.cs`, `AddressBookDirectory.cs` | `["Migrations"]` |

### Manifest correctness corrections made by AMSC

| Wave | Change | Nature |
| --- | --- | --- |
| W1 | `certificationNote` += W1 disposition; `Tooba.AddressBook.Domain.rootAllowlistJustification` records the new Domain→Contracts reference | honest record |
| W2 | `certificationNote` += W2 disposition; **`Tooba.AddressBook.Application.rootAllowlistJustification` corrected** — it previously documented `Commands/<UseCase>` / `Queries/<UseCase>` as the accepted shape, which was the certification drift itself | honest record (drift repair) |

No allowlist **value** was changed; only prose. `structureCertified` was never flipped by W2 (skill section
23 — Structure prepares, Certify verdicts).

### Pre-cert duplicate

| Check | Result |
| --- | --- |
| `AddressBook` present in `preCertModules` | NO (only `ProductWorkspace` legitimately remains there, uncertified) |
| duplicate certified `AddressBook` entry | NONE |

Asserted by `AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry`
and `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_manifest_entry_is_single_and_disk_reconciled`.

## SoT state

`docs/architecture/tmar-current-state.json`:

| Field | Value |
| --- | --- |
| `currentHostCheckpoint` | `HOST_ROOT_FINAL_CERTIFIED` (preserved) |
| `structureLock.version` | `ARCH-COMPLETE-002` (unchanged) |
| `structureLock.certifiedModules` | unchanged — `AddressBook` was already present |
| `addressBookModuleAmsc001W0` | present (Analyze) |
| `addressBookModuleAmsc001W1` | present (Migrate) |
| `addressBookModuleAmsc001W2` | present (Structure) |
| `addressBookModuleAmsc001W3` | present (Certify) |

### W3 record highlights

| Field | Value |
| --- | --- |
| `state` | `ADDRESSBOOK_AMSC_001_CERTIFIED` |
| `verdict` | `COMPLETE_REFERENCE_PATTERN` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `structureCertified` | `true` |
| `structureState` | `READY_FOR_CERTIFY` (consumed from the W2 gate) |
| `folderGranularityState` / `solutionExplorerState` / `pathNamespaceState` / `physicalCopyState` / `rootAllowlistState` | `PROFESSIONAL_SHALLOW` / `CANONICAL` / `EXACT` / `CLEAN` / `ENFORCED` |
| `technicalAxisFirstState` / `singleFileRequestLeafState` / `rootDumpState` | `ZERO` / `ZERO` / `ZERO` |
| `aliasWorkaround` | `NONE` |
| `endpointOwnershipState` / `hostHttpOwnershipState` | `MODULE_OWNED` / `ZERO` |
| `endpointReachableRequests` / `requestHandlerPairs` | `6` / `6` |
| `validatorCoverageState` | `EXHAUSTIVE_5_REQUIRED_1_NO_VALIDATOR_REQUIRED` |
| `apiResultPatternState` / `rawResultsState` | `CANONICAL` / `ZERO` |
| `localizationState` / `hardcodedTextState` | `CANONICAL` / `ZERO` |
| `errorCodeDescriptorOwnership` | `UNIQUE_OWNER_11_CODES_PLUS_SHARED_SESSION_REQUIRED_CONSUMED_NOT_REGISTERED` |
| `loggingState` / `sensitiveLoggingState` | `CANONICAL` / `NONE` |
| `openTelemetryState` / `correlationTraceState` | `CANONICAL` / `CANONICAL` |
| `crossModuleBoundaryState` / `crossModuleJoinState` | `CONTRACTS_ONLY` / `ZERO` |
| `foreignAppInfraDomainCoupling` | `ZERO` |
| `persistenceOwnershipState` / `schemaMigrationState` | `CORRECT` / `UNCHANGED` |
| `behaviorChange` / `schemaChange` / `routesChanged` / `statusCodesChanged` / `errorCodesChanged` / `dtoShapeChanged` | `NONE` × 6 |
| `productionCsFiles` / `productionProjects` | `32` / `5` |
| `solutionFolder` / `solutionProjectEntries` | `/Modules/AddressBook/` / `5` |
| `manifestEntryCount` / `manifestDiskReconciliation` | `1` / `EXACT` |
| `microserviceExtractable` | `true` |
| `blockingResidualDebt` | `ZERO` |
| `guardsWeakened` / `baselinesWidened` | `NONE` / `NONE` |
| `workflowStop` | `USER_REVIEW_ADDRESSBOOK_AMSC_001` |
| `automaticNextImplementationTask` | `NONE` |

### Unrelated history preserved

No pre-existing SoT entry was rewritten or deleted. The `Catalog`/`structureLock` divergence that causes the
pre-existing `TmarDurableGuardTests` / `TmarCompleteReferenceStructureGateTests` failures was **not**
"fixed" as part of this AddressBook task — it is out of scope and recorded as residual watch R5.

Both JSON files parse cleanly (`JSON.parse` verified for the SoT; the manifest is parsed by
`TmarCompleteReferenceStructureGateTests` and by the W3 cert guard).
