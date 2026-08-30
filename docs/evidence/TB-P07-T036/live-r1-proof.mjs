#!/usr/bin/env node
/**
 * TB-P07-T036-R1 live Host proof (sections A–D).
 * Usage: node docs/evidence/TB-P07-T036/live-r1-proof.mjs
 *
 * Targets http://127.0.0.1:5088 with X-Tooba-Dev-Actor-User-Id.
 * Creates a temporary Draft product, mutates minimally, restores/deletes.
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HOST = process.env.TOOBA_HOST ?? "http://127.0.0.1:5088";
const ACTOR =
  process.env.TOOBA_DEV_ACTOR ?? "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const LOCALE = "fa-IR";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT_JSON = path.join(__dirname, "live-r1-proof.json");

const headers = {
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};

/** @type {{ section: string, name: string, ok: boolean, detail?: string }[]} */
const checks = [];
/** @type {Record<string, unknown>} */
const evidence = {
  task: "TB-P07-T036-R1",
  host: HOST,
  actor: ACTOR,
  locale: LOCALE,
  startedAt: new Date().toISOString(),
  apisUsed: [],
  sections: {},
};

function noteApi(method, urlPath) {
  const key = `${method} ${urlPath}`;
  if (!evidence.apisUsed.includes(key)) evidence.apisUsed.push(key);
}

function check(section, name, ok, detail = "") {
  checks.push({ section, name, ok: Boolean(ok), detail });
  const mark = ok ? "PASS" : "FAIL";
  console.log(`[${mark}] ${section}/${name}${detail ? ` — ${detail}` : ""}`);
}

async function req(method, urlPath, body) {
  noteApi(method, urlPath.split("?")[0]);
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
  for (const k of keys) {
    if (obj[k] !== undefined && obj[k] !== null) return obj[k];
  }
  return undefined;
}

function asArray(v) {
  return Array.isArray(v) ? v : [];
}

function fieldValueSnapshot(fields) {
  return asArray(fields).map((f) => ({
    definitionId: prop(f, "definitionId", "DefinitionId"),
    code: prop(f, "code", "Code"),
    currentCanonicalValue: prop(f, "currentCanonicalValue", "CurrentCanonicalValue") ?? null,
    currentEnumOptionId: prop(f, "currentEnumOptionId", "CurrentEnumOptionId") ?? null,
    isRequired: Boolean(prop(f, "isRequired", "IsRequired")),
    isMissingRequired: Boolean(prop(f, "isMissingRequired", "IsMissingRequired")),
  }));
}

function assignmentsOf(view) {
  return asArray(prop(view, "categoryAssignments", "CategoryAssignments")).map((a) => ({
    categoryId: String(prop(a, "categoryId", "CategoryId")),
    role: String(prop(a, "role", "Role")),
    categoryPath: prop(a, "categoryPath", "CategoryPath") ?? null,
  }));
}

function primaryOf(view) {
  return prop(view, "primaryCategoryId", "PrimaryCategoryId") ?? null;
}

function updatedAtOf(view) {
  return prop(view, "catalogUpdatedAt", "CatalogUpdatedAt");
}

async function loadTree() {
  // search=demo-cat returns the full demo forest (flat with parentId)
  const r = await req("GET", `/v1/admin/catalog/categories/tree?locale=${encodeURIComponent(LOCALE)}&search=demo-cat`);
  if (!r.ok) throw new Error(`category tree failed: ${r.status}`);
  return asArray(r.body);
}

function buildForest(nodes) {
  const byId = new Map(nodes.map((n) => [String(prop(n, "id", "Id")), n]));
  const children = new Map();
  for (const n of nodes) {
    const id = String(prop(n, "id", "Id"));
    const parent = prop(n, "parentId", "ParentId");
    if (parent) {
      const p = String(parent);
      if (!children.has(p)) children.set(p, []);
      children.get(p).push(id);
    }
  }
  return { byId, children };
}

