#!/usr/bin/env node
'use strict';

/*
 * TB-SHIPPING-MVP-SAFETY-REPAIR-060 — fail-closed OFFLINE scratch-target preflight.
 *
 * What happened (TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059): an isolated test Host was supposed to point at a
 * disposable clone, but the override key used an underscore (`...ConnectionReferences__tenant_alpha`)
 * while the configuration key is hyphenated (`tenant-alpha`). The override never matched, the Host silently
 * resolved the real Development connection string from appsettings.Development.json and wrote to
 * `tooba_alpha`.
 *
 * This script exists so that mistake cannot repeat silently. It resolves the EFFECTIVE configuration for a
 * target tenant from (a) an explicit config file and (b) an explicit environment-override file, then
 * refuses to pass unless the resolved target is exactly the expected scratch database:
 *
 *   - the tenant must exist exactly once;
 *   - every `Tooba__*` override that touches the connection-reference map must actually be the reference the
 *     target tenant resolves, otherwise it is reported as a shadowed/unused override (the 059 signature);
 *   - the resolved host/port/database must match the expected values exactly;
 *   - the database must be a unique, per-run, `tooba_<nnn>_scratch_<sentinel>` name and must not be any known
 *     live database, nor another tenant's database.
 *
 * It NEVER opens a database connection, never starts a Host, never reads appsettings, never reads the real
 * process environment, and never prints credentials.
 *
 * TRUST BOUNDARY (read before relying on a PASS):
 *   1. This checks configuration *as supplied to it*. It is a preflight, not a runtime guarantee.
 *   2. A PASS is only meaningful if the process that is started afterwards is launched in a context that
 *      cannot override these values again. Anything that can mutate the environment (parent shell, launch
 *      profile, user-secrets, a later `appsettings.*.json`, a wrapper script) or the config file AFTER this
 *      check invalidates the result. Prohibit that: clear `Tooba__*` / `ASPNETCORE__*` from the launching
 *      environment, pass the same config file, and do not let any later step re-set them.
 *   3. The emitted FINGERPRINT is the only machine-checkable link between "what was checked" and "what runs".
 *      A launching script MUST pass `--expect-fingerprint` with the value printed here, so a different
 *      environment cannot be substituted between the check and the launch.
 *   4. This script cannot prove anything about the database's contents, and it cannot detect misrouting that
 *      happens inside a process after it starts. It does not fix every possible misrouting path.
 *
 * Exit codes: 0 pass, 2 input, 3 unknown target, 4 ambiguous, 5 env shadowing/unused override,
 *             6 target mismatch, 7 live/foreign database, 8 unparseable connection data.
 */

const fs = require('fs');
const crypto = require('crypto');

const EXIT = {
  PASS: 0,
  INPUT: 2,
  UNKNOWN_TARGET: 3,
  AMBIGUOUS: 4,
  ENV_SHADOW: 5,
  MISMATCH: 6,
  LIVE_DATABASE: 7,
  UNPARSEABLE: 8,
};

const LIVE_DATABASES = [
  'tooba',
  'tooba_alpha',
  'tooba_disabled',
  'tooba_marketplace',
  'tooba_messaging',
  'postgres',
  'template0',
  'template1',
];

const LIVE_DATABASE_PATTERNS = [
  /^tooba_(prod|production|staging|stage|live|main|master|dev|develop|shared)/i,
];

const SCRATCH_DATABASE_PATTERN = /^tooba_\d{3}_scratch[a-z0-9_]*$/;
const SENTINEL_PATTERN = /^[a-z0-9]{8,32}$/;

const report = [];

function line(text) {
  report.push(text);
  process.stdout.write(text + '\n');
}

