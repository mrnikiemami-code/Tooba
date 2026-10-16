/**
 * TB-SHIPPING-ONE-LEVEL-TABS-111 — build the task-owned ISOLATED launch directory.
 * Same guarded pattern as 109 (proven recipe). No secrets are written to evidence.
 * RunLegacyBootstraps=true is the only supported way to reach
 * CatalogAttributeSchemaDevelopmentSeed.PublishProductAsync + CatalogAttributeSchemaSellableEnricher.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-111-run');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-111-scratch',
    messagingReference: 'messaging-111-scratch',
    host: '127.0.0.1',
    port: '55447',
    database: 'tooba_111_scratch',
    messagingDatabase: 'tooba_111_messaging',
    user: 'scratch111',
    password: process.env.TOOBA_111_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must be supplied via the environment, never hard-coded');
    process.exit(2);
}

const refs = Object.values(SCRATCH).filter((v) => typeof v === 'string');
for (const ref of refs) {
    if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) {
        console.error('ASSERT_FAIL: a scratch reference must never point at the shared alpha target');
        process.exit(2);
    }
}
if (SCRATCH.port === '5432') {
    console.error('ASSERT_FAIL: scratch port must never be the shared 5432 listener');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-111',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [
                {
                    TenantId: SCRATCH.tenantId,
                    DisplayName: 'Isolated 111 Store',
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
                    `Database=${SCRATCH.database};Application Name=tooba-111-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-111-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
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
        // The subject of this task: the TB-110 two-phase seam must let the Catalog post-migration step run after
        // every module schema (Party included) has migrated. HTTP reset/seed stays disabled (no 545-product reset).
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const effectiveText = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), effectiveText); // ignored path; NOT evidence

// Offline isolation guard results (no secrets: only reference NAMES and shared-target assertions).
const guard = {
    tenants: effective.Tooba.SingleStore.Tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    tenantCount: effective.Tooba.SingleStore.Tenants.length,
    connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
    messagingReference: effective.Tooba.Messaging.ConnectionReference,
    containsSharedAlphaDatabase: effectiveText.includes('tooba_alpha'),
    containsSharedAlphaReference: effectiveText.includes('tenant-alpha'),
    containsSharedAlphaPort: /Port=5432/.test(effectiveText),
    scratchPort: SCRATCH.port,
    runLegacyBootstraps: true,
    allowResetAndSeed: false,
    shippingRateAuthoritySet: effectiveText.includes('ShippingRateAuthority'),
    shippingOperationalShadowSet: effectiveText.includes('ShippingOperationalShadow'),
    shippingOneLevelCatalogSet: effectiveText.includes('ShippingOneLevelCatalog'),
};
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));

const env = [
    'ASPNETCORE_ENVIRONMENT=Development',
    'DOTNET_ENVIRONMENT=Development',
    'ASPNETCORE_URLS=http://127.0.0.1:5395',
].join('\n') + '\n';
fs.writeFileSync(path.join(outDir, 'isolated.env.txt'), env);

console.log('PREPARE=PASS');
console.log(`RUN_DIR=${outDir}`);
console.log(`GUARD=${JSON.stringify(guard)}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
