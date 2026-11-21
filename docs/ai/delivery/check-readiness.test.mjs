import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import crypto from "node:crypto";
import { checkReadiness } from "./check-readiness.mjs";

const hash = value => crypto.createHash("sha256").update(value).digest("hex");
function fixture() {
  const folder = fs.mkdtempSync(path.join(os.tmpdir(), "tooba-delivery-"));
  const task = "Task-ID: TB-GATE-TEST\nExpected-Base-SHA: " + "a".repeat(40) + "\n| REQ-01 | Save and independently reread |\n";
  const plan = "Architecture: existing form -> endpoint -> validator -> owning persistence -> DTO -> verifier -> independent GET. Wave W1 proves Save, Cancel and transport loss.";
  fs.writeFileSync(path.join(folder, "TASK.md"), task);
  fs.writeFileSync(path.join(folder, "ARCHITECTURE-PLAN.md"), plan);
  fs.writeFileSync(path.join(folder, "receipt.log"), "controlled fixture: save and reread observed\n");
  const manifest = { protocol: "TOOBA-DELIVERY-001", taskId: "TB-GATE-TEST", baseSha: "a".repeat(40),
    codeSha: "b".repeat(40), planCommit: "c".repeat(40), architecturePlan: "ARCHITECTURE-PLAN.md", architecturePlanSha256: hash(plan),
    waves: [{id:"W1",dependsOn:[],requirementIds:["REQ-01"],status:"VERIFIED"}],
    requirements: [{id:"REQ-01",status:"VERIFIED",scenarios:[{id:"S-01",kind:"controlled-component",
      steps:"Open existing item, change title, save, reread same id; cancel sends zero requests",status:"PASS",
      evidence:[{file:"receipt.log",kind:"fixture-log",provider:"CONTROLLED_TEST_ONLY",phase:"after",
        codeSha:"b".repeat(40),sha256:hash("controlled fixture: save and reread observed\n")}]}]}]};
  const write = () => fs.writeFileSync(path.join(folder, "DELIVERY.json"), JSON.stringify(manifest));
  write();
  return { folder, manifest, write, taskSha:hash(task), cleanup:()=>fs.rmSync(folder,{recursive:true,force:true}) };
}
function run(f, stage="review"){f.write();return checkReadiness(f.folder,{stage,taskSha:f.taskSha});}
test("plan may be unfinished, review refuses the same open scenario",()=>{
  const f=fixture();try{f.manifest.requirements[0].status="OPEN";f.manifest.requirements[0].scenarios[0].status="NOT_RUN";
    assert.equal(run(f,"plan").ok,true);assert.equal(run(f).ok,false);}finally{f.cleanup();}
});
test("complete controlled fixture is structurally reviewable, never product acceptance",()=>{
  const f=fixture();try{assert.equal(run(f).ok,true);}finally{f.cleanup();}
});
test("a dropped issued requirement fails even when the remaining report claims complete",()=>{
  const f=fixture();try{f.manifest.requirements=[];assert.equal(run(f).ok,false);}finally{f.cleanup();}
});
test("rewritten TASK bytes fail against architect-held hash",()=>{
  const f=fixture();try{fs.appendFileSync(path.join(f.folder,"TASK.md"),"changed scope\n");assert.equal(run(f).ok,false);}finally{f.cleanup();}
});
test("missing, altered, stale or outside-folder evidence cannot pass",()=>{
  for(const mode of ["missing","altered","stale","escape"]){const f=fixture();try{
    const e=f.manifest.requirements[0].scenarios[0].evidence[0];
    if(mode==="missing")fs.unlinkSync(path.join(f.folder,"receipt.log"));
    if(mode==="altered")fs.appendFileSync(path.join(f.folder,"receipt.log"),"changed");
    if(mode==="stale")e.codeSha="d".repeat(40);
    if(mode==="escape")e.file="../outside.log";
    assert.equal(run(f).ok,false,mode);
  }finally{f.cleanup();}}
});
test("dependency order/cycles and template plans cannot pass",()=>{
  const f=fixture();try{f.manifest.waves[0].dependsOn=["W2"];assert.equal(run(f,"plan").ok,false);
    f.manifest.waves[0].dependsOn=[];fs.writeFileSync(path.join(f.folder,"ARCHITECTURE-PLAN.md"),"REPLACE_WITH_PLAN");
    assert.equal(run(f,"plan").ok,false);}finally{f.cleanup();}
});
