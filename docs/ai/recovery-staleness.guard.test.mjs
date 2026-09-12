import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const recoveryPath = path.join(root, "docs/ai/TOOBA-RECOVERY-CONTEXT.md");
const statePath = path.join(root, "docs/PROJECT-STATE.md");
const CURRENT_TASK_ID = "TB-P10-T004-R11";
const REQUIRED_MARKERS = ["P10","TB-P10-T004-R5","TB-P10-T004-R6","TB-P10-T004-R7","TB-P10-T004-R8","TB-P10-T004-R9","TB-P10-T004-R10","TB-P10-T004-R11","USER_VISUAL_ACCEPTED","BRIDGE-WAKE-V1"];
const STALE = ["TB-P10-T004","TB-P10-T004-R1","TB-P10-T004-R4","TB-P10-T004-R5","TB-P10-T004-R6","TB-P10-T004-R7","TB-P10-T004-R8","TB-P10-T004-R9","TB-P10-T004-R10"];
const read = (p) => fs.readFileSync(p, "utf8");
const escapeRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

test("recovery SoT files exist", () => {
  for (const p of [recoveryPath, statePath]) assert.ok(fs.existsSync(p));
});
test("recovery SoT contains required historical markers", () => {
  const recovery = read(recoveryPath); const state = read(statePath);
  for (const marker of REQUIRED_MARKERS) {
    assert.match(recovery, new RegExp(escapeRe(marker)));
    assert.match(state, new RegExp(escapeRe(marker)));
  }
});
test("recovery SoT points Architect at TB-P10-T004-R10; Impl TB-P10-T004-R11; Phase P10", () => {
  const recovery = read(recoveryPath); const state = read(statePath);
  assert.match(state, /Last Architect Accepted Task:\s*```text\s*TB-P10-T004-R10\s*```/);
  assert.match(recovery, /Last Architect Accepted Task:\s*```text\s*TB-P10-T004-R10\s*```/);
  assert.match(state, /Last Implementation Task:\s*```text\s*TB-P10-T004-R11\s*```/);
  assert.match(recovery, /Last Implementation Task:\s*```text\s*TB-P10-T004-R11\s*```/);
  assert.match(state, /Current Issued Task:\s*```text\s*\(none\)\s*```/);
  assert.match(state, /USER_VISUAL_ACCEPTED:\s*```text\s*NO\s*```/);
  assert.doesNotMatch(state, /Current Issued Task:\s*```text\s*TB-P10-T005\s*```/);
  assert.match(state, /do NOT invent TB-P10-T005/);
  assert.equal(CURRENT_TASK_ID, "TB-P10-T004-R11");
});
test("recovery SoT does not leave stale tasks as Last Implementation", () => {
  const impl = (t) => t.match(/Last Implementation Task:\s*```text\s*([^\s`]+)\s*```/)[1];
  const a = impl(read(recoveryPath)); const b = impl(read(statePath));
  assert.equal(a, "TB-P10-T004-R11"); assert.equal(b, "TB-P10-T004-R11");
  for (const s of STALE) { assert.notEqual(a, s); assert.notEqual(b, s); }
});
