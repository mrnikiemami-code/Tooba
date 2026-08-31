#!/usr/bin/env node
/**
 * TB-P07-T038 live Host proof — leaf primary + additional arrays on product grid.
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HOST = process.env.TOOBA_HOST ?? "http://127.0.0.1:5088";
const ACTOR = process.env.TOOBA_DEV_ACTOR ?? "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(__dirname, "live-proof.json");

const headers = {
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};

const checks = [];
function check(name, ok, detail = "") {
  checks.push({ name, ok: Boolean(ok), detail });
  console.log(`[${ok ? "PASS" : "FAIL"}] ${name}${detail ? ` — ${detail}` : ""}`);
}

async function req(method, urlPath, body) {
  const res = await fetch(`${HOST}${urlPath}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = text; }
  return { ok: res.ok, status: res.status, body: json };
}

function prop(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const k of keys) if (obj[k] !== undefined && obj[k] !== null) return obj[k];
  return undefined;
}

const health = await req("GET", "/health");
check("health", health.ok, `status=${health.status}`);

const grid = await req("POST", "/v1/admin/products/query", {
  page: 1,
  pageSize: 50,
  sort: [{ field: "updatedAt", direction: "desc" }],
  filters: [],
});

check("product-grid-query", grid.ok, `status=${grid.status}`);
const items = Array.isArray(prop(grid.body, "items", "Items"))
  ? prop(grid.body, "items", "Items")
  : [];

let leafOk = items.length > 0;
let hasPrimary = false;
let hasAdditional = false;
let hasMoreThanThree = false;
let pathLeak = false;
for (const row of items) {
  const primary = prop(row, "primaryCategoryName", "PrimaryCategoryName");
  const additional = prop(row, "additionalCategoryNames", "AdditionalCategoryNames") || [];
  const summary = String(prop(row, "categorySummary", "CategorySummary") || "");
  const count = prop(row, "additionalCategoryCount", "AdditionalCategoryCount");
  if (primary) hasPrimary = true;
  if (Array.isArray(additional) && additional.length) hasAdditional = true;
  if (Array.isArray(additional) && additional.length > 3) hasMoreThanThree = true;
  if (primary && String(primary).includes(" > ")) { pathLeak = true; leafOk = false; }
  if (Array.isArray(additional) && additional.some((n) => String(n).includes(" > "))) { pathLeak = true; leafOk = false; }
  if (summary.includes(" > ")) { pathLeak = true; leafOk = false; }
  if (!("primaryCategoryName" in row || "PrimaryCategoryName" in row)) leafOk = false;
  if (!("additionalCategoryNames" in row || "AdditionalCategoryNames" in row)) leafOk = false;
  if (typeof count !== "number" && count !== undefined) leafOk = false;
}

check("grid-leaf-only-fields", leafOk && !pathLeak, JSON.stringify({ n: items.length, hasPrimary, hasAdditional, hasMoreThanThree, pathLeak }));
check("grid-has-primary-sample", hasPrimary, "at least one primary leaf on page");
check("grid-has-additional-sample", hasAdditional || items.length === 0, "additional arrays present when memberships exist");

const evidence = {
  task: "TB-P07-T038",
  host: HOST,
  finishedAt: new Date().toISOString(),
  checks,
  sample: items[0] || null,
};
fs.writeFileSync(OUT, JSON.stringify(evidence, null, 2));
if (checks.some((c) => !c.ok)) process.exit(1);
console.log("ALL PASS");
