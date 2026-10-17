/**
 * TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — build the task-owned ISOLATED launch directory.
 *
 * Same guarded recipe as TB-166/170, with the single scratch-only difference this task authorizes:
 * Tooba:ShippingOwnedRateAuthority is set from TOOBA_173_GATE (ON for the main leg, OFF for the control).
 * Committed appsettings stay false; the observe-only shadow gates stay unset.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-173-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173');
const base = JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json'), 'utf8'));

const GATE = String(process.env.TOOBA_173_GATE || 'true').toLowerCase() === 'true';
const HOST_PORT = process.env.TOOBA_173_HOST_PORT || '5427';
const WEB_PORT = process.env.TOOBA_173_WEB_PORT || '3122';

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-173-scratch',
    messagingReference: 'messaging-173-scratch',
    host: '127.0.0.1',
    port: process.env.TOOBA_173_PG_PORT || '55493',
    database: process.env.TOOBA_173_DATABASE || 'tooba_173_scratch',
    messagingDatabase: process.env.TOOBA_173_MESSAGING_DATABASE || 'tooba_173_messaging',
    user: process.env.TOOBA_173_USER || 'scratch173',
    password: process.env.TOOBA_173_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) { console.error('ASSERT_FAIL: scratch password must come from the environment'); process.exit(2); }
for (const ref of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
    if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) { console.error('ASSERT_FAIL: shared alpha target'); process.exit(2); }
}
const forbiddenPorts = [
    '5432', '55447', '55448', '55449', '55450', '55451', '55452', '55453', '55454', '55455',
    '55458', '55460', '55461', '55462', '55463', '55470', '55471', '55473', '55475', '55486',
    '55487', '55488', '55489', '55490',
];
if (forbiddenPorts.includes(SCRATCH.port)) { console.error('ASSERT_FAIL: scratch port collides: ' + SCRATCH.port); process.exit(2); }
if (!fs.existsSync(path.join(repo, '.tmp-173-run/host/Tooba.Host.exe'))) { console.error('ASSERT_FAIL: Host publish missing'); process.exit(2); }

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning', 'Microsoft.EntityFrameworkCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-173',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [{
                TenantId: SCRATCH.tenantId,
                DisplayName: 'Isolated 173 Store',
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
                    `Database=${SCRATCH.database};Application Name=tooba-173-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-173-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
            },
        },
        Messaging: {
            Enabled: true, Transport: 'PostgreSql', ConnectionReference: SCRATCH.messagingReference,
            Schema: 'transport', UseInProcessTestDouble: false,
        },
        Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
        AuthSecurity: {
            CorsAllowedOrigins: [`http://127.0.0.1:${WEB_PORT}`, `http://localhost:${WEB_PORT}`],
            AuthRateLimitPermitLimit: 120, AuthRateLimitWindowSeconds: 60,
        },
        Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
        ShippingOneLevelCatalog: { Enabled: true },
        // The ONLY authority flag this task may enable, and only in this disposable overlay.
        ShippingOwnedRateAuthority: { Enabled: GATE },
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

const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
    gate: GATE,
    gateFlagLocation: 'scratch overlay only (.tmp-173-run/appsettings.Development.json)',
    committedDefaults: {
        appsettings: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
        development: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.Development.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
        production: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.Production.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
    },
    tenantCount: tenants.length,
    activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
    tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
    scratchPort: SCRATCH.port,
    hostPort: HOST_PORT,
    webPort: WEB_PORT,
    containsSharedAlphaDatabase: text.includes('tooba_alpha'),
    containsSharedAlphaReference: text.includes('tenant-alpha'),
    containsSharedPort5432: /Port=5432\b/.test(text),
    shippingRateAuthorityShadowSet: text.includes('ShippingRateAuthority"') || text.includes('ShippingRateAuthority:'),
    shippingOperationalShadowSet: text.includes('ShippingOperationalShadow'),
    edition: effective.Tooba.Edition,
    runLegacyBootstraps: effective.Tooba.CatalogDemo.RunLegacyBootstraps === true,
    allowResetAndSeed: effective.Tooba.CatalogDemo.AllowResetAndSeed === true,
    sharedFrontendPort3000Touched: false,
    sharedHostPort5088Touched: false,
};
guard.failClosed =
    guard.tenantCount === 1 && guard.activeTenantCount === 1 &&
    !guard.containsSharedAlphaDatabase && !guard.containsSharedAlphaReference &&
    !guard.containsSharedPort5432 &&
    !guard.shippingRateAuthorityShadowSet && !guard.shippingOperationalShadowSet &&
    guard.edition === 'SingleStore' &&
    guard.committedDefaults.appsettings === false &&
    guard.committedDefaults.development === false &&
    guard.committedDefaults.production === false &&
    guard.webPort !== '3000' && guard.hostPort !== '5088';
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(path.join(evidenceDir, `isolation-guard-${GATE ? 'on' : 'off'}-173.json`), JSON.stringify(guard, null, 2) + '\n');
console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL') + ' GATE=' + GATE);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) process.exit(3);
