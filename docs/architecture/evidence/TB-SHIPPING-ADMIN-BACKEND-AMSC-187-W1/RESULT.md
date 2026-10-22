# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1 — RESULT

Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_MIGRATE_CANONICAL_ERRORS_AND_EDITION_GUARD
Parent: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 (accepted bounded source map)
Status: PASS
Stop-State: STOP_FOR_ARCHITECT

## 1. Mandatory first check (recovery)

| Check | Result |
|---|---|
| `git fetch` state | `HEAD = e5c155ff910f817ce027023346c4483587cd9dc8`, `origin/main = e5c155ff910f817ce027023346c4483587cd9dc8` |
| Expected-Base-SHA from the task | `e5c155ff…` — matches; **not** the stale `eea03754` named in W0's pasted prose |
| Working tree before edits | clean (no dirty/untracked/stash replay; nothing reset, rebased, stashed or force-pushed) |
| Branch | `main` |

No recovery conflict. The W0 result was not repeated.

## 2. Skills and authorities read

`AGENTS.md`, `docs/ai/AGENT-SKILLS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`,
`.cursor/skills/tooba-architecture-migrate/SKILL.md`, `.cursor/skills/tooba-architecture-structure/SKILL.md`,
W0 evidence (`SOURCE-MAP.md`, `INPUT-VALIDATION-MATRIX.md`, `MODEL-LEGACY-AND-HANDOFF.md`, `RESULT.md`),
existing error-catalog/resource precedent (`FoundationErrorCatalogContributor`, `PaymentErrorCatalogContributor`,
`FulfillmentErrorCatalogContributor` + `FulfillmentErrorResourceSet`), `ObservabilityFoundationRegistration`,
`SafeErrorMapper`, `ApiResponseFactory`, `ResourceErrorMessageLocalizer`, `ICurrentEdition`/`ICurrentTenant`,
`AuthorizationCallContext`, `SellerPanelAccess`/`AdminPanelAccess` canonical patterns, and
`SHIPPING-ADMIN-RULES-DECISION.md` sections 13–15. The opt-in findings register was **not** consulted.

## 3. Architect review corrections recorded (W0 evidence preserved unchanged)

1. **Visible-text correction.** Registering descriptors + localized resources does change visible failure
   *text* (previously the generic `"Request rejected."` safe fallback); it does **not** change success wire
   contracts, failure HTTP statuses, classification or the validation envelope. This slice is therefore not
   described as "zero observable change".
2. **Preview min/max delivery duration** is *not* `PreparationDays + LeadDays`; preparation and transit remain
   separate. No mapping or column repurposing was done.
3. **Composite freight + packaging + other shipping cost / free-shipping funding** decisions in the dated
   financial appendix remain approved; they were not reopened and no customer-charge or financial consumer
   changed here.
4. **Manifest absence is not missing foundation.** Shipping's existing projects/capabilities were reused; no
   manifest promotion, no whole-module certification.
5. **D1–D7 remain deferred** and did not block this explicitly authorized mode/owner-neutral repair.

## 4. Boundary A — canonical Shipping errors (implemented)

| Item | Evidence |
|---|---|
| Declared code set | 44 `public const string` members in `ShippingErrorCodes.cs` (44 distinct) |
| Registered descriptor set | 44 descriptors, exact set equality with the declared set (proved by reflection over the constants, not by a hard-coded count) |
| Descriptor owner | `Tooba.Shipping.Endpoints.Errors.ShippingErrorCatalogContributor` — single owner of `shipping.*` |
| Resource owner | `Tooba.Shipping.Endpoints.Resources.ShippingErrorResourceSet` over `Resources/ShippingErrors.resx` + `Resources/ShippingErrors.fa.resx` |
| Localization key | the stable code itself (no alias keys) |
| HTTP/classification/severity | 400 / `Business` / `Warning` for every code — exactly the values `SafeErrorMapper` already produced for an unregistered code, so legacy statuses are preserved. `*.not_found` was **not** turned into 404 and `*duplicate*`/`*mismatch*` were **not** turned into 409 |
| Duplicate/foreign ownership | Foundation owns `admin.*`/`validation.*`/`platform.*`; `shipping_service*` remains Fulfillment-owned. Shipping registers neither; the resource set claims only `shipping.` and the composed catalog is built without duplicate registration (guard test passes) |
| Fallback | `ResourceManager.GetString(key, culture)` → neutral `.resx` for any locale without a translation; proved with `de-DE` on the localizer **and** through the real `ApiResponseFactory` map |
| Composition | `ShippingEndpointModule.AddShippingEndpointPresentation()` registers one `IErrorCatalogContributor` + one `IErrorResourceSet` as singletons; invoked exactly once from `Host/Tooba.Host/Program.cs`. Infrastructure does not reference Endpoints; no new project, no schema migrator, no hosted service, no custom mapper/localizer/locale parser |

