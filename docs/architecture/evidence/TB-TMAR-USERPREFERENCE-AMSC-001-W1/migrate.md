# TB-TMAR-USERPREFERENCE-AMSC-001-W1 — Migrate (`tooba-architecture-migrate`)

```text
TASK:          TB-TMAR-USERPREFERENCE-AMSC-001-W1
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-migrate
TARGET:        src/backend/Modules/UserPreference/Tooba.UserPreference.*
PARENT:        TB-TMAR-USERPREFERENCE-AMSC-001-W0
PARENT COMMIT: 27c273be
STARTING HEAD: 27c273be
STATE:         MIGRATE_COMPLETE
VERDICT:       READY_TO_STRUCTURE
STRUCTURE HANDOFF: REQUIRED
LOCK VERSION:  ARCH-COMPLETE-002
```

W1 closes the five W0 findings by making the module's stable-error surface self-contained and its
typed-fault seam complete. No route, schema, migration, DI lifetime or wire-visible string changes.

---

## 1. Stable code home (finding §10 / §9.2 of W0)

**Single canonical home:** `Tooba.UserPreference.Contracts/Errors/UserPreferenceErrorCodes.cs`
(`Tooba.UserPreference.Contracts.Errors`), exactly one declaration in the whole production surface.

Added:

- `private static readonly HashSet<string> KnownCodes` (ordinal) seeded from the ten
  UserPreference-owned declared constants;
- `public static bool IsKnown(string? code)` — `!IsNullOrWhiteSpace(code) && KnownCodes.Contains(code)`;
- `public const string OutboxUnmappedEventType = "user_preference.outbox.unmapped_event_type"`.

**Foundation-owned code respected.** `SessionRequired = "customer.session.required"` keeps its
constant (the HTTP boundary consumes it at two `api.FromFailure` sites) and is **deliberately
excluded** from `KnownCodes`, so `IsKnown("customer.session.required") == false`. This mirrors the
certified `CustomerProfile` / `AddressBook` / `Settlement` / `Returns` / `Support` precedent and
means the guard never treats a Foundation descriptor as a UserPreference use-case fault.

`Declared-code guard: KnownCodes + IsKnown(string?)` — present.

| | Count |
| --- | --- |
| Declared string constants (incl. Foundation-owned `SessionRequired`) | 11 |
| UserPreference-owned declared codes (`KnownCodes`) | 10 |
| Registered descriptors | 10 |
| EN resource keys | 10 |
| FA resource keys | 10 |
| Raw prose faults remaining | 0 |

All ten pre-existing `preference.*` / `ui_preference.*` wire values are **byte-identical** to W0.

---

## 2. Error catalog (finding §10)

`UserPreferenceErrorCatalogContributor` now registers **10** descriptors (was 9) with
`LocalizationKey = code`, `Severity = ErrorSeverity.Warning` and a safe English fallback:

| # | Code | Classification | HTTP |
| --- | --- | --- | --- |
| 1 | `preference.rejected` | Validation | 400 |
| 2 | `ui_preference.rejected` | Validation | 400 |
| 3 | `ui_preference.invalid_json` | Validation | 400 |
| 4 | `ui_preference.json_required` | Validation | 400 |
| 5 | `preference.validation.actor_required` | Validation | 400 |
| 6 | `preference.validation.locale_required` | Validation | 400 |
| 7 | `ui_preference.validation.actor_required` | Validation | 400 |
| 8 | `ui_preference.validation.key_required` | Validation | 400 |
| 9 | `ui_preference.validation.json_required` | Validation | 400 |
| 10 | `user_preference.outbox.unmapped_event_type` | **Platform** | **500** |

`customer.session.required` is deliberately absent — the comment naming
`FoundationErrorCatalogContributor` as its owner is retained. Duplicate usage allowed, duplicate
descriptor ownership not.

Registration is unchanged and stays exactly once in
`UserPreferenceModule.AddServices`
(`AddSingleton<IErrorCatalogContributor, UserPreferenceErrorCatalogContributor>()` +
`AddSingleton<IErrorResourceSet, UserPreferenceErrorResourceSet>()`).

