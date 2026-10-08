# TB-TMAR-PROMOTION-AMSC-001-W2 — Structure (module-local)

| Field | Value |
| --- | --- |
| Task | `TB-TMAR-PROMOTION-AMSC-001-W2` |
| Skill | `tooba-architecture-structure` (3rd of the 4 AMSC skills) |
| Target | `src/backend/Modules/Promotion/Tooba.Promotion.*` |
| Parent wave | `TB-TMAR-PROMOTION-AMSC-001-W1` Migrate — `06858933` |
| Starting head | `06858933` (branch `main`) |
| Structure-State | **`READY_FOR_CERTIFY`** |
| Verdict | hand off to `tooba-architecture-certify` (W3) |

## Classification states

| State | Value |
| --- | --- |
| Module applicability gate | `HTTP_OWNING` (21 module-owned routes; Endpoints project genuinely required) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` (was `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED`) |
| Single-file request leaf folders | `ZERO` (was 9) |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` (was `MISMATCH` — 2 files) |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |

## What W2 changed

W2 is a **pure structure wave**: files were relocated, two namespaces were corrected, and the durable
structure guard + manifest record were added. No business rule, route, method, status code, DTO,
schema, migration, error code, localization key, telemetry name or project boundary changed.

1. **Capability-first Application tree.** `Application/Promotions/{Commands,Queries,Models,Ports}` and
   `Application/Merchandising/{Ports,Models,Admin/{Commands,Queries}}` are now the axes; only the
   genuinely shared `Checkout/`, `Composition/` (single `PromotionOperation` typed-fault seam) and
   `Validation/` remain at the Application capability root.
2. **Nine use-case leaf folders flattened.** `Application/Commands/<UseCase>` and
   `Application/Queries/<UseCase>` (each wrapping exactly one production source file) were removed and
   their files placed directly on the `Promotions` capability axes.
3. **Retired technical-axis / empty folders deleted**: `Application/Commands`, `Application/Queries`,
   `Application/Models`, `Application/Ports`, `Application/Promotions/Validators`,
   `Application/Merchandising/Admin/Validators`.
4. **Path↔namespace repaired** for `Merchandising/Models/MerchandisingCampaignAdminModels.cs` and
   `Merchandising/Models/MerchandisingCampaignReferences.cs` (both still declared `…Merchandising.Ports`),
   plus the 15 consumers gained the explicit `…Merchandising.Models` using.
5. **One malformed file normalized**: `Endpoints/Seller/IPromotionSellerAuthorizer.cs` (single-line
   namespace + no XML docs) was reformatted to the canonical documented shape; same interface, same
   signature, same namespace.
6. **Manifest record added** (`preCertModules[Promotion]`, `structureCertified: false`) with honest
   per-project allowlists/forbidden lists; Promotion removed from `uncertifiedHttpOwningModules`.
7. **Durable guard added**: `PromotionModuleAmsc001W2StructureGuardTests` (7 tests).
8. **Guards aligned to the real surface** (same intent, nothing removed):
   `PromotionArchitectureGuardTests.AllowedApplicationFolders` narrowed to the real W2 layout;
   `HostAdminAmcW33MerchandisingGuardTests` and `PromotionFoundationTests` repointed from the pre-W2
   paths to `Merchandising/Ports/…` and `Promotions/Ports/IPromotionEvaluator.cs`.

## Host final closure

* No new Host folder, file, route, `PromotionDbContext` reference or business/persistence authority.
* Host residue is unchanged and still limited to the composition root + the accepted
  `Host/Security/Seller/HostPromotionSellerAuthorizer.cs` platform security adapter.
* `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` preserved.

## Microservice-extractability

Structure-only wave; the W1 Contracts-only boundary is unchanged and the physical layout no longer
carries any cross-capability technical-axis coupling. Post-W2 the extraction dependency set for
`Tooba.Promotion.Endpoints` remains `Promotion.Application` + `Promotion.Contracts` + BuildingBlocks.

## Validation

See `validation.md`. Summary: endpoints chain build 0 errors; Host.Tests build 0 errors; module tests
9/9; W2 guard 7/7; W1 guard 10/10; focused Host guards 46 pass / 0 fail; full Host suite 77 failed vs the
W1 baseline of 77 failed (no new failure, +7 tests); both architecture JSON files parse.

## Evidence index (this folder)

* `physical-tree-before.md`, `physical-tree-after.md`
* `folder-granularity.md`
* `capability-map.md`
* `solution-explorer.md`
* `path-namespace.md`
* `root-allowlist.md`
* `stale-duplicate-copy.md`
* `cohesion-balance.md`
* `manifest-structure.md`
* `validation.md`
* `host-tests-w2-normal.txt` (raw full-suite output)

## Stop

`workflowStop = USER_REVIEW_PROMOTION_AMSC_001_W2`; `automaticNextImplementationTask = NONE`.
The next wave (`W3` Certify) is **not** self-authorized by this wave.
