#!/usr/bin/env node
/*
 * TB-OCC-284 independent OpenCode acceptance checker.
 * Read-only and dependency-free. It validates the committed source evidence,
 * the connected-284 assertion facts, source identity, cleanup claims, and the
 * preserved Cursor-gate incompatibility. It does not certify the truth of
 * externally authored logs by itself.
 */
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(__dirname, "../../../..");
const TASK_DIR = __dirname;
const MANIFEST_REL = "docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/EVIDENCE-MANIFEST.json";
const MANIFEST_ABS = path.join(REPO, MANIFEST_REL);
const REQUIRED_IDS = ["REQ-01", "REQ-02", "REQ-03", "REQ-04", "REQ-05", "REQ-06", "REQ-07"];
const EXPECTED_CHECKS = {
  "REQ-01": ["REQ-01-two-positions", "REQ-01-one-patch", "REQ-01-zero-owner-writes", "REQ-01-sku", "REQ-01-price", "REQ-01-positions", "REQ-01-reservations", "REQ-01-outbox", "REQ-01-independent-get"],
  "REQ-02": ["REQ-02-draft-stays", "REQ-02-limits-cleared", "REQ-02-still-draft"],
  "REQ-03": ["REQ-03-B-old-price", "REQ-03-A-one-price-write", "REQ-03-A-no-commercial-write", "REQ-03-price-persisted", "REQ-03-B-still-stale", "REQ-03-B-one-commercial-write", "REQ-03-B-zero-price-write", "REQ-03-both-persist", "REQ-03-stock-unchanged"],
  "REQ-04": ["REQ-04-targeted-position", "REQ-04-total", "REQ-04-other-state", "REQ-04-one-inventory-write", "REQ-04-negatives-refused", "REQ-04-negatives-zero-change"],
  "REQ-05": ["REQ-05-wrong-no-success", "REQ-05-wrong-zero-change", "REQ-05-lost-write-persisted", "REQ-05-no-automatic-second-write"],
  "REQ-06": ["REQ-06-no-horizontal-overflow", "REQ-06-controls-visible", "REQ-06-cancel-zero-writes", "REQ-06-old-value"],
  "REQ-07": ["REQ-07-no-change-zero-writes", "REQ-07-second-draft-retained", "REQ-07-both-persist"],
};
const OFFER = "01a2041e-bb6b-7000-9163-d736d71634d5";
const SELLER = "01a2041e-bb65-7000-9b34-1d049b3a788a";
const POS_A = "01a2041e-bbe0-7000-9897-006e14fe11c0";
const POS_B = "01a2041e-bbd3-7000-9fe5-0e7d2157003e";
const CONNECTED_PATH = "docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/evidence/connected-284.json";
const RESULT_PATH = "docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/RESULT.md";
const RECEIPTS_PATH = "docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/RECEIPTS.md";
const CURSOR_PATHS = [
  "docs/ai/delivery",
  "docs/architecture/TOOBA-DELIVERY-ASSURANCE.md",
  "docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283",
  "docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284",
  "AGENTS.md",
  "docs/ROADMAP.md",
];
const ALLOWED_PREEXISTING = new Set(["docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/ARCHITECT-REVIEW.md"]);
const TOP_KEYS = ["protocol", "parentTaskId", "sourceCodeSha", "sourceEvidenceCommit", "requirements", "toolEvidence", "cleanupEvidence", "result"];

