/**
 * Recovery SoT staleness guard (TB-P07-T037).
 * Deterministic, repo-local — does NOT call Bridge API.
 *
 * Authoritative files:
 *   - docs/ai/TOOBA-RECOVERY-CONTEXT.md
 *   - docs/PROJECT-STATE.md
 *
 * Comparison model:
 *   REQUIRED_MARKERS must appear in both files. CURRENT_TASK_ID is the Bridge task
 *   under implementation; STALE_MARKERS must NOT be the sole "Current Issued/Repair"
 *   pointers when CURRENT_TASK_ID is active.
 *
 * Failure shape:
 *   node:test assertion error naming the missing/stale marker (e.g. recovery missing
 *   TB-P07-T037, or PROJECT-STATE Current Issued Task still stuck on TB-P07-T020-R1).
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const recoveryPath = path.join(root, "docs/ai/TOOBA-RECOVERY-CONTEXT.md");
const statePath = path.join(root, "docs/PROJECT-STATE.md");

/** Active Bridge task under implementation (update when Architect issues next). */
const CURRENT_TASK_ID = "TB-P07-T037";

/** Markers that must appear for the active wave. */
const REQUIRED_MARKERS = [
  "P07",
  "TB-P07-T035",
  "TB-P07-T036",
  "TB-P07-T036-R1",
  CURRENT_TASK_ID,
  "USER_VISUAL_ACCEPTED",
  "BRIDGE-WAKE-V1",
];

/** Historical IDs that must not remain as Current Issued / Current Repair. */
const STALE_CURRENT_POINTERS = [
  "TB-P06-T029",
  "TB-P07-T020-R1",
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

test("recovery SoT current issued/repair points at TB-P07-T037 not stale P06/T020", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);

  assert.match(recovery, /TB-P07-T037/);
  assert.match(state, /TB-P07-T037/);

  assert.match(
    state,
    /Current Issued Task:\s*```text\s*TB-P07-T037\s*```/,
    "PROJECT-STATE Current Issued Task must be TB-P07-T037",
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
  // Repo-local smoke: REQUIRED_MARKERS includes CURRENT_TASK_ID so a stale checkout
  // that only mentions T020/T029 cannot PASS this suite.
  assert.ok(REQUIRED_MARKERS.includes(CURRENT_TASK_ID));
  assert.ok(STALE_CURRENT_POINTERS.includes("TB-P06-T029"));
  assert.ok(STALE_CURRENT_POINTERS.includes("TB-P07-T020-R1"));
});
