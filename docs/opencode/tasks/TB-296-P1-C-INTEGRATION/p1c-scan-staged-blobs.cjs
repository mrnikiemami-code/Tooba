const { execFileSync } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");

const repo = process.cwd();
const base = "origin/opencode/iso-agents-002";
function zlist(args) {
  return execFileSync("git", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 })
    .split("\0").filter(Boolean);
}
const outgoing = new Set([
  ...zlist(["diff", "--name-only", "-z", base, "HEAD"]),
  ...zlist(["diff", "--cached", "--name-only", "-z"]),
]);
const paths = [...outgoing].filter((p) => {
  try { execFileSync("git", ["cat-file", "-e", ":" + p], { stdio: "ignore" }); return true; } catch { return false; }
});

function blob(p) {
  return execFileSync("git", ["show", ":" + p], { encoding: "buffer", maxBuffer: 64 * 1024 * 1024 });
}

const oversized = [];
const dumps = [];
const credentials = [];
let totalBytes = 0;
const badExt = /\.(dump|backup|bak|dmp|pfx|pem|key|env)$/i;
const selfPath = "docs/opencode/tasks/TB-296-P1-C-INTEGRATION/p1c-scan-staged-blobs.cjs";
const patterns = [
  /password\s*[=:]\s*["']?[^"'\s]{4,}/i,
  /Host=[^;\n]+;.*Password=/i,
  /Bearer\s+[A-Za-z0-9._-]{16,}/i,
  /client_secret\s*[=:]\s*["']?[^"'\s]{8,}/i,
  /BEGIN (RSA|OPENSSH|EC|PRIVATE) KEY/i,
  /api[_-]?key\s*[=:]\s*["']?[A-Za-z0-9._-]{16,}/i,
];

for (const p of paths) {
  const size = Number(execFileSync("git", ["cat-file", "-s", ":" + p], { encoding: "utf8" }).trim());
  totalBytes += size;
  if (size > 10 * 1024 * 1024) oversized.push({ path: p, size });
  if (badExt.test(p) || /database\s+dump/i.test(p)) dumps.push({ path: p, size });
  if (p !== selfPath && size <= 2 * 1024 * 1024 && /(\.json|\.js|\.cjs|\.ts|\.sql|\.log|\.md|\.txt|\.yml|\.yaml|\.config)$/i.test(p)) {
    const text = blob(p).toString("utf8");
    for (const re of patterns) {
      if (re.test(text)) { credentials.push({ path: p, pattern: String(re) }); break; }
    }
  }
}

const ledgerPath = "docs/opencode/tasks/TB-296-P1-C-INTEGRATION/FINAL-P1C-60-ID-LEDGER.json";
const ledger = JSON.parse(blob(ledgerPath).toString("utf8"));
const evidence = ledger.rows.map((row) => {
  let p = row.evidence;
  if (/^[A-Za-z]:[\\/]/.test(p)) p = path.relative(repo, p);
  p = p.replaceAll("\\", "/").replace(/^\.\//, "");
  try {
    const bytes = blob(p);
    const actual = crypto.createHash("sha256").update(bytes).digest("hex");
    return { id: row.id, path: p, expected: row.evidenceSha256, actual, ok: bytes.length > 0 && actual === row.evidenceSha256 };
  } catch (error) {
    return { id: row.id, path: p, error: String(error), ok: false };
  }
});

const forbiddenFinalHost = paths.includes("docs/opencode/tasks/TB-296-P1-C-INTEGRATION/final-host.out.log");
const report = {
  scanner: "p1c-scan-staged-blobs.cjs",
  base,
  outgoingBlobCount: paths.length,
  totalOutgoingBytes: totalBytes,
  oversizedOver10MB: oversized,
  largeOrDumpExtensions: dumps,
  credentialPatternHits: credentials,
  forbiddenFinalHostOutgoing: forbiddenFinalHost,
  evidenceRows: evidence.length,
  evidenceOk: evidence.filter((row) => row.ok).length,
  evidenceFailures: evidence.filter((row) => !row.ok),
};
fs.writeFileSync("docs/opencode/tasks/TB-296-P1-C-INTEGRATION/ECC-STAGED-BLOB-AUDIT.json", JSON.stringify(report, null, 2));
console.log(JSON.stringify({
  outgoingBlobCount: paths.length,
  totalOutgoingBytes: totalBytes,
  oversized: oversized.length,
  dumps: dumps.length,
  credentials: credentials.length,
  forbiddenFinalHost,
  evidenceOk: report.evidenceOk + "/" + report.evidenceRows,
  evidenceFailures: report.evidenceFailures.length,
}, null, 2));
process.exitCode = oversized.length || dumps.length || credentials.length || forbiddenFinalHost || report.evidenceFailures.length ? 1 : 0;
