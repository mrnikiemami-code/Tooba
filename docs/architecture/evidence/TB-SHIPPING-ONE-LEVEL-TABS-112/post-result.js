// TB-SHIPPING-ONE-LEVEL-TABS-112 — post the complete Worker Result contract to Bridge.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const ROW_ID = '4cb32595-9fbb-42d2-b6bc-4d6ce45949e4';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-112');

const content = `BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-112
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-111
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_DEVELOPMENT_ATTRIBUTE_SEED_COLLISION_REPAIR
Bridge-Task-Row: ${ROW_ID}
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: c862d6c63ef571addb8c593f5bd63997a86e29dc
Base-SHA: 24a1337d173f2f5f6ef77104d5d4fcd03380562b
Code-SHA: 8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7
Evidence-SHA: 8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7
automaticNextImplementationTask: NONE

PREFLIGHT
Claimed exactly one Pending task on tooba-main (row ${ROW_ID}); docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-112.task.md preserved UNSTAGED as the receipt and copied into evidence.
Expected-Base-SHA c862d6c is an ancestor of the actual base. Drift c862d6c..24a1337d is DOCS-ONLY evidence transport
(TB-SHIPPING-ONE-LEVEL-TABS-111/RESULT.bridge.txt, TB-SHIPPING-ONE-LEVEL-TABS-111/post-result.js) with zero source overlap.
Pre-existing dirty/untracked files (TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/summary.md, TB-SHIPPING-FULL-OPERATIONS-075/host-stdout-075.txt,
TB-SHIPPING-FULL-OPERATIONS-079/host-stdout-OFF.txt, frontend admin fulfillment work-queue screen, and the earlier evidence trees) were left untouched, unstaged and unadopted.
Shared :5088/:3000 and tooba_alpha accessed read-only only (no connection, no DDL/DML, no restart, no kill).

MISSION A — EXACT COMPATIBILITY CONTRACT
Root cause: post-migration step CatalogDevelopmentSeed.PostMigration.EnsureAsync runs CatalogAttributeSchemaDevelopmentSeed.ApplyAsync,
which creates attribute definition "color" (Enumeration, isVariantAxis=true) at CatalogAttributeSchemaDevelopmentSeed.cs:37-44 via
EnsureDefinitionAsync (:183-230) and the option "black" under it at :74 via EnsureOptionAsync (:247-263). The LEGACY Host bootstrap then called
WorkspaceDemoProductSeed.SeedProductAsync, which unconditionally called catalog.CreateAttributeDefinitionAsync("color", Enumeration, true, ...)
at WorkspaceDemoProductSeed.cs:103 (pre-fix) -> CatalogErrorCodes.AttributeCodeDuplicate "catalog.attribute.code.duplicate" -> InvalidOperationException
-> unhandled Development startup abort. This is a Development seed coordination problem; the public rejection policy stays intact.
Canonical read seam identified: ICatalogDirectory.ListAttributeDefinitionsAsync (Application port, Tooba.Catalog.Application/Ports/ICatalogDirectory.cs:121,
view Tooba.Catalog.Application/Models/AttributeDefinitionView.cs with Code, ValueKind, IsVariantAxisAllowed, IsActive). No direct EF query is used on the definition path.
Code normalization mirrors the domain (Trim().ToLowerInvariant(), CatalogAttributeDefinition.Create:100).
LATER USE OF THE RETURNED IDENTITY: the very next statement of the same method (pre-fix line 109) called AddAttributeOptionAsync(colorId, "black", ...),
and attribute_options carries a UNIQUE index on (DefinitionId, Code) (CatalogDbContext.cs). Re-creating that option on the reused definition would have raised a raw
unique-constraint DbUpdateException on the same immediate statement path, so compatible reuse covers the option too. No other seed attribute is on that immediate path.
No new global uniqueness behavior, no direct SQL, no rewrite of existing definitions/options/variant axes.

MISSION B — MINIMAL SOURCE AND TESTS
New Development-only helper src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentAttributeReuse.cs:
 EnsureCompatibleDefinitionAsync reads the canonical code through ListAttributeDefinitionsAsync; when absent it creates via the EXISTING Catalog API;
 when present it returns the SAME DefinitionId only if ValueKind, IsVariantAxisAllowed and IsActive all match the downstream requirement, otherwise it throws the
 stable data-minimal diagnostic "catalog-dev.attribute.incompatible-reuse: code=<code>; rules=<rule>[,<rule>]" ("value-kind" | "variant-axis" | "active")
 naming only the code and the mismatched rule(s) — no display text, no localized names — and performs no write.
 EnsureCompatibleOptionAsync reuses the existing OptionId by (definitionId, code) identity (module-owned CatalogDbContext read, mirroring the sibling seed helper
 CatalogAttributeSchemaDevelopmentSeed.EnsureOptionAsync) or creates via the EXISTING AddAttributeOptionAsync.
WorkspaceDemoProductSeed.cs: 10-line change, only the two unconditional creates became the two reuse calls. Method identity, ownership, existing definitions/options,
variant-axis and downstream variant semantics are unchanged; an empty database without prior attributes still bootstraps exactly as before.
Focused tests (Testcontainers PostgreSQL 16 with the real CatalogDirectory against the real catalog schema):
CatalogDevelopmentAttributeReuseTests (7): incompatibility diagnostic names only code + mismatched rules (pure); compatible definition reports no incompatibility (pure);
 empty catalog creates the required attribute through the Catalog API; existing compatible attribute is reused with the SAME persistent identifier; existing incompatible
 attribute is refused before any downstream write (nothing rewritten, no option appended, no display text in the message); repeated preparation is idempotent and keeps
 definition+option identity; reused axis and option identifiers stay valid for real variant creation.
WorkspaceDemoProductSeedCollisionRegressionTests (1): runs the real schema seed (owner of color/black) then the real affected operation SeedProductAsync and asserts the
 workspace product publishes, exactly one "color" definition exists with the SAME DefinitionId, "black" keeps the SAME OptionId, and the created variant's attribute value
 still points at the reused definition.
Run: Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8 (evidence/focused-tests.txt). Build: dotnet build src/backend/Tooba.slnx -c Debug -> 0 Error(s).
Negative control: with the pre-fix seed restored, the regression test fails with "System.InvalidOperationException : catalog.attribute.code.duplicate" at
 WorkspaceDemoProductSeed.cs:103 — the exact TB-111 failure, same code and same line (evidence/negative-control.txt).
NOT run on purpose: the broad known-failing Host suite (pre-existing unrelated architecture-guard drift: Host_Admin_count_15_* expects 15 *.cs under
src/backend/Host/Tooba.Host/Admin while the expected base SHA c862d6c and HEAD both contain 17, proven with git ls-tree -r), the 545-product demo seed,
 live browser flows, and any Host start.
No additional blocker was surfaced after the immediate collision: source trace shows the later seed stages are guarded by absence checks (demo product/brand/product created
only when absent), so no further duplicate-code path exists in this seed.

MISSION C — EVIDENCE / STOP
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-112.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-112/**
(task.md, task-receipt.json, root-cause.md, focused-tests.txt, negative-control.txt, shared-no-change.txt, commit-message.txt) record root-cause source lines,
the semantic reuse decision, changed paths, focused test names/counts, exact rollback and the next scratch prerequisite.
MIGRATION_RISK: none — Development-only seed code; no schema/migration/data/config/contract/Host change, so rollback needs no data repair.
ROLLBACK (exact): git revert 8f00b8f52 — delete CatalogDevelopmentAttributeReuse.cs and the two test files and restore the two unconditional creates in SeedProductAsync.
SHARED PARITY: :5088 -> PID 35624, :3000 -> PID 31580, byte-identical to the TB-111 shared capture; no shared restart/stop/DDL/DML/kill, no scratch Host start in this task.
ELAPSED: ~48 minutes (over the 30-minute HARD timebox; the overrun is the Testcontainers focused-test cycle, not scope expansion).
WIDENING NOTE FOR ARCHITECT: there is no Application-level read contract for enumeration options (ICatalogDirectory exposes AddAttributeOptionAsync only). TB-112 did NOT add
 one, because an option carries no further required semantic beyond (definitionId, code); if a canonical option read seam is wanted, that is a widening decision for the Architect.
NEXT: TB-113 isolated scratch runtime Host startup (watchdog <= 180s, empty scratch PostgreSQL, distinct scratch connection strings/tenant, offline effective-config guard,
 absolute-PID cleanup) to verify party.parties 42P01 stays 0, the catalog.attribute.code.duplicate failure is gone at real startup, whether real sellable offers/prices/inventory
 appear for schema-mobile-demo-phone, and the FIRST new blocker after the former collision point. No runtime/sellable/storefront-ready claim is made by TB-112.
END_TOOBA_WORKER_RESULT`;

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, 'RESULT.bridge.txt'), content + '\n', 'utf8');

    const res = await fetch(`${BRIDGE}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            channelId: 'tooba-main',
            workerId: 'tooba-worker-01',
            taskId: 'TB-SHIPPING-ONE-LEVEL-TABS-112',
            content,
        }),
    });
    const text = await res.text();
    console.log('POST /api/results', res.status, text.slice(0, 400));
}

main().catch((err) => { console.error(err); process.exit(1); });