Explicit mapping table: `ERROR-CATALOG-MAPPING.md` (44 rows: code → constant → key → status → neutral/fa
resource → safe fallback, plus the set-equality summary).

## 5. Boundary B — authoritative authorization context (implemented)

`ShippingAdminAuthorizer` no longer hardcodes `ToobaEdition.SingleStore` / `tenant … ?? "unknown"`. It consumes
the scoped `ICurrentEdition` + `ICurrentTenant` seams and forwards a resolved `AuthorizationCallContext`:

- `SingleStore` → real edition + non-empty **Active** `TenantId` (trimmed, never rewritten/invented);
- `Marketplace` → `Marketplace` + `TenantId = null`; a leftover tenant is inconsistent ⇒ fail closed;
- `null`/`Unset`/unsupported edition, missing/inactive/empty tenant ⇒ `admin.authorization.unavailable`
  **before** `IAuthorizationService.CanAsync`;
- panel gate, permission ids, subject/resource, Allow/Deny/Unavailable semantics retained; deny stays denied;
  `Unavailable` never allows; cancellation and unexpected exceptions are not swallowed.

Matrix: `AUTHORIZATION-MATRIX.md`.

## 6. Tests actually run (commands and counts)

| # | Command (bounded ≤180 s each) | Result |
|---|---|---|
| 1 | `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Tooba.Shipping.Endpoints.csproj` | succeeded, 0 warnings, 0 errors |
| 2 | `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -t:Compile -p:BuildProjectReferences=false` | succeeded, 0 warnings, 0 errors |
| 3 | `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` | succeeded (1 pre-existing `xUnit1031` analyzer warning in `ShippingRateCoverageAdminTests`) |
| 4 | `dotnet test …Tooba.Shipping.Tests.csproj --filter "FullyQualifiedName~ShippingErrorCatalogAndResourcesTests|FullyQualifiedName~ShippingAdminAuthorizerContextTests"` | **29 passed**, 0 failed |
| 5 | `dotnet test …Tooba.Shipping.Tests.csproj` (whole module suite, `--no-build`) | **366 passed, 1 failed** (pre-existing, see §8) |
| 6 | `dotnet test …Tooba.Host.Tests.csproj -p:BuildProjectReferences=false --filter "FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests"` | **3 passed** (composed production contributors register each machine code exactly once — includes the new Shipping contributor) |

Notes on bounded validation:

- A full `dotnet build` of `Tooba.Host` could not complete because the developer's already-running Host process
  (PID 24304) holds `Tooba.Host/bin/**` DLLs. Per the isolation rule that shared app was **not** stopped,
  restarted or cleaned; compilation of the changed project was proved with the compile-only target (row 2).
- No package install, no database, no server start, no browser, no migration execution.
- No test loop: the single deterministic failure encountered in the new tests (a static assertion that
  over-matched a legitimate `SingleStore` assignment) was fixed once and the affected filter re-run.

## 7. Compatibility and contract deltas

