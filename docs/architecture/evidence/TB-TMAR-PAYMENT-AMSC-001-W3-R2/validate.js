const fs = require('fs');
const path = require('path');
const cp = require('child_process');

function sh(cmd) {
  return cp.execSync(cmd, { encoding: 'utf8' }).trim();
}

const results = [];
function check(name, ok, detail) {
  results.push({ name, ok, detail });
}

// 1. JSON parse SoT
let sot;
try {
  sot = JSON.parse(fs.readFileSync('docs/architecture/tmar-current-state.json', 'utf8').replace(/^\uFEFF/, ''));
  check('SoT JSON parse', true);
} catch (e) {
  check('SoT JSON parse', false, e.message);
  console.log(JSON.stringify(results, null, 2));
  process.exit(1);
}

// 2. Exact SHA assertions + 3. parent chain
const head = sh('git rev-parse HEAD');
const parents = {
  '2d69d82808178e786f0673dc725baeb238987829': '6839bb4a5f75a954817719386de04e36ff96c305',
  'a138ec61bcbbb719fed2949c60e21fa77104cfd1': '2d69d82808178e786f0673dc725baeb238987829',
  '502d73e0e9ccfb277a06ad397b1f0a511f586921': 'a138ec61bcbbb719fed2949c60e21fa77104cfd1',
  'affdfba4fc5fce402d05e68131bdb4a01899b93c': '502d73e0e9ccfb277a06ad397b1f0a511f586921',
};
check('starting HEAD == affdfba4', head === 'affdfba4fc5fce402d05e68131bdb4a01899b93c', head);
for (const [child, expectedParent] of Object.entries(parents)) {
  const line = sh('git rev-list --parents -n 1 ' + child).split(/\s+/);
  const p = line[1] || '';
  check('parent ' + child.slice(0, 8) + ' -> ' + expectedParent.slice(0, 8), p === expectedParent, p);
}

