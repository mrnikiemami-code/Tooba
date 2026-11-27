const assert = require("node:assert/strict");
const test = require("node:test");
const {executeCases,coverage} = require("./p1c-run-cases.cjs");
test("a failed browser scenario cannot stop independent cases; missing oracles cannot PASS",async()=>{
  const snapshots=[];
  const rows=await executeCases(["a","b","c","d"],{
    a:async()=>{throw Error("unexpected dialog")},
    b:async()=>({status:"PASS",reason:"mock execution",evidence:"mock-only"}),
    d:async()=>({status:"PASS"})
  },async rows=>snapshots.push(JSON.parse(JSON.stringify(rows))));
  assert.deepEqual(rows.map(x=>x.status),["FAIL","PASS","NOT_RUN","FAIL"]);
  assert.equal(snapshots.length,5);
  assert.match(rows[0].reason,/unexpected dialog/);
  assert.equal(rows[2].reason,"MISSING_EXECUTABLE_ORACLE");
});

test("coverage inventories missing IDs and rejects duplicate, unknown and non-callable oracles",()=>{
  const fn=async()=>({status:"PASS"});
  assert.deepEqual(coverage(["A","B"],[{A:fn}]).missing,["B"]);
  assert.throws(()=>coverage(["A"],[{A:fn},{A:fn}]),/Duplicate/);
  assert.throws(()=>coverage(["A"],[{B:fn}]),/Unknown/);
  assert.throws(()=>coverage(["A"],[{A:"PASS"}]),/Non-callable/);
});
test("targeted execution never reruns other cases and rejects invalid selection before persistence",async()=>{
  let calls=0;let writes=0;
  const f=async()=>{calls++;return {status:"PASS",reason:"mock",evidence:"mock"}};
  const rows=await executeCases(["A","B"],{A:f,B:f},async()=>{writes++},["B"]);
  assert.equal(calls,1);assert.equal(rows[0].status,"NOT_RUN");assert.equal(rows[1].status,"PASS");
  const prev=writes;
  await assert.rejects(executeCases(["A"],{A:f},async()=>{writes++},["A","A"]),/Invalid/);
  assert.equal(writes,prev);
});
