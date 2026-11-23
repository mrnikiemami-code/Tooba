#!/usr/bin/env node
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(__dirname, "../../../..");
const TASK = __dirname;
const evidencePath = path.join(TASK, "evidence/focused-286.json");
const scriptPath = path.join(TASK, "evidence/focused-286.cjs");
const read = rel => fs.readFileSync(path.join(REPO, rel));
const sha = bytes => crypto.createHash("sha256").update(bytes).digest("hex");
const git = args => execFileSync("git", ["-C", REPO, ...args], { encoding: "utf8" });
const errors = [];
const evidence = JSON.parse(fs.readFileSync(evidencePath, "utf8"));
const scriptHash = sha(fs.readFileSync(scriptPath));
const sameCounts = (a, b) => ["prices", "stockPositions", "reservations"].every(k => a?.[k] === b?.[k]);

function validate(m) {
  if (m.task !== "TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286") errors.push("task identity");
  if (m.protocol !== "TOOBA-OPENCODE-DELIVERY-V1@1.3.0") errors.push("protocol");
  if (m.producer?.script !== "docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/evidence/focused-286.cjs") errors.push("producer path");
  if (m.producer?.scriptSha256 !== scriptHash) errors.push("producer script SHA does not match committed harness");
  if (m.producer?.writesObservedJsonDirectly !== true) errors.push("producer must write observed JSON directly");
  if (!m.startedAt || !m.endedAt || !Number.isFinite(m.elapsedMs) || m.elapsedMs <= 0) errors.push("timing missing");
  if (m.environment?.codeSha !== "507d7933a54fe067c240beb6aeb62b63f4b110c0") errors.push("code SHA");
  if (m.environment?.database !== "tooba_offer286_proof") errors.push("database identity");
  if (m.environment?.hostDllSha256 !== "143af9c5dc7459a8e5833b9a35f1abaecbeef2689b704492c9f88461cb3cb007") errors.push("Host DLL hash");

  const seller = m.V2?.seller;
  if (!seller || seller.first !== 201 || seller.second !== 201 || seller.sameOfferId !== true) errors.push("V2 seller replay");
  if (!seller?.original?.includes("TB286-SELLER-A|Draft")) errors.push("V2 seller original");
  const sc = seller?.conflict;
  if (!sc || sc.responseStatus !== 409 || sc.errorCode !== "offer.create.intent_conflict") errors.push("V2 seller conflict");
  if (!sc || sc.originalRows !== 1 || sc.secondRowCount !== 0) errors.push("V2 seller row counts");
  if (!sc || sc.priceWrites !== 0 || sc.stockPositionWrites !== 0 || sc.reservationWrites !== 0 || !sameCounts(sc.before, sc.after)) errors.push("V2 seller owning writes");

  const admin = m.V2?.admin;
  if (!admin || admin.first !== 201 || admin.second !== 201 || admin.sameOfferId !== true) errors.push("V2 admin replay");
  if (!admin?.original?.includes("TB286-ADMIN-A|Draft")) errors.push("V2 admin original");
  const ac = admin?.conflict;
  if (!ac || ac.responseStatus !== 409 || ac.errorCode !== "offer.create.intent_conflict") errors.push("V2 admin conflict");
  if (!ac || ac.originalRows !== 1 || ac.secondRowCount !== 0) errors.push("V2 admin row counts");
  if (!ac || ac.priceWrites !== 0 || ac.stockPositionWrites !== 0 || ac.reservationWrites !== 0 || !sameCounts(ac.before, ac.after)) errors.push("V2 admin owning writes");

  const v3 = m.V3;
  if (!v3 || v3.first !== 201 || v3.second !== 201 || v3.sameOfferId !== true || v3.offerRows !== 1) errors.push("V3 concurrency");
  if (!v3 || (v3.errors || []).some(Boolean) || !sameCounts(v3.before, v3.after)) errors.push("V3 side effects");

  const v4 = m.V4;
  if (!v4 || v4.createStatus !== 201 || v4.initial?.sellerSku !== "TB286-LATE-A" || v4.patchStatus !== 200) errors.push("V4 create/edit");
  if (!v4 || v4.edited?.sellerSku !== "TB286-LATE-B") errors.push("V4 edited state");
  if (!v4 || v4.originalReplayStatus !== 409 || v4.originalReplayError !== "offer.create.intent_conflict") errors.push("V4 original replay");
  if (!v4 || v4.changedReplayStatus !== 201 || v4.changedReplayId !== v4.offerId) errors.push("V4 changed replay");
  if (!v4 || v4.lateReplayContractIssue !== true) errors.push("V4 issue not recorded");

  const diff = git(["diff", "--name-only", "507d7933a54fe067c240beb6aeb62b63f4b110c0..HEAD", "--", "src/backend", "src/frontend"]).trim();
  if (diff) errors.push("product source changed after reference SHA");
  const cursor = git(["diff", "--name-only", "507d7933a54fe067c240beb6aeb62b63f4b110c0..HEAD", "--", "AGENTS.md", "docs/ai/delivery", "docs/ROADMAP.md"]).trim();
  if (cursor) errors.push("Cursor/governance changed");
}

validate(evidence);

// Single mutation self-test: a wrong critical fact must be rejected.
const mutated = structuredClone(evidence);
mutated.V2.seller.conflict.secondRowCount = 1;
const before = errors.length;
validate(mutated);
const mutationErrors = errors.splice(before);
if (mutationErrors.length === 0) errors.push("self-test failed to reject mutated critical fact");

let cleanup = { databaseAbsent: false, testPortsClosed: false, canonicalHost: null, canonicalNext: null };
try {
  const out = execFileSync("docker", ["exec", "postgres-db", "psql", "-U", "admin", "-d", "postgres", "-At", "-c", "select count(*) from pg_database where datname='tooba_offer286_proof'"], { encoding: "utf8" }).trim();
  cleanup.databaseAbsent = out === "0";
} catch { cleanup.databaseAbsent = false; }
try { await fetch("http://127.0.0.1:5093/health", { signal: AbortSignal.timeout(1000) }); cleanup.testPortsClosed = false; } catch { cleanup.testPortsClosed = true; }
try { cleanup.canonicalHost = (await fetch("http://127.0.0.1:5088/health", { signal: AbortSignal.timeout(2000) })).status; } catch {}
try { cleanup.canonicalNext = (await fetch("http://127.0.0.1:3000/fa", { signal: AbortSignal.timeout(3000) })).status; } catch {}
if (!cleanup.databaseAbsent || !cleanup.testPortsClosed || cleanup.canonicalHost !== 200 || cleanup.canonicalNext !== 200) errors.push("cleanup/canonical state");

const decision = errors.length ? "FAIL" : (evidence.V4?.lateReplayContractIssue === true ? "BLOCKED_ARCHITECT_DECISION" : "PASS");
const output = {
  ok: errors.length === 0,
  decision,
  task: evidence.task,
  codeSha: evidence.environment?.codeSha,
  startedAt: evidence.startedAt,
  endedAt: evidence.endedAt,
  elapsedMs: evidence.elapsedMs,
  elapsedMinutes: Number((evidence.elapsedMs / 60000).toFixed(2)),
  checks: ["V1 provenance", "V2 conflict", "V3 concurrency", "V4 late replay"],
  v4Issue: evidence.V4?.lateReplayContractIssue === true,
  cleanup,
  errors,
};
console.log(JSON.stringify(output, null, 2));
if (errors.length) process.exitCode = 1;

