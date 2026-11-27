// Execution/journal helper ONLY. Existing immutable matrices and acceptance-gate remain authoritative.
const fs = require("node:fs");
const path = require("node:path");
async function executeCases(ids, handlers, persist, select = ids) {
  const rows = ids.map(id => ({ id, status: "NOT_RUN", reason: "not yet executed" }));
  await persist(rows);
  for (const id of select) {
    const row = rows.find(r => r.id === id);
    if (!row) throw new Error("Unknown immutable case " + id);
    const handler = handlers[id];
    if (!handler) { row.reason = "MISSING_EXECUTABLE_ORACLE"; await persist(rows); continue; }
    row.startedAt = new Date().toISOString();
    try {
      const result = await handler();
      if (!result || !["PASS", "FAIL", "NOT_RUN"].includes(result.status) || !result.evidence || !result.reason) throw new Error("Missing observed result/evidence");
      Object.assign(row, {status:result.status,reason:result.reason,evidence:result.evidence});
    } catch (error) { row.status="FAIL";row.reason=String(error?.stack ?? error); }
    row.finishedAt = new Date().toISOString();
    await persist(rows);
  }
  return rows;
}
const read = p => JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
async function main() {
  const parent = path.resolve(__dirname,"../TB-296-R10-OFFER-PRODUCTION-READINESS");
  const ids = ["ACCEPTANCE-MATRIX.json","ADDITIVE-MATRIX.json"].flatMap(n=>read(path.join(parent,n)).tests.map(t=>t.testId));
  ids.push("D2-ORDER","D2-HOLD","D2-ALLOCATION","D2-PARTIAL","D2-RECOVERY","D2-HTTP","F-BENTRY-01","F-BENTRY-02");
  if (new Set(ids).size !== 60) throw new Error("Unexpected immutable ID set");
  const [handlerFile, output] = process.argv.slice(2);
  if (!handlerFile || !output) throw new Error("Usage: node p1c-run-cases.cjs <actual-oracle-module.cjs> <new-ledger.json>");
  if (fs.existsSync(output)) throw new Error("Use new output; do not overwrite historical evidence");
  const handlers = require(path.resolve(handlerFile));
  for (const id of Object.keys(handlers)) if (!ids.includes(id)) throw new Error("Unknown case handler: "+id);
  const rows = await executeCases(ids,handlers,async rows=>{
    fs.writeFileSync(output+".tmp",JSON.stringify(rows,null,2));
    fs.renameSync(output+".tmp",output);
  });
  const counts = Object.fromEntries(["PASS","FAIL","NOT_RUN"].map(s=>[s,rows.filter(r=>r.status===s).length]));
  console.log(JSON.stringify(counts));
  process.exitCode = counts.FAIL || counts.NOT_RUN ? 1 : 0;
}
module.exports = { executeCases };
if (require.main===module) main().catch(e=>{console.error(e);process.exitCode=2;});
