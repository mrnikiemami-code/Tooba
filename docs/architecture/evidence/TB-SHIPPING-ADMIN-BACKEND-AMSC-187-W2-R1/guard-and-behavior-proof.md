# Guard replacement and behavior proof

## Old vs new invariant

| | Old (`TB-076`) | New (`TB-079`/`TB-080` boundary) |
| --- | --- | --- |
| Rule | "No production file outside `Modules/Shipping` may contain any of 5 operational markers." | "Only the reviewed observation/comparison pair plus the single Order DI registration may consume the operational Contracts; nobody outside Shipping may reference Shipping implementation; the Order edge is registration-only; the gate stays OFF and the observation is discarded." |
| Detection | blank path exclusion (`IsShippingModule`) + substring | same substring detection **plus** path allowlist of exact reviewed roles, **plus** implementation-marker scan inside the allowed files, **plus** registration-only line rule, **plus** negative cases that must fail |
| Failure mode | rejects an approved architecture decision | rejects real expansion: a 4th consumer, an implementation import inside an allowed file, an execution path in the composition root, a peer module reaching Shipping implementation |
| Weakness it keeps | none — it is strictly narrower on what may be consumed by whom | still does not prove data flow by string matching; paired with exact source review + behavioral tests |

The historical test `No_module_outside_shipping_references_the_operational_quote_types` was renamed to
`Only_the_reviewed_order_observation_files_consume_the_operational_contracts` with the reason recorded in
the class `<remarks>`; it was not deleted and was not weakened into "Order is excluded".

## What the replacement actually asserts (13 facts)

1. `Only_the_reviewed_order_observation_files_consume_the_operational_contracts` — enumerated outside-Shipping
   consumer set **equals** the three reviewed paths (set equality, not "is a subset of Order"), and the
   analyzer returns zero violations for the real tree.
2. `Approved_observation_files_use_only_public_contracts_and_never_an_implementation_import` — every
   `using Tooba.Shipping.*` in the comparer/observer must start with `Tooba.Shipping.Contracts.`; zero
   implementation markers; no `DbContext`, no `IQueryable`.
3. `The_order_module_edge_is_registration_only` — every operational line in `OrderModule.cs` must contain
   `GetService<`; no `QuoteAsync(`, no `.ObserveAsync(`, no request type; exactly one observer factory;
   the options section is registered.
4. `The_real_projection_discards_the_observation_and_the_gate_stays_off_by_default` — options have no
   `= true` default; the projection awaits the observer **without binding** the result
   (`= await operationalShadow.ObserveAsync(` must be absent), still returns `result`, and never assigns
   `SelectedShippingAmount` / `MinimumDeliveryDate`.

Negative detection proof (each one must fail; a mere "the allowed files exist" assertion would not):

5. `Detector_fails_for_an_unexpected_fourth_consumer_outside_the_reviewed_set` — a synthetic consumer under
   `Order.Application/Storefront/Shipping/RateAuthority/` yields exactly one
   `UNEXPECTED_OPERATIONAL_CONTRACT_CONSUMER` naming that file. This proves an Order-path file is **not**
   blanket-excluded.
6. `Detector_fails_for_an_implementation_reference_inside_an_otherwise_allowed_file` — injecting
   `typeof(Tooba.Shipping.Domain.Operations.ShippingOperationalQuoteEvaluator)` into the **approved**
   observer yields `FOREIGN_SHIPPING_IMPLEMENTATION_REFERENCE` for that same file. Allowlisting a file
   does not allowlist its content.
7. `Detector_fails_for_an_evaluator_reference_inside_an_allowed_comparer_file` — injecting
   `typeof(ShippingRatePolicyEngine)` into the **approved** comparer is caught.
8. `Detector_fails_for_a_non_registration_use_in_the_order_module` — a synthetic
   `GetRequiredService<...IShippingOperationalQuoteLookup>()` + `QuoteAsync(...)` inside `OrderModule.cs`
   yields `ORDER_EDGE_NOT_REGISTRATION_ONLY`: registration must not become execution.
9. `Detector_fails_for_a_peer_module_reaching_shipping_implementation` — a synthetic Geography file holding
   `Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext` is rejected (every violation is the
   foreign-implementation rule against that file).

Preserved historical invariants (unchanged assertions):

10. `The_legacy_read_seams_keep_their_exact_shape` — four read seams still exist, no writer/capacity seam,
    one method each.
