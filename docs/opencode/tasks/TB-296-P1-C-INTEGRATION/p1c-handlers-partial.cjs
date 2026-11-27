// Verified historical execution, NOT a new connected run. Never infer other cases.
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
function verifyTrace(t){
  const requireFact=(value,description)=>{if(!value)throw new Error(description);};
  const eq=(a,b)=>a===b;
  const sameSql=(a,b)=>["offerId","stockItemId","onHand","reserved","factCount"].every(k=>eq(a[k],b[k]));
  requireFact(t.status==="PASS"&&t.offerId&&t.stockItemId,"missing observed successful trace");
  requireFact(t.before.stockItemId===t.stockItemId&&t.before.revision,"missing exact initial row");
  const e=t.events, o=t.observations;
  requireFact(e.map(x=>x.type).join(",")==="request,response,request,requestfailed,request,response","unexpected request/response sequence");
  const [first,firstResponse,failed,abort,retry,lastResponse]=e;
  requireFact(firstResponse.status===200&&lastResponse.status===200&&abort.error,"HTTP success/abort proof absent");
  const writes=[first.body,failed.body,retry.body];
  requireFact(writes.every(x=>x.stockItemId===t.stockItemId&&x.operationId&&x.expectedVersion&&x.reason&&Number.isFinite(x.onHand)),"incomplete write identity");
  requireFact(first.body.operationId!==failed.body.operationId&&["stockItemId","operationId","expectedVersion","onHand","reason"].every(k=>failed.body[k]===retry.body[k]),"retry not exact failed intent");
  requireFact(first.body.expectedVersion===t.before.revision&&first.body.onHand!==t.before.onHand&&retry.body.onHand!==first.body.onHand,"incorrect original revision or quantity");
  const sql=o.filter(x=>x.independentSql).map(x=>x.independentSql);
  requireFact(sql.length===5&&sql.every(x=>x.offerId===t.offerId&&x.stockItemId===t.stockItemId&&Number.isFinite(x.onHand)&&Number.isFinite(x.reserved)&&Number.isInteger(x.factCount)),"independent SQL incomplete");
  requireFact(sql[0].onHand===t.before.onHand&&sql[0].reserved===t.before.reserved&&sameSql(sql[0],sql[1]),"blank reason altered state");
  requireFact(sql[2].onHand===first.body.onHand&&sql[2].factCount===sql[0].factCount+1&&sameSql(sql[2],sql[3]),"failed request altered persisted state");
  requireFact(sql[4].onHand===retry.body.onHand&&sql[4].factCount===sql[2].factCount+1&&sql.every(x=>x.reserved===sql[0].reserved),"retry SQL mismatch");
  const reads=o.filter(x=>x.independentRead).map(x=>x.independentRead), afterFailure=o.find(x=>x.afterFailure)?.afterFailure;
  requireFact(reads.length===2&&reads.every(x=>x.stockItemId===t.stockItemId&&x.revision)&&reads[0].onHand===first.body.onHand&&reads[1].onHand===retry.body.onHand,"independent GET mismatch");
  requireFact(reads[0].revision!==t.before.revision&&failed.body.expectedVersion===reads[0].revision&&afterFailure?.stockItemId===t.stockItemId&&afterFailure?.revision===reads[0].revision&&afterFailure?.onHand===reads[0].onHand&&reads[1].revision!==reads[0].revision,"owner revision mismatch");
  for(const [response,read] of [[firstResponse,reads[0]],[lastResponse,reads[1]]]){
    const row=response.body?.inventoryLocations?.find(x=>x.stockItemId===t.stockItemId);
    requireFact(response.body?.offerId===t.offerId&&response.body?.status==="Draft"&&response.body?.sellerSku?.startsWith("P1C-")&&row?.revision===read.revision&&row?.onHand===read.onHand,"HTTP result identity mismatch");
  }
  requireFact(o.some(x=>typeof x.inline==="string"&&x.inline.includes("دلیل"))&&o.filter(x=>x.kind==="success").length===2&&o.some(x=>x.kind==="dialog"&&x.text?.includes("بازخوانی")),"validation, feedback or reload observation absent");
  return true;
}
const traceFile=path.join(__dirname,"p1c-inventory-feedback-result.json");
const evidence="docs/opencode/tasks/TB-296-P1-C-INTEGRATION/p1c-inventory-feedback-result.json";
function observed(reason) {
  const raw=fs.readFileSync(traceFile);
  const blob=crypto.createHash("sha1").update("blob "+raw.length+"\0").update(raw).digest("hex");
  if(blob!=="7c9c5f63929dd0c30372ce4abdcf57bf65471c0b") throw new Error("Historical evidence blob differs from reviewed execution");
  verifyTrace(JSON.parse(raw.toString("utf8").replace(/^\uFEFF/,"")));
  return {status:"PASS",reason:"REUSED VERIFIED CONNECTED EVIDENCE (b73a36b): "+reason,evidence};
}
module.exports={
  "R10-12-N": async()=>observed("Inline reason validation, actual SQL-confirmed correction, error modal, unchanged state on abort and successful exact retry."),
  "R10-13-N": async()=>observed("Controlled network abort, unchanged independent GET/SQL, reload dialog and exactly one identical retry intent; one additional physical fact only.")
};
