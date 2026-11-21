import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { pathToFileURL } from "node:url";

const SHA = /^[a-f0-9]{40}$/i;
const HASH = /^[a-f0-9]{64}$/i;
const sha256 = bytes => crypto.createHash("sha256").update(bytes).digest("hex");
const filled = value => typeof value === "string" && value.trim().length > 0
  && !/REPLACE_WITH|<[^>]+>/.test(value);

/** Structural readiness only. This cannot certify the truth or sufficiency of worker-authored evidence. */
export function checkReadiness(folder, { stage, taskSha } = {}) {
  const errors = [];
  const fail = message => errors.push(message);
  if (!["plan", "review"].includes(stage)) fail("stage must be plan or review");
  if (!HASH.test(taskSha ?? "")) fail("issued TASK SHA-256 is required from the architect artifact");
  const root = fs.realpathSync(folder);
  const read = relative => {
    if (!filled(relative) || path.isAbsolute(relative)) throw new Error("relative artifact path required");
    const resolved = fs.realpathSync(path.resolve(root, relative));
    if (resolved !== root && !resolved.startsWith(root + path.sep)) throw new Error("artifact escapes task folder");
    if (!fs.statSync(resolved).isFile()) throw new Error("artifact must be a regular file");
    return fs.readFileSync(resolved);
  };
  let task, manifest;
  try {
    const bytes = read("TASK.md");
    if (HASH.test(taskSha ?? "") && sha256(bytes) !== taskSha.toLowerCase()) fail("TASK bytes differ from issued artifact");
    task = bytes.toString("utf8");
    manifest = JSON.parse(read("DELIVERY.json").toString("utf8"));
  } catch (error) {
    return { ok: false, errors: [...errors, String(error.message)] };
  }
  if (manifest.protocol !== "TOOBA-DELIVERY-001") fail("protocol mismatch");
  const taskId = task.match(/^Task-ID:\s*(.+)$/m)?.[1]?.trim();
  if (!filled(manifest.taskId) || manifest.taskId !== taskId) fail("Task-ID mismatch/placeholder");
  const expectedBase = task.match(/^Expected-Base-SHA:\s*([a-f0-9]{40})\s*$/mi)?.[1];
  if (!SHA.test(manifest.baseSha ?? "") || manifest.baseSha.toLowerCase() !== expectedBase?.toLowerCase()) fail("base SHA mismatch");
  const issuedIds = [...task.matchAll(/^\|\s*(REQ-[A-Z0-9-]+)\s*\|/gm)].map(match => match[1]);
  if (!issuedIds.length || new Set(issuedIds).size !== issuedIds.length) fail("TASK needs unique REQ table rows");
  const requirements = Array.isArray(manifest.requirements) ? manifest.requirements : [];
  const actualIds = requirements.map(row => row.id);
  if (new Set(actualIds).size !== actualIds.length
      || [...issuedIds].sort().join("|") !== [...actualIds].sort().join("|")) fail("requirement scope differs from issued TASK");
  try {
    const plan = read(manifest.architecturePlan);
    if (plan.toString("utf8").trim().length < 80 || /REPLACE_WITH|<[^>]+>/.test(plan.toString("utf8"))) fail("architecture plan is empty/template");
    if (!HASH.test(manifest.architecturePlanSha256 ?? "")
        || sha256(plan) !== manifest.architecturePlanSha256?.toLowerCase()) fail("architecture plan hash mismatch");
  } catch (error) { fail("architecture plan: " + error.message); }
  const waves = Array.isArray(manifest.waves) ? manifest.waves : [];
  if (!waves.length) fail("ordered internal waves required");
  const seenWaves = new Set();
  const covered = new Set();
  for (const wave of waves) {
    if (!filled(wave.id) || seenWaves.has(wave.id)) fail("invalid/duplicate wave");
    if (!Array.isArray(wave.dependsOn) || wave.dependsOn.some(id => !seenWaves.has(id))) fail("wave dependency missing, cyclic or out of order");
    if (!Array.isArray(wave.requirementIds) || !wave.requirementIds.length) fail("wave requirements missing");
    for (const id of wave.requirementIds ?? []) {
      if (!issuedIds.includes(id)) fail("wave refers to unissued requirement: " + id);
      covered.add(id);
    }
    seenWaves.add(wave.id);
    if (stage === "review" && wave.status !== "VERIFIED") fail("unverified wave: " + wave.id);
  }
  for (const id of issuedIds) if (!covered.has(id)) fail("requirement absent from waves: " + id);
  if (stage === "review") {
    if (!SHA.test(manifest.codeSha ?? "")) fail("final code SHA required");
    if (!SHA.test(manifest.planCommit ?? "")) fail("pre-implementation plan commit required for independent review");
  }
  const seenScenarios = new Set();
  for (const requirement of requirements) {
    if (stage === "review" && requirement.status !== "VERIFIED") fail("open requirement: " + requirement.id);
    if (!Array.isArray(requirement.scenarios) || !requirement.scenarios.length) fail("scenarios missing: " + requirement.id);
    for (const scenario of requirement.scenarios ?? []) {
      if (!filled(scenario.id) || seenScenarios.has(scenario.id)) fail("invalid/duplicate scenario");
      seenScenarios.add(scenario.id);
      if (!filled(scenario.kind) || !filled(scenario.steps)) fail("scenario needs concrete kind and steps: " + scenario.id);
      if (stage !== "review") continue;
      if (scenario.status !== "PASS") fail("scenario not passed: " + scenario.id);
      if (!Array.isArray(scenario.evidence) || !scenario.evidence.length) fail("evidence missing: " + scenario.id);
      let finalEvidence = false;
      for (const evidence of scenario.evidence ?? []) {
        if (!filled(evidence.kind) || !filled(evidence.provider)) fail("evidence kind/provider required: " + scenario.id);
        if (!["before", "after"].includes(evidence.phase)) fail("evidence phase required: " + scenario.id);
        if (!SHA.test(evidence.codeSha ?? "")) fail("evidence code SHA required: " + scenario.id);
        if (evidence.phase === "after" && evidence.codeSha === manifest.codeSha) finalEvidence = true;
        else if (evidence.phase === "after") fail("stale final evidence SHA: " + scenario.id);
        try {
          const bytes = read(evidence.file);
          if (!bytes.length || !HASH.test(evidence.sha256 ?? "") || sha256(bytes) !== evidence.sha256?.toLowerCase()) fail("evidence empty/hash mismatch: " + scenario.id);
        } catch (error) { fail("evidence: " + scenario.id + ": " + error.message); }
      }
      if (!finalEvidence) fail("no evidence for final code: " + scenario.id);
    }
  }
  return { ok: errors.length === 0, stage, taskId, requirements: requirements.length, errors };
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const args = process.argv.slice(2);
  const value = name => args[args.indexOf(name) + 1];
  try {
    const result = checkReadiness(args[0], { stage: value("--stage"), taskSha: value("--task-sha") });
    console.log(JSON.stringify(result, null, 2));
    process.exitCode = result.ok ? 0 : 1;
  } catch (error) {
    console.error(JSON.stringify({ ok: false, errors: [error.message] }));
    process.exitCode = 1;
  }
}
