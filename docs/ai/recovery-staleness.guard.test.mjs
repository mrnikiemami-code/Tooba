/**
 * Recovery SoT staleness guard (TB-P10-T004).
 * Deterministic, repo-local — does NOT call Bridge API.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const recoveryPath = path.join(root, "docs/ai/TOOBA-RECOVERY-CONTEXT.md");
const statePath = path.join(root, "docs/PROJECT-STATE.md");

const CURRENT_TASK_ID = "TB-P10-T004";

const REQUIRED_MARKERS = [
  "P10",
  "TB-P10-T001",
  "TB-P10-T002",
  "TB-P10-T002-R1",
  "TB-P10-T003",
  "TB-P10-T003-R1",
  "TB-P10-T004",
  "USER_VISUAL_ACCEPTED",
  "BRIDGE-WAKE-V1",
];

const STALE_CURRENT_POINTERS = [
  "TB-P10-T001",
  "TB-P10-T002",
  "TB-P10-T002-R1",
  "TB-P10-T003",
  "TB-P10-T003-R1",
];

function read(p) {
  return fs.readFileSync(p, "utf8");
}

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

test("recovery SoT files exist", () => {
  for (const p of [recoveryPath, statePath]) assert.ok(fs.existsSync(p), `missing SoT file: ${p}`);
});

test("recovery SoT contains required historical markers", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  for (const marker of REQUIRED_MARKERS) {
    assert.match(recovery, new RegExp(escapeRe(marker)), `recovery missing ${marker}`);
    assert.match(state, new RegExp(escapeRe(marker)), `PROJECT-STATE missing ${marker}`);
  }
});

test("recovery SoT points Architect at TB-P10-T003-R1; Impl TB-P10-T004; Phase P10", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  assert.match(recovery, /P10/);
  assert.match(state, /P10/);
  assert.match(state, /Last Architect Accepted Task:\s*```text\s*TB-P10-T003-R1\s*```/);
  assert.match(state, /Last Architect-Accepted Task:\s*```text\s*TB-P10-T003-R1\s*```/);
  assert.match(recovery, /Last Architect Accepted Task:\s*```text\s*TB-P10-T003-R1\s*```/);
  assert.match(state, /Last Implementation Task:\s*```text\s*TB-P10-T004\s*```/);
  assert.match(recovery, /Last Implementation Task:\s*```text\s*TB-P10-T004\s*```/);
  assert.match(state, /Current Issued Task:\s*```text\s*\(none\)\s*```/);
  assert.match(state, /Current Repair Task:\s*```text\s*\(none\)\s*```/);
  assert.match(recovery, /Current Issued Task:\s*```text\s*\(none\)\s*```/);
  assert.match(recovery, /Current Repair Task:\s*```text\s*\(none\)\s*```/);
  assert.match(state, /USER_VISUAL_ACCEPTED:\s*```text\s*NO\s*```/);
  assert.match(recovery, /USER_VISUAL_ACCEPTED:\s*```text\s*NO\s*```/);
  assert.doesNotMatch(state, /Current Issued Task:\s*```text\s*TB-P10-T005\s*```/);
  assert.match(state, /do NOT invent TB-P10-T005/);
  assert.match(recovery, /do NOT invent TB-P10-T005/);
  assert.equal(CURRENT_TASK_ID, "TB-P10-T004");
});

test("recovery SoT does not leave stale tasks as Last Implementation", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  const implBlock = (text) => {
    const m = text.match(/Last Implementation Task:\s*```text\s*([^\s`]+)\s*```/);
    assert.ok(m, "missing Last Implementation Task block");
    return m[1];
  };
  const implRecovery = implBlock(recovery);
  const implState = implBlock(state);
  assert.equal(implRecovery, "TB-P10-T004");
  assert.equal(implState, "TB-P10-T004");
  for (const stale of STALE_CURRENT_POINTERS) {
    assert.notEqual(implRecovery, stale);
    assert.notEqual(implState, stale);
  }
});