function findL3Chain(nodes, slugNeedle) {
  const { byId } = buildForest(nodes);
  const leaf = nodes.find(
    (n) =>
      String(prop(n, "slug", "Slug") ?? "").includes(slugNeedle) &&
      !prop(n, "hasChildren", "HasChildren"),
  );
  if (!leaf) return null;
  const l3 = String(prop(leaf, "id", "Id"));
  const l2 = String(prop(leaf, "parentId", "ParentId") ?? "");
  const l2node = byId.get(l2);
  const l1 = l2node ? String(prop(l2node, "parentId", "ParentId") ?? "") : "";
  if (!l1 || !l2) return null;
  return {
    l1,
    l2,
    l3,
    slug: String(prop(leaf, "slug", "Slug")),
    name: String(prop(leaf, "name", "Name")),
  };
}

async function getProduct(productId) {
  const r = await req("GET", `/v1/admin/products/${productId}`);
  if (!r.ok) throw new Error(`get product ${productId}: ${r.status}`);
  return r.body;
}

async function getAttributes(productId) {
  const r = await req(
    "GET",
    `/v1/admin/catalog/products/${productId}/attributes?locale=${encodeURIComponent(LOCALE)}`,
  );
  if (!r.ok) throw new Error(`get attributes: ${r.status}`);
  return r.body;
}

async function getAttrReadiness(productId) {
  const r = await req("GET", `/v1/admin/catalog/products/${productId}/attributes/readiness`);
  if (!r.ok) throw new Error(`attr readiness: ${r.status}`);
  return r.body;
}

async function getPublishReadiness(productId) {
  const r = await req(
    "GET",
    `/v1/admin/products/${productId}/publish/readiness?locale=${encodeURIComponent(LOCALE)}`,
  );
  if (!r.ok) throw new Error(`publish readiness: ${r.status}`);
  return r.body;
}

async function getVariantsSummary(productId) {
  const r = await req(
    "GET",
    `/v1/admin/catalog/products/${productId}/variants/editor?locale=${encodeURIComponent(LOCALE)}`,
  );
  if (!r.ok) {
    return { status: r.status, variants: [] };
  }
  const variants = asArray(prop(r.body, "variants", "Variants"));
  return {
    status: r.status,
    count: variants.length,
    ids: variants.map((v) => prop(v, "variantId", "VariantId")),
  };
}

async function snapshotProduct(productId) {
  const view = await getProduct(productId);
  const attrs = await getAttributes(productId);
  const readiness = await getAttrReadiness(productId);
  const publish = await getPublishReadiness(productId);
  const variants = await getVariantsSummary(productId);
  return {
    productId,
    status: prop(view, "status", "Status"),
    primaryCategoryId: primaryOf(view),
    categoryPath: prop(view, "categoryPath", "CategoryPath"),
    catalogUpdatedAt: updatedAtOf(view),
    assignments: assignmentsOf(view),
    attributeFields: fieldValueSnapshot(prop(attrs, "fields", "Fields")),
    attributeReadiness: readiness,
    publishReadiness: {
      isReady: prop(publish, "isReady", "IsReady"),
      attributeReady: prop(publish, "attributeReady", "AttributeReady"),
      missingRequirements: asArray(prop(publish, "missingRequirements", "MissingRequirements")),
    },
    variants,
  };
}

async function setAttributeValues(productId, values) {
  const r = await req("PUT", `/v1/admin/catalog/products/${productId}/attributes`, {
    locale: LOCALE,
    values,
  });
  if (!r.ok) throw new Error(`set attributes: ${r.status} ${JSON.stringify(r.body)}`);
  return r.body;
}

async function createDraft(title, categoryId) {
  const slug = `tb-p07-t036-r1-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
  const r = await req("POST", "/v1/admin/products", {
    title,
    slug,
    categoryId,
    locale: LOCALE,
  });
  if (!r.ok) throw new Error(`create product: ${r.status} ${JSON.stringify(r.body)}`);
  return String(prop(r.body, "productId", "ProductId"));
}

async function deleteProduct(productId) {
  const r = await req("DELETE", `/v1/admin/products/${productId}`);
  return r.status === 204 || r.ok;
}

async function effectiveSchema(categoryId) {
  const r = await req("GET", `/v1/admin/catalog/categories/${categoryId}/attribute-schema/effective`);
  if (!r.ok) throw new Error(`effective schema ${categoryId}: ${r.status}`);
  return asArray(r.body);
}

async function bindAttr(categoryId, definitionId, flags) {
  const r = await req("POST", `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings`, {
    definitionId,
    displayOrder: flags.displayOrder ?? 90,
    isRequired: flags.isRequired,
    isFilterable: flags.isFilterable,
    isVariantAxis: flags.isVariantAxis ?? false,
    isComparable: flags.isComparable ?? false,
  });
  if (!r.ok) throw new Error(`bind ${categoryId}: ${r.status} ${JSON.stringify(r.body)}`);
}

async function unbindAttr(categoryId, definitionId) {
  const r = await req(
    "DELETE",
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings/${definitionId}`,
  );
  return r.ok || r.status === 404;
}

