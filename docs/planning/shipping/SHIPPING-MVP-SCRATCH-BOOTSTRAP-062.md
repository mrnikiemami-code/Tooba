# TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062 — Development admin capability bootstrap repair

- Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
- Parent: TB-SHIPPING-MVP-ISOLATED-HTTP-061
- Mode: BOUNDED_ISOLATED_DEV_AUTH_BOOTSTRAP_REPAIR
- Expected base: `5b83c7c826fa494ae2d838ddf88b8e45c41561a3`
- Stop state: STOP_FOR_ARCHITECT
- Automatic next implementation task: NONE

## Problem inherited from 061

061 proved strong scratch isolation but could not exercise any authorized Shipping/Geography call: on a
fresh database the Development startup produced the admin actor (`GET /v1/admin/dev-context` returned
200) while `access_control.roles`, `role_permissions` and `user_role_assignments` were all empty, so every
admin surface correctly answered `403 admin.authorization.denied`. This task had to find *why* the
canonical bootstrap did not provision the established `platform-admin` capability and repair only that.

## Root cause (source-traced, reproduced)

`Tooba:CatalogDemo:RunLegacyBootstraps=false` is the modern Development default. In that mode
`DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync` migrates schema and then returns *before any module
seed* (`if (!seedCatalog) return;`). Consequently `SettingsFoundationDevelopmentSeedHost.ApplyAsync` — the
only caller of `PartyOrganizationProfileDevelopmentSeed`, i.e. the only producer of the Party development
organizations — never runs.

`SupportDevelopmentSeedHost.ApplyAsync` then runs the canonical admin bootstrap chain:

1. `AdminDevActorBootstrap.EnsureAsync` — succeeds (actor identity + `tenant#member` tuple), which is why
   `login_identifiers` contained `admin-actor@tooba.local` and `/v1/admin/dev-context` answered 200.
2. `AccessControlDevelopmentSeedPrelude.EnsureSupportSeedPrerequisitesAsync` →
   `SellerDevContextBootstrap.EnsureAsync` — creates the two seller demo identities (so three identities
   existed in total), then performs two **read-only** Party lookups:
   `FindDevelopmentOrganizationByDisplayNameAsync` (no Party demo organization exists) and
   `FindDevelopmentMembershipSellerPartyAsync` (empty `party.memberships`), so it returns without
   publishing its snapshot.
3. Pre-repair, the prelude then did `if (seller is null) return null;` **before** calling
   `IAccessControlDirectory.EnsureBootstrapAsync`, so the `platform-admin` role, its `PermissionCatalog`
   grants, the admin assignment and the admin capability tuples were never created.

The defect is therefore a Development-only seed **ordering/coverage** defect: the canonical authorization
bootstrap was gated behind unrelated demo seller data that the modern schema-only path deliberately does
not create. It is not an exception being swallowed — that is proven by the absence of any
`SupportDevelopmentSeed failed` / `WalletDevelopmentSeed failed` records (Program.cs logs them) and by the
fact that the same tuple writer and the same identity seam were both used successfully by the steps that
did run.

## Fix

One existing Development-only file,
`src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Development/AccessControlDevelopmentSeedPrelude.cs`:
the canonical `EnsureBootstrapAsync` call was moved ahead of the `seller is null` early return and now
receives the seller party list that is actually known (an empty list when the seller dev context could not
be resolved). Only the Support demo seed remains gated on the seller context.

This reuses the pre-existing canonical bootstrap logic — the same method behind
`POST /v1/admin/access-control/bootstrap` — and introduces no fabricated role or tuple, no new permission
identifier, no `AllowAnonymous`, no guard weakening and no Production-path change (the prelude is reachable
only from the `IsDevelopment()` block). With an empty seller list the seller-owner/ceiling work is skipped
exactly as before, and `EnsureBootstrapAsync` remains idempotent.

## Verification

Focused integration proof on a task-exclusive `postgres:17` container (`tooba-062-scratch`, loopback
55433, fresh role, database `tooba_062_scratch_r4v8c1tp`, `tooba_alpha` absent), with the effective target
proven offline (copied 060 guard: `RESULT=PASS`, fingerprint
`02dd3e724f341f9a0052273f34a503a0fb6c512fe204a114c1944f55fc314cc7`, config SHA
`72d57bf8f16a865d6fb19f9f0fe0012d9b997bf8f85812970cf6a4ea9b623599`, exactly one tenant and one
hyphenated reference) and redacted configuration committed as evidence.

| Check | Before fix (fresh DB) | After fix (fresh DB) |
| --- | --- | --- |
| `access_control.roles` | 0 | **1** |
| `access_control.role_permissions` | 0 | **55** |
| `access_control.user_role_assignments` | 0 | **1** |
| `GET /v1/admin/access-control/roles` as admin | 403 | **200** (`platform-admin`, 55 permissions, 1 assignment) |
| `GET /v1/admin/access-control/me/capabilities` as admin | 403 | **200** (`inheritedViaRoleCodes=["platform-admin"]`) |
| admin GET, no credential | 401 `admin.actor.missing` | **401** (unchanged) |
| admin GET, unknown/seller actor | 403 `admin.authorization.denied` | **403** (unchanged) |

Idempotence: a restart against the same database left `roles=1`, `role_permissions=55`,
`user_role_assignments=1`, `login_identifiers=3` — no duplicates — and the authorized GET still returned
200 with exactly one role.

Authorized `GET /v1/admin/geography/countries` and `GET /v1/admin/shipping-services/{code}/rates` now pass
authorization (no 403) but answer `500 platform.unexpected` because those module schemas are not created by
`MigrateSchemaOnlyAsync` (`42P01: relation ... does not exist`). That is an environment limitation carried
over from 061 and is out of scope here; it is not an authorization failure.

## Out of scope / remaining for the Architect

- The two seller demo organizations are still not provisioned when `RunLegacyBootstraps=false`, so the
  Support demo seed still no-ops on a fresh database. That is pre-existing intended gating, not this defect.
- A `PASS` verdict was not claimed for complete Shipping HTTP CRUD; the missing Shipping/Geography schema in
  the schema-only Development path is the next blocking environment fact if live Shipping HTTP regression is
  required.

## Changed paths

- `src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Development/AccessControlDevelopmentSeedPrelude.cs`
- `docs/planning/shipping/SHIPPING-MVP-SCRATCH-BOOTSTRAP-062.md`
- `docs/architecture/evidence/TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062/**`
- `docs/ai/tasks/TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062.task.md` (unstaged receipt only)
