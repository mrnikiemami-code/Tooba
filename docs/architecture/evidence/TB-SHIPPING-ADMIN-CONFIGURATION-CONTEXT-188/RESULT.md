# TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188 — Result Evidence

- Task: `TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188`
- Parent: `TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1`
- Program: `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY`
- Mode: `BOUNDED_DB_BACKED_SHIPPING_AUTHORING_CONTEXT_READ`
- Channel: `tooba-main` / Branch: `main`
- Expected base: `21d3900503475ce26052c33b97bb420cd8ec765c` (matched `HEAD` and `origin/main` before edits)
- Kind: **additive authorized feature**, not an AMSC recovery cycle, not a frontend connection, not module recertification.
- Stop state: `STOP_FOR_ARCHITECT`

## 1. What was delivered

One authorized, read-only admin endpoint:

```
GET /v1/admin/shipping-services/authoring-context
```

It is the first product-facing backend read for the accepted Shipping Admin: it returns the
effective installation edition, the configured default currency and the ordered **ACTIVE**
content languages with their owner-provided metadata and exactly one declared default.

Nothing in this task activates a rule, writes a setting, seeds a language, chooses a default,
grants seller/owner authority, or connects the UI.

## 2. Source authority map

| Fact | Authority | Boundary used |
| --- | --- | --- |
| Effective edition (`SingleStore` / `Marketplace`) | `ICurrentEdition` (BuildingBlocks) | existing resolved platform context |
| Configured default currency | `ICurrentStoreCommerceContext` (StoreContext.Contracts) | existing resolved commerce context |
| Active content languages + direction/names/order/default | `ILanguageLookup.ListAsync` (Localization.Contracts) | public Contracts port only |
| Readiness decision / projection | Shipping Application query handler | Shipping-owned |

