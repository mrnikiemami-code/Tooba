# TB207 Architect review — 2026-10-24

Verdict: ACCEPT_BUILD_AND_BOUNDED_SCHEMA_PREFLIGHT; proposed combined execution NOT approved.
Reviewed worker source/evidence149faf7cdb9456041374198861257ff966e21c9b against basea932fa2915fed4827635d0cb8dbb0260baf35945. Main at review equals the worker source. Diff is docs/evidence/task/recovery only. Read RESULT and readonly-sql-probe.mjs; independently inspected ShippingModule, the seven Shipping executable migrations, MigrationRunner Program/CLI/Orchestrator/Registry and previously Host Program startup.
Architect did not execute user's local SQL/build.

Accepted: reported isolated current Host build succeeds; output retained D:\temp\tb207-build\out, candidate hashes in RESULT. Shipping module explicitly has no startup migrator. Read-only worker observations establish seven pending Shipping migrations and two pending Fulfillment migrations on tenant-alpha/tooba_alpha, and no fulfillment pending-index collision at probe time. Geography has no pending migration.
Readiness, actual authorized draft cardinality and live write acceptance remain unproven.

## Required corrections to execution plan

1. MigrationRunnerCli has NO module selector. Only status/plan/apply plus tenant selectors exist; MigrationOrchestrator iterates ModuleMigrationRegistry.All. “Run existing runner for Shipping only” is unsupported. Do NOT invent --module or present the old proposal as executable.
2. The report only enumerates live applied/pending ids for Shipping/Fulfillment/Geography. It does NOT establish exactly nine pending across every startup/runner module. A full runner plan/status or equivalent read-only all-context comparison must precede any approved apply.
3. One Shipping Up (20261022075711_AuthoringRateRowConstraintParity) DROPS and REPLACES five CHECK constraints on tables created by earlier pending migrations. It is not literally create/add-only. Other Shipping Ups create draft/group/area/rate/translation/title/week/settings structures, add nullable currency/type columns and owned constraints. No seed/backfill/data SQL is present in these seven Ups. Existing data must not be guessed empty.
4. Build command sets BaseOutputPath and OutDir, NOT BaseIntermediateOutputPath. Release intermediates may be in repo obj/Release. Do not claim all intermediates are outside repo; debug output isolation is the reported evidence. No build repeat solely to repair prose.
5. Built source is verified pre-work basea932fa2915fed4827635d0cb8dbb0260baf35945 (code unchanged), not the later result commit. Verify hashes and runtime config at use. No runtime has been launched.
6. Restarting an old executable is a BINARY rollback only; it does not undo database migrations, seeds or background side effects. Actual live process environment was UNKNOWN and must not become a claimed captured rollback environment.
7. Startup calls are real, but “NO suppression” at each wrapper call is not proof every downstream operation lacks an internal gate. Full seed/worker effect control is not established. No blanket seed approval or environment switch.

## Concrete approval boundary

Prepared schema-only execution plan: see EXECUTION-PLAN.md in this directory.
Request approval ONLY for the nine named migrations on the one local target, conditional on the complete actual pending set matching exactly, identity/collision/backup checks and no concurrent migration.
This is schema/history DDL and constraint replacement, not merchant business data seeding. No Host restart or application startup is included.
Runtime refresh must be reviewed separately after this schema stage and source-grounded startup effect/gate verification; no broad apply/restart implicit approval.
No next worker task has been issued at this review. Current state AWAITING_EXPLICIT_LOCAL_SCHEMA_APPROVAL.
