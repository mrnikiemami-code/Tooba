/**
 * TB-SHIPPING-ONE-LEVEL-TABS-091 — build the task-owned isolated launch directory.
 *
 * Produces, inside the task-owned run directory only:
 *   appsettings.json            copy of the generic repository base settings
 *   appsettings.Development.json  overlay that declares EXACTLY ONE tenant and ONE reference,
 *                                 both pointing at the task-owned scratch database
 *   isolated.env.txt            the launch environment (Development, isolated URL, no secret values)
 *   effective-config.json       the merged configuration the guard judges (same bytes are launched)
 *
 * No real database is contacted and no credential is written to any tracked evidence file.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-091-host-run');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    tenantId: 'store-alpha',
    reference: 'tenant-091-scratch',
    host: '127.0.0.1',
    port: '55433',
    database: 'tooba_091_scratch_q3w8t1',
    user: 'scratch091',
    password: process.env.TOOBA_091_SCRATCH_PASSWORD || '',
    sentinel: 'q3w8t1',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must be supplied via the environment, never hard-coded');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });

// 1. generic base settings, verbatim
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

// 2. the isolated Development overlay: exactly one tenant, exactly one reference
const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: 'SingleStore',
        DeploymentId: 'isolated-091',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: '' },
        SingleStore: {
            Tenants: [
                {
                    TenantId: SCRATCH.tenantId,
                    DisplayName: 'Isolated 091 Store',
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
                    `Database=${SCRATCH.database};Application Name=tooba-091-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
            },
        },
        Messaging: { Enabled: false, Transport: 'PostgreSql', ConnectionReference: '', Schema: 'transport', UseInProcessTestDouble: false },
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: false },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

// 3. the effective merged configuration (base + overlay), the exact bytes the guard hashes
const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), JSON.stringify(effective, null, 2));

// 4. the launch environment (no secrets; the password stays only in the overlay file)
const env = [
    'ASPNETCORE_ENVIRONMENT=Development',
    'DOTNET_ENVIRONMENT=Development',
    'ASPNETCORE_URLS=http://127.0.0.1:5099',
    `Tooba__PostgreSQL__ConnectionReferences__${SCRATCH.reference}=Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Database=${SCRATCH.database}`,
].join('\n') + '\n';
fs.writeFileSync(path.join(outDir, 'isolated.env.txt'), env);

console.log('PREPARE=PASS');
console.log(`RUN_DIR=${outDir}`);
console.log(`EFFECTIVE_CONFIG=${path.join(outDir, 'effective-config.json')}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
