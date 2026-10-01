# migration-plan — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001

ANALYSIS_ONLY — recommended waves (≤20 min each). automaticNextImplementationTask = NONE until Architect issues W1.

## W1 — Core Admin access error/localization hygiene (~15–20 min)

**Files:** `AdminPanelAccess.cs`, `HostAdminPanelAccess.cs`, Foundation catalog contributor (+ codes constants), focused AdminPanelAuthorizationTests / durable guards as needed.

**Preserve:** exact Allow/Unavailable/Deny branching; HTTP 401/403/503; error code strings; DevActorHeader Development-only; Marketplace synthetic tenant path; `IAdminPanelAccess` registration.

**Change:** register `admin.actor.missing`, `admin.tenant.missing`, `admin.authorization.unavailable` exactly once in Foundation; remove hard-coded FA titles from throws (catalog/localizer presentation); keep `admin.authorization.denied` status/classification aligned.

**Dependencies:** BuildingBlocks only.  
**Out of scope:** authorizer family, Promotion delete, auth redesign.

## W2 — Authorizer family cleanup (~15–20 min)

**Files:** `HostOrderAdminAuthorizer.cs`, `HostSupportAdminAuthorizer.cs`, `HostWalletAdminAuthorizer.cs`, delete `HostPromotionAdminAuthorizer.cs`, update Canon/Certification allowlists that list the dead file.

**Preserve:** capability check semantics; module unavailable/denied codes; panel pass-through authorizers untouched unless title debt shared.

**Change:** remove hard-coded titles on capability throws; delete dead HostPromotionAdminAuthorizer residue (module already owns PromotionAdminAuthorizer).

## W3 — Access certification (~15–20 min)

Certify Admin/Access after W1+W2: durable Access guards, SoT Access CERTIFIED / NOT_CERTIFIED_BY_PANEL superseded by Access CERT, Panel/Development/Party/Grid protection remains.

## Wave count

Recommended-Wave-Count: **3**  
Recommended-Next-Task: `TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1` (Architect-issued only; not auto-started)
