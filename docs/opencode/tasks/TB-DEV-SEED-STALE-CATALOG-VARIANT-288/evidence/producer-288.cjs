// TB-DEV-SEED-STALE-CATALOG-VARIANT-288 — evidence producer. Writes observed-288.json directly.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');

const EVID = __dirname;
const TASK = path.resolve(EVID, '..');
const REPO = path.resolve(EVID, '../../../../..');
const OUT = path.join(EVID, 'observed-288.json');
const selfSha = crypto.createHash('sha256').update(fs.readFileSync(__filename)).digest('hex');
const read = f => {
  try {
    const b = fs.readFileSync(path.join(EVID, f));
    let s;
    if (b.length >= 2 && b[0] === 0xFF && b[1] === 0xFE) s = b.toString('utf16le');
    else s = b.toString('utf8');
    return s.replace(/^\uFEFF/, '');
  } catch { return ''; }
};
const git = (...a) => { try { return execFileSync('git', ['-C', REPO, ...a], { encoding: 'utf8' }).trim(); } catch { return ''; } };
const probe = async u => { try { return (await fetch(u)).status; } catch { return 0; } };

(async () => {
  const statusLines = git('status', '--porcelain').split(/\r?\n/).map(s => s.trim()).filter(Boolean);
  const paths = statusLines.map(l => l.replace(/^[^ ]+\s+/, '').replace(/^".*?\/"?/, m => m.replace(/^"|"$/g, '')));
  const allowed = p => p.startsWith('src/backend/Modules/Offer/')
    || p.startsWith('openspec/changes/dev-seed-stale-catalog-variant-288/')
    || p.startsWith('docs/opencode/tasks/TB-DEV-SEED-STALE-CATALOG-VARIANT-288/');
  const preexistingScratch = ['docs/opencode/smoke/runs/', 'iso-runs/', 'measure-one.ps1', 'prompts-local/'];
  const preexisting = p => preexistingScratch.includes(p) || p.startsWith('docs/opencode/tasks/TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287/');
  const forbidden = paths.filter(p => !allowed(p) && !preexisting(p));
  const f287 = paths.filter(p => !preexisting(p) && /(CreationIntentFingerprint|AddOfferCreationIntent)/.test(p));

  const hostBuild = read('host-build.log');
  const testLog = read('focused-test.log');
  const openspec = read('openspec-validate.txt');
  const buildOk = /0 Error\(s\)/.test(hostBuild) && !/\b[1-9]\d* Error\(s\)/.test(hostBuild);
  const testsOk = /Passed!\s+- Failed:\s+0,\s+Passed:\s+[1-9]/.test(testLog);
  const openspecOk = /is valid/.test(openspec);

  const ports = { host5088: await probe('http://127.0.0.1:5088/health'), frontend3000: await probe('http://127.0.0.1:3000/fa') };
  let dbs = [];
  try { dbs = execFileSync('docker', ['exec', 'postgres-db', 'psql', '-U', 'admin', '-d', 'postgres', '-At', '-c', 'select datname from pg_database order by 1'], { encoding: 'utf8' }).split(/\r?\n/).filter(Boolean); } catch {}
  const taskDbCreated = dbs.some(d => /288|devseed288/i.test(d));

  const S = {
    S01: { status: 'PASS_SOURCE_VERIFIED', grade: 'SOURCE_VERIFIED', proof: 'evidence/source-trace.txt + 287 evidence/fixture-crash-normal-boot.err.log (real reproduced crash inventory.catalog_variant.missing)' },
    S02: { status: 'NOT_RUN', grade: 'CONNECTED', reason: 'No authorized isolated fixture exists; the destructive Catalog reset must not run on shared dev DB / canonical Host (5088 runs non-candidate code on tenant-alpha); re-provision forbidden by task budget.' },
    S03: { status: 'NOT_RUN', grade: 'CONNECTED', reason: 'Depends on S02 fixture bootstrap.' },
    S04: { status: 'PASS_SOURCE_VERIFIED', grade: 'SOURCE_VERIFIED', proof: 'InventoryDirectory.cs:91 unchanged fail-closed CatalogVariantMissing; guard fired in 287 crash evidence' },
    S05: { status: 'NOT_RUN', grade: 'CONNECTED', reason: 'Requires fixture bootstrap (S02).' },
    S06: { status: (ports.host5088 === 200 && ports.frontend3000 === 200 && !taskDbCreated) ? 'PASS' : 'FAIL', grade: 'OBSERVED', proof: 'ports + DB list; no task DB created, no shared DB mutation by this task' },
    S07: { status: (buildOk && testsOk && openspecOk && forbidden.length === 0 && f287.length === 0) ? 'PASS' : 'FAIL', grade: 'OBSERVED', proof: 'host-build 0 errors; focused tests pass; openspec strict valid; diff scoped' },
  };
  const overall = (S.S01.status.startsWith('PASS') && S.S02.status === 'PASS' && S.S03.status === 'PASS')
    ? 'READY_FOR_ARCHITECT_REVIEW' : 'BLOCKED';

  const observed = {
    taskId: 'TB-DEV-SEED-STALE-CATALOG-VARIANT-288',
    protocol: 'TOOBA-OPENCODE-DELIVERY-V1@1.3.0',
    producer: { script: 'docs/opencode/tasks/TB-DEV-SEED-STALE-CATALOG-VARIANT-288/evidence/producer-288.cjs', scriptSha256: selfSha, writesObservedJsonDirectly: true },
    generatedAt: new Date().toISOString(),
    build: { ok: buildOk, log: 'evidence/host-build.log' },
    focusedTests: { ok: testsOk, log: 'evidence/focused-test.log' },
    openspec: { ok: openspecOk, log: 'evidence/openspec-validate.txt' },
    diff: { changedPaths: paths, forbiddenPaths: forbidden, offer287Touched: f287 },
    ports, databases: dbs, taskDbCreated,
    tests: S,
    overall,
    blocker: overall === 'BLOCKED' ? 'No authorized isolated fixture for the destructive reset; canonical/shared dev DB must not be reset and re-provision is forbidden. S02/S03/S05 connected proofs are NOT_RUN.' : null,
  };
  fs.writeFileSync(OUT, JSON.stringify(observed, null, 2) + '\n');
  console.log(JSON.stringify({ buildOk, testsOk, openspecOk, forbidden, f287, ports, taskDbCreated, statuses: Object.fromEntries(Object.entries(S).map(([k, v]) => [k, v.status])), overall, producerSha256: selfSha }, null, 2));
})();
