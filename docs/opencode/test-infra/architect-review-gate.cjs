#!/usr/bin/env node
"use strict";
// Structural architect gate: source/evidence provenance; not semantic certification.
function verify(r, ctx) {
  const errors=[], need=(ok,msg)=>{if(!ok)errors.push(msg);};
  const str=x=>typeof x==="string"&&x.trim().length>0;
  const sha=x=>typeof x==="string"&&/^[0-9a-f]{40}$/i.test(x);
  const arr=x=>Array.isArray(x)?x:[];
  const pathOK=p=>str(p)&&!p.startsWith("/")&&!p.includes("\\")&&!p.includes(":")&&!p.split("/").some(x=>x===".."||x==="."||!x);
  const index=(xs,key,label)=>{const m=new Map();for(const x of arr(xs)){const id=x?.[key];need(str(id)&&!m.has(id),label+" duplicate/missing "+id);m.set(id,x);}return m;};
  const blob=(ref,p)=>{try{return pathOK(p)?ctx.blob(ref,p):null;}catch{return null;}};
  const source=(x,label)=>{const ref=x?.revision==="base"?ctx.base:x?.revision==="candidate"?ctx.candidate:null;const b=ref&&blob(ref,x?.path);need(!!b&&x.blob===b,label+" invalid source/blob");try{const n=ctx.lines(ref,x.path);need(Number.isInteger(x.startLine)&&Number.isInteger(x.endLine)&&x.startLine>=1&&x.endLine>=x.startLine&&x.endLine<=n,label+" invalid source lines");}catch{errors.push(label+" unreadable source");}need(str(x?.finding),label+" missing observed finding");};
  const evidence=(x,label)=>{const b=blob(ctx.evidenceCommit,x?.path);need(!!b&&b===x.blob,label+" invalid evidence/blob");need(str(x?.observation),label+" missing observation");};
  need(r?.schemaVersion===1&&r?.reviewerRole==="ARCHITECT", "architect receipt required");
  need(str(r?.taskId)&&r.taskId===ctx.taskId,"task mismatch");
  need(sha(ctx.base)&&sha(ctx.candidate)&&sha(ctx.evidenceCommit)&&r.base===ctx.base&&r.candidate===ctx.candidate&&r.evidenceCommit===ctx.evidenceCommit,"revision mismatch");
  need(ctx.isAncestor(ctx.base,ctx.candidate)&&ctx.isAncestor(ctx.candidate,ctx.evidenceCommit),"invalid ancestry");
  need(ctx.changedAfter.length===0,"candidate changed after test: "+ctx.changedAfter.join(", "));
  need(r.decision==="ACCEPT","decision is not ACCEPT");
  const reviewed=index(r.reviewedFiles,"path","reviewed file");
  for(const p of ctx.changed){const x=reviewed.get(p);need(!!x,"unreviewed changed path "+p);if(x){need(x.beforeBlob===blob(ctx.base,p)&&x.afterBlob===blob(ctx.candidate,p),"file revision mismatch "+p);need(str(x.observation),"missing file observation "+p);}}
  for(const p of reviewed.keys())need(ctx.changed.includes(p),"unexpected reviewed file "+p);
  need(ctx.changed.length>0,"empty candidate diff");
  const backend=ctx.changed.some(p=>p.startsWith("src/backend/")),frontend=ctx.changed.some(p=>p.startsWith("src/frontend/"));
  const mandatory=new Set(["AQ-01","AQ-06","AQ-07","AQ-08",...(backend?["AQ-02","AQ-04"]:[]),...(frontend?["AQ-03","AQ-05"]:[])]);
  const checks=index(r.checks,"id","AQ");
  for(let i=1;i<=8;i++){const id="AQ-0"+i,g=checks.get(id);need(!!g,"missing "+id);if(!g)continue;need(g.status==="PASS"||(!mandatory.has(id)&&g.status==="NOT_APPLICABLE"),id+" unresolved or invalid waiver");need(str(g.reason),id+" missing reasoning");need(arr(g.sourceRefs).length>0,id+" missing source refs");for(const s of arr(g.sourceRefs))source(s,id);if(g.status==="PASS"){need(arr(g.evidenceRefs).length>0,id+" missing evidence");for(const e of arr(g.evidenceRefs))evidence(e,id);}}
  need(checks.size===8,"unexpected AQ IDs");
  for(const k of ["build","architectureGuards","openSpec","acceptanceVerifier","cleanup"]){const e=r.validation?.[k];need(e?.status==="PASS",k+" not PASS");if(e)evidence(e,k);}
  const tests=index(r.tests,"testId","test");
  for(const id of ctx.testIds){const t=tests.get(id);need(t?.status==="PASS","mandatory test missing/not PASS "+id);if(t)evidence(t,id);}
  for(const id of tests.keys())need(ctx.testIds.includes(id),"unknown test ID "+id);
  need(ctx.testIds.length>0,"no mandatory test IDs");
  need(Array.isArray(r.findings),"findings ledger required");
  const findings=index(r.findings,"id","finding");
  for(const [id,f] of findings){need(["FIXED","UNRELATED_BASELINE"].includes(f.disposition),id+" unresolved finding");need(str(f.rootCause)&&str(f.prevention)&&str(f.owner),id+" missing recurrence control");if(f.disposition==="FIXED"){need(f.owner==="ARCHITECT"&&sha(f.repairCommit)&&ctx.isAncestor(f.repairCommit,ctx.candidate),id+" repair provenance missing");}else{need(str(f.baselineReason),id+" baseline reason missing");}need(arr(f.sourceRefs).length>0,id+" missing source");for(const x of arr(f.sourceRefs))source(x,id);need(arr(f.evidenceRefs).length>0,id+" missing proof");for(const x of arr(f.evidenceRefs))evidence(x,id);}
  return errors;
}
function selfTest(){
  const a="a".repeat(40),b="b".repeat(40),c="c".repeat(40),d="d".repeat(40),p="src/backend/example.cs",q="docs/opencode/tasks/sample/evidence/log.txt";
  const source={revision:"candidate",path:p,blob:d,startLine:1,endLine:2,finding:"observed owner boundary"};
  const ev={path:q,blob:d,observation:"actual command exit and output"};
  const ctx={base:a,candidate:b,evidenceCommit:c,taskId:"sample",changed:[p],changedAfter:[],testIds:["T1"],isAncestor:()=>true,blob:()=>d,lines:()=>2};
  const r={schemaVersion:1,reviewerRole:"ARCHITECT",taskId:"sample",base:a,candidate:b,evidenceCommit:c,decision:"ACCEPT",reviewedFiles:[{path:p,beforeBlob:d,afterBlob:d,observation:"actual diff reviewed"}],checks:Array.from({length:8},(_,i)=>({id:"AQ-0"+(i+1),status:"PASS",reason:"actual trace",sourceRefs:[source],evidenceRefs:[ev]})),validation:Object.fromEntries(["build","architectureGuards","openSpec","acceptanceVerifier","cleanup"].map(k=>[k,{...ev,status:"PASS"}])),tests:[{...ev,testId:"T1",status:"PASS"}],findings:[]};
  const cases=[["missing gate",v=>v.checks.pop()],["worker self approval",v=>v.reviewerRole="WORKER"],["unreviewed diff",v=>v.reviewedFiles=[]],["wrong source",v=>v.checks[0].sourceRefs[0].blob=a],["invalid line",v=>v.checks[0].sourceRefs[0].endLine=100],["locale waiver",v=>v.checks[0].status="NOT_APPLICABLE"],["not run",v=>v.checks[1].status="NOT_RUN"],["missing test",v=>v.tests=[]],["failed build",v=>v.validation.build.status="FAIL"],["fake evidence",v=>v.validation.build.blob=a],["unclosed finding",v=>v.findings=[{id:"F1",disposition:"OPEN"}]],["wrong candidate",v=>v.candidate=a],["worker repair",v=>v.findings=[{id:"F1",disposition:"FIXED",owner:"WORKER",repairCommit:b,rootCause:"test omitted",prevention:"negative control",sourceRefs:[source],evidenceRefs:[ev]}]]];
  if(verify(r,ctx).length)throw Error("positive fixture failed "+verify(r,ctx));
  for(const [name,fn] of cases){const copy=JSON.parse(JSON.stringify(r));fn(copy);if(!verify(copy,ctx).length)throw Error("negative control accepted "+name);}
  if(!verify(r,{...ctx,changedAfter:[p]}).length)throw Error("post-test mutation accepted");
  return "PASS: positive fixture + "+(cases.length+1)+" negative controls";
}

