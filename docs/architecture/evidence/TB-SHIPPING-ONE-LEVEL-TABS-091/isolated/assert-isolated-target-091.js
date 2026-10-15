/**
 * TB-SHIPPING-ONE-LEVEL-TABS-091 — offline isolation guard for the task-owned isolated Host.
 *
 * Purpose: prove, BEFORE any request is sent and without contacting any database, that the effective
 * launch configuration used by the isolated Host points exclusively at the task-owned scratch target
 * and cannot reach `tooba_alpha` (the real Development database) or any other real Tooba database.
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
const expectTenant = arg('tenant');
const expectHost = arg('host');
const expectPort = arg('port');
const expectDb = arg('database');
const sentinel = arg('sentinel');

if (!configPath || !envPath || !expectTenant || !expectHost || !expectPort || !expectDb || !sentinel) {
    fail('missing required arguments');
}

const configBytes = fs.readFileSync(configPath);
const config = JSON.parse(configBytes.toString('utf8'));
const envText = fs.readFileSync(envPath, 'utf8');

const tooba = config.Tooba || {};
const edition = tooba.Edition;
const tenants = (tooba.SingleStore && tooba.SingleStore.Tenants) || [];
const refs = (tooba.PostgreSQL && tooba.PostgreSQL.ConnectionReferences) || {};

if (edition !== 'SingleStore') fail(`edition must be SingleStore, got ${edition}`);
if (tenants.length !== 1) fail(`effective config must declare exactly 1 tenant, got ${tenants.length}`);

const tenant = tenants[0];
if (tenant.TenantId !== expectTenant) fail(`tenant must be ${expectTenant}, got ${tenant.TenantId}`);

const reference = tenant.ConnectionReference;
const refKeys = Object.keys(refs);
if (refKeys.length !== 1) fail(`effective config must declare exactly 1 reference, got ${refKeys.length}`);
if (refKeys[0] !== reference) fail(`the only reference must be the tenant's own, got ${refKeys[0]}`);

const cs = refs[reference] || '';
if (!cs) fail('the tenant reference has no connection string');

const parts = {};
for (const segment of cs.split(';')) {
    const idx = segment.indexOf('=');
    if (idx > 0) parts[segment.slice(0, idx).trim().toLowerCase()] = segment.slice(idx + 1).trim();
}

if ((parts.host || '').toLowerCase() !== expectHost.toLowerCase()) fail(`connection host must be ${expectHost}`);
if (parts.port !== String(expectPort)) fail(`connection port must be ${expectPort}, got ${parts.port}`);
if (parts.database !== expectDb) fail(`connection database must be ${expectDb}, got ${parts.database}`);
if (!(parts.database || '').includes(sentinel)) fail(`connection database must contain sentinel ${sentinel}`);
if (String(expectPort) === '5432') fail('the isolated target must not use the shared 5432 port');

// The launch environment must not re-point any Tooba__ override at another target.
const overrideLines = envText.split(/\r?\n/).filter((l) => l.startsWith('Tooba__'));
for (const line of overrideLines) {
    if (/tooba_alpha/i.test(line)) fail('launch environment still mentions tooba_alpha');
    if (/Port=5432/i.test(line)) fail('launch environment still points at port 5432');
}
if (!envText.includes('ASPNETCORE_ENVIRONMENT=Development')) fail('ASPNETCORE_ENVIRONMENT=Development is required');
if (!envText.includes(`Tooba__PostgreSQL__ConnectionReferences__${reference}=`)) {
    fail('the launch environment must carry the isolated reference override');
}

const printed = JSON.stringify(config);
if (/tooba_alpha/i.test(printed)) fail('effective config still mentions tooba_alpha');

const fingerprintSource =
    `edition=${edition}|tenant=${tenant.TenantId}|reference=${reference}|host=${parts.host}|port=${parts.port}|` +
    `database=${parts.database}|sentinel=${sentinel}`;
const crypto = require('crypto');
const fingerprint = crypto.createHash('sha256').update(fingerprintSource).digest('hex');
const configSha = crypto.createHash('sha256').update(configBytes).digest('hex');

console.log('RESULT=PASS');
console.log('EXIT_CODE=0');
console.log(`EDITION=${edition}`);
console.log(`TENANT=${tenant.TenantId}`);
console.log(`CONNECTION_REFERENCE=${reference}`);
console.log(`HOST=${parts.host}`);
console.log(`PORT=${parts.port}`);
console.log(`DATABASE=${parts.database}`);
console.log(`SENTINEL=${sentinel}`);
console.log(`EFFECTIVE_TENANTS=${tenants.length}`);
console.log(`EFFECTIVE_REFERENCES=${refKeys.length}`);
console.log(`CONFIG_OVERRIDE_KEYS_APPLIED=${overrideLines.length}`);
console.log('CREDENTIALS_PRINTED=false');
console.log('DATABASE_CONTACTED=false');
console.log(`FINGERPRINT=${fingerprint}`);
console.log(`CONFIG_SHA256=${configSha}`);
