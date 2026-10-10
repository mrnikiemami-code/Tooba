# TB-TMAR-WALLET-AMSC-001-W1 — Migrate (`tooba-architecture-migrate`)

```text
TASK:          TB-TMAR-WALLET-AMSC-001-W1
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-migrate
TARGET:        src/backend/Modules/Wallet/Tooba.Wallet.*
PARENT:        TB-TMAR-WALLET-AMSC-001-W0  (commit 7ab117fb)
STARTING HEAD: 7ab117fb
STATE:         MIGRATE_COMPLETE
VERDICT:       READY_TO_STRUCTURE
LOCK VERSION:  ARCH-COMPLETE-002
```

W1 retires the module's parallel message-text error mechanism and installs the canonical
Contracts-owned stable-code home plus a typed-fault seam, exactly mirroring the AMSC-certified
`Support` shape. Behaviour (routes, success payloads, wire-visible outcome codes, domain
invariants, schema, DI lifetimes, outbox semantics) is preserved; the only intentional wire delta
is the accepted AMSC transport-validation envelope for the four caller-controlled requests.

---

## 1. Stable-code home (B1 + B2 + B4 retired)

```text
BEFORE  Tooba.Wallet.Application.Errors.WalletErrorCodes        (11 declared codes, Application-owned)
        Tooba.Wallet.Application.Errors.WalletExceptionMapper   (43-entry shadow KnownCodes set)
AFTER   Tooba.Wallet.Contracts.Errors.WalletErrorCodes          (50 declared codes, Contracts-owned)
        Tooba.Wallet.Application.Errors/                        DELETED (folder gone)
```

`Tooba.Wallet.Domain` now references `Tooba.Wallet.Contracts`, so the Domain and the Infrastructure
directory can throw the module's stable codes as typed faults instead of raw literals.

```text
HttpReachableCodes   10  (client-observable outcome codes; one catalog descriptor each)
DomainInvariantCodes 40  (typed invariant identity; localized, no dedicated HTTP descriptor)
IsKnown              union of both sets; the WalletOperation filter
IsHttpReachable      client-observable subset
IsDomainInvariant    invariant subset
```

`customer.session.required` is deliberately **not** declared: its descriptor and both-culture
resources stay Foundation-owned, and the Wallet HTTP boundary consumes
`FoundationErrorCodes.CustomerSessionRequired` directly (matching `Support`/`Returns`).

## 2. Typed-fault seam (B2 retired)

`Tooba.Wallet.Application/Composition/WalletOperation.cs`:

```text
Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string? publicOutcomeCode = null)
Task<Result>    ExecuteAsync(Func<Task> action, string? publicOutcomeCode = null)
Result<T>       NotFoundIfNull<T>(T? value, string missingCode)
SemanticError   ToSemanticError(ContractOperationException exception, string? publicOutcomeCode = null)
```

Both typed mechanisms are mapped **by declared code only**:

```text
catch (ContractOperationException ex) when (WalletErrorCodes.IsKnown(ex.Code))  -> Result failure
catch (SemanticException ex)                                                    -> Result failure
ResolveOutcomeCode: IsHttpReachable -> typed code, else publicOutcomeCode ?? typedCode
```

An unknown code (or an unrelated `InvalidOperationException`) is **rethrown** and reaches the
canonical global exception boundary — never hidden as a business error. This preserves the retired
mapper's observable outcome mapping byte-for-byte while removing message-text classification
entirely.

## 3. Raw faults and legacy base64 codes retired (B3 + B4)

```text
raw InvalidOperationException("<literal>") in Wallet production   0
base64 Persian code suffixes in Wallet production                 0
WalletExceptionMapper / TryMapExact / .Message.Contains           0
```

Every former literal is now `ContractOperationException(WalletErrorCodes.<Constant>)` in:

- `Tooba.Wallet.Domain/Aggregates/{WalletAccount, WalletLedgerEntry, GiftCard, GiftCardRedemption}.cs`
- `Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs`
- `Tooba.Wallet.Infrastructure/DependencyInjection/WalletModule.cs` (`GetEventTypeName`)
- `Tooba.Wallet.Application/Models/WalletEnumParsing.cs`

`Translate(...)`/`ResolveEventClrType(...)` remain `null`, so outbox observability is unchanged.
`WalletCurrency.Normalize` and `WalletAccount.NormalizeCurrency` now throw
`CurrencyRequired`/`CurrencyInvalid` typed faults (same message→code mapping as before).

All 11 handlers route through `WalletOperation`; each declares the stable public outcome code of its
use case (`WalletRejected`, `RedeemRejected`, `GiftCardRejected`, `GiftCardIssueRejected`,
`GiftCardRevokeRejected`, `GiftCardMissing`, `WalletMissing`, `AdjustRejected`, `DemoNotReady`).

## 4. Transport validator matrix (B5 retired)

W0 proved `11 routes == 11 endpoint-reachable requests == 4 VALIDATOR_REQUIRED + 7
NO_VALIDATOR_REQUIRED`. W1 adds exactly the four validators:

