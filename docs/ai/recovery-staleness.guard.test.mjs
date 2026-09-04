/**
 * Recovery SoT staleness guard (TB-P08-T016-R2).
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

const CURRENT_TASK_ID = "TB-P08-T016-R3";

const REQUIRED_MARKERS = [
  "P08",
  "P07",
  "TB-P07-T035",
  "TB-P07-T036",
  "TB-P07-T036-R1",
  "TB-P07-T037",
  "TB-P07-T039",
  "TB-P07-T041",
  "TB-P07-T042-R1",
  "TB-P07-T043",
  "TB-P08-T001",
  "TB-P08-T001-R1",
  "TB-P08-T001-R2",
  "TB-P08-T002",
  "TB-P08-T003",
  "TB-P08-T004",
  "TB-P08-T007-R1",
  "TB-P08-T007-R2",
  "TB-P08-T008",
  "TB-P08-T009",
  "TB-P08-T009-R1",
  "TB-P08-T009-R2",
  "TB-P08-T010",
  "TB-P08-T010-R1",
  "TB-P08-T011",
  "TB-P08-T012",
  "TB-P08-T012-R1",
  "TB-P08-T013",
  "TB-P08-T014",
  "TB-P08-T015",
  "TB-P08-T016",
  "TB-P08-T016-R1",
  "TB-P08-T016-R2",
  "TB-P08-T016-R3",
  "USER_VISUAL_ACCEPTED",
  "BRIDGE-WAKE-V1",
];

const STALE_CURRENT_POINTERS = [
  "TB-P06-T029",
  "TB-P07-T020-R1",
  "TB-P07-T036-R1",
  "TB-P07-T040",
  "TB-P07-T041",
  "TB-P07-T041-R1",
  "TB-P07-T042",
  "TB-P07-T043",
  "TB-P08-T009-R1",
  "TB-P08-T009-R2",
  "TB-P08-T010",
  "TB-P08-T010-R1",
  "TB-P08-T011",
  "TB-P08-T012",
  "TB-P08-T012-R1",
  "TB-P08-T013",
  "TB-P08-T014",
  "TB-P08-T015",
  "TB-P08-T016",
  "TB-P08-T016-R1",
  "TB-P08-T016-R2",
];

function read(p) {
  assert.ok(fs.existsSync(p), `missing SoT file: ${p}`);
  return fs.readFileSync(p, "utf8");
}

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

test("recovery SoT files exist and contain current task markers", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  for (const marker of REQUIRED_MARKERS) {
    assert.match(recovery, new RegExp(escapeRe(marker)), `recovery missing ${marker}`);
    assert.match(state, new RegExp(escapeRe(marker)), `PROJECT-STATE missing ${marker}`);
  }
});

test("recovery SoT points Last Implementation at TB-P08-T016-R3; Architect T015; Issued/Repair none", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);

  assert.match(recovery, /TB-P08-T016-R3/);
  assert.match(state, /TB-P08-T016-R3/);
  assert.match(recovery, /TB-P08-T015/);
  assert.match(state, /TB-P08-T015/);

  assert.match(
    state,
    /Last Architect Accepted Task:\s*```text\s*TB-P08-T015\s*```/,
    "PROJECT-STATE Last Architect Accepted Task must be TB-P08-T015",
  );
  assert.match(
    state,
    /Last Architect-Accepted Task:\s*```text\s*TB-P08-T015\s*```/,
    "PROJECT-STATE Last Architect-Accepted Task must be TB-P08-T015",
  );
  assert.match(
    recovery,
    /Last Architect Accepted Task:\s*```text\s*TB-P08-T015\s*```/,
    "recovery Last Architect Accepted Task must be TB-P08-T015",
  );
  assert.match(
    state,
    /Last Implementation Task:\s*```text\s*TB-P08-T016-R3\s*```/,
    "PROJECT-STATE Last Implementation Task must be TB-P08-T016-R3",
  );
  assert.match(
    recovery,
    /Last Implementation Task:\s*```text\s*TB-P08-T016-R3\s*```/,
    "recovery Last Implementation Task must be TB-P08-T016-R3",
  );
  assert.match(
    state,
    /Current Issued Task:\s*```text\s*\(none\)\s*```/,
    "PROJECT-STATE Current Issued Task must be (none)",
  );
  assert.match(
    state,
    /Current Repair Task:\s*```text\s*\(none\)\s*```/,
    "PROJECT-STATE Current Repair Task must be (none)",
  );
  assert.match(
    recovery,
    /Current Issued Task:\s*```text\s*\(none\)\s*```/,
    "recovery Current Issued Task must be (none)",
  );
  assert.match(
    recovery,
    /Current Repair Task:\s*```text\s*\(none\)\s*```/,
    "recovery Current Repair Task must be (none)",
  );
  assert.match(state, /USER_VISUAL_ACCEPTED:\s*```text\s*NO\s*```/);
  assert.match(recovery, /USER_VISUAL_ACCEPTED:\s*```text\s*NO\s*```/);
  assert.equal(CURRENT_TASK_ID, "TB-P08-T016-R3");
  assert.doesNotMatch(state, /TB-P08-T017/);
  assert.doesNotMatch(recovery, /TB-P08-T017/);
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
  assert.equal(implRecovery, "TB-P08-T016-R3");
  assert.equal(implState, "TB-P08-T016-R3");
  for (const stale of STALE_CURRENT_POINTERS) {
    assert.notEqual(implRecovery, stale, `recovery Last Implementation must not be stale ${stale}`);
    assert.notEqual(implState, stale, `PROJECT-STATE Last Implementation must not be stale ${stale}`);
  }
});