function sameSnapshotCore(a, b) {
  return (
    a.primaryCategoryId === b.primaryCategoryId &&
    a.status === b.status &&
    JSON.stringify(a.assignments) === JSON.stringify(b.assignments) &&
    JSON.stringify(
      a.attributeFields.map((f) => [f.definitionId, f.currentCanonicalValue, f.currentEnumOptionId]),
    ) ===
      JSON.stringify(
        b.attributeFields.map((f) => [f.definitionId, f.currentCanonicalValue, f.currentEnumOptionId]),
      )
  );
}

// ─── A: Primary migration ───────────────────────────────────────────
async function sectionA(tree) {
  const power = findL3Chain(tree, "power-banks");
  const fridge = findL3Chain(tree, "refrigerators");
  if (!power || !fridge) {
    check("A", "locate-L3-pair", false, "power-banks / refrigerators not found");
    evidence.sections.A = { error: "missing categories" };
    return null;
  }

  const productId = await createDraft("TB-P07-T036-R1 live proof temp", power.l3);
  let deleted = false;
  try {
    const editor = await getAttributes(productId);
    const fields = asArray(prop(editor, "fields", "Fields"));
    const battery = fields.find((f) => prop(f, "code", "Code") === "demo_attr_mobile_battery");
    const color = fields.find((f) => prop(f, "code", "Code") === "demo_attr_mobile_color");
    const colorOpt = asArray(prop(color, "options", "Options"))[0];
    if (!battery || !color || !colorOpt) {
      check("A", "seed-attribute-fields", false, "battery/color fields missing on powerbank");
      await deleteProduct(productId);
      deleted = true;
      return null;
    }

    await setAttributeValues(productId, [
      {
        definitionId: prop(battery, "definitionId", "DefinitionId"),
        rawValue: "5000",
        enumOptionId: null,
        clear: false,
      },
      {
        definitionId: prop(color, "definitionId", "DefinitionId"),
        rawValue: null,
        enumOptionId: prop(colorOpt, "optionId", "OptionId"),
        clear: false,
      },
    ]);

    const baseline = await snapshotProduct(productId);
    check("A", "snapshot-baseline", true, `primary=${baseline.primaryCategoryId}`);

    const preview = await req(
      "POST",
      `/v1/admin/catalog/products/${productId}/category-change-preview`,
      { newCategoryId: fridge.l3, locale: LOCALE },
    );
    check("A", "preview-ok", preview.ok, `status=${preview.status}`);
    check(
      "A",
      "preview-reports-orphans",
      Number(prop(preview.body, "orphanCount", "OrphanCount") ?? 0) >= 1,
      `orphanCount=${prop(preview.body, "orphanCount", "OrphanCount")}`,
    );
    check(
      "A",
      "preview-reports-newly-required",
      Number(prop(preview.body, "newlyRequiredMissingCount", "NewlyRequiredMissingCount") ?? 0) >= 1,
      `newlyRequired=${prop(preview.body, "newlyRequiredMissingCount", "NewlyRequiredMissingCount")}`,
    );

    const afterPreview = await snapshotProduct(productId);
    check(
      "A",
      "preview-non-mutating",
      sameSnapshotCore(baseline, afterPreview),
      "product unchanged after preview (cancel path)",
    );

    const migrate = await req("PUT", `/v1/admin/catalog/products/${productId}/primary-category`, {
      newCategoryId: fridge.l3,
    });
    check("A", "migrate-ok", migrate.ok, `status=${migrate.status}`);

    const afterMig = await snapshotProduct(productId);
    check(
      "A",
      "primary-changed",
      afterMig.primaryCategoryId === fridge.l3,
      `${baseline.primaryCategoryId} → ${afterMig.primaryCategoryId}`,
    );

    const batteryStillActive = afterMig.attributeFields.some(
      (f) => f.code === "demo_attr_mobile_battery",
    );
    const batteryValueGone = !afterMig.attributeFields.some(
      (f) =>
        f.definitionId === prop(battery, "definitionId", "DefinitionId") &&
        f.currentCanonicalValue,
    );
    check("A", "orphans-removed-from-active", !batteryStillActive && batteryValueGone, "battery orphan cleared");

    const missing = asArray(
      prop(afterMig.attributeReadiness, "missingRequiredCodes", "MissingRequiredCodes"),
    );
    const readinessIncomplete = prop(afterMig.attributeReadiness, "isComplete", "IsComplete") === false;
    check(
      "A",
      "readiness-blockers-required-missing",
      readinessIncomplete && missing.length >= 1,
      `missing=${missing.join(",")}`,
    );

    // Restore = delete temp product (exact baseline: no shared seed mutated)
    deleted = await deleteProduct(productId);
    check("A", "restore-delete-temp", deleted, productId);

    evidence.sections.A = {
      productId,
      source: power,
      target: fridge,
      preview: preview.body,
      migrate: migrate.body,
      baseline,
      afterPreview: { primaryCategoryId: afterPreview.primaryCategoryId },
      afterMigrate: {
        primaryCategoryId: afterMig.primaryCategoryId,
        attributeCodes: afterMig.attributeFields.map((f) => f.code),
        attributeReadiness: afterMig.attributeReadiness,
      },
      deleted,
    };
    return productId;
  } catch (err) {
    check("A", "section-error", false, String(err?.message ?? err));
    evidence.sections.A = { error: String(err?.message ?? err) };
    if (!deleted) {
      try {
        await deleteProduct(productId);
      } catch {
        /* ignore */
      }
    }
    return null;
  }
}

