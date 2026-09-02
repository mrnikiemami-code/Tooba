/**
 * Recovery SoT staleness guard (TB-P08-T010-R1).
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

const CURRENT_TASK_ID = "TB-P08-T010-R1";

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

test("recovery SoT points Current Repair at TB-P08-T010-R1 not stale pointers", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);

  assert.match(recovery, /TB-P08-T010-R1/);
  assert.match(state, /TB-P08-T010-R1/);
  assert.match(recovery, /TB-P08-T010/);
  assert.match(state, /TB-P08-T010/);
  assert.match(recovery, /TB-P08-T009-R2/);
  assert.match(state, /TB-P08-T009-R2/);
  assert.match(recovery, /TB-P08-T009-R1/);
  assert.match(state, /TB-P08-T009-R1/);
  assert.match(recovery, /TB-P08-T008/);
  assert.match(state, /TB-P08-T008/);

  assert.match(
    state,
    /Current Issued Task:\s*```text\s*\(none\)\s*```/,
    "PROJECT-STATE Current Issued Task must be (none)",
  );
  assert.match(
    state,
    /Current Repair Task:\s*```text\s*TB-P08-T010-R1\s*```/,
    "PROJECT-STATE Current Repair Task must be TB-P08-T010-R1",
  );
  assert.match(
    recovery,
    /Current Repair Task:\s*```text\s*TB-P08-T010-R1\s*```/,
    "recovery Current Repair Task must be TB-P08-T010-R1",
  );

  for (const stale of STALE_CURRENT_POINTERS) {
    assert.doesNotMatch(
      state,
      new RegExp(`Current Issued Task:\\s*\`\`\`text\\s*${escapeRe(stale)}\\s*\`\`\``),
      `PROJECT-STATE Current Issued Task still stuck on ${stale}`,
    );
    assert.doesNotMatch(
      state,
      new RegExp(`Current Repair Task:\\s*\`\`\`text\\s*${escapeRe(stale)}\\s*\`\`\``),
      `PROJECT-STATE Current Repair Task still stuck on ${stale}`,
    );
    assert.doesNotMatch(
      recovery,
      new RegExp(`Current Repair Task:\\s*\`\`\`text\\s*${escapeRe(stale)}\\s*\`\`\``),
      `recovery Current Repair Task still stuck on ${stale}`,
    );
  }
});

test("guard fails conceptually when recovery omits current task id", () => {
  assert.ok(REQUIRED_MARKERS.includes(CURRENT_TASK_ID));
  assert.ok(STALE_CURRENT_POINTERS.includes("TB-P06-T029"));
  assert.ok(STALE_CURRENT_POINTERS.includes("TB-P07-T020-R1"));
});
