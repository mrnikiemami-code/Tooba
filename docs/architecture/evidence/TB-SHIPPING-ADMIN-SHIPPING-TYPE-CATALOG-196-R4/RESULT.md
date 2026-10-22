# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4 — RESULT

Mode: `BOUNDED_API_NONEMPTY_NOOP_ERRORCODE_AND_DISPATCH_PROOF_CORRECTION`
Status: **PASS for the three repaired evidence claims only** (whole TB-196 remains
`PARTIAL_PENDING_REMAINING_PROOF`).
Expected-Base-SHA: `151440a2bde6aa6daed96549beda27273a553ae0`
Base-SHA: `151440a2bde6aa6daed96549beda27273a553ae0`
Branch: `main`

TEST/EVIDENCE ONLY. The existing R3 harness file was corrected in place; no second harness was written and no
production C# / Contracts / DI / schema / EF / migration / designer / snapshot / resource / Host / frontend /
Shipping file changed.

## CORRECTION A — actual nonempty no-op with an advancing clock

`R196-R3-01` repaired inside `Authorized_lifecycle_create_read_activate_deactivate_noop_and_conflict`:

1. After the real activate `PUT`, the test `GET`s the row through the real detail delegate and builds the patch
   from the **actually stored** `languageId`, `name` and `description` — it no longer invents `translations:[]`.
2. `MutableClock` (test fixture) is advanced `+5 minutes` before the repeat; the test asserts the advanced
   instant differs from the persisted `UpdatedAt`.
3. The repeat `PUT` carries that **nonempty** translation. Assertions: `200`, raw text unchanged
   (name + description), `revision` still `2`, and the persisted `UpdatedAt` is **exactly** the pre-repeat
   value and **not** the advanced clock instant. So both a mistaken revision bump and a timestamp-only
   restamp are detected.
4. A stale `expectedRevision=1` with the same identical **nonempty** payload returns `409`
   `shipping_type.revision_conflict`, with revision and persisted timestamp unchanged.

No sleep, no production clock change, no direct-store substitute.

## CORRECTION B — exact root/child validation codes and measured arrival

`R196-R3-02` repaired:

- The unused `"_"` theory argument is replaced by `(property, expectedChildCode)` from the authoritative
  `FulfillmentValidationCodes` declarations.
- Every shaped-invalid `POST` case now asserts the canonical root `validation.failed` **and** the exact child
  code under its FluentValidation property, e.g. `Model.SortOrder` →
  `fulfillment.validation.shipping_type_sort_order_shape`, `Model.Code` →
  `fulfillment.validation.shipping_type_code_shape`, `Model.Translations` →
  `…translations_required` / `…translation_shape` / `…translation_duplicate_language`. The same applies to
  every `PUT` case (`Model.IsActive`, `Model.ExpectedRevision`, `Model.Translations`) and to the
  `Guid.Empty` `GET` (`ShippingTypeId` → `fulfillment.validation.shipping_type_id_required`).
- One overlength-name case (`new string('A', 129)` = `NameMaxLength + 1`) was added; it asserts the root code
  and `…translation_shape`.
- Transport shape is explicitly distinguished from validation: malformed bodies assert
  `shipping_type.body.malformed`, **not** `validation.failed`, and **no** `errors` map.
- `Measured-PostValidation-Arrival`: a test-only `PostValidationArrivalBehavior<,>` is registered **last**, so
  MediatR runs it after the real `ValidationBehavior`; it increments `Counters.Arrivals`. Validation
  rejections assert `Arrivals == 0`, while a valid `POST` asserts `Arrivals == 1`. The claim is precisely
  "arrival past validation", not an instrumentation inside a production handler.
- No message parsing, no tolerant status-only expectation, no production error change.

## CORRECTION C — observed delegate → request → handler/validator

`R196-R3-03` repaired:

- Both-direction route-string equality against the actual four shipped routes is retained.
- Each of the four actual delegates is now invoked with VALID input (counters reset per invocation,
  `200/201` asserted). The dispatched request type is read from the **observed** `CountingSender.SenderRequests`
  (exactly one per invocation), not assigned from a route-string switch; the observed mapping is compared to
  the capability's expected table in both directions.
- `IRequestHandler<TRequest,TResponse>` is resolved from real DI per route (response type taken from the
  request's own `IRequest<TResponse>`) and must be exactly `ListShippingTypesHandler`,
  `GetShippingTypeHandler`, `CreateShippingTypeHandler`, `UpdateShippingTypeHandler`; the three validators
  resolve with their exact types and `List` stays `NO_VALIDATOR_REQUIRED` with zero validators.
- A new mapping guard `VerifyObserved(routes, observed)` — same four route strings — throws when any observed
  request is swapped. Negative: `GET list → GetShippingTypeQuery`, `GET detail → ListShippingTypesQuery`,
  `POST → UpdateShippingTypeCommand`, `PUT → CreateShippingTypeCommand` is rejected; the real mapping passes
  the same guard. The existing extra-export and dropped-route negatives remain.

## Focused commands and counts

```
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj -> 0 errors
dotnet test  ... --filter "FullyQualifiedName~ShippingTypeCatalogEndpointTests"
                                       -> Failed: 0, Passed: 45, Skipped: 0, Total: 45
```

One bounded harness correction was needed after the first corrected run: the observation test's invented code
`observed-post` violated the authoritative code shape (lowercase + digits + `_` only) and was changed to
`observed_post`; the filter was then rerun once. No expected semantics were relaxed.

## Honest qualification of the R3 harness (appended correction, not a rewrite)

- The original R3 no-op fact repeated `translations:[]`; it did not execute a nonempty patch.
- The original validation facts asserted only `400` with an unused theory argument; they did not assert the
  root/child codes and did not measure post-validation arrival.
- The original route mapping was assigned manually from route strings; it did not observe dispatch types.
- The harness invokes the production endpoint-presentation registration directly, but the store and
  language-adapter registrations are **manually reproduced** in the test composition — this is production
  types in a test composition, not independent verification that every production Infrastructure DI
  registration is intact.
- The registry fixture is `fa` (default) + active non-fa `en`; it is **not** a single non-fa default and not a
  live Localization DB.

## Still pending (not waived)

`Remaining-Registry-Listener-DI-Constraint-Relational-Gates: PENDING_NOT_RUN` — full dynamic multilingual
registry lifecycle, real `ActivityListener` tracing evidence, independent production Infrastructure DI
verification, exact-constraint lookalike negative (`ix_shipping_types_code`,
`ix_shipping_type_translations_type_language`), and PostgreSQL snapshot/relational/race proof.

EF InMemory only. TB-196 acceptance remains `PARTIAL_PENDING_REMAINING_PROOF`. Production functional
acceptance: PENDING.

## Changed files

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingTypeCatalogEndpointTests.cs` (corrected)
- `docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4.task.md` (received artifact, verbatim)
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4/RESULT.md` (this file)
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3/RESULT.md` (appended correction)
- `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md` (Worker status pointer only)

Production schema/contract changes: NONE. Shared database/data/flag changes: NONE.