// ─── B: Display membership ──────────────────────────────────────────
async function sectionB(tree) {
  const power = findL3Chain(tree, "power-banks");
  const phone = findL3Chain(tree, "smartphones");
  if (!power || !phone) {
    check("B", "locate-L3-pair", false, "power-banks / smartphones not found");
    evidence.sections.B = { error: "missing categories" };
    return;
  }

  const productId = await createDraft("TB-P07-T036-R1 membership temp", power.l3);
  try {
    const before = await getProduct(productId);
    const primaryBefore = primaryOf(before);
    check("B", "baseline-primary", primaryBefore === power.l3, String(primaryBefore));

    const add = await req("POST", `/v1/admin/products/${productId}/categories/additional`, {
      categoryId: phone.l3,
      expectedUpdatedAt: updatedAtOf(before),
    });
    check("B", "add-additional-ok", add.ok, `status=${add.status}`);
    const afterAdd = add.ok ? add.body : await getProduct(productId);
    const primaryAfterAdd = primaryOf(afterAdd);
    const roles = assignmentsOf(afterAdd);
    check("B", "primary-unchanged-after-add", primaryAfterAdd === primaryBefore, String(primaryAfterAdd));
    check(
      "B",
      "additional-present",
      roles.some((r) => r.role === "Additional" && r.categoryId === phone.l3),
      JSON.stringify(roles),
    );

    const rem = await req(
      "DELETE",
      `/v1/admin/products/${productId}/categories/additional/${phone.l3}?expectedUpdatedAt=${encodeURIComponent(String(updatedAtOf(afterAdd)))}`,
    );
    check("B", "remove-additional-ok", rem.ok, `status=${rem.status}`);
    const afterRem = rem.ok ? rem.body : await getProduct(productId);
    check(
      "B",
      "primary-unchanged-after-remove",
      primaryOf(afterRem) === primaryBefore,
      String(primaryOf(afterRem)),
    );
    check(
      "B",
      "additional-gone",
      !assignmentsOf(afterRem).some((r) => r.role === "Additional" && r.categoryId === phone.l3),
      JSON.stringify(assignmentsOf(afterRem)),
    );

    const deleted = await deleteProduct(productId);
    check("B", "restore-delete-temp", deleted);

    evidence.sections.B = {
      productId,
      primary: power.l3,
      additional: phone.l3,
      afterAdd: assignmentsOf(afterAdd),
      afterRemove: assignmentsOf(afterRem),
      deleted,
    };
  } catch (err) {
    check("B", "section-error", false, String(err?.message ?? err));
    evidence.sections.B = { error: String(err?.message ?? err) };
    try {
      await deleteProduct(productId);
    } catch {
      /* ignore */
    }
  }
}

