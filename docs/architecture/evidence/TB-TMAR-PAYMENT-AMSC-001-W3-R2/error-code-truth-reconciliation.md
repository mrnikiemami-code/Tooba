# Payment Error-Code Truth Reconciliation — TB-TMAR-PAYMENT-AMSC-001-W3-R2

Independent repository verification of Payment stable-error-code truth, and the exact
distinction between *declared/consumed* foreign codes and *KnownCodes*-admitted foreign codes.

## 1. Exact authoritative counts (verified on disk)

| Measure | Value | Source of verification |
|---|---|---|
| Declared stable constants (`public const string`) | **28** | `PaymentErrorCodes.cs` |
| `KnownCodes` initializer members | **27** | `PaymentErrorCodes.cs` `KnownCodes = new(StringComparer.Ordinal) { ... }` |
| Payment-owned registered descriptors | **24** | `PaymentErrorCatalogContributor.Contribute()` (`D(PaymentErrorCodes.*)`) |
| Foreign-owned declared/consumed codes | **4** | 28 declared − 24 registered |
| Foreign codes admitted by `IsKnown` | **3** | `KnownCodes` membership |

## 2. The four foreign declared/consumed codes

| Code constant | Machine code | Natural owner | Registered by Payment? |
|---|---|---|---|
| `ReservationRetryLimit` | `inventory.reservation.retry_limit_reached` | Order (`ReservationCycleErrors.RetryLimitReached`) | NO |
| `SupplyUnavailable` | `inventory.supply.unavailable` | Inventory (`InventoryErrorCodes.SupplyUnavailable`) | NO |
| `AdminAuthorizationDenied` | `admin.authorization.denied` | Foundation | NO |
| `CheckoutAuthenticationRequired` | `checkout.authentication_required` | Foundation | NO |

Directly asserted: all four are absent from `PaymentErrorCatalogContributor` output
(`D(PaymentErrorCodes.<name>,` never appears for any of the four).

## 3. `KnownCodes` membership (exact truth)

| Foreign constant | In `KnownCodes`? |
|---|---|
| `ReservationRetryLimit` | **YES** |
| `SupplyUnavailable` | **YES** |
| `CheckoutAuthenticationRequired` | **YES** |
| `AdminAuthorizationDenied` | **NO** |

`PaymentErrorCodes.IsKnown` therefore returns `true` for the first three foreign codes and
`false` for `admin.authorization.denied`. `PaymentOperation`'s
`catch (ContractOperationException ex) when (PaymentErrorCodes.IsKnown(ex.Code))` branch
consequently recognises three foreign codes and does **not** map
`admin.authorization.denied` through that branch (it remains declared/consumed and, if
raised, propagates to the canonical global exception boundary).

## 4. Additive reconciliation metadata recorded in R2

| Field | Value |
|---|---|
| `declaredStableCodeCount` | `28` |
| `knownCodeGuardMemberCount` | `27` |
| `paymentOwnedDescriptorCount` | `24` |
| `foreignOwnedDeclaredConsumedCount` | `4` |
| `foreignOwnedKnownGuardCount` | `3` |
| `foreignOwnedNotInKnownGuard` | `admin.authorization.denied` |
| `w1StableErrorCodeStateHistorical` | `STALE_3_CONSUMED_COUNT` |
| `currentStableErrorTruth` | `AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS` |
| `descriptorOwnershipState` | `UNIQUE_24_PAYMENT_4_FOREIGN_NOT_REREGISTERED` |
| `productionErrorCatalogChangedByR2` | `false` |

## 5. Historical-text reconciliation (additive; no history rewrite)

- **W1 SoT** `stableErrorCodeState` said `..._3_INTENTIONALLY_CONSUMED_...`. That text is
  **stale/inaccurate**: the truthful foreign declared/consumed count is **4**. The historical
  field is preserved as history and an explicit `stableErrorCodeStateReconciliation` marker
  (`HISTORICAL_W1_TEXT_SAID_3_CONSUMED; AUTHORITATIVE_COUNT_IS_4_...`) plus the corrected
  `4_INTENTIONALLY_CONSUMED` value were added.
- **W0** historical 27-code analysis is **not** rewritten (it was true at W0 analysis time).
- **W3 / Master-Recovery** wording that all four foreign declarations were "for the known-code
  guard" was inaccurate. It is corrected to the exact truth: 4 foreign codes are
  declared/consumed without re-registration; only 3 participate in `IsKnown` / `PaymentOperation`
  expected-fault recognition; `admin.authorization.denied` is excluded from `KnownCodes`.

## 6. No descriptor duplication

Composed `ErrorDefinitionCatalog` (Foundation + Payment contributors) yields unique machine
codes; the four foreign codes are never re-registered by Payment. Repo-wide
`ErrorCatalogUniqueCodeGuardTests` remains green (see `validation.md`).

## 7. Production code

**No production file was changed by this wave.** `PaymentErrorCodes.cs`,
`PaymentErrorCatalogContributor.cs`, `PaymentErrorResourceSet.cs`, and all Payment resources
are byte-identical to the W3-certified state.
