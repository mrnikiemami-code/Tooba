# W3-R1 — actual outbound call sites and canonical module-call spans

Scope: the two Shipping-owned outbound Contracts read adapters. Every executed foreign read gets
exactly one canonical child span; a branch that does not execute gets no span. Verified against the
actual adapter source, `ShippingModule` registration and every direct constructor caller (none outside
`Tooba.Shipping.Tests` — production composition is DI-only).

| # | Adapter (Shipping-owned) | Foreign Contracts member | Executed when | Span name | Tags (canonical only) | Outcome annotation |
|---|---|---|---|---|---|---|
| 1 | `GeographyDestinationScopeValidator` | `IGeographyLookup.GetByIdsAsync(DestinationIdentifiers, ct)` | Always, after the `scope` null-guard; exactly once | `module.Shipping.Geography.ValidateDestinationIdentifiers` | `tooba.module.source=Shipping`, `tooba.module.target=Geography`, `tooba.module=Geography`, `tooba.operation=ValidateDestinationIdentifiers`, `tooba.request_kind=module_call`, `tooba.correlation_id=<ambient>` | `SetOk()` on accepted scope; `SetBusinessFailure(shipping.destination_scope_invalid)` when Geography returns `null`; `SetError(ex)` + `throw;` on dependency throw |
| 2 | `FulfillmentShippingMethodOptionValidator` | `IShippingCatalogReader.ListAsync(ct)` | Always, after the local empty-service-code rejection | `module.Shipping.Fulfillment.ListShippingCatalog` | `tooba.module.source=Shipping`, `tooba.module.target=Fulfillment`, `tooba.module=Fulfillment`, `tooba.operation=ListShippingCatalog`, `tooba.request_kind=module_call`, `tooba.correlation_id=<ambient>` | `SetOk()` on a completed lookup (unknown service is still a completed lookup); `SetError(ex)` + `throw;` on dependency throw |
| 3 | `FulfillmentShippingMethodOptionValidator` | `IShippingOneLevelCanonicalMethodReader.IsCanonicalMethodBindingAsync(code, option, ct)` | Only when the reader is present **and** the legacy option row was absent (canonical fallback branch) | `module.Shipping.Fulfillment.ResolveCanonicalMethodBinding` | same canonical key set, `tooba.operation=ResolveCanonicalMethodBinding` | `SetOk()` for both `true` and `false` (false is a completed lookup, not an infrastructure error); `SetError(ex)` + `throw;` on dependency throw |

## Span counts proven by real listener tests

| Scenario | Spans | Test |
|---|---|---|
| Geography accepted scope | 1 (Geography) | `Geography_read_emits_one_child_span_with_canonical_identity_and_restores_the_parent` |
| Geography `null` (unknown/inactive/inconsistent) | 1, business failure | `Geography_rejection_is_one_completed_span_marked_business_failure` |
| Geography throws | 1, `exception.type` only, same exception rethrown | `Geography_dependency_failure_records_type_only_and_rethrows_the_same_exception` |
| Geography cancels | 1, `OperationCanceledException` still propagates | `Geography_cancellation_stays_cancellation_and_never_becomes_a_business_failure` |
| Fulfillment service-level (option `null`) | 1 (catalog) | `Service_level_success_emits_exactly_one_catalog_span_and_skips_the_canonical_branch` |
| Fulfillment legacy option hit | 1 (catalog) | `Legacy_option_success_emits_one_catalog_span_and_no_canonical_span` |
| Fulfillment canonical fallback, binding `true` | 2 distinct (catalog + canonical) | `Canonical_fallback_emits_two_distinct_fulfillment_spans` |
| Fulfillment canonical fallback, binding `false` | 2, both `Ok`, Shipping rejection unchanged | `Canonical_false_is_a_completed_lookup_and_the_shipping_rejection_is_unchanged` |
| Canonical reader absent | 1 (catalog only) | `Absent_canonical_reader_emits_one_span_and_no_fabricated_call` |
| Empty/whitespace service code | 0 foreign spans | `Local_rejection_emits_zero_foreign_spans` |
| Fulfillment throws | 1, `exception.type` only, same exception rethrown | `Fulfillment_dependency_failure_records_type_only_and_rethrows_the_same_exception` |

## Sanitized exported trace shape (asserted values, not invented)

```
trace_id=<incoming trace>                      # shared with the MediatR/HTTP span and the correlation scope
└── module.Shipping.Geography.ValidateDestinationIdentifiers
      parent_span_id=<incoming span>           # child of the ambient span, not the root
      status=Ok | Error
      tags: tooba.correlation_id, tooba.module=Geography, tooba.module.source=Shipping,
            tooba.module.target=Geography, tooba.operation=ValidateDestinationIdentifiers,
            tooba.request_kind=module_call
      (business rejection) result.status=business_failure, error.code=shipping.destination_scope_invalid
      (dependency throw)   exception.type=System.InvalidOperationException   # type only, no message
└── module.Shipping.Fulfillment.ListShippingCatalog
      tags: ... tooba.module=Fulfillment, tooba.operation=ListShippingCatalog ...
└── module.Shipping.Fulfillment.ResolveCanonicalMethodBinding
      tags: ... tooba.module=Fulfillment, tooba.operation=ResolveCanonicalMethodBinding ...
```

Exclusion proof: `Submitted_identifiers_and_exception_messages_are_never_exported_as_tags` asserts the
exported tag values never contain the service code, the option code, the country/city ids, or the
sentinel exception message, and that no span carries any event (no logging was added).