function fail(code, reason, details) {
  line('');
  line(`RESULT=FAIL`);
  line(`EXIT_CODE=${code}`);
  line(`REASON=${reason}`);
  for (const detail of details || []) {
    line(`DETAIL=${detail}`);
  }
  line('TRUST_BOUNDARY=configuration-only preflight; see script header for the full boundary');
  process.exit(code);
}

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const token = argv[i];
    if (!token.startsWith('--')) {
      fail(EXIT.INPUT, 'unexpected_argument', [`argument=${token}`]);
    }
    const eq = token.indexOf('=');
    if (eq > 2) {
      out[token.slice(2, eq)] = token.slice(eq + 1);
      continue;
    }
    const value = argv[i + 1];
    if (value === undefined || value.startsWith('--')) {
      fail(EXIT.INPUT, 'missing_argument_value', [`argument=${token}`]);
    }
    out[token.slice(2)] = value;
    i++;
  }
  return out;
}

function readRequiredFile(label, filePath) {
  if (!filePath) {
    fail(EXIT.INPUT, 'missing_argument', [`argument=${label}`]);
  }
  if (!fs.existsSync(filePath)) {
    fail(EXIT.INPUT, 'input_not_found', [`argument=${label}`, `path=${filePath}`]);
  }
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch (error) {
    fail(EXIT.INPUT, 'input_unreadable', [`argument=${label}`, `path=${filePath}`, `error=${error.code}`]);
  }
}

function deepClone(value) {
  if (value === null || typeof value !== 'object') {
    return value;
  }
  if (Array.isArray(value)) {
    return value.map(deepClone);
  }
  const copy = {};
  for (const key of Object.keys(value)) {
    copy[key] = deepClone(value[key]);
  }
  return copy;
}

/** Configuration keys are case-insensitive in Microsoft.Extensions.Configuration. */
function findKey(node, segment) {
  if (!node || typeof node !== 'object' || Array.isArray(node)) {
    return undefined;
  }
  const segments = Object.keys(node);
  for (const key of segments) {
    if (key.toLowerCase() === segment.toLowerCase()) {
      return key;
    }
  }
  return undefined;
}

function getPath(root, segments) {
  let node = root;
  for (const segment of segments) {
    if (node === null || typeof node !== 'object') {
      return undefined;
    }
    if (Array.isArray(node)) {
      const index = Number(segment);
      if (!Number.isInteger(index) || index < 0) {
        return undefined;
      }
      node = node[index];
      continue;
    }
    const key = findKey(node, segment);
    if (key === undefined) {
      return undefined;
    }
    node = node[key];
  }
  return node;
}

function setPath(root, segments, value) {
  let node = root;
  for (let i = 0; i < segments.length - 1; i++) {
    const segment = segments[i];
    if (node === null || typeof node !== 'object') {
      node = {};
    }
    if (Array.isArray(node)) {
      const index = Number(segment);
      if (!Number.isInteger(index) || index < 0) {
        return false;
      }
      if (node[index] === undefined || node[index] === null) {
        node[index] = {};
      }
      node = node[index];
      continue;
    }
    let key = findKey(node, segment);
    if (key === undefined) {
      key = segment;
      node[key] = {};
    }
    node = node[key];
  }

  const last = segments[segments.length - 1];
  if (Array.isArray(node)) {
    const index = Number(last);
    if (!Number.isInteger(index) || index < 0) {
      return false;
    }
    node[index] = value;
    return true;
  }
  if (node === null || typeof node !== 'object') {
    return false;
  }
  let lastKey = findKey(node, last);
  if (lastKey === undefined) {
    lastKey = last;
  }
  node[lastKey] = value;
  return true;
}

function readEnvOverrideFile(filePath) {
  const overrides = [];
  if (!filePath) {
    return overrides;
  }
  const text = readRequiredFile('--env', filePath);
  const lines = text.split(/\r?\n/);
  for (const rawLine of lines) {
    const trimmed = rawLine.trim();
    if (trimmed.length === 0 || trimmed.startsWith('#')) {
      continue;
    }
    const eq = trimmed.indexOf('=');
    if (eq <= 0) {
      fail(EXIT.INPUT, 'env_line_malformed', [`file=${filePath}`, `line=${trimmed.slice(0, 40)}`]);
    }
    overrides.push({ key: trimmed.slice(0, eq).trim(), value: trimmed.slice(eq + 1) });
  }
  return overrides;
}

