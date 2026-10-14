#!/usr/bin/env node
'use strict';

/*
 * TB-SHIPPING-MVP-SAFETY-REPAIR-060 — proof runner for the offline scratch-target preflight.
 *
 * Runs `assert-scratch-target.js` against SYNTHETIC fixtures only and asserts the exact exit code for each
 * case. No database is contacted, no Host is started, no HTTP request is made and no real credentials or
 * appsettings file is used as an input.
 *
 * Exits 0 only when every expectation holds.
 */

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const here = __dirname;
const guard = path.join(here, 'assert-scratch-target.js');
const fixtures = path.join(here, 'fixtures');
const baseConfig = path.join(fixtures, 'base.appsettings.json');
const ambiguousConfig = path.join(fixtures, 'ambiguous-target.appsettings.json');
const sentinel = 'abcd1234';
const scratchDatabase = `tooba_060_scratch_${sentinel}`;

const cases = [
  {
    name: 'synthetic-059-faulty-underscore-override',
    expect: 5,
    expectDetail: 'shadowed_override_key=Tooba__PostgreSQL__ConnectionReferences__tenant_alpha',
    args: ['--config', baseConfig, '--env', path.join(fixtures, '059-faulty.env.txt'),
      '--tenant', 'store-alpha', '--host', '127.0.0.1', '--port', '55432',
      '--database', scratchDatabase, '--sentinel', sentinel],
  },
  {
    name: 'synthetic-safe-scratch-target',
    expect: 0,
    expectDetail: `DATABASE=${scratchDatabase}`,
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'safe-scratch.env.txt'),
      '--tenant', 'store-alpha', '--host', '127.0.0.1', '--port', '55432',
      '--database', scratchDatabase, '--sentinel', sentinel],
  },
  {
    name: 'synthetic-production-like-target',
    expect: 7,
    expectDetail: 'resolved_database_is_a_known_live_database',
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'production-like.env.txt'),
      '--tenant', 'store-alpha', '--host', 'db.prod.internal', '--port', '5432',
      '--database', 'tooba_prod', '--sentinel', sentinel],
  },
  {
    name: 'synthetic-missing-override-leaves-config-target',
    expect: 7,
    expectDetail: 'live',
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'missing.env.txt'),
      '--tenant', 'store-alpha', '--host', '10.255.255.9', '--port', '5432',
      '--database', 'tooba_alpha', '--sentinel', sentinel],
  },
  {
    name: 'synthetic-unused-override-never-referenced',
    expect: 5,
    expectDetail: 'shadowed_override_reason=not_referenced_by_any_tenant',
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'unused-override.env.txt'),
      '--tenant', 'store-alpha', '--host', '127.0.0.1', '--port', '55432',
      '--database', scratchDatabase, '--sentinel', sentinel],
  },
  {
    name: 'synthetic-unknown-tenant',
    expect: 3,
    expectDetail: 'tenant_not_found',
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'safe-scratch.env.txt'),
      '--tenant', 'store-missing', '--host', '127.0.0.1', '--port', '55432',
      '--database', scratchDatabase, '--sentinel', sentinel],
  },
  {
    name: 'synthetic-ambiguous-tenant-declaration',
    expect: 4,
    expectDetail: 'tenant_declared_more_than_once',
    args: ['--config', ambiguousConfig, '--env', path.join(fixtures, 'missing.env.txt'),
      '--tenant', 'store-alpha', '--host', '10.255.255.9', '--port', '5432',
      '--database', 'tooba_alpha', '--sentinel', sentinel],
  },
  {
    name: 'synthetic-sentinel-not-unique-enough',
    expect: 2,
    expectDetail: 'sentinel_not_unique_enough',
    args: ['--config', baseConfig, '--env', path.join(fixtures, 'safe-scratch.env.txt'),
      '--tenant', 'store-alpha', '--host', '127.0.0.1', '--port', '55432',
      '--database', scratchDatabase, '--sentinel', 'short'],
  },
];

function run(args) {
  return spawnSync(process.execPath, [guard, ...args], { encoding: 'utf8' });
}

const failures = [];
const rows = [];

function record(name, expected, actual, ok) {
  rows.push(`| ${name} | ${expected} | ${actual} | ${ok ? 'OK' : 'MISMATCH'} |`);
  if (!ok) {
    failures.push(`${name}: expected exit ${expected}, got ${actual}`);
  }
  process.stdout.write(`case=${name} expected=${expected} actual=${actual} ${ok ? 'OK' : 'MISMATCH'}\n`);
}

for (const testCase of cases) {
  const result = run(testCase.args);
  const combined = `${result.stdout || ''}${result.stderr || ''}`;
  if (testCase.expectDetail && !combined.includes(testCase.expectDetail)) {
    failures.push(`${testCase.name}: output did not contain "${testCase.expectDetail}"`);
  }
  record(testCase.name, testCase.expect, result.status, result.status === testCase.expect);
}

// The fingerprint is the only machine-checkable link between "what was checked" and "what runs".
const safeCase = cases.find((entry) => entry.name === 'synthetic-safe-scratch-target');
const safeResult = run(safeCase.args);
const fingerprintLine = (safeResult.stdout || '').split(/\r?\n/).find((row) => row.startsWith('FINGERPRINT='));
if (!fingerprintLine) {
  failures.push('safe case did not emit a FINGERPRINT line');
} else {
  const fingerprint = fingerprintLine.slice('FINGERPRINT='.length).trim();

  const agreed = run([...safeCase.args, '--expect-fingerprint', fingerprint]);
  record('synthetic-fingerprint-agrees', 0, agreed.status, agreed.status === 0);

  const differed = run([...safeCase.args, '--expect-fingerprint', 'f'.repeat(64)]);
  record('synthetic-fingerprint-differs', 6, differed.status, differed.status === 6);
}

// The preflight must stay offline: prove by source inspection as well as by behaviour.
const guardSource = fs.readFileSync(guard, 'utf8');
const forbiddenInSource = ['require(\'pg\')', 'require("pg")', 'Npgsql', 'new Client(', 'new Pool(',
  'psql', '5432?', 'fetch(', 'http.request', 'net.connect', 'child_process'];
const foundForbidden = forbiddenInSource.filter((token) => guardSource.includes(token));
record('static-guard-source-has-no-database-or-network-client', 'none', foundForbidden.length === 0 ? 'none' : foundForbidden.join(','), foundForbidden.length === 0);

// The guard must read its overrides from an explicit file, never from the real process environment.
const readsRealEnvironment = /process\.env\s*\[/.test(guardSource) || /process\.env\.[A-Za-z_]/.test(guardSource);
record('static-guard-source-never-reads-process-env', 'false', String(readsRealEnvironment), !readsRealEnvironment);

process.stdout.write('\n| case | expected | actual | verdict |\n| --- | --- | --- | --- |\n');
process.stdout.write(rows.join('\n') + '\n');

if (failures.length > 0) {
  process.stdout.write(`\nFIXTURE_PROOFS=FAIL\n`);
  for (const failure of failures) {
    process.stdout.write(`FAILURE=${failure}\n`);
  }
  process.exit(1);
}

process.stdout.write('\nFIXTURE_PROOFS=PASS\n');
process.exit(0);