11. `The_operational_contract_never_carries_a_success_shaped_zero` — all five money fields stay
    `decimal?`.
12. `The_operational_slice_adds_no_migration_and_no_money_movement` — no `20261015090000_OperationalQuote`
    migration; no migrator/`Database.MigrateAsync`/transaction call inside Shipping.
13. `Capacity_is_declared_unenforced_by_the_type_system` — enum still only `NotEnforced`.

## Behavioral proof (executed, not source-pinned)

`src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingOperationalShadowObserverTests.cs`
already provides the execution-level proof and was **reused without modification** (no duplicate tests
were added). It runs the real observer/comparer with fakes and covers, per test:

| Behavioral guarantee | Test |
| --- | --- |
| both gates default OFF ⇒ zero quote calls, zero geography calls | `Operational_shadow_is_off_by_default_and_performs_no_query` |
| enabled without confirmed prerequisites ⇒ zero quote calls | `Operational_shadow_enabled_without_confirmed_prerequisites_performs_no_lookup` |
| simple-rate gate cannot enable operational observation | `Simple_rate_shadow_gate_and_operational_gate_are_independent` |
| missing measurement is reported, a tiered comparison is never fabricated | `Missing_measurement_is_reported_and_no_tiered_comparison_is_fabricated` |
| measurement without trusted provenance is refused before any call | `Measurement_without_a_trusted_provenance_is_refused` |
| real grams/count/subtotal/currency/option forwarded verbatim | `Real_measurement_reaches_the_authority_verbatim` |
| tier boundary decided by the authority, never re-derived here | `Tier_boundary_is_priced_by_the_authority_and_never_re_derived_here` |
| absent/unresolvable destination is not comparable and never guessed | `Absent_destination_is_not_comparable_and_never_guessed`, `Unresolved_legacy_label_is_not_comparable` |
| freight parity with no packaging fee is a match | `Freight_parity_with_no_packaging_fee_is_a_match` |
| charged packaging is separate and never folded into the customer amount | `Charged_packaging_is_reported_separately_and_never_folded_into_the_customer_amount` |
| COD split is an observed fact with no payment effect | `Collect_on_delivery_split_is_observed_as_a_fact_without_any_payment_effect` |
| freight mismatch leaves the legacy amount authoritative | `Freight_amount_mismatch_is_reported_and_the_legacy_amount_stays_authoritative` |
| missing coverage never becomes allowed/free | `Missing_coverage_is_reported_and_never_becomes_allowed_or_free` |
| authority-side measurement requirement is a measurement gap | `Authority_side_measurement_requirement_is_reported_as_a_measurement_gap` |
| ambiguous packaging reported without any amount | `Ambiguous_packaging_is_reported_without_any_amount` |
| carrier window is carrier availability, not a customer promise | `Carrier_window_is_observed_as_carrier_availability_not_a_customer_promise` |
| configured capacity is never presented as enforced/remaining | `Configured_capacity_is_never_presented_as_enforced_or_remaining` |
| not-evaluated schedule is disclosed, never treated as available | `Not_evaluated_schedule_is_disclosed_and_never_treated_as_available` |
| a throwing authority degrades per candidate and never escapes | `Throwing_authority_degrades_per_candidate_and_never_escapes` |
| uncomposed seams are shadow-unavailable without any query | `Uncomposed_read_seams_are_shadow_unavailable_without_any_query` |

Plus the composition guard class in the same file (`StorefrontShippingOperationalShadowCompositionGuardTests`,
5 facts) which pins the default-OFF options, the projection discarding the observation, the single Order
registration, no path to price/draft/snapshot, and no manufactured weight.

Cancellation semantics: the observer rethrows `OperationCanceledException` only when the caller's token is
cancelled and converts everything else into an observation code — visible at the two `catch` blocks in the
observer and covered by `Throwing_authority_degrades_per_candidate_and_never_escapes` (non-cancellation
exceptions) while no behavior change was made.

## Honest limits

* The Shipping-side guard is a source guard; it is **not** claimed as runtime/browser parity or E2E.
* No production file, schema, migration, flag, setting or DI lifetime was changed; nothing was made green
  by relaxing behavior — the only red test removed was the obsolete assertion itself.
* `structureCertified` remains `false`; the W2 structure manifest entry was not touched.