| Surface | Delta |
|---|---|
| Shipping admin routes, methods, request/body parsing, handlers, validators | none |
| Success status/body/`Content-Type`/`Location` | none |
| Failure HTTP status for every `shipping.*` code | none (400 before, 400 after) |
| `validation.failed` envelope + child codes | unchanged (proved through the real `ApiResponseFactory`) |
| Failure **title text** for `shipping.*` | now a real localized message instead of the generic `"Request rejected."` — the only visible delta, explicitly acknowledged in §3.1 |
| Foundation/shared codes | unchanged and still Foundation-owned |
| Persistence/DbContext/schema/migrations | none |
| Contracts (`Tooba.Shipping.Contracts`) | unchanged (read-only in this slice) |
| DI | one new presentation extension (contributor + resource set); authorizer registration unchanged and still in `Program.cs` |
| Default-OFF gates, `PendingAllocation`, financial authority | untouched |

## 8. Pre-existing failure (reported, not fixed)

`Tooba.Shipping.Tests.Compatibility.OperationalQuoteCompatibilityTests.No_module_outside_shipping_references_the_operational_quote_types`
fails on `main` before this slice: three `Order` sources reference the operational-quote seam —
`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowComparer.cs`,
`…/StorefrontShippingOperationalShadowObserver.cs`, `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs`.
No file changed by this task references those types; the failure is unrelated and was neither repaired nor
re-baselined.

## 9. Structure / handoff

- New files follow the existing capability-first placement: `Endpoints/Errors/`, `Endpoints/Resources/`,
  path-derived `Tooba.Shipping.Endpoints.*` namespaces, no one-folder-per-request, no decorative project.
- `ShippingEndpointModule` (the module's existing composition class inside its existing route file) hosts the
  registration-only extension; `Program.cs` received one registration line (no new Host file/folder, no
  business logic).
- `Structure-Handoff-State: REQUIRED` for the broader delivery surface. This slice does not claim
  `READY_TO_STRUCTURE`, `COMPLETE_REFERENCE_PATTERN`, final Structure/Certify, whole-module certification,
  owner isolation, full marketplace readiness or production release. Manifests / `tmar-current-state` untouched.

## 10. Remaining owner/business decisions (unchanged, not reopened)

- **D1** Canonical merchant/owner identity for single-store + marketplace seller scope + ownership of existing rows.
- **D2** Shipping-type taxonomy: owner, value set, configurability, translation, pricing/coverage effect.
- **D3** Region-group persistence shape and legacy rows without a group.
- **D4** Explicit calculation modes and payment timing vs `FreeAboveThreshold` vs Promotion vs carrier integration.
- **D5** Closed-day vs absent-window semantics, and where the cutoff lives.
- **D6** Language enablement semantics (`ILanguageLookup` extension vs HTTP-only) and per-seller enablement.
- **D7** Migration execution authority against the shared database.

The owner-scoped business/data gap is still open: `ICurrentEdition`/`ICurrentTenant` are installation context,
not merchant identity, and Marketplace shipping authoring remains non-owner-scoped.

## 11. Changed files

Production:

- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/ShippingErrorResources.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/ShippingErrors.resx` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/ShippingErrors.fa.resx` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAdminAuthorizer.cs` (modified)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs` (modified — registration-only extension on the existing composition class; routes/bodies unchanged)
- `src/backend/Host/Tooba.Host/Program.cs` (modified — one registration line)

Tests:

- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingErrorCatalogAndResourcesTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingAdminAuthorizerContextTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` (modified — reference to the module's own Endpoints project, matching Payment/Order test-project precedent)

Evidence/task artifact:

- `docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1.task.md` (received artifact, verbatim)
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1/` (this file, `ERROR-CATALOG-MAPPING.md`, `AUTHORIZATION-MATRIX.md`, claim/receipt, poster)

Nothing outside Shipping.Endpoints, the Shipping test project, `Program.cs`'s single registration line and the
task evidence/task artifact was touched.

## 12. Isolation and cleanup

- No process was stopped, no cache cleared, no port touched (`:3000/:3419/:5088/:5432` untouched), no shared app
  restarted, no secret or token logged.
- Only task-owned artifacts were created; temporary local helper files used for evidence generation were
  deleted before commit.
- Scoped working tree is clean after commit; unrelated dirty/untracked work (none present at claim time) was
  preserved.