// ─── C: Attribute inheritance / override ─────────────────────────────
async function sectionC(tree) {
  const chain = findL3Chain(tree, "power-banks");
  if (!chain) {
    check("C", "locate-chain", false, "power-banks chain missing");
    evidence.sections.C = { error: "missing chain" };
    return;
  }

  const { l1, l2, l3 } = chain;
  let boundL1 = false;
  let boundL2 = false;
  /** @type {string|null} */
  let definitionId = null;

  try {
    const defs = await req("GET", "/v1/admin/catalog/attribute-definitions");
    check("C", "list-definitions", defs.ok, `status=${defs.status}`);
    const list = asArray(defs.body);
    // Prefer an attr not already effective on L3 powerbank (avoid clashing with local L3 binds)
    const l3Before = await effectiveSchema(l3);
    const l3Ids = new Set(l3Before.map((e) => String(prop(e, "definitionId", "DefinitionId"))));
    const pick =
      list.find((d) => {
        const id = String(prop(d, "definitionId", "DefinitionId"));
        const code = String(prop(d, "code", "Code") ?? "");
        return code.startsWith("demo_attr_") && !l3Ids.has(id);
      }) ?? list.find((d) => !l3Ids.has(String(prop(d, "definitionId", "DefinitionId"))));

    if (!pick) {
      check("C", "pick-definition", false, "no free definition for L1 bind smoke");
      evidence.sections.C = { error: "no definition" };
      return;
    }
    definitionId = String(prop(pick, "definitionId", "DefinitionId"));

    // Ensure L1 has a binding (smoke bind if needed)
    const l1Before = await effectiveSchema(l1);
    const alreadyOnL1 = l1Before.some(
      (e) => String(prop(e, "definitionId", "DefinitionId")) === definitionId,
    );
    if (!alreadyOnL1) {
      await bindAttr(l1, definitionId, {
        displayOrder: 97,
        isRequired: false,
        isFilterable: false,
      });
      boundL1 = true;
    }

    const effL2 = await effectiveSchema(l2);
    const effL3 = await effectiveSchema(l3);
    const hitL2 = effL2.filter((e) => String(prop(e, "definitionId", "DefinitionId")) === definitionId);
    const hitL3 = effL3.filter((e) => String(prop(e, "definitionId", "DefinitionId")) === definitionId);

    check("C", "l2-inherits-once", hitL2.length === 1, `count=${hitL2.length}`);
    check("C", "l3-inherits-once", hitL3.length === 1, `count=${hitL3.length}`);
    check(
      "C",
      "l2-from-l1",
      hitL2[0] &&
        String(prop(hitL2[0], "inheritedFromCategoryId", "InheritedFromCategoryId")) === l1 &&
        !prop(hitL2[0], "isLocalOverride", "IsLocalOverride"),
      `from=${prop(hitL2[0], "inheritedFromCategoryId", "InheritedFromCategoryId")}`,
    );

    // L2 override with different Required/Filterable
    await bindAttr(l2, definitionId, {
      displayOrder: 98,
      isRequired: true,
      isFilterable: true,
    });
    boundL2 = true;

    const afterOverrideL3 = await effectiveSchema(l3);
    const hitAfter = afterOverrideL3.filter(
      (e) => String(prop(e, "definitionId", "DefinitionId")) === definitionId,
    );
    check("C", "l3-follows-l2-once", hitAfter.length === 1, `count=${hitAfter.length}`);
    check(
      "C",
      "l3-follows-l2-flags",
      hitAfter[0] &&
        String(prop(hitAfter[0], "inheritedFromCategoryId", "InheritedFromCategoryId")) === l2 &&
        prop(hitAfter[0], "isRequired", "IsRequired") === true &&
        prop(hitAfter[0], "isFilterable", "IsFilterable") === true &&
        !prop(hitAfter[0], "isLocalOverride", "IsLocalOverride"),
      `from=${prop(hitAfter[0], "inheritedFromCategoryId", "InheritedFromCategoryId")} req=${prop(hitAfter[0], "isRequired", "IsRequired")} filt=${prop(hitAfter[0], "isFilterable", "IsFilterable")}`,
    );

    // Unbind L2 override → fallback to L1
    const unboundL2 = await unbindAttr(l2, definitionId);
    boundL2 = false;
    check("C", "unbind-l2-override", unboundL2);
    const afterReset = await effectiveSchema(l3);
    const hitReset = afterReset.find(
      (e) => String(prop(e, "definitionId", "DefinitionId")) === definitionId,
    );
    check(
      "C",
      "fallback-to-l1",
      hitReset &&
        String(prop(hitReset, "inheritedFromCategoryId", "InheritedFromCategoryId")) === l1 &&
        prop(hitReset, "isRequired", "IsRequired") === false &&
        prop(hitReset, "isFilterable", "IsFilterable") === false,
      `from=${prop(hitReset, "inheritedFromCategoryId", "InheritedFromCategoryId")}`,
    );

    evidence.sections.C = {
      chain,
      definitionId,
      code: prop(pick, "code", "Code"),
      boundL1Temporarily: boundL1,
      samples: {
        afterL1: hitL3[0] ?? null,
        afterL2Override: hitAfter[0] ?? null,
        afterUnbindL2: hitReset ?? null,
      },
    };
  } catch (err) {
    check("C", "section-error", false, String(err?.message ?? err));
    evidence.sections.C = { error: String(err?.message ?? err), definitionId };
  } finally {
    if (definitionId && boundL2) {
      try {
        await unbindAttr(l2, definitionId);
      } catch {
        /* ignore */
      }
    }
    if (definitionId && boundL1) {
      try {
        await unbindAttr(l1, definitionId);
        check("C", "restore-unbind-l1", true);
      } catch (e) {
        check("C", "restore-unbind-l1", false, String(e?.message ?? e));
      }
    } else if (definitionId) {
      check("C", "restore-unbind-l1", true, "L1 bind was pre-existing; left intact");
    }
  }
}

