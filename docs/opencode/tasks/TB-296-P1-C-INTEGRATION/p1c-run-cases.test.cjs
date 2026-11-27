const assert = require("node:assert/strict");
const test = require("node:test");
const {executeCases} = require("./p1c-run-cases.cjs");
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
