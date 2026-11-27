// Real source/guard oracles. No live source writes, Host/DB changes, or baseline edits.
const assert=require("node:assert/strict");
const fs=require("node:fs");
const os=require("node:os");
const path=require("node:path");
const {spawnSync}=require("node:child_process");
const repo=path.resolve(__dirname,"../../../..");
const BASE="295d8241d277f4320f90ee9e5e75e907dfa5905f";
const guardDir="src/frontend/lib/architecture";
const guards=["frontend-source-size.guard.test.ts","frontend-import-boundary.guard.test.ts","frontend-flat-folder.guard.test.ts","frontend-feature-boundary.guard.test.ts","frontend-test-discovery.guard.test.ts","seo-rendering.guard.test.ts"];
const baselineDirs=["docs/evidence/TB-TMAR-FE-BASELINE","docs/evidence/TB-TMAR-FE-F1"];
function command(exe,args,cwd=repo){
  const r=spawnSync(exe,args,{cwd,encoding:"utf8",timeout:180000,maxBuffer:32*1024*1024,windowsHide:true});
  if(r.error)throw r.error;
  return {command:[exe,...args],exit:r.status,stdout:r.stdout,stderr:r.stderr};
}
function checked(exe,args,cwd){const r=command(exe,args,cwd);assert.equal(r.exit,0,r.stderr||r.stdout);return r.stdout;}
function snapshot(root,sha,label){
  const target=path.join(root,label);fs.mkdirSync(target);
  const tar=path.join(root,label+".tar");
  checked("git",["archive","--format=tar","--output="+tar,sha,"src/frontend",...baselineDirs]);
  checked("tar",["-xf",tar,"-C",target]);fs.unlinkSync(tar);return target;
}
const driver="const {run}=require('node:test');const files=JSON.parse(process.argv[2]);const out=[];const stream=run({files,concurrency:1});for(const type of ['test:pass','test:fail'])stream.on(type,d=>{if(d.nesting===0){const e=d.details?.error;out.push({type,name:d.name,file:d.file,message:e?.cause?.message??e?.message??'',code:e?.cause?.code??e?.code??null});}});stream.on('end',()=>{process.stdout.write(JSON.stringify(out));process.exitCode=out.some(x=>x.type==='test:fail')?1:0;});stream.resume();";
function runGuards(root,names=guards){
  const files=names.map(n=>path.join(root,guardDir,n));files.forEach(f=>assert.ok(fs.existsSync(f),f));
  const runner=path.join(root,"p1c-node-guard-driver.cjs");
  if(!fs.existsSync(runner))fs.writeFileSync(runner,driver,{flag:"wx"});
  else assert.equal(fs.readFileSync(runner,"utf8"),driver);
  const result=command(process.execPath,["--experimental-strip-types",runner,JSON.stringify(files)],root);
  const cases=JSON.parse(result.stdout);
  assert.ok(cases.length>=names.length,"guard runner did not report all files");
  assert.ok(cases.every(c=>c.type==="test:pass"||c.type==="test:fail"));
  assert.equal(result.exit,cases.some(c=>c.type==="test:fail")?1:0);
  assert.equal(new Set(cases.map(c=>c.name)).size,cases.length,"duplicate test identity");
  return {cases,exit:result.exit,stderr:result.stderr};
}
function debt(result){
  return result.cases.filter(c=>c.type==="test:fail").map(c=>{
    assert.equal(c.code,"ERR_ASSERTION","non-assertion failure cannot be historical debt");
    const facts=c.message.split(/\r?\n/).map(x=>x.trim()).filter(x=>/^src\/frontend\/|^(OVERSIZED_GROWTH|NEW_OVERSIZED_FILE|BASELINE_ENTRY_MISSING_FILE) |^[A-Za-z][A-Za-z0-9_]*$/.test(x));
    assert.ok(facts.length,"unrecognized guard failure: "+c.name+" "+c.message);
    // Exact assertions including numeric LOC, paths, and exports; never just counts.
    return {name:c.name,facts:[...new Set(facts)].sort()};
  }).sort((a,b)=>a.name.localeCompare(b.name));
}
function compare(base,current){
  const baseNames=base.cases.map(c=>c.name).sort(),currentNames=current.cases.map(c=>c.name).sort();
  assert.deepEqual(currentNames,baseNames,"guard inventory changed; inspect separately, do not waive");
  const baseline=debt(base),candidate=debt(current);
  const allowed=new Map(baseline.map(x=>[x.name,new Set(x.facts)]));
  const novel=candidate.flatMap(x=>x.facts.filter(f=>!allowed.get(x.name)?.has(f)).map(f=>({name:x.name,fact:f})));
  const inScope=candidate.flatMap(x=>x.facts.filter(f=>/offer[-_]?workspace|offer[-_]?create|\/offers\/|\/vendor-panel\/products\/|admin-offer|seller-offer/i.test(f)).map(f=>({name:x.name,fact:f})));
  return {baseline,candidate,novel,inScope};
}
async function record(id,body){
  const evidenceRoot=process.env.P1C_EVIDENCE_DIR;assert.ok(evidenceRoot,"P1C_EVIDENCE_DIR required");
  fs.mkdirSync(evidenceRoot,{recursive:true});
  const evidence=path.resolve(evidenceRoot,id+".json");assert.ok(!fs.existsSync(evidence),"Do not overwrite evidence");
  const trace={id,base:BASE,status:"NOT_RUN"};const root=fs.mkdtempSync(path.join(os.tmpdir(),"tooba-p1c-guards-"));
  try{
    trace.candidate=checked("git",["rev-parse","HEAD"]).trim();
    // Require exact committed sources; no hidden working-tree fixes.
    const dirty=checked("git",["status","--porcelain","--","src/frontend",...baselineDirs]);assert.equal(dirty.trim(),"","Dirty frontend/guard baseline");
    const changes=checked("git",["diff","--name-only",BASE,trace.candidate,"--",guardDir,...baselineDirs]).trim();
    trace.guardChanges=changes;assert.equal(changes,"","Guard or baseline changed; independent review required");
    await body(trace,root);
    trace.status="PASS";trace.reason="Executed original guards with exact assertion identities and isolated cleanup";
  }catch(error){trace.status="FAIL";trace.reason=String(error?.stack??error);}
  finally{
    try{fs.rmSync(root,{recursive:true,force:true});trace.tempRemoved=!fs.existsSync(root);assert.equal(trace.tempRemoved,true);}
    catch(error){trace.status="FAIL";trace.reason+="; cleanup: "+String(error);}
    fs.writeFileSync(evidence,JSON.stringify(trace,null,2),{flag:"wx"});
  }
  return {status:trace.status,reason:trace.reason,evidence};
}
let cached=null;
function compareSnapshots(trace,root){
  if(cached?.sha===trace.candidate){Object.assign(trace,cached.observed);return trace.comparison;}
  const base=snapshot(root,BASE,"base"),candidate=snapshot(root,trace.candidate,"candidate");
  trace.baseResult=runGuards(base);trace.candidateResult=runGuards(candidate);
  trace.comparison=compare(trace.baseResult,trace.candidateResult);
  cached={sha:trace.candidate,observed:{baseResult:trace.baseResult,candidateResult:trace.candidateResult,comparison:trace.comparison}};
  return trace.comparison;
}
module.exports={
  "R10-10-P":()=>record("R10-10-P",(trace,root)=>{
    const c=compareSnapshots(trace,root);assert.deepEqual(c.novel,[],"new debt");assert.deepEqual(c.inScope,[],"offer-owned violation");
    trace.changedSource=checked("git",["diff","--name-only",BASE,trace.candidate,"--","src/frontend"]).trim().split(/\r?\n/);
    assert.ok(trace.changedSource.length&&trace.changedSource[0]);
  }),
  "R10-10-B":()=>record("R10-10-B",(trace,root)=>{
    const c=compareSnapshots(trace,root);assert.ok(c.baseline.length,"historic debt fixture missing");assert.deepEqual(c.novel,[],"new or increased debt");assert.deepEqual(c.inScope,[],"no waiver for Offer");
  }),
  "R10-10-N":()=>record("R10-10-N",(trace,root)=>{
    const target=snapshot(root,trace.candidate,"negative");
    const names=guards.slice(0,2);
    trace.before=runGuards(target,names);
    const rel="src/frontend/lib/p1c-forbidden-import-negative-control.ts";
    const file=path.join(target,rel);
    const baseline=JSON.parse(fs.readFileSync(path.join(target,baselineDirs[0],"frontend-source-size-baseline.json"),"utf8"));
    const threshold=baseline.thresholdNewFileLoc??800;
    assert.ok(Number.isInteger(threshold)&&threshold>0&&threshold<100000);
    assert.ok(!fs.existsSync(file));
    fs.writeFileSync(file,"import { absent } from '../app/admin/p1c-forbidden';\n"+("// negative control\n".repeat(threshold+2)),{flag:"wx"});
    try{
      trace.injected=runGuards(target,names);
      const failures=trace.injected.cases.filter(c=>c.type==="test:fail");
      assert.ok(failures.some(c=>c.name.startsWith("FE-SIZE:")&&c.message.includes("NEW_OVERSIZED_FILE "+rel)),"size guard did not reject exact control");
      assert.ok(failures.some(c=>c.name.startsWith("FE-BOUNDARY-001:")&&c.message.includes(rel+" -> ../app/admin/p1c-forbidden")),"boundary guard did not reject exact control");
    }finally{fs.unlinkSync(file);}
    assert.ok(!fs.existsSync(file));
    trace.after=runGuards(target,names);
    assert.deepEqual(debt(trace.after),debt(trace.before),"negative control altered baseline");
    assert.deepEqual(trace.after.cases.map(x=>[x.name,x.type]).sort(),trace.before.cases.map(x=>[x.name,x.type]).sort());
  })
};
