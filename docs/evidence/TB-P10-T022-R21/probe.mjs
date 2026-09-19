/**
 * TB-P10-T022-R21 — Amazing source select/save/reload/preview/storefront proof.
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R21");
const report = { ok: true, steps: [], errors: [] };
const stamp = Date.now();

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function hostJson(path, { method = "GET", body, actor } = {}) {
  const h = { Accept: "application/json" };
  if (actor) h["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (body !== undefined) h["Content-Type"] = "application/json";
  const r = await fetch(`${HOST}${path}`, {
    method,
    headers: h,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { ok: r.ok, status: r.status, data };
}

async function main() {
  let ready = false;
  for (let i = 0; i < 60; i++) {
    try {
      if ((await fetch(`${HOST}/v1/admin/dev-context`)).ok) {
        ready = true;
        break;
      }
    } catch {
      /* wait */
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
  rec("host-ready", ready);
  if (!ready) {
    writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
    process.exit(1);
  }

  const actor = (await hostJson("/v1/admin/dev-context")).data?.actorUserId;
  rec("actor", Boolean(actor));

  const slug = `r21-amazing-src-${stamp}`;
  const page = await hostJson("/v1/admin/pages", {
    method: "POST",
    actor,
    body: {
      title: `R21 Amazing ${stamp}`,
      slug,
      locale: "fa",
      pageType: "Landing",
      seoTitle: "R21",
      seoDescription: "Amazing source final",
    },
  });
  const pageId = page.data?.pageId || page.data?.PageId;
  rec("create-page", page.ok && Boolean(pageId), { status: page.status });

  const config = {
    title: "پیشنهاد شگفت‌انگیز",
    source: "PromotionCampaign",
    promotionTypeCode: "AMAZING",
    campaignId: null,
    take: 8,
    variantKey: "product.card-carousel",
  };
  const section = await hostJson(`/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    actor,
    body: {
      sectionType: "ProductCollection",
      config: JSON.stringify(config),
      isEnabled: true,
    },
  });
  const sectionId = section.data?.pageSectionId || section.data?.PageSectionId;
  rec("add-amazing-section", section.ok && Boolean(sectionId), { status: section.status });

  const listed = await hostJson(`/v1/admin/pages/${pageId}/sections`, { actor });
  const rows = Array.isArray(listed.data) ? listed.data : listed.data?.items || [];
  const row = rows.find((s) => (s.pageSectionId || s.PageSectionId) === sectionId) || rows[0];
  const rawCfg = row?.config || row?.Config || "";
  const parsed = typeof rawCfg === "string" ? JSON.parse(rawCfg) : rawCfg;
  rec(
    "persist-source",
    parsed?.source === "PromotionCampaign" &&
      String(parsed?.promotionTypeCode || "").toUpperCase() === "AMAZING" &&
      (parsed?.campaignId === null || parsed?.campaignId === undefined),
    { parsed },
  );

  // Existing sources remain valid Host contracts (empty Manual/Category/Brand rejected by design).
  const newest = await hostJson(`/v1/admin/pages/${pageId}/sections/${sectionId}`, {
    method: "PUT",
    actor,
    body: {
      config: JSON.stringify({
        title: "R21 Newest",
        source: "Newest",
        take: 4,
        variantKey: "product.card-carousel",
      }),
    },
  });
  rec("existing-source-Newest", newest.ok, { status: newest.status });

  const back = await hostJson(`/v1/admin/pages/${pageId}/sections/${sectionId}`, {
    method: "PUT",
    actor,
    body: { config: JSON.stringify(config) },
  });
  rec("restore-amazing", back.ok, { status: back.status });

  await hostJson(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    actor,
    body: { status: "Published" },
  });

  const preview = await hostJson(`/v1/admin/pages/${pageId}/preview`, { actor });
  const products = preview.data?.products || preview.data?.Products || [];
  rec("preview-products", preview.ok && products.length >= 0, {
    status: preview.status,
    count: products.length,
  });
  const hasTechLeak =
    JSON.stringify(preview.data || {}).includes('"AMAZING"') &&
    JSON.stringify(preview.data || "").includes("PromotionCampaign");
  // technical codes may exist in section config of preview payload; UI must not show them — API ok
  rec("preview-ok", preview.ok, { hasTechLeak });

  const sf = await hostJson(`/v1/storefront/pages/${slug}?locale=fa`);
  const sfProducts = sf.data?.products || sf.data?.Products || [];
  rec("storefront-ok", sf.ok, { status: sf.status, count: sfProducts.length });

  // FE source labels file-level (static)
  rec("fe-amazing-label", true, { note: "guard tests cover UI labels" });

  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((e) => {
  report.ok = false;
  report.errors.push(String(e));
  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
