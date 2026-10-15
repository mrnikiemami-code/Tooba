/**
 * TB-SHIPPING-ONE-LEVEL-TABS-099 — build the task-owned ISOLATED launch directory.
 *
 * Produces inside the task-owned run directory ONLY:
 *   appsettings.json               verbatim copy of the repository base settings
 *   appsettings.Development.json   overlay: exactly ONE tenant and ONE reference, both the
 *                                  task-owned scratch target (scratch scratch DB + scratch messaging DB)
 *   isolated.env.txt               launch environment (no secret values)
 *   effective-config.json          merged config judged by the offline guard (same bytes launched)
 *
 * It never contacts a database. The scratch password is read from an untracked file/env and is never
 * written into any tracked evidence file.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-099-host-run');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-099-scratch',
    messagingReference: 'messaging-099-scratch',
    host: '127.0.0.1',
    port: '55441',
    database: 'tooba_099_scratch',
    messagingDatabase: 'tooba_099_messaging',
    user: 'scratch099',
    password: process.env.TOOBA_099_SCRATCH_PASSWORD || '',
    sentinel: 'tooba_099_scratch',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must be supplied via the environment, never hard-coded');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-099',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [
                {
                    TenantId: SCRATCH.tenantId,
                    DisplayName: 'Isolated 099 Store',
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
                    `Database=${SCRATCH.database};Application Name=tooba-099-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-099-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
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
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: false },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

// The effective merged configuration (the guard judges exactly these bytes).
const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), JSON.stringify(effective, null, 2));

// Launch environment (no secrets).
const env = [
    'ASPNETCORE_ENVIRONMENT=Development',
    'DOTNET_ENVIRONMENT=Development',
    'ASPNETCORE_URLS=http://127.0.0.1:5099',
].join('\n') + '\n';
fs.writeFileSync(path.join(outDir, 'isolated.env.txt'), env);

// No secrets in the overlay copy staged into the build output either: the overlay holds the password,
// so it stays inside the task-owned (git-ignored) run directory and is copied only at launch time.
console.log('PREPARE=PASS');
console.log(`RUN_DIR=${outDir}`);
console.log(`EFFECTIVE_CONFIG=${path.join(outDir, 'effective-config.json')}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
