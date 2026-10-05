# TB-TMAR-IDENTITY-AMSC-001-W2 — Structure

## Mode

`ARCHITECT_DIRECT_AMSC` — Wave 2 (Structure). Physical/folder normalization, path↔namespace
exactness, capability-first validators, stale-residue removal, and durable structure guards.
**No business behavior, route, schema, persistence or telemetry change.**

Baseline: `branch = main`, `HEAD == origin/main == 93a6b192` (W1).

Skills: `.cursor/skills/tooba-architecture-structure/SKILL.md` (V2).

## Structure work executed

| # | Change | Rationale |
|---|---|---|
| 1 | `Tooba.Identity.Contracts/Problems/` → `Tooba.Identity.Contracts/Errors/` (4 files) + namespace `Tooba.Identity.Contracts.Problems` → `Tooba.Identity.Contracts.Errors` | Canonical capability folder vocabulary shared by BulkInquiry, Offer, AccessControl, Cart, Catalog, Content, Fulfillment, AddressBook, CustomerProfile, Order, Party, Media. The folder held only machine codes, the catalog contributor, the resource set and one typed fault — never ProblemDetails presentation — so `Problems` mis-described the capability. |
| 2 | `Application/Validators/IdentityValidationCodes.cs` → `Application/Auth/Validators/IdentityValidationCodes.cs` + namespace `Tooba.Identity.Application.Validators` → `Tooba.Identity.Application.Auth.Validators` | All six consumers are `Auth/Validators/*`. Removes the last technical-axis root folder from Application. |
| 3 | Deleted 4 unreferenced internal transport records (`RegisterResponse`, `SessionResponse`, `AcceptedResponse`, `MeResponse`) from `Endpoints/Auth/IdentityAuthHttpModels.cs` | Dead residue; the endpoints serialize the Application DTOs directly. Verified zero references repo-wide before deletion. |
| 4 | Added 3 shape-only validators (`LoginWithPasswordCommandValidator`, `RefreshAuthSessionCommandValidator`, `CompleteOtpLoginCommandValidator`) and reclassified the coverage manifest to `9 required present / 4 no-validator-required` | Closes the W0 validator-coverage blocker. Shape only: `Identifier`/`Password`/`RefreshToken`/`ChallengeId`/`Secret` presence. `LoginWithPasswordCommand` deliberately does **not** validate `IdentifierKind`, so an unknown kind still reaches the handler and collapses to `identity.authentication.failed` (401) instead of `identity.validation.failed` (400). |

Total files touched by the namespace rename: **35** (30 Identity + `AccessControl` dev bootstrap +
`Host/Admin/Development/AdminDevActorBootstrap.cs` + 3 Host test files).

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`.

```text
Tooba.Identity.Contracts/   Auth/  Actors/  Contacts/  Errors/  Resources/
Tooba.Identity.Domain/      Aggregates/  Enums/  Events/  Rules/
Tooba.Identity.Application/ Auth/{Commands,Models,Queries,Validators}/
                            Composition/  Models/  Options/  Ports/
Tooba.Identity.Infrastructure/ (root: IdentityModule.cs)
                            Adapters/ Authentication/ Contacts/ ExternalIdentity/
                            Mfa/ Otp/ PasswordHashing/ Persistence/ SecurityEvents/ Sessions/
Tooba.Identity.Endpoints/   (root: IdentityEndpointModule.cs)  Auth/  Errors/
```

- No single-file request leaf folders.
- No technical-axis-first `Commands/`/`Queries/`/`Validators/` root.
- `Application/{Models,Ports,Options}` remain shared because they are consumed by Auth use cases **and**
  by Infrastructure adapters — a genuine cross-capability concern, not a technical-axis dump.
- Every folder maps 1:1 to its namespace, except `Persistence/Migrations` (EF-generated block-scoped
  namespace, untouched by policy).

## Visual Studio solution grouping

`src/backend/Tooba.slnx`:

```xml
<Folder Name="/Modules/Identity/">
  <Project Path="Modules/Identity/Tooba.Identity.Domain/Tooba.Identity.Domain.csproj" />
  <Project Path="Modules/Identity/Tooba.Identity.Contracts/Tooba.Identity.Contracts.csproj" />
  <Project Path="Modules/Identity/Tooba.Identity.Application/Tooba.Identity.Application.csproj" />
  <Project Path="Modules/Identity/Tooba.Identity.Infrastructure/Tooba.Identity.Infrastructure.csproj" />
  <Project Path="Modules/Identity/Tooba.Identity.Endpoints/Tooba.Identity.Endpoints.csproj" />
</Folder>
```

Project paths, assembly names and ordering unchanged — the existing grouping was already canonical and
was **not** rewritten. No project path or assembly name was altered for visual grouping.

## Guard repairs (pre-existing RED, not caused by this run)

Two durable guards asserted a **flat `/Modules/` solution folder** that does not exist in `Tooba.slnx`
(the repository groups every module under its own nested `/Modules/<Module>/` folder). They were red at
the W1 base commit and would stay red forever:

| Guard | Repair |
|---|---|
| `IdentityModuleAmcW1SolutionGuardTests.Identity_projects_group_under_Modules_Identity_in_slnx` | Replaced the flat-folder comparison with a **stricter** assertion: the `/Modules/Identity/` folder must contain exactly the five canonical project paths, and no other solution folder may contain any `/Identity/` project path. |
| `HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact` | Same class of repair for `CustomerProfile`: keep the exact 5-project nested folder assertion, and replace the flat-folder lookup with a scan asserting no `CustomerProfile` project leaks into any other solution folder. |

Neither repair weakens the guarded invariant; both replace a check against a layout that no longer
exists with a strictly stronger whole-solution uniqueness check. **No guard was deleted or disabled.**

## New durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmsc001W2StructureGuardTests.cs`
(8 facts) locks: `Contracts/Errors` present + `Problems` gone + exact `Contracts.Errors` namespace,
`Application/Validators` gone + `Auth/Validators/IdentityValidationCodes.cs` namespace,
zero residual references to either retired namespace anywhere in the module,
transport models request-only, the three shape-only validators registered through
`AddToobaCqrsFoundation` with no `IdentifierKind` rule, single typed-fault classification seam
(`IdentityOperation` catches only `ContractOperationException`), OTP codes as constants rather than
string literals, no hard-coded Persian fault prose in `Domain/Rules`, and the exact
`/Modules/Identity/` Solution Explorer grouping.

## Verification

- `dotnet build src/backend/Tooba.slnx` → **0 errors**, 92 warnings (down from 127; the 12 Identity
  CS0105 warnings are gone and the 35-file namespace rename introduced none).
- Focused suite (all Identity + `HostCustomerProfileEvacuationGuardTests` +
  `HostAdminCanon007GuardTests` + `HostAdminCanonicalCertificationGuardTests` +
  `AuthenticationV2CanonicalizationGuardTests`) → **93 passed / 6 skipped / 0 failed**.

## Wave 2 outcome

`STRUCTURE_COMPLETE` → W3 (Certify) is unblocked.
