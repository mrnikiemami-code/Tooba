# Durable guards — AccessControl (W3)

## Module-scoped durable guards

| Guard | Added | Locks |
| --- | --- | --- |
| `AccessControlManifestDiskReconciliationGuardTests` | W2 | manifest↔disk project set, per-project `rootAllowlist` vs disk root `.cs`, `/Modules/AccessControl/` solution folder membership + entry resolution, no technical-axis-first request root, no single-file request/use-case leaf folder, exact path↔namespace |
| `AccessControlModuleAmsc001W1MigrateGuardTests` | W1 | capability-cohesive Models (no mixed dump), single shared `Validation/` folder, owned error-code constants (no raw `"access.*"` literals), no transport type shadowing CQRS `*Command` vocabulary, non-stale solution parser |
| `AccessControlValidatorTests` | pre-cert | 19-request inventory (6 required / 13 not-required), DI discoverability, validator placement + namespace, transport-shape-only rules, stable machine codes |
| `AccessControlModuleAmcW1SolutionGuardTests` | AMC W1 | `/Modules/AccessControl/` dedicated folder with exactly 5 project entries |
| `AccessControlModuleAmcW2SemanticGuardTests` | AMC W2 | `AccessControlException` carries a code only (no message prose) |
| `AccessControlModuleAmcW3ResultGuardTests` | AMC W3 | canonical result/error mapping in endpoints |
| `AccessControlModuleAmcW4StructureGuardTests` | AMC W4 | Domain `Aggregates/`, Contracts enums, no Endpoints→Domain reference |
| `AccessControlModuleAmcW5CertGuardTests` | AMC W5 | certification invariants + manifest/SoT presence |
| `AccessControlModuleAmc002W2CertGuardTests` | AMC-002 W2 | certification invariants |
| `AccessControlStructureRepair001GuardTests` | structure repair | Validation consolidation, `Exceptions/`/`Validators/` absence |
| `AccessControlStructureRecert001GuardTests` | structure recert | capability-first roots, no `Application/Commands`/`Queries`, `/Modules/AccessControl/` grouping, manifest certified, SoT checkpoints |
| `AccessControlFoundationTests` / `AccessControlRuntimeScopeTests` | pre-cert | module behavior + runtime scope |

## Coverage of the skill's §15 guard checklist

| Required guard concern | Guarded by |
| --- | --- |
| root allowlists | `TmarCompleteReferenceStructureGateTests` (shared) + W2 guard |
| forbidden root files | shared gate + W2 guard + manifest |
| forbidden top-level folders | shared gate + W1/W2 guards |
| capability-first folder granularity | W2 guard + `AccessControlStructureRecert001GuardTests` |
| no technical-axis-first request tree | W2 guard + recert guard |
| no unjustified single-file request folder | W2 guard |
| semantic Contracts/Application ownership, no mixed `*Contracts.cs` dump | W1 guard |
| no duplicate CQRS command/query shape | W1 guard |
| path↔namespace exactness | shared gate + W2 guard |
| no alias workaround | shared gate (`NO_NAMESPACE_ALIAS_WORKAROUND`) + W2 guard |
| endpoint-reachable request inventory | `AccessControlValidatorTests` |
| validator coverage | `AccessControlValidatorTests` |
| certified manifest membership | shared gate + W2 guard + `AccessControlModuleAmcW5CertGuardTests` |
| Host ownership expectations | `AccessControlStructureRecert001GuardTests` + Host closure guards |
| canonical API result/error mapping | `AccessControlModuleAmcW3ResultGuardTests` |
| canonical localization coverage | `AccessControlModuleAmcW2SemanticGuardTests` + error catalog guards |
| correlation/trace continuity | repo-wide telemetry guards |
| source-size/cohesion | `TmarSourceSizeAndInfraAppTests` (shared) |

## Guard integrity

| Check | Result |
| --- | --- |
| Guard weakened to reach PASS | `NONE` |
| Baseline widened improperly | `NONE` |
| Guard suppressed/disabled | `NONE` |
| Compatibility shim added to hide structure | `NONE` |
| Alias used to hide debt | `NONE` |

## Result

All AccessControl-scoped guards pass (see `validation.md`).
