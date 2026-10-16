/**
 * TB-SHIPPING-ONE-LEVEL-TABS-141 — build the task-owned ISOLATED launch directory.
 * Same guarded recipe as TB-138/TB-140 (TB-117/119/121/122/123/132/134 lineage), unchanged in every
 * safety dimension: one Active tenant, no tooba_alpha / tenant-alpha / Port=5432 / prior scratch port,
 * no ShippingRateAuthority, no ShippingOperationalShadow. The password comes from the environment and is
 * never written into committed evidence.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-141-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-141');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-141-scratch',
    messagingReference: 'messaging-141-scratch',
    host: '127.0.0.1',
    port: '55462',
    database: 'tooba_141_scratch',
    messagingDatabase: 'tooba_141_messaging',
    user: 'scratch141',
    password: process.env.TOOBA_141_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must come from the environment, never hard-coded');
    process.exit(2);
}
for (const ref of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
    if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) {
        console.error('ASSERT_FAIL: a scratch reference must never point at the shared alpha target');
        process.exit(2);
    }
}
const priorPorts = ['5432', '55447', '55448', '55449', '55450', '55451', '55452', '55453', '55454', '55455', '55458', '55460', '55461'];
if (priorPorts.includes(SCRATCH.port)) {
    console.error('ASSERT_FAIL: scratch port must not be the shared 5432 listener nor a prior scratch port');
    process.exit(2);
}
if (!fs.existsSync(path.join(repo, '.tmp-140-host-out/Tooba.Host.exe'))) {
    console.error('ASSERT_FAIL: isolated Host publish output is missing');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-141',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [
                {
                    TenantId: SCRATCH.tenantId,
                    DisplayName: 'Isolated 141 Store',
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
                    `Database=${SCRATCH.database};Application Name=tooba-141-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-141-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
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
            CorsAllowedOrigins: ['http://127.0.0.1:3107', 'http://localhost:3107'],
            AuthRateLimitPermitLimit: 30,
            AuthRateLimitWindowSeconds: 60,
        },
        Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
        ShippingOneLevelCatalog: { Enabled: true },
        ShippingMethods: {
            EnabledCodes: ['post', 'tipax', 'snapp_courier', 'store_courier', 'in_person'],
            DefaultSellerPreparationDays: 1,
            DeliveryHorizonDays: 7,
        },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const effectiveText = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), effectiveText);

const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
    tenantCount: tenants.length,
    activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
    tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    canonicalGatewayTenantPresent: tenants.some((t) => t.TenantId === 'store-alpha' && t.Status === 'Active'),
    connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
    messagingReference: effective.Tooba.Messaging.ConnectionReference,
    containsSharedAlphaDatabase: effectiveText.includes('tooba_alpha'),
    containsSharedAlphaReference: effectiveText.includes('tenant-alpha'),
    containsSharedAlphaPort: /Port=5432\b/.test(effectiveText),
    containsPriorScratchPort: /Port=(55447|55448|55449|55450|55451|55452|55453|55454|55455|55458|55460|55461)\b/.test(effectiveText),
    scratchPort: SCRATCH.port,
    runLegacyBootstraps: true,
    allowResetAndSeed: false,
    oneLevelCatalogEnabledInOverlay: effective.Tooba.ShippingOneLevelCatalog.Enabled === true,
    edition: effective.Tooba.Edition,
    settlementHandlersRegistered: false,
    shippingRateAuthoritySet: effectiveText.includes('ShippingRateAuthority'),
    shippingOperationalShadowSet: effectiveText.includes('ShippingOperationalShadow'),
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
    !guard.shippingOperationalShadowSet;
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(
    path.join(evidenceDir, 'isolation-guard.json'),
    JSON.stringify(guard, null, 2) + '\n');

console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL'));
console.log(`GUARD=${JSON.stringify(guard)}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) { process.exit(3); }
