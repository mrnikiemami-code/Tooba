/**
 * TB-SHIPPING-ONE-LEVEL-TABS-099 — offline isolation guard for the task-owned isolated Host.
 *
 * Proves, BEFORE any request is sent and without contacting any database, that the effective launch
 * configuration points exclusively at the task-owned scratch targets and cannot reach `tooba_alpha`
 * (the real Development database) or any other real Tooba database.
 *
 * Read-only. Prints no credentials. Exits non-zero on any failed assertion.
 */
const fs = require('fs');

function fail(msg) {
    console.error(`ASSERT_FAIL: ${msg}`);
    process.exit(2);
}

function arg(name) {
    const i = process.argv.indexOf(`--${name}`);
    return i >= 0 ? process.argv[i + 1] : undefined;
}

const configPath = arg('config');
const envPath = arg('env');
const expectDeployment = arg('deployment');
const expectTenant = arg('tenant');
const expectPort = arg('port');
const expectDb = arg('database');
const expectMessagingDb = arg('messagingDatabase');
const sentinel = arg('sentinel');

for (const [name, value] of Object.entries({
    configPath, envPath, expectDeployment, expectTenant, expectPort, expectDb, expectMessagingDb, sentinel,
})) {
    if (!value) fail(`missing required argument ${name}`);
}

const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
const envText = fs.readFileSync(envPath, 'utf8');

const tooba = config.Tooba || {};
if (tooba.Edition !== 'SingleStore') fail(`edition must be SingleStore, got ${tooba.Edition}`);
if (tooba.DeploymentId !== expectDeployment) fail(`deployment must be ${expectDeployment}, got ${tooba.DeploymentId}`);

const tenants = (tooba.SingleStore && tooba.SingleStore.Tenants) || [];
if (tenants.length !== 1) fail(`effective config must declare exactly 1 tenant, got ${tenants.length}`);
if (tenants[0].TenantId !== expectTenant) fail(`tenant must be ${expectTenant}, got ${tenants[0].TenantId}`);
if (tenants[0].Status !== 'Active') fail(`tenant must be Active`);

const refs = (tooba.PostgreSQL && tooba.PostgreSQL.ConnectionReferences) || {};
const refKeys = Object.keys(refs);
if (refKeys.length !== 2) fail(`effective config must declare exactly 2 references, got ${refKeys.length}`);
if (!refKeys.includes(tenants[0].ConnectionReference)) fail('tenant reference is missing');
if (tooba.Marketplace && tooba.Marketplace.ConnectionReference) fail('marketplace reference must be empty');

const forbidden = ['tooba_alpha', 'tooba_disabled', 'tooba_marketplace'];
const allText = JSON.stringify(config) + envText;
for (const bad of forbidden) {
    if (allText.includes(bad)) fail(`forbidden database name present in effective configuration: ${bad}`);
}

function parseConnectionString(cs) {
    const parts = {};
    for (const segment of cs.split(';')) {
        const idx = segment.indexOf('=');
        if (idx > 0) parts[segment.slice(0, idx).trim().toLowerCase()] = segment.slice(idx + 1).trim();
    }
    return parts;
}

const databases = [];
for (const key of refKeys) {
    const parts = parseConnectionString(refs[key] || '');
    if (parts.host !== '127.0.0.1') fail(`reference ${key} host must be loopback, got ${parts.host}`);
    if (parts.port !== expectPort) fail(`reference ${key} port must be ${expectPort}, got ${parts.port}`);
    if (parts.username !== 'scratch099') fail(`reference ${key} user must be the scratch user`);
    databases.push(parts.database);
    const app = parts['application name'] || '';
    if (!app.startsWith('tooba-099-isolated')) fail(`reference ${key} application name must be task-owned, got ${app}`);
}

if (!databases.includes(expectDb)) fail(`scratch database ${expectDb} is not referenced`);
if (!databases.includes(expectMessagingDb)) fail(`scratch messaging database ${expectMessagingDb} is not referenced`);

const cs = refs[tenants[0].ConnectionReference];
if (!cs.includes(sentinel)) fail(`tenant connection string must carry the sentinel ${sentinel}`);

if (!envText.includes('ASPNETCORE_ENVIRONMENT=Development')) fail('launch env must be Development');
if (!envText.includes('ASPNETCORE_URLS=http://127.0.0.1:5099')) fail('launch env URL must be the isolated port');
if (/Password=/.test(envText)) fail('launch env file must not contain a credential');

console.log('ISOLATION_GUARD=PASS');
console.log(`DEPLOYMENT=${tooba.DeploymentId}`);
console.log(`TENANT=${tenants[0].TenantId}`);
console.log(`REFERENCES=${refKeys.length}`);
console.log(`DATABASES=${databases.sort().join(',')}`);
console.log('TOOBA_ALPHA_REACHABLE=false');
console.log('CREDENTIALS_PRINTED=false');
