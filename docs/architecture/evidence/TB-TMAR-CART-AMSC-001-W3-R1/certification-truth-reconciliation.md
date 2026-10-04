# TB-TMAR-CART-AMSC-001-W3-R1 — Certification truth reconciliation

- **Parent:** `TB-TMAR-CART-AMSC-001-W3`
- **Mode:** `CERTIFICATION_TRUTH_RECONCILIATION` (documentation/SoT/guard truth only)
- **Starting HEAD:** `3c2119d7` (== `origin/main` at claim time)
- **Skill:** `tooba-architecture-certify` (truth reconciliation, no re-certification)
- **Verdict after reconciliation:** `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

Cart production implementation and structure remain **ACCEPTED**. W1/W2 were not reopened. Runtime
behavior did not change.

## 1. Accepted W1 repair (unchanged, verified)

`cartModuleAmsc001W1.behaviorChangeState` reads:

```text
PREVIOUSLY_500_PLATFORM_UNEXPECTED_PATHS_NOW_TYPED_LOCALIZED_400_409_503_ALL_PREVIOUSLY_MAPPED_CODES_UNCHANGED
```

W1 evidence (`docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W1/migrate.md` §10, §3.4) proves the
exact client-visible remap:

| Scenario | Before | After |
|---|---|---|
| Domain/Infrastructure guard tripped (owner, expiry, market, currency, merge, adopt) | HTTP **500** `platform.unexpected` | HTTP **400** (Business) / **409** (Conflict) with the module's own localized `cart.*` code |
| Pricing returns no quote | HTTP **500** `platform.unexpected` | HTTP **409** `cart.pricing.quote_missing` |
| Store commerce context unresolved | HTTP **500** `platform.unexpected` | HTTP **503** `cart.commerce.*` |
| All previously-mapped codes (`cart.missing`, `cart.version.conflict`, …) | mapped code | **same** code, same status |

## 2. Stale W3 / manifest wording (the defect)

| Location | Stale value | Problem |
|---|---|---|
| `cartModuleAmsc001W3.behaviorChange` | `NONE` | Contradicted the accepted W1 bounded expected-failure repair |
| `cartModuleAmsc001W3` | (no `statusCodesChanged` field) | W1 proves a 500 → 400/409/503 remap |
| `cartModuleAmsc001W3.cartOwnedCodeCount` | `27` | Ambiguously called all 27 declared/consumed codes Cart-owned |
| `cartModuleAmsc001W3.cartOwnedDescriptorCount` | `26` | Disk registers **25** (see §4) |
| Cart manifest `certificationNote` | ended with “Behavior, routes, DTO shapes and schema unchanged.” | Implied the complete AMSC preserved behavior |

## 3. Corrected final truth

`cartModuleAmsc001W3`:

```text
behaviorChange              = BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING
statusCodesChanged          = BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_400_409_503
w3RuntimeBehaviorChange     = NONE   (W3 itself changed zero runtime behavior; the repair is W1's)
schemaChange                = NONE
routesChanged               = NONE
errorCodesChanged           = NONE
dtoShapeChanged             = NONE
```

`behaviorChangeDetail` / `statusCodesChangedDetail` state truthfully: success payloads, routes, DTO
shapes, schema and previously-mapped expected failures preserved; only previously unexpected
defective failure paths became typed/localized expected failures (500 → 400/409/503).

Manifest `certificationNote` now distinguishes the W1 accepted bounded repair from W3's zero runtime
behavior change. **Structural manifest meaning is unchanged** (module, `structureCertified`,
`lockVersion`, projects, `rootAllowlist`, `forbiddenRootFiles`, `forbiddenTopLevelFolders` all
untouched).

## 4. Code / descriptor ownership truth (re-discovered from disk)

| Fact | Value |
|---|---|
| Constants declared in `CartErrorCodes` | **27** |
| Descriptors registered by `CartErrorCatalogContributor` | **25** |
| `checkout.authentication_required` | declared/consumed by Cart, owned+registered by `FoundationErrorCatalogContributor` (`FoundationErrorCodes.CheckoutAuthenticationRequired`), **not** registered by Cart |
| `cart.line.currency_missing` | declared + localized + thrown fail-closed, but registered by **no** contributor |
| Duplicate descriptor ownership | **ZERO** |

Reconciled fields: `declaredOrConsumedCodeCount = 27`, `cartOwnedDescriptorCount = 25`,
`cartOwnedDescriptorCount = 25`, `sharedConsumedCode = checkout.authentication_required`,
`sharedConsumedCodeOwner = Foundation`, `sharedConsumedCodeRegisteredByCart = false`.

### Variance from the task's expected value

The task expected `cartOwnedDescriptorCount = 26`. Disk truth is **25**. The expected 26 would
double-count the shared Foundation code as Cart-owned:

```text
27 declared
- 1 shared Foundation-owned (checkout.authentication_required)
- 1 registered by no contributor (cart.line.currency_missing)
= 25 Cart-registered descriptors
```

`cart.line.currency_missing` is recorded as a non-blocking residual (`R1`): the composed
`ErrorDefinitionCatalog` is fail-fast on duplicate codes and `SafeErrorMapper` falls back to
`platform.unexpected` for an unregistered code, so the raw 500 fallback on that path is a real but
non-blocking residual risk. Registering it would be an implementation change outside the scope of a
truth-reconciliation task, so it was **not** done here. This variance is recorded honestly rather than
silently matching the expected number.

## 5. Deltas (all zero except documentation)

| Axis | Delta |
|---|---|
| Production code | **ZERO** |
| Routes / DTO shapes / error-code values / resources (resx) | **ZERO** |
| Schema / migrations | **ZERO** |
| Manifest structural fields | **ZERO** (documentation-only note wording) |
| Certification state | **PRESERVED** |

Preserved: `verdict=COMPLETE_REFERENCE_PATTERN`, `lockVersion=ARCH-COMPLETE-002`,
`structureCertified=true`, `structureState=READY_FOR_CERTIFY`, `manifestDiskReconciliation=EXACT`,
`foreignAppInfraDomainCoupling=ZERO`, `crossModuleBoundaryState=CONTRACTS_ONLY`,
`microserviceExtractable=true`, `blockingResidualDebt=ZERO`, `guardsWeakened=NONE`,
`baselinesWidened=NONE`, `automaticNextImplementationTask=NONE`.

## 6. Durable guard

`CartModuleAmsc001W3CertGuardTests` was **extended** with
`Cart_certification_truth_records_the_accepted_w1_bounded_defect_repair`, which reads W1 and W3 from
the same SoT document and asserts:

- W1 records the bounded repair (`PREVIOUSLY_500…` with 400/409/503);
- final W3 `behaviorChange != NONE` and equals `BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING`;
- final `statusCodesChanged != NONE`;
- `w3RuntimeBehaviorChange = NONE` (repair is W1's, not W3's);
- 27 declared/consumed != 25 Cart-owned descriptors;
- the shared checkout code is Foundation-owned and consumed-not-registered by Cart;
- the R1 checkpoint record carries the same truth.

The existing descriptor-count assertion was corrected from the stale `26` to the disk-true `25`
(comment updated to state `cart.line.currency_missing` is declared+localized+thrown but registered by
no contributor). No assertion was weakened; no broad repo machinery was added.
