# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 — Input / validation matrix

Inspected commit: `68d1aeb3525e8b2d43c719578b433874d26e09d4`.
Scope: the **16 shipped Shipping-owned admin routes** in `SOURCE-MAP.md §3`. Provenance is traced per input; classification is derived from the real route map plus real validators, then checked for set equality.

## 1. Set-equality gate (reachable set == classified set)

| Derived from | Count |
|---|---|
| Shipped Shipping admin route mappings (`Map(Get|Post|Put|Patch)`) | 16 |
| Distinct MediatR requests dispatched by those endpoints (`ISender.Send`) | 16 |
| Requests with exactly one declared validator | 16 |
| Requests classified `NO_VALIDATOR_REQUIRED` | 0 |

`Validator-Coverage-State = EXHAUSTIVE` **for this bounded surface**. No orphan, duplicate or unclassified request.
This is a statement about transport-shape coverage only; it is **not** a claim that business preconditions are complete (see §4).

## 2. Matrix

Body-bound inputs are **not** model-bound by the framework: every write endpoint calls a local `ReadBodyAsync<T>` that reads `HttpContext.Request.ReadFromJsonAsync<T>` and converts "unreadable/absent/non-JSON body" into a declared `validation.failed` (400) with stable child code `shipping.validation.body_invalid`. This is deliberate (documented in each endpoint's remarks) and keeps malformed input a client error instead of `platform.unexpected` 500.

| # | Route | Request type | Inputs and provenance | Validator(s) | Classification |
|---|---|---|---|---|---|
| 1 | `GET .../{serviceCode}/rates` | `GetShippingRatesAndCoverageQuery` | `serviceCode` ← route | `GetShippingRatesAndCoverageQueryValidator` | `VALIDATOR_REQUIRED` |
| 2 | `POST .../{serviceCode}/rates` | `CreateShippingRatePolicyCommand` | `serviceCode` ← route; `Scope{CountryId,AdministrativeAreaId,CityId,ServiceAreaId}`, `ChargeAmount`, `CurrencyCode`, `PreparationDays`, `LeadDays`, `FreeAboveThreshold?`, `OptionCode?`, `Condition{WeightGrams{Min,Max?}?,ItemCount{Min,Max?}?}?`, body `ServiceCode` ← JSON body | `CreateShippingRatePolicyCommandValidator` → `ShippingRatePolicyWriteRequestValidator`; route/body code equality via `ShippingCommandScopeRules` | `VALIDATOR_REQUIRED` |
| 3 | `PUT .../{serviceCode}/rates/{ratePolicyId:guid}` | `UpdateShippingRatePolicyCommand` | `ratePolicyId` ← route; `serviceCode` ← route; body as #2 | `UpdateShippingRatePolicyCommandValidator` (+ request validator) | `VALIDATOR_REQUIRED` |
| 4 | `PATCH .../{serviceCode}/rates/{ratePolicyId:guid}/active` | `SetShippingRatePolicyActiveCommand` | `ratePolicyId` ← route; `serviceCode` ← route; `IsActive` ← body | `SetShippingRatePolicyActiveCommandValidator` | `VALIDATOR_REQUIRED` |
| 5 | `POST .../{serviceCode}/coverage-grants` | `CreateShippingCoverageGrantCommand` | `serviceCode` ← route; `Scope`, body `ServiceCode`, `OptionCode?` ← body | `CreateShippingCoverageGrantCommandValidator` → `ShippingCoverageGrantWriteRequestValidator` | `VALIDATOR_REQUIRED` |
| 6 | `PATCH .../{serviceCode}/coverage-grants/{coverageGrantId:guid}/active` | `SetShippingCoverageGrantActiveCommand` | `coverageGrantId`, `serviceCode` ← route; `IsActive` ← body | `SetShippingCoverageGrantActiveCommandValidator` | `VALIDATOR_REQUIRED` |
| 7 | `PUT .../{serviceCode}/method-coverage` | `SetShippingMethodCoverageCommand` | `serviceCode` ← route; `OptionCode?`, `Mode` ← body | `SetShippingMethodCoverageCommandValidator` → `ShippingMethodCoverageWriteRequestValidator` (enum definedness) | `VALIDATOR_REQUIRED` |
| 8 | `GET .../{serviceCode}/financial-status` | `GetShippingMethodFinancialStatusQuery` | `serviceCode` ← route; `optionCode?` ← **query string** (implicit minimal-API binding) | `GetShippingMethodFinancialStatusQueryValidator` (length on the optional query value) | `VALIDATOR_REQUIRED` |
| 9 | `GET .../{serviceCode}/operating-schedules` | `GetShippingOperatingSchedulesQuery` | `serviceCode` ← route | `GetShippingOperatingSchedulesQueryValidator` | `VALIDATOR_REQUIRED` |
| 10 | `PUT .../{serviceCode}/operating-schedules` | `UpsertShippingOperatingScheduleCommand` | `serviceCode` ← route; `TimeZoneId`, `Windows[]{Weekday,StartTime,EndTime,Capacity?}`, `OptionCode?`, body `ServiceCode` ← body | `UpsertShippingOperatingScheduleCommandValidator` → `ShippingOperatingScheduleWriteRequestValidator` (incl. cross-row weekday overlap) | `VALIDATOR_REQUIRED` |
| 11 | `PATCH .../{serviceCode}/operating-schedules/{scheduleId:guid}/active` | `SetShippingOperatingScheduleActiveCommand` | `scheduleId`, `serviceCode` ← route; `IsActive` ← body | `SetShippingOperatingScheduleActiveCommandValidator` | `VALIDATOR_REQUIRED` |
| 12 | `POST .../{serviceCode}/operational-quote` | `EvaluateShippingOperationalQuoteQuery` | `serviceCode` ← route; `Scope`, `OptionCode?`, `CartSubtotalExclusive`, `CurrencyCode?`, `WeightGrams?`, `ItemCount?` ← body | `EvaluateShippingOperationalQuoteQueryValidator` → `ShippingOperationalQuoteEvaluationRequestValidator` | `VALIDATOR_REQUIRED` |
| 13 | `GET .../{serviceCode}/packaging-policies` | `GetShippingPackagingPoliciesQuery` | `serviceCode` ← route | `GetShippingPackagingPoliciesQueryValidator` | `VALIDATOR_REQUIRED` |
| 14 | `POST .../{serviceCode}/packaging-policies` | `CreateShippingPackagingPolicyCommand` | `serviceCode` ← route; `Scope`, `FeeAmount`, `CurrencyCode`, `FreightPayerMode?`, `OptionCode?`, body `ServiceCode` ← body | `CreateShippingPackagingPolicyCommandValidator` → `ShippingPackagingPolicyWriteRequestValidator` | `VALIDATOR_REQUIRED` |
| 15 | `PUT .../{serviceCode}/packaging-policies/{packagingPolicyId:guid}` | `UpdateShippingPackagingPolicyCommand` | `packagingPolicyId`, `serviceCode` ← route; body as #14 | `UpdateShippingPackagingPolicyCommandValidator` (+ request validator) | `VALIDATOR_REQUIRED` |
| 16 | `PATCH .../{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active` | `SetShippingPackagingPolicyActiveCommand` | `packagingPolicyId`, `serviceCode` ← route; `IsActive` ← body | `SetShippingPackagingPolicyActiveCommandValidator` | `VALIDATOR_REQUIRED` |

## 3. Success/error mapping

- Every handler returns `Tooba.BuildingBlocks.Results.Result` / `Result<T>`; every endpoint maps it with the canonical `ApiResponseFactory.From(...)`.
- No `Results.Json`, `Results.BadRequest`, local ProblemDetails mapper or `ex.Message` parsing was found in `Tooba.Shipping.Endpoints/**`.
- Malformed-body path uses `api.FromException(ValidationException)` → declared 400 `validation.failed` with child code `shipping.validation.body_invalid`.
- **`Canonical-API-Result-Pattern-State = CANONICAL` for this bounded surface.** No proposed factory delta is needed.

## 4. Transport vs business boundary (honest limits)

The validators own transport shape only. These remain outside FluentValidation by design and are executed in handlers/domain/infrastructure:

| Rule | Actual owner | Evidence |
|---|---|---|
| Method identity exists in the real catalog | `IShippingMethodOptionValidator` -> Fulfillment `IShippingCatalogReader` / `IShippingOneLevelCanonicalMethodReader` | `FulfillmentShippingMethodOptionValidator.cs` |
| Destination ids exist, active and coherent | `IShippingDestinationScopeValidator` -> Geography `IGeographyLookup.GetByIdsAsync` | `GeographyDestinationScopeValidator.cs` |
| Amount/currency/day/tier normalization | `ShippingRatePolicy`, `ShippingOperatingSchedule`, `ShippingPackagingPolicy` (Domain) | aggregates |
| Scope uniqueness, tier overlap, schedule uniqueness | `ShippingRateCoverageDirectory` / `ShippingOperatingScheduleDirectory` + DB unique indexes | `ShippingDbContext.OnModelCreating` |

**Gap (reported, not repaired):** the enable/activation preconditions that the owner target requires (owner scope, mode, currency authority) have no validator or policy at all, because no such concept is persisted. See `MODEL-LEGACY-AND-HANDOFF.md §2`.

## 5. Duplication finding inside the bounded surface

`ReadBodyAsync` / `TryReadBodyAsync` / `InvalidBodyException` are copied **verbatim** in four endpoint files (`ShippingRateCoverageEndpoints`, `ShippingOperatingScheduleEndpoints`, `ShippingPackagingPolicyEndpoints`, `ShippingOperationalQuoteEndpoints`). Also `shipping.validation.service_code_required` / `..._too_long` / `body_service_code_mismatch` / `option_code_too_long` are redeclared in three `static class`es (`ShippingValidationCodes`, `ShippingScheduleValidationCodes`, `ShippingOperationalValidationCodes`) with identical string values.

This is cohesion duplication, not a behavioral defect: one file per responsibility must remain, but the shared body-reading seam and the shared validation codes need a single owner during Migrate. No repair performed in W0.
