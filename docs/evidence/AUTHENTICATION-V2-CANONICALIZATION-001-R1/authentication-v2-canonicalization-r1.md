# AUTHENTICATION-V2-CANONICALIZATION-001-R1 — Host/Authentication Contracts-Only Repair

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: `tooba-main`
Scope: `src/backend/Host/Tooba.Host/Authentication` + the **minimum** Identity Contracts surface required to repair its V2 boundary.
Predecessor commit (canonicalization): `4c5931cef89b63075416e1cb572998c71f7b9f0b`
Not started: Identity recovery, CustomerProfile recovery, next Host folder.

## 1. Remove parallel error fallback

`AuthenticationHttpProblem.AuthProblem` previously resolved `ApiResponseFactory` optionally and fell back to a local
`Results.Problem(...)` 500 when composition was missing. That fallback was a second (parallel) presentation pipeline.

Final state:

```csharp
public static IResult AuthProblem(HttpContext http, string errorCode)
{
    var factory = http.RequestServices.GetRequiredService<ApiResponseFactory>();
    return factory.FromFailure(new SemanticError(errorCode));
}
```

- `Parallel-Problem-Pipeline = ZERO`
- Missing canonical presentation infrastructure now surfaces as a wiring failure (`GetRequiredService`) instead of a
  silent local `ProblemDetails` downgrade.
- Observable HTTP behavior is unchanged whenever composition is valid (factory was always registered in the composed
  Host, so the removed branch was unreachable).

## 2. Contracts-only repair

Before this task `Host/Authentication` imported `Tooba.Identity.Application` and `Tooba.Identity.Domain`.

Capabilities actually consumed by `Host/Authentication`, and their final lawful owner:

| Capability | Final owner |
| --- | --- |
| `IIdentityAuthenticationService`, `RegisterUserCommand/Result`, `AuthenticationResult`, `AuthenticationOutcome` | `Tooba.Identity.Contracts` |
| `IIdentityCredentialLifecycle`, `PasswordResetRequestResult`, `ChallengeConsumeOutcome` | `Tooba.Identity.Contracts` |
| `IIdentityOtpLoginService`, `OtpChallengeHandle`, `DevelopmentOtpLoginFixtureOptions` | `Tooba.Identity.Contracts` |
| `IIdentitySessionResolver`, `AuthenticatedIdentity` | `Tooba.Identity.Contracts` |
| `IIdentityContactLookup`, `IdentityContactSnapshot` | `Tooba.Identity.Contracts` |
| `LoginIdentifierKind`, `OtpPurpose` | `Tooba.Identity.Contracts` |
| `AuthenticationTicket`, `PublicAuthenticationError` | `Tooba.Identity.Contracts` |

New files:

- `src/backend/Modules/Identity/Tooba.Identity.Contracts/Auth/IdentityPrimitiveContracts.cs`
- `src/backend/Modules/Identity/Tooba.Identity.Contracts/Auth/IdentityAuthenticationContracts.cs`

`Tooba.Identity.Domain` now references `Tooba.Identity.Contracts` for the two neutral enums still used by the
identity aggregate (`LoginIdentifierKind`, `OtpPurpose`); the duplicate declarations were deleted from
`IdentityDomain.cs` so there is exactly one declaration. Domain entities, `DbContext`, repositories, migrations and
business logic were **not** moved or exposed.

`IOtpDeliveryProvider` / `OtpDeliveryMessage` / `OtpDeliveryOutcome` / `OtpDeliveryOutcomeKind` were also duplicated
between `Identity.Application` and the new Contracts surface; the `Identity.Application/OtpDeliveryContracts.cs`
duplicate was deleted so the Contracts declaration is single-source (it is an outbound port, not an entity).

Host files repaired:

- `Authentication/AuthenticationHttpBoundary.cs` — `Identity.Application`/`Identity.Domain` → `Identity.Contracts`
- `Authentication/CurrentAuthenticatedSession.cs` — → `Identity.Contracts`
- `Authentication/SessionAuthenticationMiddleware.cs` — → `Identity.Contracts`
- `Admin/AdminDevActorBootstrap.cs`, `Seller/SellerDevActorBootstrap.cs`, `Customer/CustomerPanelComposer.cs` — same
  neutral enum/capability relocation (dev-actor bootstrap + profile composition consume the same contract surface).

Target reached:

```text
Host/Authentication -> Identity.Contracts / CustomerProfile.Contracts / BuildingBlocks only
Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY
Cross-Module-Join-State = NONE
Identity-Business-Authority-In-Host = ZERO
Identity-Persistence-Authority-In-Host = ZERO
```

No entities, repositories, `DbContext`, infrastructure implementations or domain internals are exposed by the new
contracts.

## 3. Behavior lock

Preserved exactly:

- all 13 routes and HTTP methods;
- success response shapes (201/200/204);
- failure status codes (400/401/409/429);
- `identity.*` machine error codes;
- Bearer + `tooba_session` cookie semantics;
- OTP / password / reset behavior;
- tenant spoof protection;
- throttle behavior;
- telemetry event names;
- trace/correlation behavior;
- schema/migrations (`Behavior-Change = NONE`, no migration added).

## 4. Test discipline / evidence

- `dotnet build Tooba.slnx` → only the pre-existing `Tooba.Order.Tests.OrderSellerPanelArchitectureGuardTests`
  missing `Tooba.AccessControl` reference errors remain (untouched by this task).
- `AuthenticationV2CanonicalizationGuardTests` — 22 passed / 0 failed. Guards strengthened:
  - `Authentication_files_do_not_leak_foreign_Application_or_Infrastructure` now also forbids `using Tooba.Identity.Application` and `using Tooba.Identity.Domain` in `Host/Authentication`;
  - new `Authentication_files_consume_cross_module_capability_only_through_contracts` forbids `Tooba.Identity.Application`, `Tooba.Identity.Domain`, `Tooba.CustomerProfile.Application`, `IdentityDbContext`, `AuthSession`, `UserAccount` anywhere in `Host/Authentication`;
  - `Authentication_problem_helper_uses_canonical_factory_not_a_parallel_pipeline` now also asserts `Results.Problem` is absent.
- Focused Host auth/identity suites: 49 passed / 10 skipped (Testcontainers/Docker unavailable in this environment) / 0 failed.

Pre-existing failures **not** caused by this task (unchanged):

- `ErrorContractTests` (unregistered `Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort`);
- `TmarSourceSizeAndInfraAppTests`;
- `Tooba.Order.Tests.OrderSellerPanelArchitectureGuardTests`.

## 5. Final disposition

```text
API-Result-Pattern-State = CANONICAL
Stable-Error-Code-State = CATALOGUED
Localization-State = CANONICAL
Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY
Cross-Module-Join-State = NONE
Identity-Business-Authority-In-Host = ZERO
Identity-Persistence-Authority-In-Host = ZERO
Behavior-Change = NONE
Parallel-Problem-Pipeline = ZERO
```

Authentication is the **current Host-folder checkpoint**. The next Host folder has **not** been started.
Identity/CustomerProfile were **not** independently recovered; only the minimum contract surface required by this
migration was touched.
