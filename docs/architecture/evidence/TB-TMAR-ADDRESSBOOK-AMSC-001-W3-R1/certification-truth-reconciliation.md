# TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1 — certification truth reconciliation

## Scope

```text
Mode: CERTIFICATION_TRUTH_RECONCILIATION
Parent: TB-TMAR-ADDRESSBOOK-AMSC-001-W3
Starting HEAD: 3312a436c05e96120bb6dc686ae545b5857e029a  (== origin/main)
```

Documentation / SoT / guard truth only. **No production source, no migration, no manifest structure change.**

## The defect (Architect verdict)

AddressBook production implementation and structure are ACCEPTED (W1/W2 not reopened). W1 correctly recorded
the accepted bounded expected-failure repair:

| Scenario | W1 recorded |
| --- | --- |
| missing / foreign address | unexpected `500 platform.unexpected` → catalogued `404 customer.address.missing` |
| actor / escaped field-shape faults | unexpected `500` → catalogued `400 customer.address.*` |

But the W3 certification record contradicted it:

| Field | Stale W3 value | Truth |
| --- | --- | --- |
| `behaviorChange` | `NONE` | bounded expected-failure repair inherited from W1 |
| `statusCodesChanged` | `NONE` | externally observable failure statuses remapped `500 → 404/400` |

That contradiction was the only remaining certification-truth defect.

## A. Behavior truth — corrected

`docs/architecture/tmar-current-state.json` → `addressBookModuleAmsc001W3`:

```text
behaviorChange        = NONE
                    -> BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING
behaviorChangeDetail  = inherited from W1 and NOT revoked by certification:
                        success behavior/payload preserved; routes preserved;
                        DTO shapes preserved; schema preserved;
                        only expected-failure semantics intentionally changed
                        (missing/foreign address -> catalogued 404, actor/field-shape -> catalogued 400)
```

`BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING` is the task-preferred vocabulary. It is consistent with
the repository's existing W1 wording (`BOUNDED_DEFECT_REPAIR: … 500 platform.unexpected -> 404 …`), which is
left unchanged as historical fact.

## B. Status-code truth — corrected

```text
statusCodesChanged        = NONE
                        -> BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_400
statusCodesChangedDetail  = externally observable failure status only:
                            unexpected 500 platform.unexpected -> catalogued 404 customer.address.missing
                            and 400 customer.address.*;
                            success status codes unchanged
```

Unaffected axes remain `NONE` (so the bounded claim cannot silently widen):

```text
routesChanged     = NONE
dtoShapeChanged   = NONE
errorCodesChanged = NONE
schemaChange      = NONE
```

## C. Code / descriptor count truth — re-discovered, not assumed

Re-discovered from repository files at `3312a436` before any edit:

| Source | Observation |
| --- | --- |
| `Tooba.AddressBook.Contracts/Errors/AddressBookErrorCodes.cs` | **13** constants, 13 distinct values |
| `AddressBookErrorCatalogContributor.cs` | registers exactly **12** codes through their constants |
| `AddressBookErrors.resx` / `.fa.resx` | **12** keys each |
| `git show 3256fc7a:…/AddressBookErrorCodes.cs` (pre-W1) | **2** constants: `customer.address.missing`, `customer.session.required` |
| `git show 3256fc7a:…/AddressBookErrors.resx` | **1** key: `customer.address.missing` |

Reconciled truth:

```text
new AddressBook-owned stable codes added in W1        = 11
pre-existing AddressBook-owned code retained           =  1  (customer.address.missing)
total AddressBook-owned catalog/resource codes after W1 = 12
customer.session.required                              = shared / Foundation-owned,
                                                         CONSUMED but NOT registered by AddressBook
duplicate descriptor ownership                         = ZERO
13th AddressBook-owned descriptor                      = does not exist (never claimed)
```

The existing W3 field
`errorCodeDescriptorOwnership = UNIQUE_OWNER_11_CODES_PLUS_SHARED_SESSION_REQUIRED_CONSUMED_NOT_REGISTERED`
is **already correct** and was not rewritten. W3's own guard already asserts `12` owned codes registered and
`customer.session.required` deliberately absent.

