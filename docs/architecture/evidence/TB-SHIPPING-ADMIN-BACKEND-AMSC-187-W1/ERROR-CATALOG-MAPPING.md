# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1 — code -> descriptor -> key -> status -> resource mapping

Generated from the committed sources; every row is the exact registration this slice ships.

- Descriptor owner: `Tooba.Shipping.Endpoints.Errors.ShippingErrorCatalogContributor` (single owner of the `shipping.*` keyspace).
- Resource owner: `Tooba.Shipping.Endpoints.Resources.ShippingErrorResourceSet` behind `ShippingErrors.resx` / `ShippingErrors.fa.resx`.
- Localization key = the stable code (no invented key aliases).
- HTTP/classification/severity are the legacy-compatible values the unregistered fallback in `SafeErrorMapper` already produced: 400 / Business / Warning. No code is re-mapped by name suffix (`*.not_found` stays 400; `*duplicate*` stays 400).

Declared constants: 44 (distinct: 44)
Registered descriptors: 44
Declared-but-unregistered: none
Registered-but-undeclared: none
Neutral resx entries: 44; missing for a code: none; undeclared entries: none
fa resx entries: 44; missing for a code: none; undeclared entries: none

| # | Code (stable) | Constant | Registered | LocalizationKey | HTTP | Classification | Neutral resx | fa resx | SafeTitleFallback |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `shipping.service_code.required` | `ServiceCodeRequired` | yes | `shipping.service_code.required` | 400 | Business | yes | yes | A shipping service code is required. |
| 2 | `shipping.service_code.invalid` | `ServiceCodeInvalid` | yes | `shipping.service_code.invalid` | 400 | Business | yes | yes | The shipping service code is invalid. |
| 3 | `shipping.destination.country_id.required` | `DestinationCountryIdRequired` | yes | `shipping.destination.country_id.required` | 400 | Business | yes | yes | A destination country is required. |
| 4 | `shipping.charge.amount.negative` | `ChargeAmountNegative` | yes | `shipping.charge.amount.negative` | 400 | Business | yes | yes | The shipping charge amount cannot be negative. |
| 5 | `shipping.charge.amount.out_of_range` | `ChargeAmountOutOfRange` | yes | `shipping.charge.amount.out_of_range` | 400 | Business | yes | yes | The shipping charge amount is out of range. |
| 6 | `shipping.charge.currency.invalid` | `CurrencyCodeInvalid` | yes | `shipping.charge.currency.invalid` | 400 | Business | yes | yes | The shipping currency code is invalid. |
| 7 | `shipping.preparation_days.negative` | `PreparationDaysNegative` | yes | `shipping.preparation_days.negative` | 400 | Business | yes | yes | Preparation days cannot be negative. |
| 8 | `shipping.lead_days.negative` | `LeadDaysNegative` | yes | `shipping.lead_days.negative` | 400 | Business | yes | yes | Lead days cannot be negative. |
| 9 | `shipping.free_above_threshold.negative` | `FreeAboveThresholdNegative` | yes | `shipping.free_above_threshold.negative` | 400 | Business | yes | yes | The free-shipping threshold cannot be negative. |
| 10 | `shipping.coverage.scope.invalid` | `CoverageScopeInvalid` | yes | `shipping.coverage.scope.invalid` | 400 | Business | yes | yes | The shipping coverage scope is invalid. |
| 11 | `shipping.rate_policy.not_found` | `RatePolicyNotFound` | yes | `shipping.rate_policy.not_found` | 400 | Business | yes | yes | The shipping rate policy was not found. |
| 12 | `shipping.coverage_grant.not_found` | `CoverageGrantNotFound` | yes | `shipping.coverage_grant.not_found` | 400 | Business | yes | yes | The shipping coverage grant was not found. |
| 13 | `shipping.rate_policy.scope_duplicate` | `RatePolicyScopeDuplicate` | yes | `shipping.rate_policy.scope_duplicate` | 400 | Business | yes | yes | A shipping rate policy already exists for this scope. |
| 14 | `shipping.coverage_grant.scope_duplicate` | `CoverageGrantScopeDuplicate` | yes | `shipping.coverage_grant.scope_duplicate` | 400 | Business | yes | yes | A shipping coverage grant already exists for this scope. |
| 15 | `shipping.service_code.unknown` | `ServiceCodeUnknown` | yes | `shipping.service_code.unknown` | 400 | Business | yes | yes | The shipping service code is unknown. |
| 16 | `shipping.service_scope.mismatch` | `ServiceScopeMismatch` | yes | `shipping.service_scope.mismatch` | 400 | Business | yes | yes | The shipping service scope does not match the request. |
| 17 | `shipping.destination_scope.invalid` | `DestinationScopeInvalid` | yes | `shipping.destination_scope.invalid` | 400 | Business | yes | yes | The shipping destination scope is invalid. |
| 18 | `shipping.option_code.invalid` | `OptionCodeInvalid` | yes | `shipping.option_code.invalid` | 400 | Business | yes | yes | The shipping option code is invalid. |
| 19 | `shipping.service_option.unknown` | `ServiceOptionUnknown` | yes | `shipping.service_option.unknown` | 400 | Business | yes | yes | The shipping service option is unknown. |
| 20 | `shipping.rate_policy.option_mismatch` | `RatePolicyOptionMismatch` | yes | `shipping.rate_policy.option_mismatch` | 400 | Business | yes | yes | The shipping rate policy does not match the requested method. |
| 21 | `shipping.schedule.timezone.required` | `ScheduleTimeZoneRequired` | yes | `shipping.schedule.timezone.required` | 400 | Business | yes | yes | A schedule time zone is required. |
| 22 | `shipping.schedule.timezone.invalid` | `ScheduleTimeZoneInvalid` | yes | `shipping.schedule.timezone.invalid` | 400 | Business | yes | yes | The schedule time zone cannot be resolved on this runtime. |
| 23 | `shipping.schedule.weekday.invalid` | `ScheduleWeekdayInvalid` | yes | `shipping.schedule.weekday.invalid` | 400 | Business | yes | yes | The schedule weekday is invalid. |
| 24 | `shipping.schedule.window.range_invalid` | `ScheduleWindowRangeInvalid` | yes | `shipping.schedule.window.range_invalid` | 400 | Business | yes | yes | The operating window range is invalid. |
| 25 | `shipping.schedule.window.overlap` | `ScheduleWindowOverlap` | yes | `shipping.schedule.window.overlap` | 400 | Business | yes | yes | Operating windows overlap on the same weekday. |
| 26 | `shipping.schedule.window.capacity.invalid` | `ScheduleWindowCapacityInvalid` | yes | `shipping.schedule.window.capacity.invalid` | 400 | Business | yes | yes | The operating window capacity must be positive. |
| 27 | `shipping.schedule.windows.too_many` | `ScheduleWindowsTooMany` | yes | `shipping.schedule.windows.too_many` | 400 | Business | yes | yes | Too many operating windows were provided. |
| 28 | `shipping.schedule.not_found` | `ScheduleNotFound` | yes | `shipping.schedule.not_found` | 400 | Business | yes | yes | The shipping schedule was not found. |
| 29 | `shipping.schedule.duplicate` | `ScheduleDuplicate` | yes | `shipping.schedule.duplicate` | 400 | Business | yes | yes | A shipping schedule already exists for this method. |
| 30 | `shipping.schedule.option_mismatch` | `ScheduleOptionMismatch` | yes | `shipping.schedule.option_mismatch` | 400 | Business | yes | yes | The shipping schedule does not match the requested method. |
| 31 | `shipping.rate_policy.weight_tier.invalid` | `WeightTierInvalid` | yes | `shipping.rate_policy.weight_tier.invalid` | 400 | Business | yes | yes | The weight tier range is invalid. |
| 32 | `shipping.rate_policy.weight_tier.out_of_range` | `WeightTierOutOfRange` | yes | `shipping.rate_policy.weight_tier.out_of_range` | 400 | Business | yes | yes | The weight tier is out of range. |
| 33 | `shipping.rate_policy.count_tier.invalid` | `CountTierInvalid` | yes | `shipping.rate_policy.count_tier.invalid` | 400 | Business | yes | yes | The item-count tier range is invalid. |
| 34 | `shipping.rate_policy.count_tier.out_of_range` | `CountTierOutOfRange` | yes | `shipping.rate_policy.count_tier.out_of_range` | 400 | Business | yes | yes | The item-count tier is out of range. |
| 35 | `shipping.rate_policy.tier_overlap` | `RatePolicyTierOverlap` | yes | `shipping.rate_policy.tier_overlap` | 400 | Business | yes | yes | Active shipping rate tiers overlap. |
| 36 | `shipping.rate_policy.condition.not_found` | `RatePolicyConditionNotFound` | yes | `shipping.rate_policy.condition.not_found` | 400 | Business | yes | yes | The shipping rate policy condition was not found. |
| 37 | `shipping.packaging.fee.negative` | `PackagingFeeNegative` | yes | `shipping.packaging.fee.negative` | 400 | Business | yes | yes | The packaging fee cannot be negative. |
| 38 | `shipping.packaging.fee.out_of_range` | `PackagingFeeOutOfRange` | yes | `shipping.packaging.fee.out_of_range` | 400 | Business | yes | yes | The packaging fee is out of range. |
| 39 | `shipping.packaging.currency.invalid` | `PackagingCurrencyInvalid` | yes | `shipping.packaging.currency.invalid` | 400 | Business | yes | yes | The packaging currency code is invalid. |
| 40 | `shipping.packaging.policy.not_found` | `PackagingPolicyNotFound` | yes | `shipping.packaging.policy.not_found` | 400 | Business | yes | yes | The packaging policy was not found. |
| 41 | `shipping.packaging.policy.scope_duplicate` | `PackagingPolicyScopeDuplicate` | yes | `shipping.packaging.policy.scope_duplicate` | 400 | Business | yes | yes | A packaging policy already exists for this scope. |
| 42 | `shipping.packaging.policy.option_mismatch` | `PackagingPolicyOptionMismatch` | yes | `shipping.packaging.policy.option_mismatch` | 400 | Business | yes | yes | The packaging policy does not match the requested method. |
| 43 | `shipping.packaging.policy.ambiguous` | `PackagingPolicyAmbiguous` | yes | `shipping.packaging.policy.ambiguous` | 400 | Business | yes | yes | Multiple active packaging policies make this method ambiguous. |
| 44 | `shipping.packaging.freight_payer_mode.invalid` | `PackagingFreightPayerModeInvalid` | yes | `shipping.packaging.freight_payer_mode.invalid` | 400 | Business | yes | yes | The freight payer mode is invalid. |
