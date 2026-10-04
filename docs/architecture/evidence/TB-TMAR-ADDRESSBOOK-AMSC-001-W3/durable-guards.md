# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — durable-guards

## Guards protecting the certified surface

| Guard | Tests | Status |
| --- | --- | --- |
| `AddressBookModuleAmsc001W3CertGuardTests` (NEW in W3, +1 in W3-R1) | 11 | PASS |
| `AddressBookPhysicalStructureGuardTests` (+2 in W2) | 6 | PASS |
| `AddressBookValidatorCoverageGuardTests` (F8 repaired in W1) | 6 | PASS |
| `AddressBookCanonicalPresentationGuardTests` | 3 | PASS |
| `ErrorCatalogUniqueCodeGuardTests` | 3 | PASS |
| `AddressBookFoundationTests` | (module behaviour) | PASS |
| `StorefrontRecipientCanonicalizationTests` | (cross-module consumer) | PASS |

## New in W3 — `AddressBookModuleAmsc001W3CertGuardTests`

| Test | Locks |
| --- | --- |
| `AddressBook_certification_state_is_recorded_honestly_in_sot` | SoT verdict `ADDRESSBOOK_AMSC_001_CERTIFIED` / `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002`, `structureCertified`, `microserviceExtractable`, `blockingResidualDebt = ZERO`, `guardsWeakened = NONE`, `baselinesWidened = NONE`, `schemaChange = NONE`, the structure states, `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, and the presence of all four AMSC SoT records |
| `AddressBook_manifest_entry_is_single_and_disk_reconciled` | exactly one certified manifest entry; the Application `rootAllowlistJustification` describes the capability-first truth and explicitly records the W2 removal; manifest project list == disk |
| `AddressBook_owns_exactly_one_error_descriptor_per_owned_code` | 13 distinct code constants; every locally-owned constant registered; `SessionRequired` **not** re-registered; no `Contains(`/`StartsWith(` heuristics; no other contributor registers an AddressBook code |
| `AddressBook_error_codes_resolve_to_localized_resources_in_both_cultures` | every owned code present in both `AddressBookErrors.resx` and `AddressBookErrors.fa.resx`; `IErrorResourceSet` + `IErrorCatalogContributor` registered in the endpoint module |
| `AddressBook_endpoints_use_only_the_canonical_result_factory` | no `Results.Json`/`NoContent`/`BadRequest`/`Problem`, no `new ProblemDetails`, no manual `StatusCodes.Status201Created`, no `Accept-Language`, no Domain/Infrastructure import, no prose-based failure classification; `api.From(` present on endpoint files |
| `AddressBook_has_no_raw_error_code_literals_ad_hoc_logging_or_typed_fault_residue` | no raw `customer.address.*` / `customer.session.*` literals outside the declared code owners; no `Console.Write`/`Debug.Write`; no `ActivitySource.StartActivity`/`traceparent`; no `exception.Message`; `CustomerAddress.cs` + `AddressBookDirectory.cs` contain `SemanticException` and zero `InvalidOperationException` |
| `AddressBook_has_no_foreign_module_project_or_namespace_dependency` | 18 forbidden foreign Application/Domain/Infrastructure references absent from csproj **and** usings; the one legal foreign edge positively asserted |
| `AddressBook_owns_its_http_surface_with_zero_host_http_ownership` | endpoint module present, capability `*Endpoints.cs` files absent from the Endpoints root, `Customer/`+`Errors/`+`Resources/` present, **exactly 6** routes, `Host/Tooba.Host/AddressBook/` absent |
| `AddressBook_schema_and_migrations_are_unchanged` | exact migration file set |
| `AddressBook_application_is_capability_first_with_no_single_file_use_case_leaves` | `Application/Commands` + `Application/Queries` absent; `Addresses/{Commands,Queries,Validators}` present; all 11 legacy leaf folders absent |
| `AddressBook_certification_truth_records_the_accepted_w1_bounded_defect_repair` (NEW in W3-R1) | cross-record lock: the W3 record cannot regress to `behaviorChange = NONE` / `statusCodesChanged = NONE` while `addressBookModuleAmsc001W1` still records the accepted `500 -> 404/400` bounded defect repair; unaffected axes (`schema`/`routes`/`errorCodes`/`dtoShape`) stay `NONE`; verdict/`structureCertified`/`certificationReconciliation = W3_R1_BEHAVIOR_TRUTH_EXACT`/`workflowStop`/`automaticNextImplementationTask = NONE`; the `addressBookModuleAmsc001W3R1` checkpoint records `ELEVEN` new / `TWELVE` owned / `CONSUMED_NOT_OWNED` session / `ZERO` duplicate ownership / `ZERO` production & migration delta |

## Guards added across the AMSC run

| Wave | Guard change | Direction |
| --- | --- | --- |
| W1 | `AddressBookPhysicalStructureGuardTests.AllowedApplicationFolders += Composition` | legitimate folder addition (canonical seam folder used by 14 sibling modules) — not a weakening |
| W1 | `AddressBookValidatorCoverageGuardTests` F8 assertion narrowed to its documented intent | **repaired** an over-broad stale assertion (W0 F8); the AddressBook assertions were not weakened |
| W1 | `AddressBookValidatorCoverageGuardTests` + `AddressBookPhysicalStructureGuardTests` new path expectations | updated to the canonical paths |
| W2 | 2 new tests in `AddressBookPhysicalStructureGuardTests` (capability-first root, no single-file use-case leaves) | **strengthened** |
| W2 | `AllowedApplicationFolders` re-pointed to capability-first | **strengthened** |
| W2 | regex leaf guard consolidated from the validator-coverage guard into the physical-structure guard | single ownership; no coverage lost |
| W3 | 10 new tests in `AddressBookModuleAmsc001W3CertGuardTests` | **strengthened** |
| W3-R1 | 1 new test in `AddressBookModuleAmsc001W3CertGuardTests` (`AddressBook_certification_truth_records_the_accepted_w1_bounded_defect_repair`) | **strengthened** — locks the reconciled behavior/status-code truth so the certification cannot regress to `NONE` |

## Guards weakened

```text
NONE
```

## Baselines widened

```text
NONE
```

No entry was added to `Baselines/tmar-source-size-baseline.json`; no `rootAllowlist` was widened; no
`forbiddenRootFiles` / `forbiddenTopLevelFolders` entry was removed; no exemption was invented.

## No open-ended repair loop

W3 encountered three first-run failures in its own new guard, each with one deterministic local cause, each
repaired with **one bounded edit** and re-run:

1. `Assert.Equal(12, constants.Count)` → the module owns 13 constants (12 + the shared
   `customer.session.required`); the count was corrected, the invariant was not weakened.
2. An over-broad `InvalidOperationException` scan flagged the **legitimate** `AddressBookValidationCodes`
   owner file; the scan was scoped to the two files that actually produce user-facing faults (plus a
   positive assertion that both use `SemanticException`).
3. A manifest-prose assertion forbade the literal `Commands/<UseCase>` even inside the sentence that
   records its **removal**; replaced with assertions on the positive capability-first text.

No required guard was disabled, skipped or relaxed to reach PASS.
