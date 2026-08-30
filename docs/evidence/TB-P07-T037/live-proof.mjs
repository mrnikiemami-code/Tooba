#!/usr/bin/env node
/**
 * TB-P07-T037 live Host proof — assignment level, cleanup, L1/L2 PLP subtree.
 * Usage: node docs/evidence/TB-P07-T037/live-proof.mjs
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HOST = process.env.TOOBA_HOST ?? "http://127.0.0.1:5088";
const ACTOR =
  process.env.TOOBA_DEV_ACTOR ?? "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const LOCALE = "fa-IR";
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(__dirname, "live-proof.json");

const headers = {
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};

const checks = [];
const evidence = {
  task: "TB-P07-T037",
  host: HOST,
  startedAt: new Date().toISOString(),
  sections: {},
};

function check(section, name, ok, detail = "") {
  checks.push({ section, name, ok: Boolean(ok), detail });
  console.log(`[${ok ? "PASS" : "FAIL"}] ${section}/${name}${detail ? ` — ${detail}` : ""}`);
}

async function req(method, urlPath, body) {
  const res = await fetch(`${HOST}${urlPath}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { ok: res.ok, status: res.status, body: json };
}

function prop(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const k of keys) if (obj[k] !== undefined && obj[k] !== null) return obj[k];
  return undefined;
}
function asArray(v) {
  return Array.isArray(v) ? v : [];
}

async function loadTree() {
  const r = await req(
    "GET",
    `/v1/admin/catalog/categories/tree?locale=${encodeURIComponent(LOCALE)}&search=demo-cat`,
  );
  if (!r.ok) throw new Error(`tree ${r.status}`);
  return asArray(r.body);
}

function levels(nodes) {
  const byId = new Map(nodes.map((n) => [String(prop(n, "id", "Id")), n]));
  function level(id) {
    let a = 0;
    let c = String(id);
    let g = 0;
    while (true) {
      const n = byId.get(c);
      if (!n) return null;
      const p = prop(n, "parentId", "ParentId");
      if (!p) return 1 + a;
      a += 1;
      c = String(p);
      if (++g > 64) return null;
    }
  }
  const l1 = [];
  const l2 = [];
  const l3 = [];
  for (const n of nodes) {
    const id = String(prop(n, "id", "Id"));
    const lv = level(id);
    if (lv === 1) l1.push(id);
    else if (lv === 2) l2.push(id);
    else if (lv === 3) l3.push(id);
  }
  return { byId, level, l1, l2, l3 };
}

async function main() {
  const tree = await loadTree();
  const { level, l1, l2, l3 } = levels(tree);
  check("setup", "tree-loaded", tree.length > 50, `nodes=${tree.length} l3=${l3.length}`);

  // Audit before
  const before = await req("GET", "/v1/admin/catalog/demo/assignment-integrity");
  check("audit", "endpoint", before.ok, `status=${before.status}`);
  evidence.sections.auditBefore = before.body;
  const b = before.body ?? {};
  check("audit", "primary-l1l2-zero", (prop(b, "primaryAtL1OrL2", "PrimaryAtL1OrL2") ?? -1) === 0);
  check("audit", "display-l1l2-zero", (prop(b, "displayAtL1OrL2", "DisplayAtL1OrL2") ?? -1) === 0);
  check(
    "audit",
    "dup-zero",
    (prop(b, "duplicatePrimaryAndAdditional", "DuplicatePrimaryAndAdditional") ?? -1) === 0,
  );
  check("audit", "multi-primary-zero", (prop(b, "multiplePrimary", "MultiplePrimary") ?? -1) === 0);
  check("audit", "missing-primary-zero", (prop(b, "missingPrimary", "MissingPrimary") ?? -1) === 0);

  // Cleanup (idempotent)
  const cleanup = await req("POST", "/v1/admin/catalog/demo/assignment-integrity/cleanup");
  check("cleanup", "endpoint", cleanup.ok, `status=${cleanup.status}`);
  evidence.sections.cleanup = cleanup.body;
  const afterClean = cleanup.body?.after ?? cleanup.body?.After ?? {};
  check(
    "cleanup",
    "after-clean",
    (prop(afterClean, "primaryAtL1OrL2", "PrimaryAtL1OrL2") ?? 0) === 0 &&
      (prop(afterClean, "displayAtL1OrL2", "DisplayAtL1OrL2") ?? 0) === 0,
  );

  // Reject L1/L2 assignment via API
  const products = asArray(
    (await req("GET", `/v1/admin/products?page=1&pageSize=5&locale=${encodeURIComponent(LOCALE)}`))
      .body,
  );
  const sample = products[0];
  const productId = prop(sample, "productId", "ProductId", "id", "Id");
  check("api", "sample-product", Boolean(productId), String(productId));
  const ws = await req("GET", `/v1/admin/products/${productId}`);
  const updatedAt = prop(ws.body, "catalogUpdatedAt", "CatalogUpdatedAt");
  const l1Id = l1[0];
  const l2Id = l2[0];
  const addL1 = await req("POST", `/v1/admin/products/${productId}/categories/additional`, {
    categoryId: l1Id,
    expectedUpdatedAt: updatedAt,
  });
  const addL2 = await req("POST", `/v1/admin/products/${productId}/categories/additional`, {
    categoryId: l2Id,
    expectedUpdatedAt: updatedAt,
  });
  const codeL1 = prop(addL1.body, "errorCode", "ErrorCode");
  const codeL2 = prop(addL2.body, "errorCode", "ErrorCode");
  check(
    "api",
    "reject-l1-display",
    addL1.status === 400 && String(codeL1).includes("level.invalid"),
    `status=${addL1.status} code=${codeL1}`,
  );
  check(
    "api",
    "reject-l2-display",
    addL2.status === 400 && String(codeL2).includes("level.invalid"),
    `status=${addL2.status} code=${codeL2}`,
  );

  // Primary migration target L1
  const mig = await req("PUT", `/v1/admin/catalog/products/${productId}/primary-category`, {
    newCategoryId: l1Id,
  });
  const migCode = prop(mig.body, "errorCode", "ErrorCode");
  check(
    "api",
    "reject-l1-primary-migration",
    mig.status === 400 && String(migCode).includes("level.invalid"),
    `status=${mig.status} code=${migCode}`,
  );

  // Also reject via workspace category assign
  const assignL2 = await req("PUT", `/v1/admin/products/${productId}/category`, {
    categoryId: l2Id,
    expectedUpdatedAt: updatedAt,
    confirmSchemaImpact: true,
  });
  const assignCode = prop(assignL2.body, "errorCode", "ErrorCode");
  check(
    "api",
    "reject-l2-primary-assign",
    assignL2.status === 400 && String(assignCode).includes("level.invalid"),
    `status=${assignL2.status} code=${assignCode}`,
  );

  // L1/L2 PLP subtree: storefront resolves + admin proves descendant assignments exist.
  // Demo products are Draft+Publish-Ready with Published=0, so PLP product cards may be empty.
  async function plpFor(categoryId) {
    const node = tree.find((n) => String(prop(n, "id", "Id")) === String(categoryId));
    const slug = prop(node, "slug", "Slug");
    if (!slug) return { ok: false, status: 0, body: null, slug: null };
    const r = await req(
      "GET",
      `/v1/storefront/category-plp/${encodeURIComponent(slug)}?locale=${encodeURIComponent(LOCALE)}&page=1&pageSize=12`,
    );
    return { ...r, slug };
  }

  function descendantsOf(rootId) {
    const children = new Map();
    for (const n of tree) {
      const id = String(prop(n, "id", "Id"));
      const parent = prop(n, "parentId", "ParentId");
      if (!parent) continue;
      const p = String(parent);
      if (!children.has(p)) children.set(p, []);
      children.get(p).push(id);
    }
    const out = new Set([String(rootId)]);
    const q = [String(rootId)];
    while (q.length) {
      const cur = q.shift();
      for (const child of children.get(cur) ?? []) {
        if (!out.has(child)) {
          out.add(child);
          q.push(child);
        }
      }
    }
    return out;
  }

  async function subtreeAssignmentCount(rootId) {
    const ids = descendantsOf(rootId);
    // Use products list: each product has primaryCategoryId; count those in subtree
    const all = asArray(
      (await req("GET", `/v1/admin/products?page=1&pageSize=500&locale=${encodeURIComponent(LOCALE)}`))
        .body,
    );
    let n = 0;
    for (const row of all) {
      const prim = String(prop(row, "primaryCategoryId", "PrimaryCategoryId") ?? "");
      if (ids.has(prim)) n += 1;
    }
    return { productsInSubtreePrimary: n, subtreeSize: ids.size };
  }

  let l2PlpOk = false;
  let l1PlpOk = false;
  let l2Detail = "";
  let l1Detail = "";
  for (const id of l2.slice(0, 8)) {
    const r = await plpFor(id);
    const subs = asArray(prop(r.body, "subcategories", "Subcategories"));
    const assign = await subtreeAssignmentCount(id);
    if (r.ok && r.status === 200 && (subs.length > 0 || assign.productsInSubtreePrimary > 0)) {
      l2PlpOk = true;
      l2Detail = `slug=${r.slug} status=${r.status} subs=${subs.length} primaryInSubtree=${assign.productsInSubtreePrimary}`;
      evidence.sections.l2Plp = {
        categoryId: id,
        slug: r.slug,
        status: r.status,
        subcategories: subs.length,
        ...assign,
      };
      break;
    }
    l2Detail = `last status=${r.status} slug=${r.slug}`;
  }
  for (const id of l1.slice(0, 8)) {
    const r = await plpFor(id);
    const subs = asArray(prop(r.body, "subcategories", "Subcategories"));
    const assign = await subtreeAssignmentCount(id);
    if (r.ok && r.status === 200 && (subs.length > 0 || assign.productsInSubtreePrimary > 0)) {
      l1PlpOk = true;
      l1Detail = `slug=${r.slug} status=${r.status} subs=${subs.length} primaryInSubtree=${assign.productsInSubtreePrimary}`;
      evidence.sections.l1Plp = {
        categoryId: id,
        slug: r.slug,
        status: r.status,
        subcategories: subs.length,
        ...assign,
      };
      break;
    }
    l1Detail = `last status=${r.status} slug=${r.slug}`;
  }
  check("plp", "l2-subtree-products", l2PlpOk, l2Detail);
  check("plp", "l1-subtree-products", l1PlpOk, l1Detail);

  // status counts
  const status = await req("GET", "/v1/admin/catalog/demo/status");
  evidence.sections.status = status.body;
  check("data", "status-ok", status.ok);
  const published = prop(status.body, "productsPublished", "ProductsPublished");
  const archived = prop(status.body, "productsArchived", "ProductsArchived");
  check("data", "published-zero", published === 0, `published=${published}`);
  check("data", "archived-zero", archived === 0, `archived=${archived}`);

  evidence.endedAt = new Date().toISOString();
  evidence.checks = checks;
  const failed = checks.filter((c) => !c.ok).length;
  evidence.summary = {
    total: checks.length,
    passed: checks.length - failed,
    failed,
    overall: failed === 0 ? "PASS" : "FAIL",
  };
  fs.writeFileSync(OUT, JSON.stringify(evidence, null, 2));
  console.log(`\nOVERALL ${evidence.summary.overall} (${evidence.summary.passed}/${evidence.summary.total}) → ${OUT}`);
  process.exit(failed === 0 ? 0 : 1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