function normalizeReferenceName(name) {
  return String(name).toLowerCase().replace(/[-_]/g, '');
}

function parseConnectionString(connectionString) {
  const parts = {};
  for (const chunk of String(connectionString).split(';')) {
    const trimmed = chunk.trim();
    if (trimmed.length === 0) {
      continue;
    }
    const eq = trimmed.indexOf('=');
    if (eq <= 0) {
      fail(EXIT.UNPARSEABLE, 'connection_string_segment_malformed');
    }
    parts[trimmed.slice(0, eq).trim().toLowerCase()] = trimmed.slice(eq + 1).trim();
  }

  const host = parts['host'] || parts['server'] || parts['datasource'] || parts['data source'];
  const port = parts['port'] || '5432';
  const database = parts['database'] || parts['initial catalog'];
  if (!host || !database || !/^\d+$/.test(port)) {
    fail(EXIT.UNPARSEABLE, 'connection_string_incomplete', [
      `host_present=${Boolean(host)}`,
      `port_numeric=${/^\d+$/.test(port)}`,
      `database_present=${Boolean(database)}`,
    ]);
  }

  return {
    host: host.toLowerCase(),
    port,
    database: database.toLowerCase(),
    usernamePresent: Boolean(parts['username'] || parts['user id']),
    passwordPresent: Boolean(parts['password']),
  };
}