const sha256 = bytes => crypto.createHash("sha256").update(bytes).digest("hex");
const stable = value => JSON.stringify(value);
const list = value => Array.isArray(value) ? value : [];
const isNonEmpty = value => typeof value === "string" && value.trim().length > 0;
const inside = rel => {
  if (!isNonEmpty(rel) || path.isAbsolute(rel)) throw new Error(`invalid relative path: ${rel}`);
  const resolved = path.resolve(REPO, rel);
  const real = fs.realpathSync(resolved);
  if (real !== REPO && !real.startsWith(REPO + path.sep)) throw new Error(`path escapes repository: ${rel}`);
  return real;
};
const read = rel => fs.readFileSync(inside(rel));
const git = (args, encoding = "utf8") => execFileSync("git", ["-C", REPO, ...args], { encoding });
const lines = text => text.split(/\r?\n/).filter(Boolean);
const manifest = JSON.parse(fs.readFileSync(MANIFEST_ABS, "utf8"));
const connectedBytes = fs.readFileSync(inside(CONNECTED_PATH));
const connected = JSON.parse(connectedBytes.toString("utf8"));
const artifactPaths = Object.keys(manifest.toolEvidence.artifacts ?? {});
const artifactMap = Object.fromEntries(artifactPaths.map(rel => [rel, fs.readFileSync(inside(rel))]));
const artifactHashes = Object.fromEntries(artifactPaths.map(rel => [rel, sha256(artifactMap[rel])]));

function validateStructure(m, errors) {
  if (!m || typeof m !== "object" || Array.isArray(m)) return errors.push("manifest must be an object");
  const keys = Object.keys(m).sort();
  if (keys.join("|") !== [...TOP_KEYS].sort().join("|")) errors.push(`exact top-level keys required: ${keys.join(",")}`);
  if (m.protocol !== "TOOBA-OPENCODE-ACCEPTANCE-V1") errors.push("protocol mismatch");
  if (m.parentTaskId !== "TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284") errors.push("parentTaskId mismatch");
  if (m.sourceCodeSha !== "611f97a85ec7c2e464e4f649820992202ca3754c") errors.push("sourceCodeSha mismatch");
  if (m.sourceEvidenceCommit !== "fc40d412aa6d085d3db3f1deac461eb93a10f48d") errors.push("sourceEvidenceCommit mismatch");
  const requirements = list(m.requirements);
  const ids = requirements.map(row => row?.id);
  if (ids.length !== REQUIRED_IDS.length || [...ids].sort().join("|") !== [...REQUIRED_IDS].sort().join("|")) errors.push("requirements must be exactly REQ-01..REQ-07");
  if (new Set(ids).size !== ids.length) errors.push("duplicate requirement id");
  for (const row of requirements) {
    if (!row || typeof row !== "object") { errors.push("requirement must be an object"); continue; }
    if (!REQUIRED_IDS.includes(row.id)) errors.push(`unknown requirement: ${row.id}`);
    if (row.status !== "PASS") errors.push(`requirement not PASS: ${row.id}`);
    if (!Array.isArray(row.evidencePaths) || row.evidencePaths.length === 0) errors.push(`evidencePaths required: ${row.id}`);
    if (!Array.isArray(row.checks) || row.checks.length === 0) errors.push(`checks required: ${row.id}`);
    if (!["connected-real", "controlled-error"].includes(row.evidenceClass)) errors.push(`invalid evidenceClass: ${row.id}`);
    if (!isNonEmpty(row.limitations)) errors.push(`limitations required: ${row.id}`);
    const expected = EXPECTED_CHECKS[row.id] ?? [];
    const names = list(row.checks).map(check => check?.name);
    if (names.length !== expected.length || [...names].sort().join("|") !== [...expected].sort().join("|")) errors.push(`manifest checks differ from required assertions: ${row.id}`);
    if (new Set(names).size !== names.length) errors.push(`duplicate manifest check: ${row.id}`);
    for (const check of list(row.checks)) {
      if (!check || !isNonEmpty(check.name) || !isNonEmpty(check.observed)) errors.push(`check name/observed required: ${row.id}`);
    }
    for (const evidencePath of list(row.evidencePaths)) {
      if (!isNonEmpty(evidencePath)) errors.push(`invalid evidencePath: ${row.id}`);
      else if (!artifactPaths.includes(evidencePath)) errors.push(`evidencePath absent from artifact hashes: ${evidencePath}`);
    }
  }
  const tool = m.toolEvidence;
  if (!tool || typeof tool !== "object") errors.push("toolEvidence required");
  else {
    for (const key of ["openSpec", "playwright", "host", "next", "postgresql", "cursorGate", "artifacts"]) if (!(key in tool)) errors.push(`toolEvidence missing: ${key}`);
    if (tool.openSpec?.version !== "2.1.1" || !isNonEmpty(tool.openSpec?.evidencePath) || !/^[a-f0-9]{64}$/.test(tool.openSpec?.sha256 ?? "")) errors.push("openSpec evidence identity invalid");
    if (tool.playwright?.version !== "1.65.1" || !isNonEmpty(tool.playwright?.evidencePath) || !/^[a-f0-9]{64}$/.test(tool.playwright?.sha256 ?? "")) errors.push("playwright evidence identity invalid");
    if (tool.host?.endpoint !== "http://127.0.0.1:5090" || tool.host?.pid !== 40848 || !/^[a-f0-9]{64}$/i.test(tool.host?.dllSha256 ?? "")) errors.push("Host evidence identity invalid");
    if (tool.next?.endpoint !== "http://127.0.0.1:3100" || tool.next?.buildId !== "oWTA29FFSAnndeeVyDFsx") errors.push("Next evidence identity invalid");
    if (tool.postgresql?.version !== "17.10" || tool.postgresql?.database !== "tooba_offer283_connected") errors.push("PostgreSQL evidence identity invalid");
    if (tool.cursorGate?.state !== "KNOWN_FORMAT_INCOMPATIBILITY" || tool.cursorGate?.productVerdict !== false) errors.push("Cursor gate must remain KNOWN_FORMAT_INCOMPATIBILITY and not be relabelled as product PASS");
    if (!tool.artifacts || typeof tool.artifacts !== "object") errors.push("artifact hashes required");
    else for (const [rel, hash] of Object.entries(tool.artifacts)) {
      if (!/^[a-f0-9]{64}$/.test(String(hash))) errors.push(`invalid artifact hash: ${rel}`);
    }
  }
  const cleanup = m.cleanupEvidence;
  if (!cleanup || cleanup.databaseRemoved !== true || cleanup.database !== "tooba_offer283_connected" || !Array.isArray(cleanup.portsClosed) || cleanup.portsClosed.join(",") !== "5090,3100" || cleanup.canonicalHealth?.host5088 !== 200 || cleanup.canonicalHealth?.next3000 !== 200) errors.push("cleanup evidence invalid");
  if (m.result?.status !== "PASS" || !Array.isArray(m.result?.blockingItems) || m.result.blockingItems.length !== 0) errors.push("result must be PASS with no blockingItems");
}

