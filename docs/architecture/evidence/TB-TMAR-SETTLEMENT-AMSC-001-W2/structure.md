# TB-TMAR-SETTLEMENT-AMSC-001 — Wave 2 (Structure)

Skill: `tooba-architecture-structure` (V2) · Wave 2 of the AMSC re-standardization of
`src/backend/Modules/Settlement`.

Baseline: `main` @ `4ca4aafc` (W1 Migrate commit; `HEAD == origin/main` at wave start).
Wave lineage: W0 `bac4dbe3` (Analyze) → W1 `4ca4aafc` (Migrate) → **W2 (this wave)**.
W0/W1 evidence consumed:
`docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W0/analyze.md`,
`docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W1/migrate.md`.

Scope discipline: **structure only**. No business rule, route, DTO, schema, error code, localization
key, telemetry name or Contracts API was changed by this wave. Every production move was performed in
W1 (behavior-preserving); W2 verifies, normalizes and durably locks the resulting physical
organization, and adds the missing certification note / forbidden-top-level-folder locks to the
manifest.

---

## Classification states (skill §4)

| # | Dimension | State |
|---|---|---|
| 1 | Module Applicability Gate | `HTTP_OWNING` |
| 2 | Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| 3 | Solution-Explorer-State | `CANONICAL` |
| 4 | Path-Namespace-State | `EXACT` |
| 5 | Physical-Copy-State | `CLEAN` |
| 6 | Root-Allowlist-State | `ENFORCED` |
| 7 | File-Cohesion-State | `COHESIVE` (`OVERSIZED_ONLY` watch on `SettlementDirectory.cs`, single responsibility) |
| 8 | **Structure-State (overall)** | **`READY_FOR_CERTIFY`** |
| 9 | Host final closure | `PRESERVED` |

---

## Module Applicability Gate (skill §4a)

`HTTP_OWNING` — proven, not assumed:

- `Tooba.Settlement.Endpoints` owns **10 real module HTTP routes**: 5 Seller
  (`/v1/seller/settlements/...`) + 5 Admin (`/v1/admin/settlements/...`).
- `SettlementEndpointModule.cs` is a real route mapper (`MapSettlementEndpoints()`), not an empty
  `MapGroup(...)` placeholder.
- `Host` holds only the `MapSettlementEndpoints()` invocation, `AddSettlementEndpointPresentation()`
  and the two module-owned authorizer seams (`ALLOWED_SECURITY_ADAPTER`); **zero** Settlement route
  literal, **zero** `Host/Settlement` folder, **zero** Settlement DbContext in Host.
- 10 endpoint-reachable MediatR requests, each with exactly one real `IRequestHandler<,>` (W1).

Therefore the canonical six-project shape (Contracts, Domain, Application, Infrastructure, Endpoints,
Tests) is legitimate and no project is ceremonial.

---

## What W2 changed

| Change | Path | Rationale |
|---|---|---|
| Manifest entry normalized + `certificationNote` / `evidence` added | `docs/architecture/tmar-module-structure-manifests.json` | §23: honest record for the touched module; the W1 edit had left the Settlement projects under-indented and carried no `certificationNote` unlike the other 28 certified entries |
| `forbiddenTopLevelFolders` populated for Application + Infrastructure | same | §19: lock the retired technical-axis-first roots so a future wave cannot silently re-create them |
| W2 evidence set | `docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W2/*` | §26 required evidence (12 documents) |

No production `.cs` was added, moved, renamed or deleted in W2: the physical tree produced by W1 was
already `PROFESSIONAL_SHALLOW`; W2 verified it exhaustively, recorded it honestly and locked it with
durable guards. The pre-existing W2 durable guard
(`src/backend/Host/Tooba.Host.Tests/Architecture/SettlementModuleAmsc001W2StructureGuardTests.cs`,
9 tests) is the machine enforcement for this wave and is green (see `validation.md`).

---

## Handoff

`Structure-State = READY_FOR_CERTIFY` ⇒ hand off to `tooba-architecture-certify` (W3).

Explicitly **not** claimed by this wave:

- whole-module certification verdict (that remains Certify);
- validator semantic coverage verdict (preserved, not re-owned — skill §4b);
- Result/localization/telemetry semantics (W1-owned, untouched here);
- Host final closure verdict (preserved, re-verified in `root-allowlist.md`).

Do not self-authorize the next Architect task.
