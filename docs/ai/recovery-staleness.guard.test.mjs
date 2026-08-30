/**
 * Recovery SoT staleness guard (TB-P07-T036-R1 §G).
 * Deterministic, repo-local — does NOT call Bridge API.
 *
 * Ensures TOOBA-RECOVERY-CONTEXT.md + PROJECT-STATE.md mention the current
 * implemented/repair task markers so a PASS cannot leave recovery many tasks behind.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const recoveryPath = path.join(root, "docs/ai/TOOBA-RECOVERY-CONTEXT.md");
const statePath = path.join(root, "docs/PROJECT-STATE.md");

/** Markers that must appear for the active repair wave. Update when Architect accepts. */
const REQUIRED_MARKERS = [
  "P07",
  "TB-P07-T035",
  "TB-P07-T036",
  "TB-P07-T036-R1",
  "USER_VISUAL_ACCEPTED",
  "BRIDGE-WAKE-V1",
];

function read(p) {
  assert.ok(fs.existsSync(p), `missing SoT file: ${p}`);
  return fs.readFileSync(p, "utf8");
}

test("recovery SoT files exist and contain current repair markers", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  for (const marker of REQUIRED_MARKERS) {
    assert.match(recovery, new RegExp(marker.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")), `recovery missing ${marker}`);
    assert.match(state, new RegExp(marker.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")), `PROJECT-STATE missing ${marker}`);
  }
});

test("recovery SoT last-implementation is not stuck on pre-T035 catalog polish only", () => {
  const recovery = read(recoveryPath);
  const state = read(statePath);
  // Must explicitly record T036-R1 as current repair / issued work
  assert.match(recovery, /TB-P07-T036-R1/);
  assert.match(state, /TB-P07-T036-R1/);
  assert.doesNotMatch(
    state,
    /Current Issued Task:\s*```text\s*TB-P07-T020-R1\s*```/,
    "PROJECT-STATE Current Issued Task still stuck on T020-R1",
  );
});