| Request | Validator | Stable codes |
| --- | --- | --- |
| `RedeemCustomerGiftCardCommand` | `RedeemCustomerGiftCardCommandValidator` | `wallet.validation.code_required`, `wallet.validation.code_length`, `wallet.validation.idempotency_key_too_long` |
| `IssueAdminGiftCardCommand` | `IssueAdminGiftCardCommandValidator` | `wallet.validation.initial_amount_positive`, `wallet.validation.currency_invalid`, `wallet.validation.idempotency_key_too_long` |
| `ListAdminGiftCardsQuery` | `ListAdminGiftCardsQueryValidator` | `wallet.validation.status_invalid`, `wallet.validation.search_too_long` |
| `AdjustAdminWalletCommand` | `AdjustAdminWalletCommandValidator` | `wallet.validation.initial_amount_positive`, `wallet.validation.status_invalid`, `wallet.validation.code_required`, `wallet.validation.search_too_long`, `wallet.validation.idempotency_key_too_long` |

`WalletValidationCodes` is transport identity: never localized, never catalogued as an HTTP
descriptor (it surfaces inside the canonical `validation.failed` envelope and the per-property
`validationErrors` map). No `WithMessage(...)` prose is emitted. The seven
`NO_VALIDATOR_REQUIRED` requests stay validator-free for their W0-recorded reasons (`:guid` route
constraint, server-derived actor, executed canonical clamp, or `Development`-gated parameterless).

## 5. Localization surface (B6 + B7 retired)

```text
BEFORE  WalletErrors.resx / .fa.resx : 1 key each (wallet.authorization.unavailable)
AFTER   WalletErrors.resx / .fa.resx : 50 keys each == 50 declared codes
```

- `WalletErrorCatalogContributor` registers the 10 client-observable descriptors exactly once, plus
  `WalletAdminAuthorizationCodes.AuthorizationUnavailable`. No domain-only invariant is catalogued.
- `WalletErrorResourceSet.Owns` is prefix-based over the two Wallet-owned keyspaces
  (`wallet.` **and** `giftcard.`); both keyspaces are collision-free repository-wide (the only other
  `giftcard.*` strings are the AccessControl permission ids `giftcard.view`/`giftcard.manage`).
- `Tooba.Wallet.Endpoints.csproj` now pins the explicit `EmbeddedResource` + `LogicalName` pair, so
  the bilingual logical names are **locked** rather than left to SDK convention:

```text
Tooba.Wallet.Endpoints.Resources.WalletErrors.resources
Tooba.Wallet.Endpoints.Resources.WalletErrors.fa.resources
```

The catalog/resource-set/authorization-code surface stays in `Tooba.Wallet.Endpoints` because the
certified Host guards (`HostAdminAccessAmcCertGuardTests`, `HostAdminCanon003GuardTests`,
`HostAdminCanon009GuardTests`) pin that location — recorded in W0 §18 as an architecture constraint,
not a preference.

## 6. Boundary / coupling

```text
Cross-Module-Coupling-State = NONE
Cross-Module-Join-State     = NONE
```

- The only legal foreign edge stays `Tooba.Wallet.Infrastructure -> Tooba.Notification.Contracts`.
- `Tooba.Wallet.Domain` references only BuildingBlocks + its own `Tooba.Wallet.Contracts`.
- `Tooba.Wallet.Endpoints` references `Wallet.Application` + BuildingBlocks only.
- The customer endpoint consumes `FoundationErrorCodes.CustomerSessionRequired`; no Wallet-owned
  duplicate of the session code exists.
- Zero foreign Application/Infrastructure/Domain using-directive in any Wallet production file.

## 7. Guard updates (atomic, no weakening)

| Guard | Change |
| --- | --- |
| `Tooba.Wallet.Tests/Architecture/WalletArchitectureGuardTests.cs` | Application allowlist `Errors` → `Composition` + `Validation`; Domain/Contracts assertion **inverted** to require the Contracts reference; `WalletExceptionMapper` positive assertion → `WalletOperation` + negative assertions; localized-prose filter widened (no `wallet.` exemption) |
| `Tooba.Wallet.Tests/Behavior/WalletCqrsAndHttpContractTests.cs` | `WalletSemanticPresentationTests` repointed from `WalletExceptionMapper` + `InvalidOperationException` to `WalletOperation` + `ContractOperationException`; new assertion that a client-observable typed code is surfaced as-is |
| `Tooba.Wallet.Tests/Behavior/WalletFinancialCharacterizationTests.cs` | `wallet.rejected.2YXZiNis` → `WalletErrorCodes.BalanceInsufficient` |
| `Tooba.Wallet.Tests/Architecture/NotificationContractsArchitectureGuardTests.cs` | **Pre-existing staleness repaired**: the guard was RED at HEAD (it predates `c660b933`, the Notification AMSC W1 that added `Errors/` + `Resources/` and the BuildingBlocks reference). Allowlist gains `Errors`/`Resources`; `Assert.Empty(refs)` → the canonical "no implementation project reference" assertion (BuildingBlocks foundation allowed, matching every certified module Contracts project) |
| `Tooba.Host.Tests/WalletCurrencyContractsTests.cs` | `InvalidOperationException` + message-string assertions → `ContractOperationException` + `WalletErrorCodes.CurrencyRequired/CurrencyInvalid` |
| `Tooba.Host.Tests/Architecture/WalletModuleAmsc001W1MigrateGuardTests.cs` | **New** durable W1 guard (9 facts) |
| `Tooba.Host.Tests/Tooba.Host.Tests.csproj` | Adds `Wallet.Application` + `Wallet.Endpoints` project references so the new guard can consume the public surfaces |

