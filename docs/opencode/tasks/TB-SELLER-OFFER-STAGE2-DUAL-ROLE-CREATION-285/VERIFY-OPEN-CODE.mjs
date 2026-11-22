#!/usr/bin/env node
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(__dirname, "../../../..");
const TASK = __dirname;
const MANIFEST = "docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/EVIDENCE-MANIFEST.json";
const REQ_IDS = ["REQ-01","REQ-02","REQ-03","REQ-04","REQ-05","REQ-06","REQ-07","REQ-08"];
const EXPECTED = {
 "REQ-01":["admin-create-one-request","admin-create-row-seller","seller-A-reads-admin-offer","seller-B-cannot-read-A","admin-edit-price-allowed","admin-price-visible-to-seller"],
 "REQ-02":["admin-view-only-denied","seller-B-denied-price"],
 "REQ-03":["lost-create-same-id","lost-create-price-stock"],
 "REQ-04":["old-variant-server-search","picker-selection-retained"],
 "REQ-05":["seller-server-page2","seller-server-search","admin-server-page2","admin-server-search"],
 "REQ-06":["cancel-zero-business-writes","partial-explicit-and-recovered","wrong-response-no-success-no-write"],
 "REQ-07":["geometry-1440-fa","geometry-1440-en","geometry-360-fa","geometry-360-en"],
 "REQ-08":["source-code-ancestor","no-feature-diff-after-code","no-cursor-change","no-migration","openspec-strict","verifier-self-test","cleanup"]
};
const SELLER_A="01a2045c-9b4a-7000-ba31-538e7f3c3166";
const sha = bytes => crypto.createHash("sha256").update(bytes).digest("hex");
const read = rel => fs.readFileSync(path.join(REPO, rel));
const git = (args, enc="utf8") => execFileSync("git", ["-C",REPO,...args], {encoding:enc});
const filled = v => typeof v === "string" && v.trim().length > 0 && !v.includes("PENDING");

