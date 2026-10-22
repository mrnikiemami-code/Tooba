# RESULT — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_REGISTRY_LIFECYCLE_MODULE_TRACING_AND_REAL_DI_EVIDENCE
Expected-Base-SHA: 0399dd5933165f6ca473eb2d2a6751e2ac41ec1c
Base-SHA: 0399dd5933165f6ca473eb2d2a6751e2ac41ec1c
Verdict: worker PASS for this bounded nonrelational slice only — NOT Architect ACCEPT.
Claim-UTC: 2026-10-22T11:11:10Z

## Scope and honesty statement

Test/evidence only. No production file, Contracts, endpoint, resource, Domain, EF mapping,
migration, designer, snapshot, csproj or package was changed. The single new file is a
Fulfillment test file; no skill/manifest/architecture promotion, no activation, no UI claim.

EF **InMemory** and a **controlled Contracts `ILanguageLookup` fixture** were used. This is not
PostgreSQL, not migration/schema discovery, not a live merchant Localization database, not a
relational snapshot/constraint/race proof and not production-functional acceptance.

## A. Actual production registration invoked, only data boundaries overridden

`new FulfillmentModule().AddServices(services, new ConfigurationBuilder().Build(), StubHostEnvironment)`
is invoked verbatim — the real module composition entry, not a hand-copied capability wiring.
Foundation/CQRS needed for the slice come from the canonical registrations
`AddToobaObservabilityFoundation()` and `AddToobaCqrsFoundation(typeof(CreateShippingTypeCommand).Assembly)`.

Boundaries overridden (recorded so production capability wiring stays independently assessed):

| Replaced | Exactly what was removed / added | Still production-owned |
| --- | --- | --- |
| EF provider | removed `FulfillmentDbContext`, `DbContextOptions<FulfillmentDbContext>`, `DbContextOptions` descriptors (whose option action resolves Npgsql via `ICurrentCommerceContext`/`IDatabaseConnectionResolver`) and re-added `AddDbContext<FulfillmentDbContext>(UseInMemoryDatabase)` | descriptor shape only; store/reader/adapter/gate untouched |
| Clock | removed `IClock` descriptor, added a test clock fixed at 2026-10-22T11:00:00Z | no production clock logic |
| Language registry | added `ILanguageLookup` singleton fixture (module does not register it) | the real `LocalizationShippingTypeLanguageFacts` consumes it |

Not overridden and therefore proven production: `ShippingTypeStore`, its two alias factories,
`IShippingTypeLanguageFacts -> LocalizationShippingTypeLanguageFacts`, `IModuleCallTracer ->
ModuleCallTracer`, all four handlers and `CreateShippingTypeCommandValidator`.

Asserted results (test `Actual_module_registration_wires_the_capability_and_scoped_aliases`):

- concrete `ShippingTypeStore` == `IShippingTypeStore` == `IShippingTypeReader` in one scope; a second
  scope resolves a **different** store instance (real scoped lifetime, not a singleton masquerade);
- `IShippingTypeLanguageFacts` is exactly `LocalizationShippingTypeLanguageFacts`;
- `IModuleCallTracer` is exactly `ModuleCallTracer`;
- `CreateShippingTypeHandler`, `UpdateShippingTypeHandler`, `GetShippingTypeHandler`,
  `ListShippingTypesHandler` and `CreateShippingTypeCommandValidator` resolve from real DI;
- a real create + read through `ISender` works on the InMemory boundary.

`Production_registration_has_no_duplicate_capability_owner` asserts exactly one descriptor for
`ShippingTypeStore`, `IShippingTypeStore`, `IShippingTypeReader`, `IShippingTypeLanguageFacts`,
`IModuleCallTracer` and exactly one `FulfillmentErrorCatalogContributor` (no second resource owner).

`The_module_migrator_is_registered_but_never_executed_or_resolved` records the module migrator
descriptors as registration evidence only. No migrator, hosted service or provider connection was
resolved or started; no schema discovery or migration call was made.

## B. Real adapter -> handlers -> store language matrix

Every case below executes the **registered** adapter (not a hand-built `ShippingTypeLanguageFacts`),
real CQRS handlers/validation and the real store over the fixture registry; registry rows carry
genuine fixture ids plus `IsActive`/`IsDefault`/`SortOrder`/`Direction`.