// 4. exact 28/27/24 counts + 5. foreign set
const codes = fs.readFileSync('src/backend/Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs', 'utf8');
const contrib = fs.readFileSync('src/backend/Modules/Payment/Tooba.Payment.Endpoints/Errors/PaymentErrorCatalogContributor.cs', 'utf8');
const decl = (codes.match(/public const string \w+/g) || []).length;
const desc = (contrib.match(/D\(PaymentErrorCodes\./g) || []).length;
const m = codes.match(/KnownCodes = new\(StringComparer\.Ordinal\)\s*\{([\s\S]*?)\};/);
const members = (m[1].match(/[A-Za-z][A-Za-z0-9]*/g) || []);
check('declarations == 28', decl === 28, String(decl));
check('descriptors == 24', desc === 24, String(desc));
check('KnownCodes members == 27', members.length === 27, String(members.length));

const foreign = ['ReservationRetryLimit', 'SupplyUnavailable', 'AdminAuthorizationDenied', 'CheckoutAuthenticationRequired'];
for (const n of foreign) {
  check('foreign not registered: ' + n, !new RegExp('D\\(PaymentErrorCodes\\.' + n + ',').test(contrib));
}
check('KnownCodes has ReservationRetryLimit', members.includes('ReservationRetryLimit'));
check('KnownCodes has SupplyUnavailable', members.includes('SupplyUnavailable'));
check('KnownCodes has CheckoutAuthenticationRequired', members.includes('CheckoutAuthenticationRequired'));
check('KnownCodes EXCLUDES AdminAuthorizationDenied', !members.includes('AdminAuthorizationDenied'));

// 6. SoT recovery blocks
const r1 = sot.paymentAmsc001W3R1;
const r2 = sot.paymentAmsc001W3R2;
check('W3-R1 block recorded', r1 && r1.state === 'PAYMENT_AMSC_001_RECOVERY_RECONCILED' && r1.commit === 'affdfba4');
check('W3-R1 commitFull exact', r1 && r1.commitFull === 'affdfba4fc5fce402d05e68131bdb4a01899b93c');
check('W3-R2 block closed', r2 && r2.state === 'PAYMENT_AMSC_001_RECOVERY_CLOSED_RECONCILED');
check('W3-R2 truth counts', r2 && r2.declaredStableCodeCount === 28 && r2.knownCodeGuardMemberCount === 27 && r2.paymentOwnedDescriptorCount === 24 && r2.foreignOwnedDeclaredConsumedCount === 4 && r2.foreignOwnedKnownGuardCount === 3);
check('W3-R2 admin auth excluded', r2 && r2.foreignOwnedNotInKnownGuard === 'admin.authorization.denied');
check('W3 authority preserved', r2 && r2.currentCertificationAuthority === 'TB-TMAR-PAYMENT-AMSC-001-W3' && r2.currentCertifiedCommit === '502d73e0e9ccfb277a06ad397b1f0a511f586921');
check('W3 verdict preserved', sot.paymentAmsc001W3.verdict === 'COMPLETE_REFERENCE_PATTERN' && sot.paymentAmsc001W3.lockVersion === 'ARCH-COMPLETE-002' && sot.paymentAmsc001W3.structureCertified === true);
check('W3 structure fields preserved', sot.paymentAmsc001W3.endpointReachableRequests === 16 && sot.paymentAmsc001W3.validatorRequiredCount === 15 && sot.paymentAmsc001W3.noValidatorRequiredCount === 1 && sot.paymentAmsc001W3.foreignAppInfraDomainCoupling === 'ZERO' && sot.paymentAmsc001W3.microserviceExtractable === true);
check('W0/W1/W2 commit truth durable', sot.paymentAmsc001W0.commitFull === '6839bb4a5f75a954817719386de04e36ff96c305' && sot.paymentAmsc001W1.commitFull === '2d69d82808178e786f0673dc725baeb238987829' && sot.paymentAmsc001W2.commitFull === 'a138ec61bcbbb719fed2949c60e21fa77104cfd1');
check('global host checkpoint preserved', sot.currentHostCheckpoint === 'HOST_ROOT_FINAL_CERTIFIED', sot.currentHostCheckpoint);

// 7. git diff scope proof (compare against the recorded pre-existing unrelated artifacts)
const baselinePath = path.join(__dirname, 'preexisting-unrelated-artifacts.txt');
const baseline = fs.existsSync(baselinePath)
  ? fs.readFileSync(baselinePath, 'utf8').replace(/^\uFEFF/, '').split('\n').map((l) => l.replace(/\r$/, '')).filter(Boolean)
  : [];
const baselineSet = new Set(baseline);
const changed = [
  ...sh('git diff --name-only HEAD').split('\n'),
  ...sh('git ls-files --others --exclude-standard').split('\n'),
].map((l) => l.trim().replace(/\\/g, '/')).filter(Boolean);
const allowed = [
  'docs/architecture/tmar-current-state.json',
  'docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md',
  'src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W3CertGuardTests.cs',
];
const unexpected = changed.filter((p) => {
  if (allowed.includes(p)) return false;
  if (p.startsWith('docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R2/')) return false;
  if (p === 'docs/ai/tasks/TB-TMAR-PAYMENT-AMSC-001-W3-R2.task.md') return false;
  if (baselineSet.has(p)) return false;
  return true;
});
check('no unexpected changed files', unexpected.length === 0, JSON.stringify(unexpected));
check('no Payment production file changed', !changed.some((p) => /^src\/backend\/Modules\/Payment\/.*\.(cs|csproj)$/.test(p)));

let failed = 0;
for (const r of results) {
  if (!r.ok) failed++;
  console.log((r.ok ? 'PASS ' : 'FAIL ') + r.name + (r.detail ? ' :: ' + r.detail : ''));
}
console.log('---');
console.log('TOTAL ' + results.length + ' FAILED ' + failed);
process.exit(failed === 0 ? 0 : 1);
