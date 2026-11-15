// Posts the TB-269 worker result, completes the task row and returns the heartbeat to Idle.
// Run ONLY after every code/doc/commit/push step of this task is finished.
const fs = require("node:fs");
const path = require("node:path");
const contract = fs.readFileSync(path.join(__dirname, "result-contract.txt"), "utf8");
const TASK_ROW = "58d11af5-9ccd-4a52-b7df-c1b2e3e0934a";
const BASE = "http://127.0.0.1:17321";

const summary = [
  "TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269 — RESULT",
  "",
  "STATUS: PASS (within the issued presentation-only scope)",
  "DECISION: QUEUE_HANDOFF_CUSTOMER_AND_OPERATIONAL_DEADLINE_CELLS_RENDER_ONE_PERSIAN_JALALI_CALENDAR_WITH_DATE_ONLY_STABILITY_ACCEPTED_ZONE_INSTANTS_ISOLATED_LTR_RANGES_AND_TRUTHFUL_INVALID_MARKERS_WITH_ZERO_DATA_CHANGE",
  "NOT CLAIMED: Architect ACCEPT, user VISUAL/FUNCTIONAL ACCEPT, all-shipping completeness, any backend/data/payment/provider outcome.",
  "",
  "ROOT (source-confirmed): the fulfilment queue owned its own three formatters. formatPlannedHandoff returned plannedHandoffDate + raw window + (Asia/Tehran); formatCustomerReceiving returned its raw date; formatOperationalDeadline used fa-IR 2-digit fields and echoed raw ISO/zone on refusal. TB-267 had standardized only the order-operations page, so cell AND title showed the raw Gregorian value.",
  "",
  "REPAIR (display only, one shared authority): all four queue formatters now delegate to the EXISTING lib/operational-format.ts helpers (operationalDayLabel / operationalInstantLabel / operationalTimeZoneLabel / operationalTimeWindowLabel).",
  " - date-only acceptance -> that calendar day, anchored at local noon, so no UTC/local-midnight conversion can slide it: 2026-11-15 -> ۲۴ آبان ۱۴۰۵.",
  " - accepted instant -> rendered IN the accepted carrier zone: 2026-11-15T21:30Z in Asia/Tehran -> ۲۵ آبان ۱۴۰۵، ۰۱:۰۰ (carrier day, not the UTC day).",
  " - clock range -> Persian digits, stored start/end order preserved, isolated LTR: ۰۸:۰۰–۲۰:۰۰.",
  " - zone -> ONE human label per cell («به وقت تهران»), derived from the existing accepted zone id, never the raw Asia/Tehran repeated beside each instant.",
  " - missing -> «مشخص نشده» / «—» unchanged, nothing invented.",
  " - unreadable input / refused zone / missing plan zone -> «تاریخ نامعتبر» / «منطقهٔ زمانی نامعتبر» / «منطقهٔ زمانی مشخص نشده»; never a raw ISO spill and never a browser-zone fallback.",
  "Wire values untouched: accessors still sort/filter on the ISO facts; no server, contract, endpoint, query, filter, «today» logic, locale registry or new formatting framework; no unrelated label or TB-268 tab/count change; 64px rows and the scoped mobile scroll unchanged.",
  "",
  "PROOF ONCE",
  " - new focused suite fulfillment-work-queue-persian-dates-269.test.ts 7/7 (Jalali date-only; date-only byte-identical under TZ=Pacific/Kiritimati vs TZ=Pacific/Midway in separate processes; UTC-midnight crossing keeps the carrier day/time; range order + LTR isolation; three instants with exactly ONE zone label; missing values; invalid date / refused zone / missing zone with no raw ISO or IANA id in the operator string).",
  " - retained queue contract fulfillment-work-queue-260.test.ts 6/6 (its one expectation that pinned the old raw string now pins the Persian shape). Combined run 13/13.",
  " - npm run test:fulfillment 6/6; npx tsc --noEmit 0; npm run build 0 -> BUILD_ID UmAnnlHYHCf-Sa5-dTMkm.",
  " - served build: Next restarted on 3000 (PID 38800), /fa 200, _buildManifest.js 200, page chunk 200, and chunk 967-076c4bd18e2dcc76.js 200 carrying the new markers.",
  " - live read-only /fa/admin/fulfillments AND the user's /admin/fulfillments: handoff «۲۴ آبان ۱۴۰۵ · ۰۸:۰۰–۲۰:۰۰ · به وقت تهران», deadlines «آماده‌سازی تا ۲۴ آبان ۱۴۰۵، ۱۳:۰۰ · خروج تا ۲۴ آبان ۱۴۰۵، ۱۸:۴۰ · تحویل به حامل تا ۲۴ آبان ۱۴۰۵، ۲۰:۰۰ · به وقت تهران», customer «۲۶ آبان ۱۴۰۵ · ۰۹:۰۰-۱۰:۰۰»; cell text == title; no raw YYYY-MM-DD and no raw IANA id; document overflow 0 at 1440 and 390; grid keeps its scoped scroll (2882 > 340 at 390).",
  "",
  "ENVIRONMENT: FE-only change. Next production 127.0.0.1:3000 PID 38800 (BUILD_ID UmAnnlHYHCf-Sa5-dTMkm); Host 5088 untouched (/health 200, /health/ready 200); ONE canonical tooba_alpha @5432 SingleStore store-alpha; no migration, DDL, business SQL, reset, seed, fixture, provider/waybill call; DataProtection/CapturedPreview unchanged; user orders, shipping draft and the existing example group/parcel read-only (GET only).",
  "",
  "GIT: base at task start f62bd7564d5394dc8e36df4193b065d17be38682 (local main == origin/main then). Code commit 463609685d797c2ea21e424ba66116aa026834e4; origin advanced with the 269 issue + 268 review docs, integrated by a plain --no-ff merge c3920a1d499f62f349ad46857b61a60652a38bcb (no rebase/force/amend/reset/stash); contract pin 960a0eb175f746139ddc369a734f251ba69984f1. FINAL_HEAD 960a0eb17 and origin/main 960a0eb17 are IDENTICAL (parity verified). Only conflict was the two CURRENT-banner files: the 269 PASS banner is CURRENT and the incoming banners were demoted to SUPERSEDED.",
  "",
  "KNOWN LIMITS: the envelope Expected-Base-SHA d4fb3b8e3505a445b123efc8d63badea52df1172 is ABSENT in this clone (git cat-file fails), so work began from the real local main head and that is declared, not substituted; the design-system grid unmounts a column scrolled outside its virtualized viewport, so the live check scrolled each cell into the DOM (pre-existing grid behaviour); createdAt/updatedAt keep their existing fa-IR formatter and were not part of the report; no user trial run, so no visual acceptance is claimed. 268 remains PASS-within-scope awaiting the user's visual look; no functional reopening.",
  "Eight scripts/tb266r1-*.mjs scratch files from the earlier 266-R1 round remain untracked and are not part of this change set.",
  "",
  "REVIEW URL: http://127.0.0.1:3000/fa/admin/fulfillments (user route /admin/fulfillments renders identically)",
  "Evidence: docs/ai/tasks/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/TASK.md and docs/architecture/evidence/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,date-display-proof.md,user-review.md}",
  "",
  "STOP_FOR_ARCHITECT",
  "automaticNextImplementationTask: NONE",
  "",
  "--- result-contract.txt ---",
  contract,
].join("\n");

const post = async (url, payload) => {
  const r = await fetch(BASE + url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });
  const text = await r.text();
  console.log(url, r.status, text.slice(0, 400));
  return r.status;
};
(async () => {
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Working",
  });
  const s = await post("/api/results", {
    channelId: "tooba-main",
    taskId: TASK_ROW,
    content: summary,
  });
  if (s >= 300) return;
  await post(`/api/tasks/${TASK_ROW}/complete`, undefined);
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Idle",
  });
})();