function validateArtifacts(m, readFile, expectedHashes, errors) {
  const tool = m.toolEvidence?.artifacts;
  if (!tool || typeof tool !== "object") return;
  for (const [rel, expected] of Object.entries(tool)) {
    let bytes;
    try { bytes = readFile(rel); } catch (error) { errors.push(`artifact unreadable: ${rel}: ${error.message}`); continue; }
    const actual = sha256(bytes);
    if (actual !== String(expected).toLowerCase()) errors.push(`artifact hash mismatch: ${rel}`);
    if (expectedHashes && expectedHashes[rel] && actual !== expectedHashes[rel]) errors.push(`working-tree hash mismatch: ${rel}`);
  }
  for (const rel of artifactPaths) if (!(rel in (tool ?? {}))) errors.push(`artifact hash entry missing: ${rel}`);
}

function validateConnectedFacts(c, errors) {
  if (!c || c.ok !== true) errors.push("connected ok must be true");
  const scenarios = list(c.scenarios);
  const ids = scenarios.map(row => row.id);
  if (ids.length !== 7 || [...ids].sort().join("|") !== [...REQUIRED_IDS].sort().join("|")) errors.push("connected must contain exactly seven scenarios");
  if (new Set(ids).size !== ids.length) errors.push("duplicate connected scenario");
  if (scenarios.some(row => row.status !== "PASS")) errors.push("all connected scenarios must PASS");
  if (!Array.isArray(c.checks) || c.checks.length < 38) errors.push("at least 38 concrete connected assertions required");
  const checkIds = list(c.checks).map(row => row.id);
  if (new Set(checkIds).size !== checkIds.length) errors.push("duplicate connected assertion");
  if (c.checks.some(row => row.ok !== true)) errors.push("all connected assertions must be true");
  const expectedNames = Object.values(EXPECTED_CHECKS).flat();
  if (expectedNames.some(name => !checkIds.includes(name))) errors.push("required connected assertion absent");
  const byReq = Object.fromEntries(scenarios.map(row => [row.id, row.result ?? {}]));
  const checks = Object.fromEntries(list(c.checks).map(row => [row.id, row]));

  const req1 = byReq["REQ-01"];
  const req1Writes = list(req1.writes);
  if (req1Writes.length !== 1 || req1Writes[0].method !== "PATCH" || !String(req1Writes[0].url).endsWith(`/offers/${OFFER}`)) errors.push("REQ-01 must have exactly one owning offer PATCH");
  else {
    const body = JSON.parse(req1Writes[0].body || "{}");
    if (body.sellerSku !== "ARM-LN-01-C01" || "amount" in body || "onHand" in body || "stockItemId" in body) errors.push("REQ-01 PATCH must carry SKU only");
  }
  const p1 = list(req1.before?.positions).find(row => row.id === POS_A);
  const p2 = list(req1.before?.positions).find(row => row.id === POS_B);
  if (!p1 || p1.onHand !== "3.000000" || p1.reserved !== "1.000000" || !p2 || p2.onHand !== "7.000000" || p2.reserved !== "2.000000") errors.push("REQ-01 fixture must be positions 3/1 and 7/2");
  if (stable(req1.before?.positions) !== stable(req1.after?.positions) || stable(req1.before?.reservations) !== stable(req1.after?.reservations)) errors.push("REQ-01 positions/reservations changed");
  if (req1.before?.price !== "2500000.0000" || req1.after?.price !== "2500000.0000" || req1.independent?.body?.amount !== 2500000) errors.push("REQ-01 price did not survive");
  if (req1.after?.sku !== "ARM-LN-01-C01" || req1.independent?.body?.sellerSku !== "ARM-LN-01-C01") errors.push("REQ-01 SKU did not survive independent read");
  if (req1.independent?.body?.offerId !== OFFER || req1.independent?.body?.sellerPartyId !== SELLER) errors.push("REQ-01 independent GET identity mismatch");
  if (req1.before?.outbox !== 27 || req1.after?.outbox !== 27) errors.push("REQ-01 inventory outbox must remain 27");

  const req3 = byReq["REQ-03"];
  const aPrice = list(req3.writesA).filter(row => String(row.url).includes("/price"));
  const bPrice = list(req3.writesB).filter(row => String(row.url).includes("/price"));
  const bPatch = list(req3.writesB).filter(row => row.method === "PATCH");
  const aPatch = list(req3.writesA).filter(row => row.method === "PATCH");
  if (aPrice.length !== 1 || aPrice[0].method !== "PUT" || JSON.parse(aPrice[0].body || "{}").amount !== 2750000 || aPatch.length !== 0) errors.push("REQ-03 A must perform one owning price write and no PATCH");
  if (bPatch.length !== 1 || bPrice.length !== 0) errors.push("REQ-03 B must perform one SKU PATCH and no price write");
  if (req3.afterPrice?.price !== "2750000.0000" || req3.after?.price !== "2750000.0000" || req3.after?.sku !== "ARM-LN-01-C03" || req3.independent?.body?.amount !== 2750000 || req3.independent?.body?.sellerSku !== "ARM-LN-01-C03") errors.push("REQ-03 new price and SKU must both survive");
  if (stable(req3.before?.positions) !== stable(req3.after?.positions) || stable(req3.before?.reservations) !== stable(req3.after?.reservations)) errors.push("REQ-03 stock/reservations changed");
  if (!String(checks["REQ-03-B-old-price"]?.detail).includes("2500000") || !String(checks["REQ-03-B-still-stale"]?.detail).includes("2500000")) errors.push("REQ-03 B must observe the old price");

  const req2 = byReq["REQ-02"];
  if (req2.before?.status !== "Draft" || req2.afterDraft?.status !== "Draft" || req2.afterDraft?.sku !== "ARM-LN-01-C02" || req2.after?.min !== null || req2.after?.max !== null || req2.after?.status !== "Draft") errors.push("REQ-02 Draft/limit-clear facts missing");

  const req4 = byReq["REQ-04"];
  const p4a = list(req4.after?.positions).find(row => row.id === POS_A);
  const p4b = list(req4.after?.positions).find(row => row.id === POS_B);
  if (!p4a || p4a.onHand !== "3.000000" || !p4b || p4b.onHand !== "8.000000") errors.push("REQ-04 must show positions 3 and 8");
  if (Number(p4a?.onHand) + Number(p4b?.onHand) !== 11) errors.push("REQ-04 total must be 11");
  if (req4.after?.sku !== req4.before?.sku || req4.after?.price !== req4.before?.price) errors.push("REQ-04 SKU/price changed");
  if (!Array.isArray(req4.negative) || req4.negative.length !== 3 || req4.negative.map(row => row.status).sort().join(",") !== "400,409,409") errors.push("REQ-04 negatives must be 400,409,409");
  if (stable(req4.negativeBefore?.positions) !== stable(req4.negativeAfter?.positions) || stable(req4.negativeBefore?.reservations) !== stable(req4.negativeAfter?.reservations) || req4.negativeBefore?.outbox !== req4.negativeAfter?.outbox) errors.push("REQ-04 negatives changed state");

  const req5 = byReq["REQ-05"];
  if (stable(req5.afterWrong) !== stable(req5.before)) errors.push("REQ-05 wrong response changed state");
  if (req5.afterLost?.sku !== "ARM-LN-01-C05-LOST" || req5.independent?.body?.sellerSku !== "ARM-LN-01-C05-LOST") errors.push("REQ-05 lost response did not reconcile");
  if (list(req5.writes).filter(row => row.method === "PATCH").length !== 2) errors.push("REQ-05 must show exactly two PATCH attempts without automatic extra retry");
  if (!String(checks["REQ-05-wrong-no-success"]?.detail).includes("\"wrongNotice\":0")) errors.push("REQ-05 wrong response must not show success");

  const req6 = byReq["REQ-06"];
  if (req6.geometry?.innerWidth !== 360 || req6.geometry?.scrollWidth !== 360 || req6.geometry?.overflowX !== 0) errors.push("REQ-06 mobile overflow must be zero");
  if (!req6.boxes?.save || !req6.boxes?.cancel || !req6.boxes?.position || req6.boxes.save.width <= 0 || req6.boxes.cancel.width <= 0 || req6.boxes.position.width <= 0) errors.push("REQ-06 controls must be reachable");
  if (list(req6.writes).length !== 0 || req6.skuAfterReopen !== "ARM-LN-01") errors.push("REQ-06 cancel/reopen fact missing");

  const req7 = byReq["REQ-07"];
  if (String(checks["REQ-07-no-change-zero-writes"]?.detail) !== "[]") errors.push("REQ-07 no-change save must send zero writes");
  if (Number(String(req7.priceDraft).replace(/[^\d]/g, "")) !== 2800000) errors.push("REQ-07 second draft must be retained");
  if (req7.after?.sku !== "ARM-LN-01-C07" || req7.after?.price !== "2800000.0000" || req7.independent?.body?.sellerSku !== "ARM-LN-01-C07" || req7.independent?.body?.amount !== 2800000) errors.push("REQ-07 both values must persist");

  for (const id of REQUIRED_IDS) {
    const expected = EXPECTED_CHECKS[id];
    for (const name of expected) if (!checkIds.includes(name)) errors.push(`missing ${name}`);
  }
}

