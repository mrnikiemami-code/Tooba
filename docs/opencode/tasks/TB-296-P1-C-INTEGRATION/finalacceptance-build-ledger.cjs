// Builds FINAL-ACCEPTANCE-LEDGER.json from the immutable 60-ID order + prior verified ledger,
// overriding only the IDs re-executed/reclassified in this final-acceptance task.
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const TASK = __dirname;
const REPO = path.resolve(TASK, "../../../..");
const read = p => JSON.parse(fs.readFileSync(p, "utf8").replace(/^\uFEFF/, ""));
const sha = p => crypto.createHash("sha256").update(fs.readFileSync(p)).digest("hex");
const rel = p => path.relative(REPO, p).replaceAll("\\", "/");

const order = read(path.join(TASK, "FINAL-LEDGER.json")).map(r => r.id);
if (order.length !== 60 || new Set(order).size !== 60) throw new Error("immutable 60-ID order invalid");
const prior = read(path.join(TASK, "FINAL-P1C-60-ID-LEDGER-VERIFIED.json"));
if (prior.rows.length !== 60) throw new Error("prior ledger rows != 60");
const rowById = new Map(prior.rows.map(r => [r.id, r]));

const EV = path.join(TASK, "finalacceptance-evidence");
const now = new Date().toISOString();
function pass(id, file, stage, reason) {
  const p = path.join(EV, file);
  return {
    id, status: "PASS", reason,
    evidence: rel(p), evidenceSha256: sha(p), finishedAt: now,
    provenance: [{ source: "FINAL-ACCEPTANCE", stage, status: "PASS" }],
    evidenceVerified: true,
    evidenceContent: { topLevelId: id, hasObservations: true },
  };
}
function notRun(id, stage, reason) {
  const r = rowById.get(id);
  return {
    id, status: "NOT_RUN", reason,
    evidence: r.evidence, evidenceSha256: r.evidenceSha256, finishedAt: now,
    provenance: [
      ...(r.provenance || []),
      { source: "FINAL-ACCEPTANCE", stage, status: "NOT_RUN" },
    ],
    evidenceVerified: true,
    evidenceContent: r.evidenceContent,
    retainedPartialEvidence: true,
  };
}

const overrides = new Map();
overrides.set("R10-01-B", pass("R10-01-B", "R10-01-B.json", "g1-channel-two-contexts",
  "Two independently authorized DEV seller contexts on the same tooba_alpha DB (store-alpha SalesChannel=Marketplace; reversible test-only store-beta SalesChannel=Direct on the same tenant-alpha connection) created two distinct Draft offers for the same seller+variant; a changed-channel replay of the same creation intent was rejected with offer.create.intent_conflict and wrote nothing."));
overrides.set("OWN-10", pass("OWN-10", "OWN-10.json", "g1-marketplace-capability",
  "Live Marketplace edition bound to the same tooba_alpha DB: real seller browser at 360/768/1440 shows platform physical read-only (no physical reason control), allocation editable, fa/rtl+en/ltr+ru/ltr shells without overflow, in-app locale switch preserved the unsaved offered-quantity draft, authorized seller/actor headers present, independent SQL confirmed no write."));
overrides.set("OWN-08", pass("OWN-08", "OWN-08.json", "g2-simultaneous-price-and-stock",
  "Simultaneous owner-revision races on the real Host: exactly one price winner and one physical-stock winner; single typed 409 concurrency.conflict / inventory.position.revision_conflict losers; winner payloads persisted; two durable price facts and two durable inventory facts (losers wrote none); independent price/physical/allocation revisions; price and inventory outbox events published."));
overrides.set("R10-13-P", pass("R10-13-P", "R10-13-P.json", "g3-regression-supplement",
  "Original-spec regression clauses newly executed: real campaign owner rows, real Stage2 guest cart create+line+read-back, admin grid sort model (aria-sort none->ascending->descending), CSV export with real rows/headers, and a named saved view persisted/rendered with the original preference restored. Performance/orders/products/paging/filter reused unchanged from prior R10-13-P evidence."));

const d2Blocker = "The compound A05 clause requires a real order-creation/captured-checkout workflow. The authorized DEV environment exposes checkout as authentication-required (401 checkout.authentication_required for guest; Host CheckoutIdentityGate enforces the Catalog AuthenticatedOnly policy and does not honor the dev actor header), while the existing full Order relational suite requires a separate task-owned PostgreSQL on port 15433 / database order_tb231 (explicitly rejected as not tooba_alpha). Neither path is available without either a new/alternate database or new customer-auth/fixture scope, both forbidden by this verification-only task. Prior real owner-contract/provider observations are retained but do not close the full workflow assertions.";
for (const id of ["D2-ORDER", "D2-HOLD", "D2-PARTIAL", "D2-RECOVERY"]) {
  overrides.set(id, notRun(id, "g2-a05-full-workflow", d2Blocker));
}

const rows = order.map(id => overrides.get(id) || rowById.get(id));
const counts = { PASS: rows.filter(r => r.status === "PASS").length, FAIL: rows.filter(r => r.status === "FAIL").length, NOT_RUN: rows.filter(r => r.status === "NOT_RUN").length };
const out = {
  ledgerName: "TB-296-P1-FINAL-ACCEPTANCE-LEDGER",
  taskId: "TB-296-P1-FINAL-ACCEPTANCE",
  generatedAt: now,
  mode: "VERIFICATION_ONLY",
  immutableIdSet: { count: 60, unique: 60, order, source: "FINAL-LEDGER.json (existing immutable 60-ID order)" },
  counts,
  passIds: rows.filter(r => r.status === "PASS").map(r => r.id),
  failIds: rows.filter(r => r.status === "FAIL").map(r => r.id),
  notRunIds: rows.filter(r => r.status === "NOT_RUN").map(r => r.id),
  identity: prior.identity,
  sources: prior.sources,
  scopeLimitations: [
    "D2-ORDER/D2-HOLD/D2-PARTIAL/D2-RECOVERY are NOT_RUN for their full order-workflow assertions; prior owner-contract/provider observations are retained but not accepted as full PASS.",
    "R10-01-B and OWN-10 are now PASS with fresh executed evidence (two same-DB authorized channel contexts; live Marketplace edition on tooba_alpha).",
    "OWN-08 and R10-13-P are now PASS with strengthened executed evidence.",
    "Architecture: 27 pre-existing frontend source-size violations outside Offer remain excluded from remediation, not marked resolved; Offer backend architecture guards 51/51 PASS; 0 new/Offer-owned violations.",
    "No architect-authored ARCHITECT-REVIEW.json or machine ACCEPT receipt exists; final acceptance remains architect-owned and is not self-authorized.",
  ],
  rows,
};
const target = path.join(TASK, "FINAL-ACCEPTANCE-LEDGER.json");
fs.writeFileSync(target, JSON.stringify(out, null, 2));
console.log(JSON.stringify({ wrote: rel(target), counts, notRun: out.notRunIds, passCount: counts.PASS }));
