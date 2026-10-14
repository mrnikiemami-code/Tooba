#!/usr/bin/env node
'use strict';

/*
 * TB-SHIPPING-MVP-ISOLATED-HTTP-061 — build the EFFECTIVE isolated launch configuration and gate it with
 * the copied 060 preflight guard BEFORE any process is launched and before any HTTP request is made.
 *
 * The launch directory is a task-owned temporary directory containing the freshly built Host. This script:
 *   1. reads the generic `appsettings.json` that the build produced and REFUSES to continue if it already
 *      declares any tenant or any connection reference (so the isolation cannot be diluted by the base);
 *   2. writes a task-owned `appsettings.Development.json` overlay that declares exactly one Active tenant and
 *      exactly one connection reference, both pointing at the task-owned scratch container, using the
 *      canonical HYPHENATED reference key;
 *   3. computes the effective merged configuration exactly as the Host configuration system would
 *      (objects merged per key, arrays merged per index with the later source winning) and asserts that the
 *      result still contains exactly one tenant / one reference;
 *   4. asserts that NO effective connection string targets a live host/port/database (5432, tooba_alpha,
 *      tooba_disabled, tooba_marketplace, tooba_messaging, any tooba_<env> name);
 *   5. writes the environment override file that the launch will actually set, and runs the copied 060 guard
 *      against exactly (effective config, override file) so the resolved target, the sentinel and the
 *      fingerprint are proven before launch;
 *   6. writes redacted copies of all of the above into the evidence directory. The password is supplied
 *      through an environment variable and is never printed or written to evidence.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { spawnSync } = require('child_process');

const EXIT = { OK: 0, INPUT: 2, BASE_NOT_CLEAN: 3, MERGE_NOT_ISOLATED: 4, LIVE_TARGET: 5, GUARD_FAILED: 6 };

function arg(name) {
  const index = process.argv.indexOf(`--${name}`);
  if (index < 0 || index === process.argv.length - 1) {
    throw new Error(`missing --${name}`);
  }
  return process.argv[index + 1];
}

function sha256(text) {
  return crypto.createHash('sha256').update(text).digest('hex');
}

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

/** Microsoft.Extensions.Configuration semantics: objects merge per key; arrays merge per index. */
function mergeConfig(base, overlay) {
  if (Array.isArray(base) && Array.isArray(overlay)) {
    const length = Math.max(base.length, overlay.length);
    const merged = [];
    for (let i = 0; i < length; i++) {
      merged.push(i < overlay.length && overlay[i] !== undefined
        ? mergeConfig(base[i], overlay[i])
        : base[i]);
    }
    return merged;
  }
  if (overlay === undefined) {
    return base;
  }
  if (base === null || overlay === null || typeof base !== 'object' || typeof overlay !== 'object') {
    return overlay;
  }
  const merged = {};
  for (const key of Object.keys(base)) {
    merged[key] = base[key];
  }
  for (const key of Object.keys(overlay)) {
    merged[key] = key in base ? mergeConfig(base[key], overlay[key]) : overlay[key];
  }
  return merged;
}

function collectConnectionStrings(config) {
  const references = config?.Tooba?.PostgreSQL?.ConnectionReferences ?? {};
  return Object.entries(references).map(([reference, connectionString]) => ({ reference, connectionString }));
}

const LIVE_DATABASES = [
  'tooba', 'tooba_alpha', 'tooba_disabled', 'tooba_marketplace', 'tooba_messaging',
  'postgres', 'template0', 'template1',
];

function failing(exitCode, reason, details) {
  process.stdout.write(`RESULT=FAIL\nEXIT_CODE=${exitCode}\nREASON=${reason}\n`);
  for (const detail of details ?? []) {
    process.stdout.write(`DETAIL=${detail}\n`);
  }
  process.exit(exitCode);
}