| Fixture case | Asserted fact | Result |
| --- | --- | --- |
| ONE active non-`fa` default (`ar` default, `fa` active non-default, rows ordered `fa`,`ar`) | create uses `ar`, `DefaultLanguageId == ar`, `HasDefaultLanguageName == true`, only `ar` stored, no `fa` fallback, name trimmed (`  نوع عادی  ` -> `نوع عادی`) | PASS |
| TWO active mixed RTL/LTR (`fa` RTL default + `en` LTR) | patch `en` keeps `fa` name/description **byte-identical**; explicit `null` description clears only `en`; empty patch `[]` preserves all text | PASS |
| THREE+ known languages, NON-first active default, one inactive (`fa`,`ar`,`en` active with `ar` default; `de` inactive) | create honours the declared `ar` default (not list position); explicit active `en` patch succeeds; editing inactive `de` fails `shipping_type.language_invalid` with revision/active/text unchanged and no `de` row | PASS |
| same stored type deactivate/reactivate | GET retains the exact stored `en` name/description while `en` is inactive; edit of the inactive id fails `shipping_type.language_invalid` with unchanged revision and stored text; after reactivation GET reveals the exact previous text (no delete, no auto-translation) | PASS |
| registry default switch to an active language with no stored name | GET returns `DefaultLanguageId == en` and `HasDefaultLanguageName == false`; activation attempt fails `shipping_type.default_language.name_required` and the type stays inactive; after explicitly writing `en` name, activation succeeds and the type becomes active (catalog state only — no quote-ready claim) | PASS |
| invalid registries (8 cases) | `IsActive` absent -> `language_invalid`; `SortOrder` absent -> `language_invalid`; `Guid.Empty` -> `language_invalid`; duplicate id -> `language_invalid`; no active -> `default_language.unavailable`; no default -> `default_language.unavailable`; inactive default -> `default_language.unavailable`; two defaults -> `default_language.ambiguous`; every case persisted zero rows, no guessed default/order | PASS |
| culture invariance | identical stored `LanguageId`/name/description pairs under `fa-IR` and `en-US` `CurrentCulture`+`CurrentUICulture` (restored in `finally`); no locale-to-language logic | PASS |

Successful no-op/revision semantics are **not** re-proved here; R4's advancing-clock no-op proof is
accepted and reused as-is.

## C. Real `ActivityListener` proof on the canonical tracer

A real `ActivityListener` is attached to the canonical `ToobaTelemetry.ActivitySource`, samples
`AllDataAndRecorded`, and filters to `module.Fulfillment.Localization.` only; the collection is
disabled for parallelization so unrelated tests cannot falsify counts.

| Assertion | Observed |
| --- | --- |
| successful `ILanguageLookup.ListAsync` emits EXACTLY ONE child span | 1 span; name `module.Fulfillment.Localization.ListLanguages`; `tooba.module.source=Fulfillment`; `tooba.module.target=Localization`; `tooba.module=Localization`; `tooba.operation=ListLanguages`; `tooba.request_kind=module_call`; `tooba.correlation_id` equals the ambient correlation; status `Ok`; `exception.type` absent |
| parent/trace identity and restoration | `ParentSpanId == parent.SpanId`, `TraceId == parent.TraceId`, `Activity.Current` is the parent again afterwards; span duration is set (stopped/disposed once — one `ActivityStopped` per call) |
| count tracks the fixture, not a guessed handler count | three real adapter reads -> exactly 3 spans and 3 fixture `ListAsync` calls |
| invalid registry facts | lookup completed successfully (span status `Ok`, no `exception.type`); the subsequent business validation yields `shipping_type.language_invalid` with zero rows persisted — not a dependency exception |
| throwing lookup | span status `Error`, `exception.type = System.InvalidOperationException`, the **same** exception instance rethrown |
| caller cancellation | the **same** `OperationCanceledException` instance and the caller's exact token are preserved and rethrown (never swallowed into business success/empty list); span status `Error` with `exception.type = System.OperationCanceledException` |
| no sensitive telemetry | tags/events contain none of: the stored name, stored description, type code, either `LanguageId` rendering, `tenant`, `IRR`, or the sentinel exception message; no events emitted |

No mock-only tracer, no no-listener shortcut, no optional/no-op tracer fallback, no new
ActivitySource/correlation framework, no production instrumentation change.

## Focused commands and counts

```text
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj -v q --nologo
  -> 0 errors

dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj \
  --filter "FullyQualifiedName~ShippingTypeRegistryAndModuleDiTests"
  -> Passed! Failed: 0, Passed: 22, Skipped: 0, Total: 22 (~0.57 s)
```

Only `Tooba.Fulfillment.Tests` (and its references) was built; no Host/solution build, no broad
Architecture filter, no repeated confidence run. One bounded harness-only correction was applied
(a `CancellationToken` struct identity assertion corrected to value equality, keeping the exception
`Assert.Same` identity) and the slice was re-run once.

## Limitations kept explicit

- InMemory provider, controlled Contracts registry fixture: not PostgreSQL, not relational schema,
  not snapshot, not unique-constraint, not concurrent-revision-race proof.
- Registry mutations are simulated in the fixture, not executed against a real Localization database
  or its admin UI; no live registry/browser acceptance is implied.
- Activation here is catalog row state only; it makes no shipping method quote-ready and grants no
  customer-facing activation, assignment, grouping or deployment flag.
- Constraint-lookalike negative and PostgreSQL historical-schema/snapshot/unique/revision-race proof
  remain unissued gates.

## TB-196 state

`PARTIAL_PENDING_REMAINING_PROOF` — the bounded nonrelational gates now covered are Domain (R2),
HTTP/API (R3+R4), real module DI registration, adapter-backed registry lifecycle and real
module-call tracing. Still open: PostgreSQL relational/snapshot/constraint/unique/revision-race proof
and the exact-constraint lookalike negative. Worker PASS is not Architect ACCEPT.
Stop-State: STOP_FOR_ARCHITECT. No automatic next task.