---

## 3. Localization surface (finding §9.1)

- `UserPreferenceErrors.resx` / `UserPreferenceErrors.fa.resx` gained the
  `user_preference.outbox.unmapped_event_type` key in EN (`Integration event type is not
  registered.`) and FA (`نوع رویداد یکپارچه‌سازی ثبت نشده است.`). EN = FA = 10 keys, set-equal to the
  declared codes.
- `Tooba.UserPreference.Contracts.csproj` gained the explicit, locked embedded-resource pair
  (canonical `Tax`/`Pricing` shape):

```xml
<EmbeddedResource Update="Resources\UserPreferenceErrors.resx">
  <LogicalName>Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.resources</LogicalName>
</EmbeddedResource>
<EmbeddedResource Update="Resources\UserPreferenceErrors.fa.resx">
  <LogicalName>Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.fa.resources</LogicalName>
</EmbeddedResource>
```

**W0 evidence correction (recorded honestly).** The W0 draft asserted the Persian satellite was
"dead content". Verification against the actually built assemblies proved that claim wrong — both
cultures already resolved through SDK convention:

```text
Tooba.UserPreference.Contracts.dll
  -> Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.resources
fa/Tooba.UserPreference.Contracts.resources.dll (culture = fa)
  -> Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.fa.resources
```

The real W0 finding was the **narrower** one: those names were *implicit* (dependent on
`EmbeddedResourceUseDependentUponConvention`), not *locked*. W1 makes them explicit. Verified after
the change that the manifest resource names are **byte-identical** to the pre-change names, so this
is behavior-preserving and adds a durable invariant rather than a behavior change.

- `UserPreferenceErrorResourceSet.Owns` is unchanged in semantics
  (`preference.` ∪ `ui_preference.`). It stays prefix-based to match the canonical, fully green
  sibling shape used by 18 certified modules (`Tax`, `Pricing`, `Wishlist`, `Inventory`, …); a
  code-set-based `Owns` would be a novel deviation and was deliberately not introduced. The
  keyspace is proven collision-free (repository-wide scan: zero foreign `preference.*` /
  `ui_preference.*` literals), and the W1 guard pins both the true and false ownership cases.

---

## 4. Typed-fault seam (finding §10)

`Application/Composition/UserPreferenceOperation.cs` is expanded to the certified dual-mechanism
seam, mirroring `TaxOperation` / `InventoryOperation`:

- `Task<Result<T>> ExecuteAsync<T>(Func<Task<T>>)` — value overload;
- `Task<Result> ExecuteAsync(Func<Task>)` — value-less overload (new);
- `catch (ContractOperationException ex) when (UserPreferenceErrorCodes.IsKnown(ex.Code))` →
  `Result.Failure(new SemanticError(ex.Code))` (new) — both overloads;
- `catch (SemanticException ex)` → `Result.Failure(ex.Error)` / `Result.Failure<T>(ex.Error)`;
- unknown contract codes and every unknown exception propagate untouched to the canonical global
  exception boundary;
- classification is by typed code only — no message/prose heuristics, no `StartsWith`, no
  `IResult`, no `MapGroup(`.

---

## 5. Raw fault retired (finding §10)

`Infrastructure/Persistence/UserPreferenceOutboxRegistration.cs`:

```csharp
// before
public string GetEventTypeName(Type integrationEventType) =>
    throw new InvalidOperationException("UserPreference integration event is not registered.");

// after
public string GetEventTypeName(Type integrationEventType) =>
    throw new ContractOperationException(UserPreferenceErrorCodes.OutboxUnmappedEventType);
```

`Translate(...) => null` and `ResolveEventClrType(...) => null` are unchanged, so the outbox
observable behavior is unchanged; only the unreachable mis-declaration fault becomes a typed,
catalog-registered, bilingual code.

Zero raw `InvalidOperationException("<literal>")` remains anywhere in the UserPreference production
surface.

---

## 6. Coupling (unchanged)