function validateStructure(m, errors) {
  if (!m || typeof m !== "object") return errors.push("manifest object required");
  if (m.protocol !== "TOOBA-OPENCODE-DELIVERY-V1@1.0.0") errors.push("protocol mismatch");
  if (m.taskId !== "TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285") errors.push("taskId mismatch");
  if (m.parentTaskId !== "TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283") errors.push("parent mismatch");
  if (m.codeSha !== "02abb9d4b69f2bb7ebb023c657d62f203ab95475") errors.push("codeSha mismatch");
  if (m.planCommit !== "0109f550e22835570410db79fd67957fa465a9ae") errors.push("planCommit mismatch");
  if (!filled(m.sourceEvidenceCommit)) errors.push("sourceEvidenceCommit missing");
  const rows=list(m.requirements); const ids=rows.map(r=>r?.id);
  if (ids.length!==8 || [...ids].sort().join("|")!==[...REQ_IDS].sort().join("|")) errors.push("requirements must be exactly REQ-01..REQ-08");
  if (new Set(ids).size!==ids.length) errors.push("duplicate requirement");
  for (const row of rows) {
    if (row.status!=="PASS") errors.push(`requirement not PASS: ${row.id}`);
    const expected=EXPECTED[row.id]||[]; const names=list(row.checks).map(c=>c?.name);
    if (names.length!==expected.length || [...names].sort().join("|")!==[...expected].sort().join("|")) errors.push(`checks mismatch: ${row.id}`);
    if (!Array.isArray(row.evidencePaths)||row.evidencePaths.length===0) errors.push(`evidence paths: ${row.id}`);
    if (!["connected-real","controlled-error"].includes(row.evidenceClass)) errors.push(`evidenceClass: ${row.id}`);
    if (!filled(row.limitations)) errors.push(`limitations: ${row.id}`);
  }
  const tool=m.toolEvidence;
  if (!tool) errors.push("toolEvidence required");
  else {
    if (tool.host?.dllSha256 !== "E2DEE16EE9AE582E9D983321FD6ACF98818997637354E56FAF202F5466444ABE") errors.push("Host hash");
    if (tool.next?.buildId !== "UYWZYt9rgDXhubR29wG8o") errors.push("Next build");
    if (tool.postgresql?.database !== "tooba_offer285_connected") errors.push("PostgreSQL identity");
    if (tool.cursorArtifacts !== "UNCHANGED") errors.push("Cursor artifacts must be unchanged");
    if (!tool.artifacts || typeof tool.artifacts !== "object") errors.push("artifact hashes required");
  }
  const c=m.cleanupEvidence;
  if (!c || c.databaseRemoved!==true || !Array.isArray(c.portsClosed) || c.portsClosed.join(",")!=="5091,5092,3101" || c.canonicalHealth?.host5088!==200 || c.canonicalHealth?.next3000!==200) errors.push("cleanup evidence invalid");
  if (m.result?.status!=="PASS" || !Array.isArray(m.result?.blockingItems) || m.result.blockingItems.length!==0) errors.push("result must be PASS");
}
const list = v => Array.isArray(v)?v:[];
function validateEvidence(m, c, errors) {
  if (c.ok!==true) errors.push("connected ok false");
  if (!Array.isArray(c.checks) || c.checks.length<23 || c.checks.some(x=>x.ok!==true)) errors.push("connected assertions incomplete/failing");
  const names=c.checks.map(x=>x.name);
  for(const id of REQ_IDS.slice(0,7)) for(const name of EXPECTED[id]) if(!names.includes(name)) errors.push(`missing assertion ${name}`);
  const check=Object.fromEntries(c.checks.map(x=>[x.name,x]));
  const s=c.scenarios||{};
  const r1=s.REQ01; if(!r1||r1.row?.sellerPartyId!==SELLER_A||r1.row?.status!=="Draft"||r1.row?.offerId!==r1.id) errors.push("REQ-01 identity/status");
  if(!r1||!Array.isArray(check["admin-create-one-request"]?.observed)||check["admin-create-one-request"].observed.length!==1) errors.push("REQ-01 one admin create");
  if(!r1||r1.sellerGet?.sellerPartyId!==SELLER_A||r1.sellerGet?.offerId!==r1.id) errors.push("REQ-01 seller GET");
  if(!r1||r1.sellerBStatus!==404||r1.adminPriceStatus!==200) errors.push("REQ-01 denial/edit");
  if(String(check["admin-price-visible-to-seller"]?.observed)!=="1500000") errors.push("REQ-01 price visible");
  const r2=s.REQ02; if(!r2||r2.adminViewCreate!==403||r2.adminViewPrice!==403||r2.adminViewList!==200||r2.sellerBRead!==404||r2.sellerBPrice!==403||r2.adminEditPrice!==200||r2.viewCreatePersisted!==0) errors.push("REQ-02 authority");
  const r3=s.REQ03; if(!r3||r3.rowCount!==1||!Array.isArray(r3.requests)||r3.requests.length!==2||new Set(r3.requests.map(x=>JSON.parse(x.body||"{}").requestId)).size!==1) errors.push("REQ-03 idempotency");
  if(!r3||r3.get?.amount!==1300000||r3.get?.onHand!==2) errors.push("REQ-03 price/stock");
  const r4=s.REQ04; if(!r4||r4.oldProduct!=="2020"||!String(r4.oldText).includes("TB285-OLD-VAR")||!r4.requests.some(x=>x.includes("search=TB285-OLD-VAR"))||String(r4.selectedBefore).split("·").at(-1)?.trim()!==String(r4.selectedAfter).split("·").at(-1)?.trim()) errors.push("REQ-04 old picker/selection");
  const r5=s.REQ05; if(!r5||r5.seller?.page2?.page!==2||r5.seller?.rowsAfterSearch!==1||r5.admin?.page2?.page!==2||r5.admin?.rowsAfterSearch!==1) errors.push("REQ-05 paging/search");
  const r6=s.REQ06; if(!r6||r6.cancelBusinessWritesAtCheck!==0||(r6.cancelWrites||[]).length!==0||r6.partialGet?.amount!==1600000||r6.partialGet?.onHand!==1||!String(r6.partialText).includes("پیش‌نویس")||r6.wrongNotice!==0||r6.beforeWrong!==r6.afterWrong) errors.push("REQ-06 cancel/partial/wrong");
  const r7=s.REQ07; if(!Array.isArray(r7)||r7.length!==4||r7.some(x=>x.adminGeom.sw!==x.adminGeom.iw||x.sellerGeom.sw!==x.sellerGeom.iw||x.adminBox.height!==44||x.sellerBox.height!==44)) errors.push("REQ-07 geometry");
  const r8=s.REQ08; if(!r8||r8.offerCount<200||r8.sellerAOffers<100||r8.sellerBOffers<100||r8.oldProductYear!=="2020"||r8.oldOfferCount!==1) errors.push("REQ-08 data boundary");
  const artifacts=m.toolEvidence?.artifacts||{};
  const paths=new Set(list(m.requirements).flatMap(r=>list(r.evidencePaths)));
  for(const rel of paths){ if(!Object.prototype.hasOwnProperty.call(artifacts,rel)) errors.push(`artifact hash missing: ${rel}`); else try{ if(sha(read(rel))!==String(artifacts[rel]).toLowerCase()) errors.push(`artifact hash mismatch: ${rel}`); }catch(e){ errors.push(`artifact unreadable: ${rel}`); } }
}
function validateSource(m, errors) {
  try { git(["merge-base","--is-ancestor",m.expectedBaseSha,"HEAD"]); } catch { errors.push("expected base not ancestor"); }
  try { git(["merge-base","--is-ancestor",m.codeSha,"HEAD"]); } catch { errors.push("code SHA not ancestor"); }
  if(git(["diff","--name-only",`${m.codeSha}..HEAD`,"--","src/backend","src/frontend"]).trim()) errors.push("feature source changed after code SHA");
  if(git(["diff","--name-only","2d3230e2fbfb50b0931f4e716e300628747ce112..HEAD","--","docs/ai/delivery","docs/ai/tasks","docs/architecture/TOOBA-DELIVERY-ASSURANCE.md","AGENTS.md","docs/ROADMAP.md"]).trim()) errors.push("Cursor/governance artifact changed");
  if(git(["diff","--name-only","2d3230e2fbfb50b0931f4e716e300628747ce112..HEAD","--","src/backend"]).split(/\r?\n/).some(x=>x.includes("/Migrations/"))) errors.push("schema migration added");
  const spec=read("docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/OPENSPEC-STRICT.log").toString("utf8");
  if(!spec.includes("seller-offer-dual-role-creation-285")||!spec.includes("valid")||!spec.includes("EXIT=0")) errors.push("OpenSpec strict log invalid");
  const imp=read("docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/IMPECCABLE-AFTER.log").toString("utf8");
  if(!imp.includes("anti-patterns found")) errors.push("Impeccable after log invalid");
}
function selfTest(m, c, errors) {
  const baseline=[]; validateEvidence(m,c,baseline); if(baseline.length) errors.push("self-test baseline: "+baseline[0]);
  const missing=structuredClone(m); missing.requirements=missing.requirements.filter(r=>r.id!=="REQ-08"); const e1=[]; validateStructure(missing,e1); if(!e1.length) errors.push("self-test failed to reject missing REQ");
  const bad=structuredClone(c); bad.scenarios.REQ01.row.sellerPartyId="wrong"; const e2=[]; validateEvidence(m,bad,e2); if(!e2.length) errors.push("self-test failed to reject changed fact");
  const tampered={...m, toolEvidence:{...m.toolEvidence, artifacts:{...m.toolEvidence.artifacts, "docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/evidence/connected-285.json":"0".repeat(64)}}}; const e3=[]; validateEvidence(tampered,c,e3); if(!e3.length) errors.push("self-test failed to reject tampered evidence");
}
const m=JSON.parse(read(MANIFEST).toString("utf8"));
const evidencePath="docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/evidence/connected-285.json";
const c=JSON.parse(read(evidencePath).toString("utf8"));
const errors=[]; validateStructure(m,errors); validateEvidence(m,c,errors); validateSource(m,errors); selfTest(m,c,errors);
const summary={ok:errors.length===0,protocol:m.protocol,codeSha:m.codeSha,planCommit:m.planCommit,requirements:list(m.requirements).length,assertions:list(c.checks).length,selfTest:errors.filter(e=>e.startsWith("self-test")).length===0?"PASS":"FAIL",errors};
console.log(JSON.stringify(summary,null,2)); if(errors.length) process.exitCode=1;


