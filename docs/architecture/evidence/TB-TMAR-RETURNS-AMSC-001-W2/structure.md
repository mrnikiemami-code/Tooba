# TB-TMAR-RETURNS-AMSC-001 — Wave 2 (Structure)

Skill: `tooba-architecture-structure` (V2) · Wave 2 of the AMSC re-standardization of
`src/backend/Modules/Returns`.

Baseline: `main` @ `0a573864` (W1 Migrate commit; `HEAD == origin/main` at wave start).
Wave lineage: W0 `f5c5a6db` → W1 `0a573864` → W2 (this wave).
W0/W1 evidence consumed:
`docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W0/analyze.md`,
`docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W1/migrate.md`.

Scope discipline: **structure only**. No business rule, route, DTO, schema, error code, localization
key, telemetry name or Contracts API was changed by this wave. All production moves were performed in
W1 (behavior-preserving) and W2 verifies, normalizes and durably locks the resulting physical
organization.

---

## Classification states (section 4)

| # | Dimension | State |
|---|---|---|
| 1 | Module Applicability Gate | `HTTP_OWNING` |
| 2 | Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| 3 | Solution-Explorer-State | `CANONICAL` |
| 4 | Path-Namespace-State | `EXACT` |
| 5 | Physical-Copy-State | `CLEAN` |
| 6 | Root-Allowlist-State | `ENFORCED` |
| 7 | File-Cohesion-State | `COHESIVE` |
| 8 | **Structure-State (overall)** | **`READY_FOR_CERTIFY`** |
| 9 | Host final closure | `PRESERVED` |

---

## Module Applicability Gate

`HTTP_OWNING` — proven, not assumed:

- `Tooba.Returns.Endpoints` owns **11 real module HTTP routes**: 4 Admin, 3 Seller, 4 Customer
  (W0 §route map; W1 20 HTTP-reachable + 1 platform-reachable request split).
- `ReturnEndpointModule.cs` is a real route mapper (`MapReturnEndpoints()`), not an empty
  `MapGroup(...)` placeholder.
- `Host` holds only `MapReturnEndpoints()` invocation plus the two module-owned authorizer seams
  (`ALLOWED_SECURITY_ADAPTER`); zero Returns business/persistence residue in Host.

Therefore the canonical six-project shape (Contracts, Domain, Application, Infrastructure,
Endpoints, Tests) is legitimate and no project is ceremonial.

---

## What W2 changed

| Change | Path | Rationale |
|---|---|---|
| Manifest pre-cert record added for Returns | `docs/architecture/tmar-module-structure-manifests.json` | Structure skill §23: honest physical allowlists/forbidden lists for the touched module; `structureCertified` deliberately left `false` |
| Returns removed from `uncertifiedHttpOwningModules` | same | Module is no longer an *uncertified/unknown-structure* HTTP owner; it is now a recorded pre-cert structure surface |
| Durable W2 structure guard added | `src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W2StructureGuardTests.cs` | Section 24 scoped machine enforcement (7 tests) |
| W2 evidence set | `docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W2/*` | Section 26 required evidence |

No production `.cs` was added, moved, renamed or deleted in W2: the physical tree produced by W1 was
already `PROFESSIONAL_SHALLOW`; W2 verified it exhaustively, recorded it honestly in the manifest and
locked it with durable guards.

---

## Handoff

`Structure-State = READY_FOR_CERTIFY` ⇒ hand off to `tooba-architecture-certify` (W3).

Explicitly **not** claimed by this wave:

- whole-module certification (`structureCertified` stays `false`; `modules[]` untouched);
- validator semantic coverage verdict (preserved, not re-owned — section 4b);
- Result/localization/telemetry semantics (W1-owned, untouched here);
- Host final closure verdict (preserved, re-verified in `root-allowlist.md`).

Do not self-authorize the next Architect task.
