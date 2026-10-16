/**
 * TB-SHIPPING-ONE-LEVEL-TABS-117 — build the task-owned ISOLATED launch directory.
 * Same guarded pattern as 109/111/113/115. No secrets are written to evidence.
 * RunLegacyBootstraps=true is the only way to reach the legacy Catalog bootstrap gateway that TB-116
 * repaired, which is exactly what this task must observe at runtime.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-117-run');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-117-scratch',
    messagingReference: 'messaging-117-scratch',
    host: '127.0.0.1',
    port: '55450',
    database: 'tooba_117_scratch',
    messagingDatabase: 'tooba_117_messaging',
    user: 'scratch117',
    password: process.env.TOOBA_117_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must be supplied via the environment, never hard-coded');
    process.exit(2);
}

for (const ref of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
    if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) {
        console.error('ASSERT_FAIL: a scratch reference must never point at the shared alpha target');
        process.exit(2);
    }
}
if (['5432', '55447', '55448', '55449'].includes(SCRATCH.port)) {
    console.error('ASSERT_FAIL: scratch port must not be the shared 5432 listener nor a prior scratch port');
    process.exit(2);
}
if (!fs.existsSync(path.join(repo, '.tmp-117-host-out/Tooba.Host.exe'))) {
    console.error('ASSERT_FAIL: isolated Host publish output is missing');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-117',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [
                {
                    TenantId: SCRATCH.tenantId,
                    DisplayName: 'Isolated 117 Store',
                    Status: 'Active',
                    ConnectionReference: SCRATCH.reference,
                    ThemeReference: 'theme-alpha',
                    DefaultMarketReference: 'IR',
                    StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
                    PrimaryDomain: 'alpha.localhost',
                    Hosts: ['alpha.localhost', 'alpha.localtest.me', 'localhost', '127.0.0.1'],
                },
            ],
        },
        PostgreSQL: {
            ConnectionReferences: {
                [SCRATCH.reference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.database};Application Name=tooba-117-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-117-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
            },
        },
        Messaging: {
            Enabled: true,
            Transport: 'PostgreSql',
            ConnectionReference: SCRATCH.messagingReference,
            Schema: 'transport',
            UseInProcessTestDouble: false,
        },
        Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
        AuthSecurity: {
            CorsAllowedOrigins: ['http://127.0.0.1:3000', 'http://localhost:3000'],
            AuthRateLimitPermitLimit: 30,
            AuthRateLimitWindowSeconds: 60,
        },
        Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
        // Subject of this task: the Development legacy path must be exercised on an empty database.
        // HTTP reset/seed stays disabled (no 545-product expansion).
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const effectiveText = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), effectiveText); // ignored path; NOT evidence

// Offline fail-closed isolation guard (no secrets: reference NAMES and shared-target booleans only).
const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
    tenantCount: tenants.length,
    activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
    tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    canonicalGatewayTenantPresent:
        tenants.some((t) => t.TenantId === 'store-alpha' && t.Status === 'Active'),
    connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
    messagingReference: effective.Tooba.Messaging.ConnectionReference,
    containsSharedAlphaDatabase: effectiveText.includes('tooba_alpha'),
    containsSharedAlphaReference: effectiveText.includes('tenant-alpha'),
    containsSharedAlphaPort: /Port=5432\b/.test(effectiveText),
    containsPriorScratchPort: /Port=(55447|55448|55449)\b/.test(effectiveText),
    scratchPort: SCRATCH.port,
    environment: process.env.ASPNETCORE_ENVIRONMENT || 'Development',
    runLegacyBootstraps: true,
    allowResetAndSeed: false,
    shippingRateAuthoritySet: effectiveText.includes('ShippingRateAuthority'),
    shippingOperationalShadowSet: effectiveText.includes('ShippingOperationalShadow'),
    shippingOneLevelCatalogSet: effectiveText.includes('ShippingOneLevelCatalog'),
};
guard.failClosed =
    guard.tenantCount === 1 &&
    guard.activeTenantCount === 1 &&
    guard.canonicalGatewayTenantPresent &&
    !guard.containsSharedAlphaDatabase &&
    !guard.containsSharedAlphaReference &&
    !guard.containsSharedAlphaPort &&
    !guard.containsPriorScratchPort &&
    !guard.shippingRateAuthoritySet &&
    !guard.shippingOperationalShadowSet &&
    !guard.shippingOneLevelCatalogSet;
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));

fs.writeFileSync(path.join(outDir, 'isolated.env.txt'),
    ['ASPNETCORE_ENVIRONMENT=Development', 'DOTNET_ENVIRONMENT=Development', 'ASPNETCORE_URLS=http://127.0.0.1:5398'].join('\n') + '\n');

fs.writeFileSync(path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-117', 'isolation-guard.json'),
    JSON.stringify(guard, null, 2) + '\n');

console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL'));
console.log(`RUN_DIR=${outDir}`);
console.log(`GUARD=${JSON.stringify(guard)}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) { process.exit(3); }
