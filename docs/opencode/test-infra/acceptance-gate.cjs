#!/usr/bin/env node
"use strict";
// OpenCode isolated governance. Structural/provenance gate, not a semantic oracle.
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const assert = require("node:assert/strict");
const hash = b => crypto.createHash("sha256").update(b).digest("hex");
const read = p => JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
const validHash = x => typeof x === "string" && /^[a-f0-9]{64}$/i.test(x);
const scenarioHash = t => hash(JSON.stringify(t));
function verify(matrix, plan, run, loadBytes) {
  const errors = [];
  const need = (ok, msg) => { if (!ok) errors.push(msg); };
  const index = (items, key, label) => {
    const m = new Map();
    for (const x of items || []) {
      const id = x[key];
      need(typeof id === "string" && id.length > 0, label + " missing ID");
      need(!m.has(id), label + " duplicate " + id);
      m.set(id,x);
    }
    return m;
  };
  const expected = index(matrix.tests,"testId","matrix");
  const planned = index(plan.tests,"testId","plan");
  const actual = index(run.tests,"testId","result");
  need(expected.size > 0, "empty matrix");
  need(matrix.taskId === plan.taskId && plan.taskId === run.taskId,"task mismatch");
  need(/^[a-f0-9]{40}$/i.test(run.sourceCommit || ""),"missing full source commit");
  need(plan.sourceCommit === run.sourceCommit,"source mismatch");
  need(validHash(run.hostSha256) && typeof run.nextBuildId === "string" && run.nextBuildId.length > 0,"missing build identity");
  need(run.database === "tooba_alpha","unexpected database");
  need(run.testBuildCommit === run.sourceCommit && run.testBuildSucceeded === true,"test build not bound to source");
  const seenFiles = new Set();
  for (const [id,t] of expected) {
    const p = planned.get(id), a = actual.get(id);
    if (!p || !a) { errors.push("missing plan/result " + id); continue; }
    need(p.scenarioSha256 === scenarioHash(t) && a.scenarioSha256 === p.scenarioSha256,"scenario mismatch " + id);
    need(Array.isArray(p.assertionIds) && p.assertionIds.length > 0 && new Set(p.assertionIds).size === p.assertionIds.length,"invalid assertion plan " + id);
    const facts = index(a.assertions,"assertionId",id);
    for (const aid of p.assertionIds || []) {
      const f = facts.get(aid);
      need(f && f.pass === true && Object.hasOwn(f,"expected") && Object.hasOwn(f,"observed"),"missing/failed fact " + id + "/" + aid);
    }
    need(facts.size === (p.assertionIds || []).length,"unexpected assertion " + id);
    need(a.status === "PASS" && a.executed === true,"not executed/pass " + id);
    need(a.sourceCommit === run.sourceCommit && a.hostSha256 === run.hostSha256 && a.nextBuildId === run.nextBuildId,"stale per-test identity " + id);
    need(typeof a.evidencePath === "string" && !seenFiles.has(a.evidencePath),"missing/reused evidence " + id);
    seenFiles.add(a.evidencePath);
    try {
      const bytes = loadBytes(a.evidencePath);
      need(validHash(a.evidenceSha256) && hash(bytes) === a.evidenceSha256.toLowerCase(),"hash mismatch " + id);
      const raw = JSON.parse(bytes.toString("utf8").replace(/^\uFEFF/,""));
      need(raw.testId === id && raw.scenarioSha256 === a.scenarioSha256 && raw.sourceCommit === a.sourceCommit,"evidence identity mismatch " + id);
      need(JSON.stringify(raw.assertions) === JSON.stringify(a.assertions),"evidence facts mismatch " + id);
    } catch(e) { errors.push("unreadable evidence " + id + ": " + e.message); }
  }
  for (const id of [...planned.keys(),...actual.keys()]) need(expected.has(id),"unknown ID " + id);
  return errors;
}
function selfTest() {
  const matrix = {taskId:"sample",tests:[{testId:"T-1",expected:"winner value"}]};
  const s = "a".repeat(40), h = "b".repeat(64), scenario = scenarioHash(matrix.tests[0]);
  const facts = [{assertionId:"winner",pass:true,expected:10,observed:10}];
  const plan = {taskId:"sample",sourceCommit:s,tests:[{testId:"T-1",scenarioSha256:scenario,assertionIds:["winner"]}]};
  const evidence = Buffer.from(JSON.stringify({testId:"T-1",scenarioSha256:scenario,sourceCommit:s,assertions:facts}));
  const run = {taskId:"sample",sourceCommit:s,hostSha256:h,nextBuildId:"next",database:"tooba_alpha",testBuildCommit:s,testBuildSucceeded:true,tests:[{testId:"T-1",scenarioSha256:scenario,sourceCommit:s,hostSha256:h,nextBuildId:"next",status:"PASS",executed:true,assertions:facts,evidencePath:"T-1.json",evidenceSha256:hash(evidence)}]};
  const check = (v,p=plan,bytes=evidence) => verify(matrix,p,v,()=>bytes);
  assert.deepEqual(check(run),[]);
  const cases = [
    ["missing ID",v=>v.tests=[]],
    ["duplicate ID",v=>v.tests.push(v.tests[0])],
    ["unknown ID",v=>v.tests[0].testId="OTHER"],
    ["stale build",v=>v.testBuildCommit="c".repeat(40)],
    ["wrong fact",v=>v.tests[0].assertions[0].observed=20],
    ["failed fact",v=>v.tests[0].assertions[0].pass=false],
    ["hash tamper",v=>v.tests[0].evidenceSha256="0".repeat(64)],
    ["scenario substitution",v=>v.tests[0].scenarioSha256="0".repeat(64)],
    ["not executed",v=>v.tests[0].executed=false]
  ];
  for (const [name,mutate] of cases) {
    const v=JSON.parse(JSON.stringify(run)); mutate(v);
    assert.ok(check(v).length>0,name);
  }
  assert.ok(check(run,plan,Buffer.from("{}")).length>0,"changed evidence bytes");
  console.log("PASS: positive fixture + 10 negative controls");
}
if (process.argv[2] === "--self-test") selfTest();
else if (process.argv.length === 6) {
  const [matrixPath,planPath,runPath,root] = process.argv.slice(2);
  const errors=verify(read(matrixPath),read(planPath),read(runPath),p=>{
    const base=fs.realpathSync(root), target=fs.realpathSync(path.resolve(base,p));
    if (!target.startsWith(base+path.sep)) throw new Error("evidence outside root");
    return fs.readFileSync(target);
  });
  console.log(JSON.stringify({ok:errors.length===0,errors},null,2));
  if(errors.length) process.exitCode=1;
} else { console.error("Usage: node acceptance-gate.cjs --self-test OR MATRIX PLAN RUN EVIDENCE_ROOT"); process.exitCode=2; }