## 8. Durable W1 guard — `WalletModuleAmsc001W1MigrateGuardTests`

```text
1  Stable_codes_live_in_the_single_canonical_contracts_errors_home
2  Declared_code_catalog_splits_http_reachable_from_domain_invariants     (10 / 40 / set equality)
3  Typed_fault_seam_maps_declared_codes_and_never_parses_message_text
4  Error_localization_resource_set_is_wallet_owned_and_bilingual          (50 EN + 50 FA + locked logical names)
5  Transport_validators_cover_the_four_caller_controlled_requests
6  Customer_endpoints_use_the_foundation_session_code_and_host_has_no_wallet_authority
7  Wallet_never_references_foreign_application_infrastructure_or_domain
8  Wallet_production_never_classifies_faults_by_message_text
9  Migration_did_not_change_the_wallet_schema_migrations
```

Assertions locate files **by name**, not by technical-axis path, so W2 can folder the module without
weakening this guard.

## 9. Behaviour-preservation baseline (unchanged)

| Surface | State |
| --- | --- |
| 11 routes / verbs / group prefixes / templates | unchanged |
| Success response shapes (9 DTOs + `{}`) | unchanged |
| The 10 wire-visible outcome codes | byte-identical |
| Domain invariants (locale caps, `Guid.Empty`, positive amounts, idempotency) | unchanged |
| Authorization + actor-resolution order | unchanged |
| Schema `wallet` + `20260827180000_InitialWallet` | unchanged (no migration/designer/snapshot touched) |
| Outbox `Translate`/`ResolveEventClrType`/`Schema`/`TableName` | unchanged |
| DI lifetimes | unchanged |
| Notification semantics (4 semantic types + source ids + target route) | unchanged |

Intentional wire delta: the **invalid-input** envelope of the four `VALIDATOR_REQUIRED` requests
moves from a mapped business 400 to the canonical `validation.failed` 400 — the accepted AMSC
transport-validation contract.

## 10. Verification

```text
dotnet build  Tooba.Wallet.Endpoints / Infrastructure / Tests / Host.Tests / Payment.Tests   SUCCEEDED 0 errors
dotnet test   Tooba.Wallet.Tests                                                             24 passed / 0 failed
dotnet test   Tooba.Host.Tests --filter WalletModuleAmsc001W1MigrateGuardTests                9 passed / 0 failed
dotnet test   Tooba.Host.Tests --filter Wallet|HostAdminAccessAmcCertGuardTests|
                                      HostAdminCanon003GuardTests|HostAdminCanon009GuardTests|
                                      ErrorCatalogUniqueCodeGuardTests|PaymentPrecertHygieneTests
                                                                                             64 passed / 1 skipped / 0 failed
dotnet test   Tooba.Payment.Tests                                                            107 passed / 0 failed
```

## 11. Structured state fields

```text
StableCodeHomeState          : CONTRACTS_OWNED_50_DECLARED_10_HTTP_40_INVARIANT
DeclaredCodeGuardState       : PRESENT_ISKNOWN_ISHTTPREACHABLE_ISDOMAININVARIANT
TypedFaultSeamState          : WALLETOPERATION_EXECUTEASYNC_EXECUTEASYNC_NOTFOUNDIFNULL_TOSEMANTICERROR
MessageClassificationState   : ZERO
RawFaultState                : ZERO_RAW_INVALID_OPERATION_EXCEPTION_LITERALS
LegacyBase64CodeState        : ZERO
ValidatorMatrixState         : EXHAUSTIVE_4_OF_4_REQUIRED_PLUS_7_NO_VALIDATOR_REQUIRED
LocalizationState            : CANONICAL_50_EN_50_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
CatalogOwnershipState        : 10_HTTP_DESCRIPTORS_PLUS_ADMIN_UNAVAILABLE_REGISTERED_ONCE
CrossModuleCouplingState     : NONE
CrossModuleJoinState         : NONE
PersistenceOwnershipState    : SINGLE_WALLET_SCHEMA
SchemaState                  : UNCHANGED
BehaviorPreservation         : PRESERVED
MicroserviceExtractable      : TRUE_PENDING_W2_STRUCTURE
GuardWeakened                : NONE
ProductionCodeChanged        : TRUE
HostTouched                  : FALSE
StopGate                     : USER_REVIEW_WALLET_AMSC_001_W1
```

## 12. Destination-integrity check

`HEAD == origin/main` at W1 start (`7ab117fb`). W1 changes only
`src/backend/Modules/Wallet/**`, `src/backend/Host/Tooba.Host.Tests/**` (guards + csproj) and the
SoT/evidence for this task. No Host production file was modified. No manifest mutation, no schema
change, no baseline widening.