function validateSource(m, errors) {
  const head = git(["rev-parse", "HEAD"]);
  const origin = git(["rev-parse", "origin/main"]);
  if (head !== origin) errors.push("HEAD must equal origin/main");
  try { git(["merge-base", "--is-ancestor", m.sourceCodeSha, "HEAD"]); } catch { errors.push("source code SHA is not an ancestor of HEAD"); }
  try { git(["merge-base", "--is-ancestor", m.sourceEvidenceCommit, "HEAD"]); } catch { errors.push("source evidence commit is not an ancestor of HEAD"); }
  const sourceDiff = git(["diff", "--name-only", `${m.sourceCodeSha}..HEAD`, "--", "src/backend", "src/frontend"]);
  if (sourceDiff.trim()) errors.push("feature source changed after reviewed code SHA");
  const cursorDiff = git(["diff", "--name-only", "844c23237d19aeabc453f5fb04561657c4dd967a..HEAD", "--", ...CURSOR_PATHS]);
  if (cursorDiff.trim()) errors.push(`Cursor-governed artifacts changed: ${cursorDiff.replace(/\n/g, ",")}`);
  const taskDiff = lines(git(["diff", "--name-only", "844c23237d19aeabc453f5fb04561657c4dd967a..HEAD"]));
  const allowedPrefix = "docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/";
  for (const rel of taskDiff) if (!rel.startsWith(allowedPrefix)) errors.push(`unexpected post-base change: ${rel}`);
  const baseDiff = lines(git(["diff", "--name-only", "f006fbdbdbfd4911ea439fe9a3443438af6cf786..HEAD"]));
  for (const rel of baseDiff) {
    if (rel.startsWith(allowedPrefix)) continue;
    if (ALLOWED_PREEXISTING.has(rel)) continue;
    errors.push(`unexpected change after architect base: ${rel}`);
  }
  for (const [rel, hash] of Object.entries(m.toolEvidence.artifacts)) {
    if (!rel.startsWith("docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/")) continue;
    let bytes;
    try { bytes = git(["show", `${m.sourceEvidenceCommit}:${rel}`], "buffer"); } catch { errors.push(`source evidence missing from commit: ${rel}`); continue; }
    if (sha256(bytes) !== String(hash).toLowerCase()) errors.push(`source evidence commit hash mismatch: ${rel}`);
  }
  const page = git(["show", `${m.sourceCodeSha}:src/frontend/app/vendor-panel/products/[offerId]/page.tsx`]);
  const api = git(["show", `${m.sourceCodeSha}:src/frontend/app/vendor-panel/seller-api.ts`]);
  const endpoint = git(["show", `${m.sourceCodeSha}:src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs`]);
  const inventory = git(["show", `${m.sourceCodeSha}:src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.SellerWrite.cs`]);
  const update = git(["show", `${m.sourceCodeSha}:src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/UpdateOffer/UpdateOfferCommand.cs`]);
  for (const token of ["offer-inventory-position", "data-stock-item-id", "clearOrderQuantityLimits", "reconcileVersion", "offer-save", "offer-cancel", "stockItemId"]) if (!page.includes(token)) errors.push(`reviewed page missing ${token}`);
  for (const token of ["clearOrderQuantityLimits", "seller.offer.response_mismatch", "seller.inventory.response_mismatch", "stockItemId"]) if (!api.includes(token)) errors.push(`reviewed seller API missing ${token}`);
  for (const token of ["/offers/{offerId:guid}/price", "/offers/{offerId:guid}/inventory", "body.StockItemId"]) if (!endpoint.includes(token)) errors.push(`reviewed endpoint missing ${token}`);
  for (const token of ["PositionIdRequired", "PositionIdUnknown", "AdjustmentQuantityExceeds"]) if (!inventory.includes(token)) errors.push(`reviewed inventory writer missing ${token}`);
  for (const token of ["ClearOrderQuantityLimits", "Draft"]) if (!update.includes(token)) errors.push(`reviewed Offer handler missing ${token}`);
}