function main() {
  const launchDir = arg('launch-dir');
  const evidenceDir = arg('evidence-dir');
  const sentinel = arg('sentinel');
  const database = arg('database');
  const port = arg('port');
  const tenantId = arg('tenant');
  const reference = arg('reference');
  const username = process.argv.includes('--username') ? arg('username') : 'scratch061';
  const password = process.env[arg('password-env')];
  if (!password) {
    failing(EXIT.INPUT, 'password_env_missing', [`env=${arg('password-env')}`]);
  }
  const runLegacyBootstraps = process.argv.includes('--legacy-bootstraps');

  const genericPath = path.join(launchDir, 'appsettings.json');
  if (!fs.existsSync(genericPath)) {
    failing(EXIT.INPUT, 'generic_appsettings_missing', [`path=${genericPath}`]);
  }
  const genericText = fs.readFileSync(genericPath, 'utf8');
  const generic = JSON.parse(genericText);

  // (1) the base the Host will load must not itself declare tenants or live references.
  const baseTenants = generic?.Tooba?.SingleStore?.Tenants ?? [];
  const baseReferences = Object.keys(generic?.Tooba?.PostgreSQL?.ConnectionReferences ?? {});
  if (!Array.isArray(baseTenants) || baseTenants.length !== 0 || baseReferences.length !== 0) {
    failing(EXIT.BASE_NOT_CLEAN, 'base_appsettings_is_not_isolation_clean', [
      `base_tenants=${Array.isArray(baseTenants) ? baseTenants.length : 'not-array'}`,
      `base_connection_references=${baseReferences.length}`,
    ]);
  }

  const connectionString = [
    'Host=127.0.0.1',
    `Port=${port}`,
    `Username=${username}`,
    `Password=${password}`,
    `Database=${database}`,
    'Application Name=tooba-061-isolated',
    'Timeout=15',
    'Command Timeout=120',
    'Maximum Pool Size=20',
  ].join(';');

  // (2) the task-owned overlay: exactly one Active tenant, exactly one reference, hyphenated keys only.
  const overlay = {
    Tooba: {
      Edition: 'SingleStore',
      DeploymentId: 'isolated-061',
      TrustedProxies: [],
      Marketplace: { ConnectionReference: '' },
      SingleStore: {
        Tenants: [
          {
            TenantId: tenantId,
            DisplayName: 'Isolated 061 Store',
            Status: 'Active',
            ConnectionReference: reference,
            ThemeReference: 'theme-alpha',
            DefaultMarketReference: 'IR',
            StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
            PrimaryDomain: 'alpha.localhost',
            Hosts: ['alpha.localhost', 'alpha.localtest.me', 'localhost', '127.0.0.1'],
          },
        ],
      },
      PostgreSQL: {
        ConnectionReferences: { [reference]: connectionString },
      },
      Messaging: {
        Enabled: false,
        Transport: 'PostgreSql',
        ConnectionReference: '',
        Schema: 'transport',
        UseInProcessTestDouble: false,
      },
      Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
      CatalogDemo: { AllowResetAndSeed: true, RunLegacyBootstraps: runLegacyBootstraps },
    },
  };

  const overlayText = JSON.stringify(overlay, null, 2) + '\n';
  fs.writeFileSync(path.join(launchDir, 'appsettings.Development.json'), overlayText, 'utf8');

  // (3) effective merged configuration, exactly as the configuration system would compute it.
  const effective = mergeConfig(generic, overlay);
  const effectiveText = JSON.stringify(effective, null, 2) + '\n';
  const effectivePath = path.join(launchDir, 'effective-config.json');
  fs.writeFileSync(effectivePath, effectiveText, 'utf8');

  const effectiveTenants = effective?.Tooba?.SingleStore?.Tenants ?? [];
  const effectiveReferences = collectConnectionStrings(effective);
  if (!Array.isArray(effectiveTenants) || effectiveTenants.length !== 1
    || effectiveTenants[0].TenantId !== tenantId
    || effectiveTenants[0].ConnectionReference !== reference) {
    failing(EXIT.MERGE_NOT_ISOLATED, 'effective_config_does_not_declare_exactly_the_one_expected_tenant', [
      `tenant_count=${Array.isArray(effectiveTenants) ? effectiveTenants.length : 'not-array'}`,
    ]);
  }
  if (effectiveReferences.length !== 1 || effectiveReferences[0].reference !== reference) {
    failing(EXIT.MERGE_NOT_ISOLATED, 'effective_config_does_not_declare_exactly_one_reference', [
      `reference_count=${effectiveReferences.length}`,
    ]);
  }

  // (4) no effective connection string may target a live host/port/database.
  for (const { reference: name, connectionString: value } of effectiveReferences) {
    const parts = {};
    for (const chunk of String(value).split(';')) {
      const eq = chunk.indexOf('=');
      if (eq > 0) {
        parts[chunk.slice(0, eq).trim().toLowerCase()] = chunk.slice(eq + 1).trim();
      }
    }
    const db = (parts['database'] ?? '').toLowerCase();
    const effectivePort = parts['port'] ?? '5432';
    const host = (parts['host'] ?? '').toLowerCase();
    if (effectivePort === '5432' || host === '127.0.0.1' && effectivePort === '5432') {
      failing(EXIT.LIVE_TARGET, 'effective_connection_string_targets_the_live_postgres_port', [`reference=${name}`]);
    }
    if (LIVE_DATABASES.includes(db) || /^tooba_(prod|production|staging|stage|live|main|master|dev|develop|shared)/.test(db)) {
      failing(EXIT.LIVE_TARGET, 'effective_connection_string_targets_a_live_database', [`reference=${name}`]);
    }
    if (db !== database.toLowerCase() || effectivePort !== port) {
      failing(EXIT.LIVE_TARGET, 'effective_connection_string_does_not_match_the_scratch_target', [`reference=${name}`]);
    }
    if (host !== '127.0.0.1') {
      failing(EXIT.LIVE_TARGET, 'effective_connection_string_is_not_loopback_only', [`reference=${name}`]);
    }
  }

  // (5) the environment override actually set at launch, then the guard over exactly (config, overrides).
  const overrideLines = [
    '# TB-SHIPPING-MVP-ISOLATED-HTTP-061 launch override. Hyphenated canonical key; never tenant_alpha.',
    `${'Tooba__PostgreSQL__ConnectionReferences__'}${reference}=${connectionString}`,
  ];
  const overrideText = overrideLines.join('\n') + '\n';
  const overridePath = path.join(launchDir, 'isolated.env.txt');
  fs.writeFileSync(overridePath, overrideText, 'utf8');

  const guardPath = path.join(evidenceDir, 'isolated', 'assert-scratch-target.js');
  const guardRun = spawnSync(process.execPath, [
    guardPath,
    '--config', effectivePath,
    '--env', overridePath,
    '--tenant', tenantId,
    '--host', '127.0.0.1',
    '--port', port,
    '--database', database,
    '--sentinel', sentinel,
  ], { encoding: 'utf8' });

  const guardOutput = `${guardRun.stdout ?? ''}${guardRun.stderr ?? ''}`;
  fs.writeFileSync(path.join(evidenceDir, 'log-guard-effective-target.txt'), guardOutput, 'utf8');
  if (guardRun.status !== 0) {
    process.stdout.write(guardOutput);
    failing(EXIT.GUARD_FAILED, 'preflight_guard_refused_the_effective_target_or_overrides', [
      `guard_exit=${guardRun.status}`,
    ]);
  }

  const fingerprint = (guardOutput.match(/^FINGERPRINT=(.*)$/m) ?? [, ''])[1].trim();
  const configSha = (guardOutput.match(/^CONFIG_SHA256=(.*)$/m) ?? [, ''])[1].trim();
  const recomputed = sha256(effectiveText);
  if (configSha && configSha !== recomputed) {
    failing(EXIT.GUARD_FAILED, 'guard_checked_a_different_config_than_the_effective_config', []);
  }

  // (6) redacted evidence copies. Never write the password into the repository.
  const redact = (text) => text.split(password).join('***REDACTED***');
  fs.mkdirSync(path.join(evidenceDir, 'isolated', 'launch-config'), { recursive: true });
  fs.writeFileSync(path.join(evidenceDir, 'isolated', 'launch-config', 'appsettings.Development.redacted.json'), redact(overlayText), 'utf8');
  fs.writeFileSync(path.join(evidenceDir, 'isolated', 'launch-config', 'effective-config.redacted.json'), redact(effectiveText), 'utf8');
  fs.writeFileSync(path.join(evidenceDir, 'isolated', 'launch-config', 'isolated.env.redacted.txt'), redact(overrideText), 'utf8');
  fs.writeFileSync(
    path.join(evidenceDir, 'isolated', 'launch-config', 'hashes.txt'),
    [
      `generic_appsettings_sha256=${sha256(genericText)}`,
      `overlay_sha256=${sha256(overlayText)}`,
      `effective_config_sha256=${sha256(effectiveText)}`,
      `effective_config_sha256_redacted_copy_is_not_comparable=true`,
      `override_file_sha256=${sha256(overrideText)}`,
      `guard_fingerprint=${fingerprint}`,
      `guard_file_sha256=${sha256(fs.readFileSync(guardPath, 'utf8'))}`,
      `tenant=${tenantId}`,
      `reference=${reference}`,
      `database=${database}`,
      `port=${port}`,
      `sentinel=${sentinel}`,
      `tenants_in_effective_config=1`,
      `references_in_effective_config=1`,
      `credentials_written_to_evidence=false`,
    ].join('\n') + '\n',
    'utf8');

  process.stdout.write('RESULT=PASS\nEXIT_CODE=0\n');
  process.stdout.write(`EFFECTIVE_TENANTS=1\nEFFECTIVE_REFERENCES=1\n`);
  process.stdout.write(`GUARD_EXIT=${guardRun.status}\n`);
  process.stdout.write(`FINGERPRINT=${fingerprint}\n`);
  process.stdout.write(`CONFIG_SHA256=${configSha}\n`);
  process.stdout.write(`CREDENTIALS_PRINTED=false\n`);
  process.exit(EXIT.OK);
}

main();
