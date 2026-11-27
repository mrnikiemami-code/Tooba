// Execution/journal helper ONLY. Existing immutable matrices and acceptance-gate remain authoritative.
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
function coverage(ids, modules) {
  const handlers = Object.create(null);
  for (const mod of modules) for (const [id, handler] of Object.entries(mod)) {
    if (!ids.includes(id)) throw Error("Unknown immutable case "+id);
    if (typeof handler !== "function") throw Error("Non-callable oracle "+id);
    if (Object.hasOwn(handlers,id)) throw Error("Duplicate oracle "+id);
    handlers[id]=handler;
  }
  return {handlers,missing:ids.filter(id=>!Object.hasOwn(handlers,id))};
}
async function executeCases(ids, handlers, persist, select = ids) {
  if (new Set(ids).size!==ids.length || new Set(select).size!==select.length || select.some(id=>!ids.includes(id))) throw Error("Invalid immutable ID selection");
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
      Object.assign(row, {status:result.status,reason:result.reason,evidence:result.evidence,...(result.evidenceSha256?{evidenceSha256:result.evidenceSha256}:{})});
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
  const args=process.argv.slice(2);
  const auditOnly=args[0]==="--coverage";
  if(auditOnly)args.shift();
  const [moduleFiles,output,selection]=args;
  if (!moduleFiles || (!auditOnly&&!output)) throw new Error("Usage: node p1c-run-cases.cjs [--coverage] <module1.cjs,module2.cjs> [new-ledger.json] [--only=ID1,ID2]");
  const {handlers,missing}=coverage(ids,moduleFiles.split(",").map(f=>require(path.resolve(f))));
  console.log(JSON.stringify({total:ids.length,implemented:ids.length-missing.length,missing},null,2));
  if(auditOnly){process.exitCode=missing.length?1:0;return;}
  if(fs.existsSync(output))throw Error("Use new output; do not overwrite historical evidence");
  const select=selection?(selection.startsWith("--only=")?selection.slice(7).split(","):[]):ids;
  if(!select.length)throw Error("Empty/invalid selection");
  if(selection&&select.some(id=>missing.includes(id)))throw Error("Selected oracle missing; use --coverage for inventory");
  const checked=Object.fromEntries(Object.entries(handlers).map(([id,handler])=>[id,async()=>{
    try{
      const result=await handler();
      if(!result||!["PASS","FAIL","NOT_RUN"].includes(result.status)||!result.reason||typeof result.evidence!=="string")throw Error("Malformed oracle result");
      const artifact=path.resolve(result.evidence);
      if(!fs.statSync(artifact).isFile())throw Error("Evidence must be an actual file");
      const bytes=fs.readFileSync(artifact);
      if(!bytes.length)throw Error("Empty evidence");
      return {...result,evidenceSha256:crypto.createHash("sha256").update(bytes).digest("hex")};
    }catch(error){
      const evidence=output+"."+id+".error.json";
      fs.writeFileSync(evidence,JSON.stringify({id,error:String(error?.stack??error)},null,2),{flag:"wx"});
      return {status:"FAIL",reason:String(error?.stack??error),evidence,evidenceSha256:crypto.createHash("sha256").update(fs.readFileSync(evidence)).digest("hex")};
    }
  }]));
  const rows=await executeCases(ids,checked,async rows=>{
    fs.writeFileSync(output+".tmp",JSON.stringify(rows,null,2));
    fs.renameSync(output+".tmp",output);
  },select);
  const counts = Object.fromEntries(["PASS","FAIL","NOT_RUN"].map(s=>[s,rows.filter(r=>r.status===s).length]));
  console.log(JSON.stringify(counts));
  process.exitCode = counts.FAIL || counts.NOT_RUN ? 1 : 0;
}
module.exports = { executeCases, coverage };
if (require.main===module) main().catch(e=>{console.error(e);process.exitCode=2;});
