// TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287 — independent evidence checker (T15).
// Recomputes the producer script hash, validates every critical observed value, and runs a
// three-way mutation self-test that MUST reject tampered / missing / flipped evidence.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';

const TASK = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'));
const EVID = path.join(TASK, 'evidence');
const EVIDENCE = path.join(EVID, 'connected-287.json');
const SCRIPT = path.join(EVID, 'connected-287.cjs');
const DB = 'tooba_offer287_idem';

const sha256 = f => crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const actualScriptSha = sha256(SCRIPT);

function dbExists() {
  try {
    const out = execFileSync('docker', ['exec', 'postgres-db', 'psql', '-U', 'admin', '-d', 'postgres', '-At', '-c',
      `select 1 from pg_database where datname='${DB}'`], { encoding: 'utf8' }).trim();
    return out === '1';
  } catch { return null; }
}
async function probe(url) { try { return (await fetch(url)).status; } catch { return 0; } }

function evaluate(ev) {
  const fails = [];
  const need = (cond, msg) => { if (!cond) fails.push(msg); };
  need(ev && ev.tests, 'no tests object');
  const T = ev.tests || {};
  for (let i = 1; i <= 16; i++) {
    const id = `T${String(i).padStart(2, '0')}`;
    if (i === 15) { need(T[id] && T[id].delegatedTo === 'VERIFY.mjs', `${id} must be delegated`); continue; }
    need(T[id], `${id} missing`);
    if (T[id]) need(T[id].pass === true, `${id} not PASS`);
  }
  const o = id => (T[id]?.observed || {});
  need(o('T02').status === 409 && o('T02').errorCode === 'offer.create.intent_conflict', 'T02 changed-intent not 409 intent_conflict');
  need(o('T04').status === 409 && o('T04').errorCode === 'offer.create.intent_conflict', 'T04 changed-policy not 409 intent_conflict');
  need(o('T07').offerRows === 1 && o('T07').loserStatus === 409, 'T07 concurrency not one-row+409');
  need(o('T11').fingerprintAfterNull === '<NULL>' && o('T11').fingerprintStillNull === '<NULL>' && o('T11').replayStatus === 409, 'T11 legacy not fail-closed/no-backfill');
  need(o('T13').sqlFingerprintPersisted === true && o('T13').fingerprintStable === true && o('T13').changedStatus === 409, 'T13 durability (persisted fingerprint + fresh context + SQL) not proven');
  const t16 = o('T16');
  need(Array.isArray(t16.forbiddenPaths) && t16.forbiddenPaths.length === 0, 'T16 forbidden paths present');
  need(t16.canonicalAfter && t16.canonicalAfter.host5088 === 200, 'T16 canonical 5088 not healthy');
  if (t16.teardown && t16.teardown.performed) {
    need(t16.teardown.dbExists === false, 'T16 disposable DB not removed');
    need(t16.teardown.port5094Listening === false, 'T16 test Host still listening');
  }
  need(ev.producer && ev.producer.scriptSha256 === actualScriptSha, 'producer script hash mismatch (evidence tampered)');
  return { ok: fails.length === 0, failures: fails };
}

const deepClone = x => JSON.parse(JSON.stringify(x));
function mutations(ev) {
  const list = [];
  {
    const m = deepClone(ev); delete m.tests.T05; list.push({ name: 'delete-test-id-T05', evidence: m });
  }
  {
    const m = deepClone(ev); m.tests.T02.observed.status = 201; m.tests.T02.observed.errorCode = null; list.push({ name: 'flip-T02-expected-to-201', evidence: m });
  }
  {
    const m = deepClone(ev); m.producer.scriptSha256 = 'deadbeef'.repeat(8); list.push({ name: 'tamper-producer-hash', evidence: m });
  }
  return list;
}

(async () => {
  const ev = JSON.parse(fs.readFileSync(EVIDENCE, 'utf8'));
  const real = evaluate(ev);
  const mut = mutations(ev).map(x => { const r = evaluate(x.evidence); return { name: x.name, rejected: r.ok === false, reasons: r.failures }; });
  const allRejected = mut.every(m => m.rejected);
  const t15Pass = allRejected; // T15 = the checker rejects missing/flipped/tampered evidence (independent of a green matrix)

  // Independent corroboration of teardown/canonical state.
  const corroboration = {
    databaseStillExists: dbExists(),
    canonicalHost5088: await probe('http://127.0.0.1:5088/health'),
    canonicalFrontend3000: await probe('http://127.0.0.1:3000/fa'),
  };

  const result = {
    task: 'TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287',
    script: 'VERIFY.mjs', scriptSha256: sha256(new URL(import.meta.url)),
    realEvidenceOk: real.ok, realFailures: real.failures,
    T15: { pass: t15Pass, mutations: mut, note: 'missing test / flipped fact / tampered hash must all be rejected' },
    corroboration,
    decision: real.ok && allRejected ? 'READY_FOR_ARCHITECT_REVIEW' : 'BLOCKED_EVIDENCE',
  };
  fs.writeFileSync(path.join(EVID, 'verify-287.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({ realOk: real.ok, t15Pass, decision: result.decision, mutations: mut.map(m => `${m.name}:${m.rejected ? 'REJECTED' : 'ACCEPTED!!'}`), corroboration }, null, 2));
  if (!(real.ok && allRejected)) process.exitCode = 1;
})();
