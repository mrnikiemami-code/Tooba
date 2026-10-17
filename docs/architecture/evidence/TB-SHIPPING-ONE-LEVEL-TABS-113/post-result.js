// TB-SHIPPING-ONE-LEVEL-TABS-113 — post the complete Worker Result contract to Bridge.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const ROW_ID = '30a1a2ca-1e25-43d9-9722-42295c387d73';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-113');

const content = `BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-113
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-112
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_ATTRIBUTE_COLLISION_RUNTIME_PROOF
Bridge-Task-Row: ${ROW_ID}
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7
Base-SHA: 8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7
Code-SHA: NONE
Evidence-SHA: 5c5c5d2a0072e4ed965063776c2bf299d5d05b7f
automaticNextImplementationTask: NONE

PREFLIGHT
Claimed exactly one Pending Bridge task on tooba-main (row ${ROW_ID}); docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-113.task.md preserved UNSTAGED (copy also in evidence). Expected-Base-SHA 8f00b8f52 is ancestor: HEAD == origin/main == 8f00b8f52, drift 0 files, RECOVERY_CONFLICT not triggered. All pre-existing dirty/untracked files (TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/summary.md, TB-SHIPPING-FULL-OPERATIONS-075/host-stdout-075.txt, TB-SHIPPING-FULL-OPERATIONS-079/host-stdout-OFF.txt, the frontend admin screens, earlier evidence trees) untouched and unadopted. No reset/stash/rebase/force-push.

READ-ONLY SHARED BASELINE (captured before any launch)
:5088 -> PID 35624; /health 200 {"status":"ok"}; /__platform-commerce 200 with edition SingleStore, deploymentId dev-singlestore, tenantId store-alpha, connectionReference tenant-alpha, theme alpha, market IR.
:3000 -> PID 31580; GET / -> 308 http://localhost:3000/fa.
tooba_alpha counts: methods 7, translations 14, carts 50, cart_lines 78, drafts 14, seller_orders 26, products 567, offers 3223.
These were comparison expectations, never assumptions; no shared process or database was stopped, restarted, killed or modified.

MISSION A — ONE WATCHDOG-BOUNDED ISOLATED START
Build: dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug -o .tmp-113-host-out -> exit 0, 0 errors, EXE_EXISTS=True, from HEAD 8f00b8f52 (contains TB-112 reuse + TB-110 two-phase ordering).
Scratch: fresh no-volume disposable container tooba-113-scratch-pg (postgres:16-alpine) on loopback-only 127.0.0.1:55448 (not shared 5432, not prior TB-111 scratch 55447) with task-owned databases tooba_113_scratch and tooba_113_messaging; credentials generated per-run, supplied only via the TOOBA_113_SCRATCH_PASSWORD environment variable and stored only under the ignored .tmp-113-run directory (deleted at teardown).
OFFLINE FAIL-CLOSED EFFECTIVE-CONFIG GUARD (isolation-guard.json, guard.failClosed=true): tenantCount 1, activeTenantCount 1, tenant store-alpha:Active:tenant-113-scratch, connectionReferences [tenant-113-scratch, messaging-113-scratch], containsSharedAlphaDatabase=false, containsSharedAlphaReference=false, containsSharedAlphaPort=false, containsPriorScratchPort=false, ShippingRateAuthority/ShippingOperationalShadow/ShippingOneLevelCatalog all absent/OFF, AllowResetAndSeed=false, RunLegacyBootstraps=true, ASPNETCORE_ENVIRONMENT=Development. No credentials in any retained artifact.
LAUNCH: LAUNCH_COUNT=1, no retry. ISOLATED_HOST_PID=6192 with absolute path .tmp-113-host-out\\Tooba.Host.exe, isolated ASPNETCORE_URLS http://127.0.0.1:5396, task-owned content root, external watchdog limit 180s. No shared Host/Next invocation of any kind. HTTP /admin/catalog/demo/reset-and-seed and seed-only were never called (no 545-product expansion).

ORIGINAL COLLISION: YES -> NO.
Full 1,657-line isolated stdout: "catalog.attribute.code.duplicate" occurrences = 0; "catalog-dev.attribute.incompatible-reuse" occurrences = 0; "42P01" occurrences = 0; "party.parties" referenced 22 times and resolves (party.parties holds 4 rows). The entire DevelopmentSchemaMigrator.ApplyAsync(app.Services) call at Program.cs:367 COMPLETED, including the legacy workspace demo seed chain (WorkspaceDemoSeed.SeedNewProductAsync -> WorkspaceDemoProductSeed.SeedProductAsync) that TB-111 crashed in; its last SQL was the archive update of admin-r3-archived-hat, the final statement of that chain. So the TB-112 compatible read-or-create repair holds at real startup and the incompatibility guard did not fire.

HEALTH: HEALTH_200_OBSERVED=False. The process exited by itself at ELAPSED_SECONDS=61 of the 180s budget (PROCESS_EXITED=True, KILLED_BY_WATCHDOG=False, listeners on 5396 after stop = 0). No retry was attempted, so no isolated /health body or isolated provenance could be read; no customer/cart/storefront eligibility is inferred from anything.

NEW FIRST BLOCKER (exact)
System.InvalidOperationException: Cannot resolve scoped service 'Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext' from root provider.
  at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(...)
  at Tooba.Catalog.Infrastructure.Development.FashionTemplateCatalogSeed.ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken) in ...\\FashionTemplateCatalogSeed.cs:line 59
  at Tooba.Catalog.Infrastructure.Development.CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(IServiceProvider provider, CancellationToken cancellationToken) in ...\\CatalogDevelopmentSeed.cs:line 61
  at Program.<Main>$(String[] args) in ...\\Program.cs:line 368
Blocker-Code: NONE (no Tooba SemanticError/catalog.* code; raw DI-lifetime container exception).
Module/source frame: Catalog Infrastructure Development, FashionTemplateCatalogSeed.cs:59
  (\`var catalog = provider.GetRequiredService<CatalogDbContext>();\`).
Why a different and later barrier: DevelopmentSchemaMigrator.ApplyCoreAsync creates an async scope and passes the SCOPED provider (DevelopmentSchemaMigrator.cs:46-47), which is why every earlier seed including the whole workspace demo chain succeeded. Program.cs:368 hands the ROOT app.Services directly to CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync, whose first legacy bootstrap resolves a scoped service from that root. TB-111 never reached Program.cs:368; TB-112 touched none of these files. Not fixed, not relaunched, per Mission A.

READ-ONLY SCRATCH SQL (status/quantity/referential integrity only; no fixtures, no hand-written migrations)
migration history: catalog 29, inventory 5, localization 1, offer 3, party 2, pricing 1, promotion 2, tax 1.
attribute_definitions by code: color 1, ram 1, screen_size 1, storage 1 -> exactly ONE color definition; attribute_options by (code,option): color/black 1, color/blue 1, ram/8gb 1, storage/128gb 1, storage/256gb 1 -> exactly ONE black option, so no duplicate definition and no duplicate option on the unique index attribute_options(definition_id, code).
STABLE IDENTITY (runtime, from persisted data): the LEGACY workspace variant workspace-live-shirt / LIVE-SHIRT-BLK / color stores definition_id 01a14266-d5b4-7000-9836-fedb228ab758, the SAME color definition_id used by schema-mobile-demo-phone / PHONE-BLK-128 and / PHONE-BLU-256, with canonical value equal to the shared black option id. The legacy seed consumed the schema seed's existing definition instead of creating a second one.
PRODUCT/VARIANT/DEFAULT: schema-mobile-demo-phone Published with exactly 2 variants and exactly 1 default (PHONE-BLK-128); the two variants differ on BOTH axes (distinct combination fingerprints), so no Cartesian matrix was produced. workspace-live-shirt Published (1 variant, not default), admin-r3-draft-scarf Draft, admin-r3-archived-hat Archived.
ACTIVE PRICED STOCKED OFFERS: PhONE-BLK-128 (default) 2 offers / 2 active / 1 active Base price / available 5.000000; PHONE-BLU-256 1 offer / 1 active / 1 active Base price / available 5.000000. Totals: offers 5, active_offers 5, prices 6, active_base_prices 4, stock_positions 6, positive on_hand 5, seller_parties 4 (party.parties), tax_rules 1.
LATER SEED DATA: reached only schema-mobile-demo-phone + the workspace demo product and its Admin R3 draft/archived clones; the fashion/industry template catalogs, landing pages and store menu did NOT run (the process aborted in the first legacy bootstrap). Scratch remains commerce-free: carts 0, seller_orders 0, shipping_methods 0.
INTERPRETATION CAVEAT: these are row-count/integrity observations. They are NOT proof of customer/cart/storefront eligibility; no storefront surface was exercised and no production flag was toggled.

MISSION B — TEARDOWN + NON-INTERFERENCE
Task-owned Host PID 6192 confirmed not alive; listeners on 5396 = 0. docker rm -f tooba-113-scratch-pg removed the ONLY task-owned container; no tooba-113 containers remain and no tooba-111 container was touched. Ignored .tmp-113-run and .tmp-113-host-out deleted (including the scratch credential file). Listeners on 55448 after container removal = 0. No name/wildcard termination and no broad PID cleanup; nothing on a shared port was ever a stop target.
SHARED PARITY POSTFLIGHT (read-only): :5088 -> PID 35624 and :3000 -> PID 31580, byte-identical to preflight and to the TB-111 capture; /health 200 {"status":"ok"}; /__platform-commerce unchanged (store-alpha / tenant-alpha / dev-singlestore / theme-alpha / IR); Next 308 -> /fa; tooba_alpha counts methods 7, translations 14, carts 50, cart_lines 78, drafts 14, seller_orders 26, products 567, offers 3223 — IDENTICAL to baseline. No INCIDENT, no unexpected shared change, nothing silently repaired.
EVIDENCE (committed, redacted, secrets scrubbed by programmatic scan): docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-113/** (task.md, task-receipt.json, isolation-guard.json, prepare-isolated-target-113.js, prepare-output.txt, build-record.txt, launch-isolated-host-113.ps1, watchdog-result.txt, redacted-diagnostic-excerpt.txt, program-startup-sequence.txt, scratch-inspection.sql/.txt, identity-reuse.txt, sellable-chain-evidence.txt, shared-parity-baseline.sql, shared-baseline-counts.txt, shared-preflight.txt, shared-postflight.txt, shared-postflight-counts.txt, shared-probe.js, cleanup.txt, summary.md, commit-message.txt) and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-113.md. No raw secrets, tokens, passwords, long stdout or ephemeral DB identifiers are committed.

MEASURED MINUTES: ~24 (01:46 -> 01:53 UTC+3:30), within the 30-minute hard ceiling. LAUNCHES: 1. WATCHDOG ELAPSED: 61s of 180s.
CHANGED PATHS: docs/evidence only (docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-113.md + docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-113/**). No src/, tests, migrations, Host config, frontend, scripts or shared data change. Code-SHA: NONE.
ROLLBACK: not required — evidence-only commit 5c5c5d2a0; git revert would remove documentation only and cannot affect runtime state or data.
DEVIATIONS: (1) PARTIAL rather than PASS because the single launch aborted before /health; no retry was performed as instructed. (2) Exact runtime identity stability for the reused definition is proven by (a) exactly one color definition/black option existing, (b) the legacy variant storing the same definition_id as the schema variants, and (3) the TB-112 focused tests asserting the same persistent identifier across a repeated preparation; no pre/post ID comparison is possible on a fresh scratch database.

NEXT: Architect decision on the now-first empty-scratch startup barrier — the Program.cs:368 root-provider hand-off into CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync, whose first call FashionTemplateCatalogSeed.ApplyAsync (line 59) resolves the scoped CatalogDbContext from the root provider. Suggested follow-up: a bounded repair task for that DI lifetime hand-off (or the seed creating its own scope), then ONE new isolated scratch startup to observe the next stage. No automatic next implementation task is claimed here.
END_TOOBA_WORKER_RESULT`;

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, 'RESULT.bridge.txt'), content + '\n', 'utf8');
    const res = await fetch(`${BRIDGE}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ channelId: 'tooba-main', workerId: 'tooba-worker-01', taskId: 'TB-SHIPPING-ONE-LEVEL-TABS-113', content }),
    });
    console.log('POST /api/results', res.status, (await res.text()).slice(0, 400));
}

main().catch((err) => { console.error(err); process.exit(1); });