function validateEvidenceText(m, errors) {
  const result = read(RESULT_PATH).toString("utf8");
  const receipts = read(RECEIPTS_PATH).toString("utf8");
  const spec = read("docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/OPENSPEC-STRICT.log").toString("utf8");
  const playwright = read("docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/TOOL-PLAYWRIGHT.txt").toString("utf8");
  if (!result.includes("database dropped") || !result.includes("ports 5090 and 3100") || !result.includes("canonical Host 5088")) errors.push("RESULT cleanup sentence missing");
  if (!receipts.includes("Dropped `tooba_offer283_connected`") || !receipts.includes("Ports 5090 and 3100 no longer listen")) errors.push("RECEIPTS cleanup proof missing");
  if (!spec.includes("Change 'seller-offer-safe-edit-283' is valid") || !spec.includes("EXIT=0")) errors.push("OpenSpec strict log invalid");
  if (!playwright.includes("\"playwright\":\"1.65.1\"") || !playwright.includes("chromium-1255")) errors.push("Playwright tool evidence invalid");
}

function validateAll(m, c, readFile, expectedHashes) {
  const errors = [];
  validateStructure(m, errors);
  validateConnectedFacts(c, errors);
  validateArtifacts(m, readFile, expectedHashes, errors);
  validateSource(m, errors);
  validateEvidenceText(m, errors);
  return errors;
}