`ILanguageLookup` resolves to Localization's `LanguageLookupBridge`, which reads the authoritative
`LanguageDirectory` over `LocalizationDbContext` of the resolved commerce database
(`SingleStore`: that tenant/store's database; `Marketplace`: the installation registry). No second
registry, copy, cache or fallback policy was introduced, and no LanguageDirectory/domain/HTTP/schema
change was made.

No owner selector, no seller identity inference, no Shipping-owned mode/currency flag, no locale →
currency or locale → language derivation, and no cross-schema join.

## 3. Additive Localization lookup metadata

`LanguageLookupSnapshot` keeps its five positional members and deconstructor and gained
**non-positional, init-only** members:

`IsActive`, `Direction`, `DisplayName`, `NativeName`, `SortOrder`.

- `LanguageLookupBridge` maps each of them from the authoritative `LanguageSnapshot` — `IsActive`
  comes from the actual row, never an inferred default.
- `ListAsync` still returns the **known** language set in its existing order; inactive rows remain
  available to existing callers. Shipping selects active rows in its own read projection, so no
  activation policy moved into Shipping and no existing caller behavior changed.
- A snapshot built by old code without the new members leaves them `null`/absent, and Shipping treats
  absent metadata as *not usable* rather than as a real direction/name.

## 4. Response contract

`Tooba.Shipping.Application.Authoring.Models.ShippingAuthoringContextView` (Application-internal read
model, deliberately not in `Shipping.Contracts`):

```json
{
  "edition": "SingleStore",
  "defaultCurrency": "IRR",
  "contentLanguages": [
    {
      "languageId": "5f0f0a4c-0000-0000-0000-000000000001",
      "code": "fa",
      "culture": "fa-IR",
      "urlPrefix": "",
      "direction": "rtl",
      "displayName": "فارسی",
      "nativeName": "فارسی",
      "isDefault": true,
      "sortOrder": 0
    },
    {
      "languageId": "5f0f0a4c-0000-0000-0000-000000000002",
      "code": "en",
      "culture": "en-US",
      "urlPrefix": "en",
      "direction": "ltr",
      "displayName": "English",
      "nativeName": "English",
      "isDefault": false,
      "sortOrder": 1
    }
  ],
  "defaultContentLanguageId": "5f0f0a4c-0000-0000-0000-000000000001"
}
```

(Sample values are sanitized fixture values, not a live tenant's data.)

`defaultContentLanguageId` always references one member of `contentLanguages`; ordering is the
owner-declared `SortOrder`, never the request locale or a hardcoded fa/en order.

## 5. Fail-closed readiness and the new error

`shipping.authoring.configuration_unavailable` — explicit **HTTP 503**, `ErrorClassification.Platform`,
`ErrorSeverity.Error`, with neutral + `fa` resources, emitted through the canonical
`ApiResponseFactory` / problem-details path:

```json
{
  "type": "https://tooba/errors/shipping.authoring.configuration_unavailable",
  "title": "Shipping authoring configuration is unavailable",
  "status": 503,
  "code": "shipping.authoring.configuration_unavailable",
  "severity": "Error",
  "classification": "Platform"
}
```

Readiness is unavailable when: edition is `Unset`/not `SingleStore|Marketplace`; default currency is
blank; zero **active** languages; any active language lacks required identity/direction/display name/
native name/sort order; zero or more than one active default (an inactive-only default is therefore
unavailable — Shipping does not promote it).

Dependency exceptions and caller cancellation are **not** caught into a fake success/empty context;
they propagate through the existing canonical boundary, and the module-call span records only the
error type and outcome.

The 44 pre-existing Shipping semantic codes keep their **HTTP 400 / Business / Warning** contract
unchanged; only the new code carries the explicit 503 descriptor.

## 6. Guarded module call

Every real Shipping → `ILanguageLookup.ListAsync` call executes inside one canonical
`IModuleCallTracer` child span:

- source `Shipping`, target `Localization`, operation `ListConfiguredLanguages`
  (`module.Shipping.Localization.ListConfiguredLanguages`);
- correct parent/trace/correlation, single disposal, parent restoration;
- tags carry source/target/operation/outcome and at most the exception **type** — no language IDs,
  codes, names, currency, tenant or connection data.

When the trusted edition/currency context is already unresolved, the adapter performs **no** foreign
read and returns an incomplete snapshot that the handler turns into the explicit unavailable result,
so a missing platform context can never manufacture a language list or currency.

## 7. Route/input classification

- Literal, parameterless route mapped under the existing `/v1/admin` group with the real
  `IShippingAdminAuthorizer` and existing `fulfillment.view` permission.
- No route parameter, no query/body binding, no owner/mode/tenant/language/currency override; browser
  headers or query strings cannot alter the resolved authority. Authorization is therefore the only
  gate, and it is not standing in for absent input validation.
- Classified as the single `NO_VALIDATOR_REQUIRED` request with explicit provenance: parameterless
  record query constructed in the endpoint, literal route, no `[FromRoute]/[FromQuery]/[FromBody]`
  binding and no validator type. Totals updated: 17 routes / 17 requests / 16 request validators /
  1 no-validator request / 22 total validators.
- Literal-vs-`{serviceCode}`/`{serviceId}` precedence is proven by an actual in-process routing test
  (`WebApplication` + `TemplateMatcher`), not a string assertion.

## 8. Tests and commands

| Command | Result |
| --- | --- |
| `dotnet test .../Tooba.Shipping.Tests.csproj --filter FullyQualifiedName~Authoring` | **43 passed**, 0 failed |
| `dotnet test .../Tooba.Shipping.Tests.csproj` (final full module suite) | **451 passed**, 0 failed, 0 skipped |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~LanguageLookup` | **6 passed**, 0 failed |
| `dotnet build src/backend/Tooba.slnx` | **0 errors** (46 pre-existing warnings) |

Coverage added (Shipping, 43 tests across three files):

- `Authoring/ShippingAuthoringContextQueryTests.cs` — one active non-`fa` language; two active; three+
  active with RTL+LTR and an excluded inactive row; default not first; owner order preserved; default
  always inside the active list; all readiness failures; context isolation between two resolved
  fixtures; exception/cancellation propagation.
- `Authoring/ShippingAuthoringContextSourceTests.cs` — real module DI resolution, single
  `module.Shipping.Localization.ListConfiguredLanguages` child span with parent/correlation preserved,
  no foreign read when context is unresolved, error-type-only annotation, exception rethrow.
- `Authoring/ShippingAuthoringContextEndpointTests.cs` — authorized in-process GET status/typed body,
  unauthorized denial before the query runs, 503 problem details, literal route precedence over
  `{serviceCode}`/`{serviceId}`, query-parameter immunity.

Localization coverage (`Host/Tooba.Host.Tests/LanguageLookupBridgeMappingTests.cs`, existing test
home): positional constructor/deconstruction and known-language set/order preserved, active/inactive/
default/direction/name/order projected unchanged, absent metadata stays absent.

Existing W3-R1 trace tests, module structure/composition guard and error catalog/resource guard remain
green after their TB-188-specific updates.

## 9. DB provenance: source-backed vs executed

- **SOURCE-backed**: the ACTIVE-language and currency facts originate from
  `Localization LanguageDirectory / ILanguageLookup.ListAsync -> LocalizationDbContext` and
  `StoreContext` resolution in the resolved commerce database.
- **Executed in tests**: controlled in-memory fakes and an in-process HTTP/middleware host only.
  No real user database, no running merchant server and no browser was exercised, so runtime DB
  behavior is **not** claimed as demonstrated.

## 10. Changed files

Localization (additive Contracts/metadata only):
- `src/backend/Modules/Localization/Tooba.Localization.Contracts/Ports/LanguageLookupContracts.cs`
- `src/backend/Modules/Localization/Tooba.Localization.Infrastructure/Adapters/LanguageLookupBridge.cs`

Shipping — new Authoring capability:
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringContextView.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Ports/IShippingAuthoringContextSource.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Queries/GetShippingAuthoringContextQuery.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringContextSource.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAuthoringContextEndpoints.cs`

Shipping — wiring, error contract, composition:
- `Tooba.Shipping.Contracts/Errors/ShippingErrorCodes.cs`
- `Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs`
- `Tooba.Shipping.Endpoints/Resources/ShippingErrors.resx`, `ShippingErrors.fa.resx`
- `Tooba.Shipping.Endpoints/ShippingEndpointModule.cs`
- `Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs`
- `Tooba.Shipping.Infrastructure/Tooba.Shipping.Infrastructure.csproj` (Localization.Contracts +
  StoreContext.Contracts references only)

Tests:
- `Tooba.Shipping.Tests/Authoring/*` (3 files)
- `Tooba.Shipping.Tests/Certification/ShippingRequestClassificationTests.cs`
- `Tooba.Shipping.Tests/Composition/ShippingModuleStructureGuardTests.cs`
- `Tooba.Shipping.Tests/Admin/ShippingErrorCatalogAndResourcesTests.cs`
- `Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` (Contracts test seams only)
- `src/backend/Host/Tooba.Host.Tests/LanguageLookupBridgeMappingTests.cs`

Governance/evidence:
- `docs/architecture/shipping/shipping-request-classification.json`
- `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` (appended dated section 17)
- `docs/architecture/tmar-current-state.json` (additive feature record; W3-R1 baseline untouched)
- `docs/architecture/tmar-module-structure-manifests.json` (additive-authorization record; W3-R1
  certification text preserved and explicitly marked as the historical baseline)
- `docs/ai/tasks/TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188.task.md` (verbatim receipt)
- this file

## 11. Explicit non-changes

No schema, migration, snapshot, seed, bootstrap, config, flag or package change. No new project, no
`Host` production business authority, no startup service or migrator change, no frontend/preview work,
no owner-scope persistence or seller grants, no region/taxonomy/rate/mode/schedule/financial/carrier
work. Default-OFF gates, `PendingAllocation`, existing Shipping routes, the public language API and its
HTTP behavior, TB-185 pause and every other module's certification remain as they were.

## 12. Remaining product gates

`PASS` for this task means only the wired, authorized, traced, fail-closed context read is implemented
and focused-proven. It is **not**:

- UI connection or visual acceptance (`D1`–`D5`, `D7` still pending);
- owner-scope persistence, seller-scoped authoring grants, taxonomy/region groups, rate/mode/schedule
  authoring or financial engine;
- a claim that `Marketplace` edition means seller-scoped writes are ready;
- module recertification: the W3-R1 AMSC certification stays the historical whole-module baseline;
- production functional acceptance — status remains `PENDING`.