Zero outbound foreign Application/Infrastructure/Domain reference. The single legal edge remains
`Tooba.UserPreference.Endpoints -> Tooba.Order.Contracts` (`StorefrontGuestActor`). The Domain
references only its own `Tooba.UserPreference.Contracts`. No new project, no new project reference,
no Host production file touched. `Cross-Module-Coupling-State = NONE`, `Cross-Module-Join-State =
NONE`.

---

## 7. Behavior preservation

| Surface | State |
| --- | --- |
| 6 routes / verbs / group prefixes / templates | UNCHANGED |
| Response shapes (`PreferenceLocaleResponse`, `UiPreferenceResponse`, `{}` / `fa` defaults) | UNCHANGED |
| The 9 pre-existing status codes | UNCHANGED (one new Platform 500 for a previously unregistered prose fault) |
| The 9 pre-existing wire error-code strings | UNCHANGED (byte-identical) |
| Domain invariants (`fa`/`en`, length caps, Guid.Empty, JSON required) | UNCHANGED |
| Authorization + actor-resolution order | UNCHANGED |
| `user_preference` schema, both migrations, designer, snapshot | UNTOUCHED |
| Outbox `Translate` / `ResolveEventClrType` / `Schema` / `TableName` | UNCHANGED |
| DI lifetimes | UNCHANGED |
| EN/FA resolved resource names | byte-identical before/after |

`Behavior-Preservation = PRESERVED`.

---

## 8. Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/UserPreferenceModuleAmsc001W1MigrateGuardTests.cs`
— 8 facts:

1. `Stable_codes_live_in_the_single_canonical_contracts_errors_home`
2. `Declared_code_catalog_is_the_single_known_code_source` (incl. `IsKnown("customer.session.required") == false`)
3. `Typed_fault_seam_maps_declared_codes_and_never_parses_message_text`
4. `Error_catalog_and_bilingual_resources_are_registered_once_by_the_composition_root` (incl. live EN+FA `GetString` for all 10 codes and the explicit logical-name pin)
5. `Stable_code_literals_are_declared_once_and_never_reinlined`
6. `UserPreference_production_never_classifies_faults_by_message_text`
7. `Cross_module_boundary_stays_contracts_only`
8. `No_userpreference_production_source_uses_a_raw_invalid_operation_fault`

**Result: 8 of 8 PASSED.**

---

## 9. Verification

```text
dotnet build Tooba.UserPreference.Infrastructure.csproj     -> SUCCEEDED, 0 warnings, 0 errors
dotnet build Tooba.Host.Tests.csproj                        -> SUCCEEDED, 0 errors
dotnet test --filter UserPreferenceModuleAmsc001W1MigrateGuardTests
             | UserPreferenceModuleAmc* | HostPreferencesAmcGuardTests
             | ErrorCatalogUniqueCodeGuardTests              -> Failed: 0, Passed: 23, Skipped: 0
```

No existing guard was weakened, repointed or deleted. No baseline widened.

```text
Stable-Error-Code-State     : SINGLE_CANONICAL_CONTRACTS_HOME_10_DECLARED_10_REGISTERED_10_LOCALIZED
Declared-Code-Guard-State   : PRESENT (KnownCodes + IsKnown)
Typed-Fault-Seam-State      : CANONICAL_DUAL_MECHANISM_VALUE_AND_VALUELESS
Raw-Fault-State             : 0
Localization-State          : CANONICAL_10_EN_10_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
Presentation-Registration   : INFRASTRUCTURE_COMPOSITION_ROOT_EXACTLY_ONCE
Cross-Module-Coupling-State : NONE
Schema-Migration-State      : UNCHANGED
Behavior-Preservation       : PRESERVED
Microservice-Extractable    : TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE
Structure-Handoff-State     : REQUIRED
```

## 10. Next wave

W2 (`tooba-architecture-structure`) records the physical-tree structure authority for the touched
surface, adds the durable structure guard and moves `structureAuthorityTask` on the manifest entry
to `TB-TMAR-USERPREFERENCE-AMSC-001-W2`. No project is added, removed, renamed or re-pathed.