function main() {
  const args = parseArgs(process.argv.slice(2));

  const tenantId = args['tenant'];
  const expectedHost = args['host'];
  const expectedPort = args['port'];
  const expectedDatabase = args['database'];
  const sentinel = args['sentinel'];
  if (!tenantId || !expectedHost || !expectedPort || !expectedDatabase || !sentinel) {
    fail(EXIT.INPUT, 'missing_required_argument', [
      'required=--config --tenant --host --port --database --sentinel',
    ]);
  }
  if (!/^\d+$/.test(expectedPort)) {
    fail(EXIT.INPUT, 'expected_port_not_numeric');
  }
  if (!SENTINEL_PATTERN.test(sentinel)) {
    fail(EXIT.INPUT, 'sentinel_not_unique_enough', ['rule=^[a-z0-9]{8,32}$']);
  }

  const configText = readRequiredFile('--config', args['config']);
  let baseConfig;
  try {
    baseConfig = JSON.parse(configText);
  } catch (error) {
    fail(EXIT.INPUT, 'config_not_json', [`error=${error.message.split('\n')[0]}`]);
  }
  if (baseConfig === null || typeof baseConfig !== 'object') {
    fail(EXIT.INPUT, 'config_root_not_object');
  }

  const overrides = readEnvOverrideFile(args['env']).filter((entry) =>
    /^tooba__/i.test(entry.key));

  const resolved = deepClone(baseConfig);
  const applied = [];
  const sorted = overrides.slice().sort((a, b) => (a.key < b.key ? -1 : a.key > b.key ? 1 : 0));
  for (const entry of sorted) {
    const segments = entry.key.split('__');
    segments[0] = 'Tooba';
    if (!setPath(resolved, segments, entry.value)) {
      fail(EXIT.INPUT, 'override_path_not_settable', [`key=${entry.key}`]);
    }
    applied.push({ key: entry.key, segments });
  }

  const tenantsPath = ['Tooba', 'SingleStore', 'Tenants'];
  const tenants = getPath(resolved, tenantsPath);
  if (!Array.isArray(tenants)) {
    fail(EXIT.INPUT, 'tenant_registry_missing', [`path=${tenantsPath.join(':')}`]);
  }

  const matches = [];
  for (let index = 0; index < tenants.length; index++) {
    const entry = tenants[index];
    if (entry && typeof entry === 'object' && entry.TenantId === tenantId) {
      matches.push({ index, entry });
    }
  }
  if (matches.length === 0) {
    fail(EXIT.UNKNOWN_TARGET, 'tenant_not_found', [`tenant=${tenantId}`]);
  }
  if (matches.length > 1) {
    fail(EXIT.AMBIGUOUS, 'tenant_declared_more_than_once', [
      `tenant=${tenantId}`,
      `indexes=${matches.map((m) => m.index).join(',')}`,
    ]);
  }

  const targetReference = matches[0].entry.ConnectionReference;
  if (!targetReference || typeof targetReference !== 'string') {
    fail(EXIT.UNKNOWN_TARGET, 'tenant_connection_reference_missing', [`tenant=${tenantId}`]);
  }

  const referencesPath = ['Tooba', 'PostgreSQL', 'ConnectionReferences'];
  const references = getPath(resolved, referencesPath);
  if (!references || typeof references !== 'object' || Array.isArray(references)) {
    fail(EXIT.INPUT, 'connection_reference_map_missing', [`path=${referencesPath.join(':')}`]);
  }

  const referenceKeys = Object.keys(references);
  const referencedByTenants = new Set();
  for (const entry of tenants) {
    if (entry && typeof entry === 'object' && typeof entry.ConnectionReference === 'string') {
      referencedByTenants.add(entry.ConnectionReference.toLowerCase());
    }
  }

  // The 059 signature: an override under the connection-reference map that never became effective.
  const shadowedReferences = new Map();
  for (const entry of applied) {
    if (entry.segments.length !== 4) {
      continue;
    }
    if (entry.segments[1].toLowerCase() !== 'postgresql'
      || entry.segments[2].toLowerCase() !== 'connectionreferences') {
      continue;
    }
    const referenceName = entry.segments[3];
    if (referenceName === targetReference) {
      continue;
    }
    if (normalizeReferenceName(referenceName) === normalizeReferenceName(targetReference)) {
      shadowedReferences.set(referenceName, 'normalizes_to_the_target_reference_name');
      continue;
    }
    if (!referencedByTenants.has(referenceName.toLowerCase())) {
      shadowedReferences.set(referenceName, 'not_referenced_by_any_tenant');
    }
  }
  if (shadowedReferences.size > 0) {
    const details = [`tenant=${tenantId}`, `effective_reference=${targetReference}`];
    for (const [name, why] of shadowedReferences) {
      details.push(`shadowed_override_key=Tooba__PostgreSQL__ConnectionReferences__${name}`);
      details.push(`shadowed_override_reason=${why}`);
    }
    fail(EXIT.ENV_SHADOW, 'override_did_not_apply_to_the_target_tenant', details);
  }

  // Two override keys that mean the same reference to a human but not to the configuration system.
  const seenNormalized = new Map();
  for (const key of referenceKeys) {
    const normalized = normalizeReferenceName(key);
    if (seenNormalized.has(normalized)) {
      fail(EXIT.AMBIGUOUS, 'connection_reference_names_are_ambiguous', [
        `first=${seenNormalized.get(normalized)}`,
        `second=${key}`,
      ]);
    }
    seenNormalized.set(normalized, key);
  }

  const resolvedReferenceKey = referenceKeys.find(
    (key) => key.toLowerCase() === targetReference.toLowerCase());
  if (resolvedReferenceKey === undefined) {
    fail(EXIT.UNKNOWN_TARGET, 'connection_reference_not_declared', [
      `tenant=${tenantId}`,
      `reference=${targetReference}`,
    ]);
  }
  const connectionString = references[resolvedReferenceKey];
  if (typeof connectionString !== 'string' || connectionString.trim().length === 0) {
    fail(EXIT.INPUT, 'connection_reference_empty', [`reference=${targetReference}`]);
  }

  const target = parseConnectionString(connectionString);

  const mismatch = [];
  if (target.host !== expectedHost.toLowerCase()) {
    mismatch.push(`host expected=${expectedHost} resolved=${target.host}`);
  }
  if (target.port !== expectedPort) {
    mismatch.push(`port expected=${expectedPort} resolved=${target.port}`);
  }
  if (target.database !== expectedDatabase.toLowerCase()) {
    mismatch.push(`database expected=${expectedDatabase} resolved=${target.database}`);
  }
  if (mismatch.length > 0) {
    fail(EXIT.MISMATCH, 'resolved_target_differs_from_expected', mismatch);
  }

  if (LIVE_DATABASES.includes(target.database)
    || LIVE_DATABASE_PATTERNS.some((pattern) => pattern.test(target.database))) {
    fail(EXIT.LIVE_DATABASE, 'resolved_database_is_a_known_live_database', [
      `database=${target.database}`,
    ]);
  }
  if (!SCRATCH_DATABASE_PATTERN.test(target.database)) {
    fail(EXIT.LIVE_DATABASE, 'resolved_database_is_not_an_exact_scratch_name', [
      `database=${target.database}`,
      `required_pattern=${SCRATCH_DATABASE_PATTERN.source}`,
    ]);
  }
  if (!target.database.endsWith(`_${sentinel}`)) {
    fail(EXIT.LIVE_DATABASE, 'resolved_database_does_not_carry_the_run_sentinel', [
      `database=${target.database}`,
      `required_suffix=_${sentinel}`,
    ]);
  }

  for (const key of referenceKeys) {
    if (key.toLowerCase() === targetReference.toLowerCase()) {
      continue;
    }
    const other = parseConnectionString(references[key]);
    if (other.database === target.database) {
      fail(EXIT.LIVE_DATABASE, 'resolved_database_is_shared_with_another_reference', [
        `database=${target.database}`,
        `other_reference=${key}`,
      ]);
    }
    if (other.host === target.host && other.port === target.port && other.database === target.database) {
      fail(EXIT.AMBIGUOUS, 'two_references_resolve_to_the_same_target', [
        `other_reference=${key}`,
      ]);
    }
  }

  const fingerprint = crypto
    .createHash('sha256')
    .update(JSON.stringify({
      config: crypto.createHash('sha256').update(configText).digest('hex'),
      overrides: sorted.map((entry) => entry.key),
      tenant: tenantId,
      reference: resolvedReferenceKey,
      host: target.host,
      port: target.port,
      database: target.database,
      sentinel,
    }))
    .digest('hex');

  if (args['expect-fingerprint'] && args['expect-fingerprint'] !== fingerprint) {
    fail(EXIT.MISMATCH, 'fingerprint_does_not_match_the_expected_check', [
      `expected=${args['expect-fingerprint']}`,
      `actual=${fingerprint}`,
    ]);
  }

  line('RESULT=PASS');
  line(`EXIT_CODE=${EXIT.PASS}`);
  line(`TENANT=${tenantId}`);
  line(`CONNECTION_REFERENCE=${resolvedReferenceKey}`);
  line(`HOST=${target.host}`);
  line(`PORT=${target.port}`);
  line(`DATABASE=${target.database}`);
  line(`SENTINEL=${sentinel}`);
  line(`CONFIG_OVERRIDE_KEYS_APPLIED=${applied.length}`);
  line(`USERNAME_PRESENT=${target.usernamePresent}`);
  line(`PASSWORD_PRESENT=${target.passwordPresent}`);
  line(`CREDENTIALS_PRINTED=false`);
  line(`DATABASE_CONTACTED=false`);
  // TB-SHIPPING-MVP-ISOLATED-HTTP-061 addition to the copied 060 guard: emit the SHA-256 of the checked
  // config text so the launch step can prove the exact same bytes are the ones the process is launched with.
  line(`CONFIG_SHA256=${crypto.createHash('sha256').update(configText).digest('hex')}`);
  line(`FINGERPRINT=${fingerprint}`);
  line('TRUST_BOUNDARY=configuration-only preflight; the launched process must not be able to override');
  line('TRUST_BOUNDARY=these values again, and must receive --expect-fingerprint with the value above');

  if (args['report']) {
    fs.writeFileSync(args['report'], report.join('\n') + '\n', 'utf8');
  }

  process.exit(EXIT.PASS);
}

main();