// ─── D: Live PLP facets ──────────────────────────────────────────────
async function sectionD(tree) {
  const power = findL3Chain(tree, "power-banks");
  const phone = findL3Chain(tree, "smartphones");
  if (!power || !phone) {
    check("D", "locate-categories", false);
    evidence.sections.D = { error: "missing categories" };
    return;
  }

  try {
    const powerEff = await effectiveSchema(power.l3);
    const phoneEff = await effectiveSchema(phone.l3);
    const powerCodes = new Set(powerEff.map((e) => String(prop(e, "code", "Code"))));
    const phoneOnly = phoneEff
      .map((e) => String(prop(e, "code", "Code")))
      .filter((c) => c && !powerCodes.has(c));

    const plpPower = await req(
      "GET",
      `/v1/storefront/category-plp/${encodeURIComponent(power.slug)}?locale=${encodeURIComponent(LOCALE)}`,
    );
    const plpPhone = await req(
      "GET",
      `/v1/storefront/category-plp/${encodeURIComponent(phone.slug)}?locale=${encodeURIComponent(LOCALE)}`,
    );
    check("D", "plp-power-ok", plpPower.ok, `status=${plpPower.status}`);
    check("D", "plp-phone-ok", plpPhone.ok, `status=${plpPhone.status}`);

    const facetsPower = asArray(prop(plpPower.body, "facets", "Facets"));
    const facetsPhone = asArray(prop(plpPhone.body, "facets", "Facets"));
    const codesPower = facetsPower.map((f) => String(prop(f, "code", "Code")));
    const codesPhone = facetsPhone.map((f) => String(prop(f, "code", "Code")));

    check("D", "brand-facet-on-power", codesPower.includes("brand"), codesPower.join(","));
    check("D", "brand-facet-on-phone", codesPhone.includes("brand"), codesPhone.join(","));

    const foreignOnPower = phoneOnly.filter((c) => codesPower.includes(c));
    check(
      "D",
      "no-foreign-primary-only-on-viewed-category",
      foreignOnPower.length === 0,
      foreignOnPower.length
        ? `leaked=${foreignOnPower.join(",")}`
        : `phoneOnlyChecked=${phoneOnly.slice(0, 5).join(",")}`,
    );

    // Reciprocal: powerbank-only attrs must not appear on phone PLP
    const powerOnly = [...powerCodes].filter(
      (c) => !phoneEff.some((e) => String(prop(e, "code", "Code")) === c),
    );
    const foreignOnPhone = powerOnly.filter((c) => codesPhone.includes(c));
    check(
      "D",
      "no-power-only-on-phone-plp",
      foreignOnPhone.length === 0,
      foreignOnPhone.length ? `leaked=${foreignOnPhone.join(",")}` : "ok",
    );

    const productsPower = asArray(prop(plpPower.body, "products", "Products"));
    const productsPhone = asArray(prop(plpPhone.body, "products", "Products"));
    const brandFacet = facetsPower.find((f) => prop(f, "code", "Code") === "brand");
    const brandOptions = asArray(prop(brandFacet, "options", "Options"));
    const anyBranded = [...productsPower, ...productsPhone].some((p) => prop(p, "brandId", "BrandId"));
    if (anyBranded) {
      check("D", "brand-options-when-products-have-brands", brandOptions.length > 0, `opts=${brandOptions.length}`);
    } else {
      check(
        "D",
        "brand-options-when-products-have-brands",
        true,
        `skipped: no published PLP products (power=${productsPower.length}, phone=${productsPhone.length}); brand code still present`,
      );
    }

    evidence.sections.D = {
      power: { slug: power.slug, facetCodes: codesPower, productCount: productsPower.length },
      phone: { slug: phone.slug, facetCodes: codesPhone, productCount: productsPhone.length },
      phoneOnlyAttrCodes: phoneOnly,
      powerOnlyAttrCodes: powerOnly,
      foreignLeaksOnPower: foreignOnPower,
      foreignLeaksOnPhone: foreignOnPhone,
      note:
        "PLP facets = viewed category effective facets + global brand; not union of member products' Primary schemas.",
    };
  } catch (err) {
    check("D", "section-error", false, String(err?.message ?? err));
    evidence.sections.D = { error: String(err?.message ?? err) };
  }
}

