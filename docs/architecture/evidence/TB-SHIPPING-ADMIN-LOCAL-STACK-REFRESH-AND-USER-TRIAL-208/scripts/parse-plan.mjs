// Parses Tooba.MigrationRunner output: structured log lines (one JSON per line) plus the final plan JSON.
// Prints the complete pending set per module so the operator can compare against the expected nine.
import fs from "node:fs";
const raw = fs.readFileSync(process.argv[2], "utf8").replace(/^\uFEFF/, "");
const lines = raw.split(/\r?\n/).filter((l) => l.trim().length > 0);
let plan = null;
for (const line of lines) {
  const t = line.trim();
  if (!t.startsWith("{")) continue;
  try {
    const o = JSON.parse(t);
    if (Array.isArray(o.modules)) plan = o;
  } catch {
    // log lines are newline-delimited JSON; ignore anything else
  }
}
if (!plan) {
  console.log(JSON.stringify({ parsed: false, lineCount: lines.length }));
  process.exit(1);
}
const pending = [];
for (const m of plan.modules) {
  const ids = m.pendingMigrations ?? [];
  if (ids.length) pending.push({ module: m.module, schema: m.schema, ids });
}
console.log(JSON.stringify({
  parsed: true,
  edition: plan.edition,
  tenantId: plan.tenantId,
  database: plan.database,
  moduleCount: plan.modules.length,
  failedModules: plan.modules.filter((m) => m.succeeded === false).map((m) => m.module),
  pending,
  totalPending: pending.reduce((a, p) => a + p.ids.length, 0),
}, null, 2));