## D. Certification remains valid

The accepted defect repair changed *expected-failure* semantics only; it does not invalidate certification.
Preserved and re-asserted:

| Field | Value |
| --- | --- |
| `verdict` | `COMPLETE_REFERENCE_PATTERN` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `structureCertified` | `true` |
| `structureState` | `READY_FOR_CERTIFY` |
| `manifestDiskReconciliation` | `EXACT` |
| `foreignAppInfraDomainCoupling` | `ZERO` |
| `crossModuleBoundaryState` | `CONTRACTS_ONLY` |
| `microserviceExtractable` | `true` |
| `blockingResidualDebt` | `ZERO` |
| `guardsWeakened` / `baselinesWidened` | `NONE` / `NONE` |
| `automaticNextImplementationTask` | `NONE` |

Minimal reconciliation metadata added to the W3 record:

```text
certificationReconciliation = W3_R1_BEHAVIOR_TRUTH_EXACT
evidenceW3R1                = docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/
workflowStop                = USER_REVIEW_ADDRESSBOOK_AMSC_001_W3_R1
```

A dedicated `addressBookModuleAmsc001W3R1` checkpoint record was appended (nothing pre-existing was
rewritten or deleted), carrying `behaviorChangeTruthState = BOUNDED_DEFECT_REPAIR_RECORDED`,
`statusCodeTruthState = BOUNDED_500_TO_404_400_RECORDED`, the count truth, `productionCodeChangeState = ZERO`,
`schemaMigrationChangeState = ZERO`, `manifestStructureChangeState = DOCUMENTATION_ONLY` and
`finalCertificationState = COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED`.

## E. Durable guard

Extended the existing focused `AddressBookModuleAmsc001W3CertGuardTests` (no new broad machinery, no new
test project) with one test:

```text
AddressBook_certification_truth_records_the_accepted_w1_bounded_defect_repair
```

It reads **both** SoT records from the same document, so it is a real cross-record consistency lock:

- `addressBookModuleAmsc001W1.behaviorChange` must still start with `BOUNDED_DEFECT_REPAIR` and mention
  `500`, `404` and `400`;
- `addressBookModuleAmsc001W3.behaviorChange` must equal `BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING`
  and `statusCodesChanged` must equal `BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_400`;
- `schemaChange` / `routesChanged` / `errorCodesChanged` / `dtoShapeChanged` must stay `NONE`;
- the verdict / `structureCertified` / `certificationReconciliation` / `workflowStop` /
  `automaticNextImplementationTask` must stay correct;
- the `addressBookModuleAmsc001W3R1` checkpoint must record `ELEVEN` new / `TWELVE` owned /
  `CONSUMED_NOT_OWNED` shared session / `ZERO` duplicate ownership / `ZERO` production & migration delta.

No tautological self-comparison: the two sides are different records produced by different waves.

## F. Evidence updates

| File | Change |
| --- | --- |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3/certification.md` | behavior/status-code truth corrected (`NONE` → bounded vocabulary); success/routes/DTO/schema preserved |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3/manifest-sot.md` | W3 record field table corrected (6 × `NONE` → 2 bounded + 4 `NONE`) |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3/api-result-error-mapping.md` | `StatusCodesChanged = NONE` sentence corrected |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3/validation.md` | behavior-preservation table now states the W1 repair as the certified behavior truth |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3/durable-guards.md` | new guard row + W3-R1 row |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/certification-truth-reconciliation.md` | this file |
| `TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/validation.md` | bounded validation record |

## Deltas (proven zero)

```text
production source delta   = ZERO   (git diff --stat over src/backend/Modules/AddressBook = empty)
migration delta           = ZERO
manifest structure change = ZERO   (no manifest file touched at all)
route / DTO / error-value / resource / schema change = ZERO
```

The single non-documentation file changed by W3-R1 is the focused guard test
`src/backend/Host/Tooba.Host.Tests/Architecture/AddressBookModuleAmsc001W3CertGuardTests.cs`
(+1 test; no existing assertion removed or relaxed).
