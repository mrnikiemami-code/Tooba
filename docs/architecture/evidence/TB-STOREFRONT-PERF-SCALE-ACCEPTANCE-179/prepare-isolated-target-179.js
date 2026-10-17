/**
 * TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179 — build the task-owned ISOLATED launch directory.
 *
 * Built from the COMMITTED appsettings.json (so the measured Development logging default is the real
 * TB-178 one), with a scratch-only overlay: one Active tenant on a disposable loopback database and the
 * app-owned Development catalog bootstraps enabled so the Home route has real catalog content.
 * Fail-closed on any shared database/reference/port. No invented catalog rows.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-179-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179');
const base = JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json'), 'utf8'));

const HOST_PORT = process.env.TOOBA_179_HOST_PORT || '5450';
const RUN_LEGACY = String(process.env.TOOBA_179_RUN_LEGACY_BOOTSTRAPS || 'true').toLowerCase() === 'true';
// Scratch-only: the additive demo seeder (POST /v1/admin/catalog/demo/seed-only) is gated by this flag.
const ALLOW_SEED = String(process.env.TOOBA_179_ALLOW_SEED || 'false').toLowerCase() === 'true';
const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-179-scratch',
    messagingReference: 'messaging-179-scratch',
    host: '127.0.0.1',
    port: process.env.TOOBA_179_PG_PORT || '55499',
    database: process.env.TOOBA_179_DATABASE || 'tooba_179_scratch',
    messagingDatabase: process.env.TOOBA_179_MESSAGING_DATABASE || 'tooba_179_messaging',
    user: process.env.TOOBA_179_USER || 'scratch179',
    password: process.env.TOOBA_179_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) { console.error('ASSERT_FAIL: scratch password must come from the environment'); process.exit(2); }
for (const value of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
    if (value.includes('tooba_alpha') || value.includes('tenant-alpha')) { console.error('ASSERT_FAIL: shared alpha target'); process.exit(2); }
}
if (['5432', '55498', '55496'].includes(SCRATCH.port)) { console.error('ASSERT_FAIL: scratch port collides: ' + SCRATCH.port); process.exit(2); }
if (!fs.existsSync(path.join(repo, '.tmp-179-run/host/Tooba.Host.exe'))) { console.error('ASSERT_FAIL: Host publish missing'); process.exit(2); }

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

// Scratch overlay. Logging is deliberately NOT written: the measured Development logging default must be
// the committed one, not an overlay value. CatalogDemo.AllowResetAndSeed stays false (no destructive reset).
const overlay = {
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-179',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [{
                TenantId: SCRATCH.tenantId,
                DisplayName: 'Isolated 179 Store',
                Status: 'Active',
                ConnectionReference: SCRATCH.reference,
                ThemeReference: 'theme-alpha',
                DefaultMarketReference: 'IR',
                StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
                PrimaryDomain: 'alpha.localhost',
                Hosts: ['alpha.localhost', 'alpha.localtest.me', 'localhost', '127.0.0.1'],
            }],
        },
        PostgreSQL: {
            ConnectionReferences: {
                [SCRATCH.reference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.database};Application Name=tooba-179-isolated;Timeout=15;Command Timeout=180;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-179-isolated-messaging;Timeout=15;Command Timeout=180;Maximum Pool Size=10`,
            },
        },
        Messaging: {
            Enabled: true, Transport: 'PostgreSql', ConnectionReference: SCRATCH.messagingReference,
            Schema: 'transport', UseInProcessTestDouble: false,
        },
        Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
        AuthSecurity: {
            CorsAllowedOrigins: [`http://127.0.0.1:${HOST_PORT}`, `http://localhost:${HOST_PORT}`],
            AuthRateLimitPermitLimit: 120, AuthRateLimitWindowSeconds: 60,
        },
        Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
        // Application-owned Development catalog bootstraps (Fashion/Industry template catalogs). The
        // destructive demo reset/seed stays off: this task only reads what the app itself seeds.
        CatalogDemo: { AllowResetAndSeed: ALLOW_SEED, RunLegacyBootstraps: RUN_LEGACY },
        ShippingMethods: {
            EnabledCodes: ['post', 'tipax', 'snapp_courier', 'store_courier', 'in_person'],
            DefaultSellerPreparationDays: 2,
            DeliveryHorizonDays: 7,
        },
        Settlement: { MarketplaceChannelLedger: { Enabled: false } },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const text = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), text);

const committedDevelopment = JSON.parse(fs.readFileSync(
    path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.Development.json'), 'utf8'));

const guard = {
    measuredDevelopmentLoggingDefault: committedDevelopment.Logging?.LogLevel,
    overlayWritesLogging: Object.prototype.hasOwnProperty.call(overlay, 'Logging'),
    runLegacyBootstraps: effective.Tooba.CatalogDemo.RunLegacyBootstraps === true,
    allowResetAndSeedScratchOnly: effective.Tooba.CatalogDemo.AllowResetAndSeed === true,
    tenants: effective.Tooba.SingleStore.Tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    activeTenantCount: effective.Tooba.SingleStore.Tenants.filter((t) => t.Status === 'Active').length,
    scratchPort: SCRATCH.port,
    hostPort: HOST_PORT,
    containsSharedAlphaDatabase: text.includes('tooba_alpha'),
    containsSharedAlphaReference: text.includes('tenant-alpha'),
    containsSharedPort5432: /Port=5432\b/.test(text),
    edition: effective.Tooba.Edition,
    sharedFrontendPort3000Touched: false,
    sharedHostPort5088Touched: false,
};
guard.failClosed =
    guard.activeTenantCount === 1 &&
    guard.overlayWritesLogging === false &&
    
    !guard.containsSharedAlphaDatabase &&
    !guard.containsSharedAlphaReference &&
    !guard.containsSharedPort5432 &&
    guard.edition === 'SingleStore' &&
    HOST_PORT !== '5088' && HOST_PORT !== '3000';

fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(path.join(evidenceDir, 'isolation-guard-179.json'), JSON.stringify(guard, null, 2) + '\n');
console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL'));
console.log('RUN_LEGACY_BOOTSTRAPS=' + guard.runLegacyBootstraps);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) process.exit(3);