function selfTest() {
  const errors = [];
  const run = (m, c, map, hashes) => validateAll(m, c, rel => {
    if (!(rel in map)) throw new Error(`no fixture: ${rel}`);
    return map[rel];
  }, hashes);
  const baseMap = { ...artifactMap };
  const good = run(manifest, connected, baseMap, artifactHashes);
  if (good.length) errors.push(`self-test baseline failed: ${good.join("; ")}`);
  const missingReq = structuredClone(manifest);
  missingReq.requirements = missingReq.requirements.filter(row => row.id !== "REQ-07");
  if (!run(missingReq, connected, baseMap, artifactHashes).length) errors.push("self-test did not reject deleted REQ-07");
  const badFact = structuredClone(connected);
  const req1 = badFact.scenarios.find(row => row.id === "REQ-01");
  req1.result.after.price = "999.0000";
  if (!run(manifest, badFact, baseMap, artifactHashes).length) errors.push("self-test did not reject changed REQ-01 price");
  const tamperedMap = { ...baseMap, [CONNECTED_PATH]: Buffer.from(JSON.stringify({ ...badFact, ok: true }, null, 2), "utf8") };
  if (!run(manifest, connected, tamperedMap, artifactHashes).length) errors.push("self-test did not reject tampered evidence/hash mismatch");
  return errors;
}

const selfErrors = selfTest();
const errors = selfErrors.length ? selfErrors : validateAll(manifest, connected, read, artifactHashes);
const summary = {
  ok: errors.length === 0,
  head: git(["rev-parse", "HEAD"]).trim(),
  originMain: git(["rev-parse", "origin/main"]).trim(),
  protocol: manifest.protocol,
  selfTest: selfErrors.length === 0 ? "PASS" : "FAIL",
  sourceCodeSha: manifest.sourceCodeSha,
  sourceEvidenceCommit: manifest.sourceEvidenceCommit,
  requirements: list(manifest.requirements).length,
  assertions: list(connected.checks).length,
  artifacts: artifactPaths.length,
  artifactHashes,
  cursorGate: manifest.toolEvidence?.cursorGate?.state,
  errors,
};
console.log(JSON.stringify(summary, null, 2));
if (errors.length) process.exitCode = 1;