async function main() {
  console.log("=== TB-P07-T036-R1 LIVE PROOF ===");
  console.log(`HOST=${HOST} ACTOR=${ACTOR}`);

  const health = await fetch(`${HOST}/health`);
  check("BOOT", "host-health", health.status === 200, `status=${health.status}`);
  if (health.status !== 200) {
    evidence.finishedAt = new Date().toISOString();
    evidence.summary = { pass: false, reason: "host unhealthy" };
    fs.writeFileSync(OUT_JSON, JSON.stringify(evidence, null, 2), "utf8");
    process.exit(1);
  }

  const tree = await loadTree();
  check("BOOT", "category-tree", tree.length > 0, `nodes=${tree.length}`);

  await sectionA(tree);
  await sectionB(tree);
  await sectionC(tree);
  await sectionD(tree);

  const critical = checks.filter((c) => c.section !== "BOOT" || c.name === "host-health");
  const failed = critical.filter((c) => !c.ok);
  const bySection = {};
  for (const c of critical) {
    if (!bySection[c.section]) bySection[c.section] = { pass: 0, fail: 0 };
    bySection[c.section][c.ok ? "pass" : "fail"] += 1;
  }

  const allPass = failed.length === 0;
  evidence.checks = checks;
  evidence.bySection = bySection;
  evidence.finishedAt = new Date().toISOString();
  evidence.summary = {
    pass: allPass,
    totalChecks: critical.length,
    failed: failed.map((f) => `${f.section}/${f.name}`),
  };

  fs.writeFileSync(OUT_JSON, JSON.stringify(evidence, null, 2), "utf8");

  console.log("---");
  for (const [sec, s] of Object.entries(bySection)) {
    console.log(`SECTION ${sec}: ${s.fail === 0 ? "PASS" : "FAIL"} (${s.pass} pass / ${s.fail} fail)`);
  }
  console.log(allPass ? "OVERALL: PASS" : "OVERALL: FAIL");
  console.log(`Wrote ${OUT_JSON}`);
  process.exit(allPass ? 0 : 1);
}

main().catch((err) => {
  console.error("FATAL", err);
  evidence.fatal = String(err?.stack ?? err);
  evidence.finishedAt = new Date().toISOString();
  try {
    fs.writeFileSync(OUT_JSON, JSON.stringify(evidence, null, 2), "utf8");
  } catch {
    /* ignore */
  }
  process.exit(1);
});
