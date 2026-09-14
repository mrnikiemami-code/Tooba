import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = "docs/evidence/TB-P10-T011/runtime-raw.json";

function headers(extra = {}) {
  return { Accept: "application/json", Host: "alpha.localhost", "Content-Type": "application/json", "X-Tooba-Dev-Actor-User-Id": ACTOR, ...extra };
}

async function json(url, init = {}) {
  const res = await fetch(url, init);
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 800); }
  return { status: res.status, body };
}

const report = { ok: true, steps: [], errors: [] };
function step(name, data, pass) {
  report.steps.push({ name, pass, data });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

try {
  const created = await json(`${HOST}/v1/admin/pages`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({ title: "آزمایش بخش", slug: "section-lab", locale: "fa" }),
  });
  const pageId = created.body?.pageId;
  const published = await json(`${HOST}/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: headers(),
    body: JSON.stringify({ status: "Published" }),
  });
  step("A-publish-page", { created: created.status, published: published.status, pageId }, created.status === 200 && published.status === 200);

  const hero = await json(`${HOST}/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({ sectionType: "Hero", config: JSON.stringify({ title: "هیرو آزمایشی" }) }),
  });
  step("B-add-hero", hero.body, hero.status === 200 && hero.body?.sectionType === "Hero");

  const products = await json(`${HOST}/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({ sectionType: "ProductCollection", config: JSON.stringify({ source: "Newest", take: 4 }) }),
  });
  step("C-add-product-collection", products.body, products.status === 200 && products.body?.sectionType === "ProductCollection");

  const banner = await json(`${HOST}/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({ sectionType: "PromoBanner", config: JSON.stringify({ title: "بنر آزمایشی" }) }),
  });
  step("D-add-banner", banner.body, banner.status === 200 && banner.body?.sectionType === "PromoBanner");

  const pubA = await json(`${HOST}/v1/storefront/pages/section-lab?locale=fa`, { headers: { Accept: "application/json", Host: "alpha.localhost" } });
  const typesA = (pubA.body?.sections ?? []).map((s) => s.sectionType);
  const idsA = (pubA.body?.sections ?? []).map((s) => s.pageSectionId);
  const productSection = (pubA.body?.sections ?? []).find((s) => s.sectionType === "ProductCollection");
  step("E-order", { typesA, items: productSection?.items?.length ?? 0 }, pubA.status === 200 && typesA.join(",") === "Hero,ProductCollection,PromoBanner" && (productSection?.items?.length ?? 0) >= 1);

  const disabled = await json(`${HOST}/v1/admin/pages/${pageId}/sections/${products.body.pageSectionId}/enabled`, {
    method: "PUT",
    headers: headers(),
    body: JSON.stringify({ isEnabled: false }),
  });
  const pubB = await json(`${HOST}/v1/storefront/pages/section-lab?locale=fa`, { headers: { Accept: "application/json", Host: "alpha.localhost" } });
  const typesB = (pubB.body?.sections ?? []).map((s) => s.sectionType);
  step("F-disable-middle", { disabled: disabled.status, typesB }, disabled.status === 200 && typesB.join(",") === "Hero,PromoBanner");

  const reorder = await json(`${HOST}/v1/admin/pages/${pageId}/sections/reorder`, {
    method: "PUT",
    headers: headers(),
    body: JSON.stringify({ sectionIds: [banner.body.pageSectionId, hero.body.pageSectionId, products.body.pageSectionId] }),
  });
  const pubC = await json(`${HOST}/v1/storefront/pages/section-lab?locale=fa`, { headers: { Accept: "application/json", Host: "alpha.localhost" } });
  const idsC = (pubC.body?.sections ?? []).map((s) => s.pageSectionId);
  step("G-reorder", { reorder: reorder.status, idsC }, reorder.status === 200 && idsC[0] === banner.body.pageSectionId && idsC[1] === hero.body.pageSectionId && !idsC.includes(products.body.pageSectionId) && idsA.includes(hero.body.pageSectionId));

  const invalid = await json(`${HOST}/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({ sectionType: "CustomHtml", config: "{}" }),
  });
  step("H-invalid-type", invalid.body, invalid.status === 400 && invalid.body?.errorCode === "landing.section.type.invalid");

  await json(`${HOST}/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: headers(),
    body: JSON.stringify({ status: "Draft" }),
  });
  const gone = await json(`${HOST}/v1/storefront/pages/section-lab?locale=fa`, { headers: { Accept: "application/json", Host: "alpha.localhost" } });
  step("I-unpublish-404", { gone: gone.status }, gone.status === 404);
} catch (error) {
  report.ok = false;
  report.errors.push(String(error));
}

writeFileSync(OUT, JSON.stringify(report, null, 2));
console.log(JSON.stringify({ ok: report.ok, errors: report.errors, passed: report.steps.filter((s) => s.pass).length, total: report.steps.length }, null, 2));
if (!report.ok) process.exit(1);