const fs=require("node:fs"), cp=require("node:child_process");
const git=(...args)=>cp.execFileSync("git",args,{encoding:"utf8",maxBuffer:32*1024*1024,stdio:["ignore","pipe","pipe"]});
const read=p=>JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
const isSha=s=>/^[a-f0-9]{40}$/i.test(s||"");
const changed=(a,b)=>git("diff","--no-renames","--name-only","-z",a,b,"--").split("\0").filter(p=>p&&!p.startsWith("docs/opencode/tasks/"));
const isAncestor=(a,b)=>{try{git("merge-base","--is-ancestor",a,b);return true;}catch{return false;}};
function main(){
 if(process.argv[2]==="--self-test"){console.log(selfTest());return;}
 const [taskId,base,candidate,evidenceCommit,receipt,...matrixPaths]=process.argv.slice(2);
 if(!taskId||![base,candidate,evidenceCommit].every(isSha)||!receipt||!matrixPaths.length)throw Error("Usage: node architect-review-gate.cjs TASK BASE_SHA CANDIDATE_SHA EVIDENCE_SHA RECEIPT MATRIX [MATRIX...]");
 const r=read(receipt), testIds=[];
 for(const p of matrixPaths){const m=read(p);if(!Array.isArray(m.tests)||!m.tests.length)throw Error("empty matrix "+p);for(const t of m.tests){if(typeof t.testId!=="string"||!t.testId||testIds.includes(t.testId))throw Error("invalid/duplicate test ID "+t.testId);testIds.push(t.testId);}}
 const ctx={taskId,base,candidate,evidenceCommit,changed:changed(base,candidate),changedAfter:changed(candidate,evidenceCommit),testIds,isAncestor,
 blob:(ref,p)=>{try{return git("rev-parse","--verify",ref+":"+p).trim();}catch{return null;}},
 lines:(ref,p)=>git("show",ref+":"+p).split("\n").length};
 const errors=verify(r,ctx);
 console.log(JSON.stringify({ok:!errors.length,taskId,base,candidate,evidenceCommit,reviewedPaths:ctx.changed.length,mandatoryTests:testIds.length,errors},null,2));
 if(errors.length)process.exitCode=1;
}
try{main();}catch(e){console.error(e.message);process.exitCode=2;}
