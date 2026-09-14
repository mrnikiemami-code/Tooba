import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const OUT = "docs/evidence/TB-P10-T010/runtime-raw.json";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

function headers(extra = {}) {
  return { Accept: "application/json", Host: "alpha.localhost", ...extra };
}

function adminHeaders(extra = {}) {
  return headers({ "Content-Type": "application/json", "X-Tooba-Dev-Actor-User-Id": ACTOR, ...extra });
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
  const ctx = await json(`${HOST}/v1/admin/dev-context`, { headers: headers() });
  step("dev-context", ctx.body, ctx.status === 200);

  const homeA = await fetch(`${FE}/fa`, { headers: { Accept: "text/html" }, cache: "no-store" });
  const homeAHtml = await homeA.text();
  const homeSelA = await json(`${HOST}/v1/storefront/home-selection`, { headers: headers() });
  step("A-canonical-home", {
    fe: homeA.status,
    homeHasShell: homeAHtml.includes("storefront") || homeAHtml.length > 200,
    homePageId: homeSelA.body?.homePageId ?? null,
    usesCanonicalHome: homeSelA.body?.usesCanonicalHome,
  }, homeA.status === 200 && homeSelA.status === 200 && homeSelA.body?.usesCanonicalHome === true && !homeSelA.body?.homePageId);

  const created = await json(`${HOST}/v1/admin/pages`, {
    method: "POST",
    headers: adminHeaders(),
    body: JSON.stringify({
      title: "فروش تابستان",
      slug: "summer-sale",
      locale: "fa",
      seoTitle: "فروش تابستان | توبا",
      seoDescription: "صفحه موقت تست Landing",
    }),
  });
  step("B-create-draft", created.body, created.status === 200 && created.body?.slug === "summer-sale" && created.body?.status === "Draft");
  const pageId = created.body?.pageId;

  const draftPublic = await json(`${HOST}/v1/storefront/pages/summer-sale?locale=fa`, { headers: headers() });
  const draftFe = await fetch(`${FE}/fa/summer-sale`, { headers: { Accept: "text/html" }, cache: "no-store" });
  step("C-draft-404", { api: draftPublic.status, fe: draftFe.status }, draftPublic.status === 404 && draftFe.status === 404);

  const published = await json(`${HOST}/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: adminHeaders(),
    body: JSON.stringify({ status: "Published" }),
  });
  const pubApi = await json(`${HOST}/v1/storefront/pages/summer-sale?locale=fa`, { headers: headers() });
  const pubFe = await fetch(`${FE}/fa/summer-sale`, { headers: { Accept: "text/html" }, cache: "no-store" });
  const pubHtml = await pubFe.text();
  step("D-publish-resolves", {
    statusWrite: published.status,
    api: pubApi.status,
    fe: pubFe.status,
    title: pubHtml.includes("فروش تابستان"),
  }, published.status === 200 && pubApi.status === 200 && pubFe.status === 200 && pubHtml.includes("فروش تابستان") && pubHtml.includes('data-testid="storefront-landing-page"'));

  const reserved = await json(`${HOST}/v1/admin/pages`, {
    method: "POST",
    headers: adminHeaders(),
    body: JSON.stringify({ title: "سبد", slug: "cart", locale: "fa" }),
  });
  const cartFe = await fetch(`${FE}/fa/cart`, { headers: { Accept: "text/html" }, cache: "no-store" });
  const cartHtml = await cartFe.text();
  step("G-reserved-rejected", {
    create: reserved.status,
    errorCode: reserved.body?.errorCode,
    cartStatus: cartFe.status,
    cartIsLanding: cartHtml.includes('data-testid="storefront-landing-page"'),
  }, reserved.status === 400 && reserved.body?.errorCode === "landing.slug.reserved" && cartFe.status === 200 && !cartHtml.includes('data-testid="storefront-landing-page"'));

  const homeSet = await json(`${HOST}/v1/admin/pages/home`, {
    method: "PUT",
    headers: adminHeaders(),
    body: JSON.stringify({ homePageId: pageId }),
  });
  const homeAfter = await json(`${HOST}/v1/storefront/home-selection`, { headers: headers() });
  const homeFe = await fetch(`${FE}/fa`, { headers: { Accept: "text/html" }, cache: "no-store" });
  const homeFeHtml = await homeFe.text();
  step("E-set-home", homeSet.body, homeSet.status === 200 && homeSet.body?.homePageId === pageId && homeSet.body?.usesCanonicalHome === false);
  step("F-home-identity-without-replacing-ui", {
    selection: homeAfter.body,
    fe: homeFe.status,
    stillCanonicalHomeUi: !homeFeHtml.includes('data-testid="storefront-landing-page"'),
  }, homeAfter.status === 200 && homeAfter.body?.homePageId === pageId && homeFe.status === 200 && !homeFeHtml.includes('data-testid="storefront-landing-page"'));

  const missingHome = await json(`${HOST}/v1/admin/pages/home`, {
    method: "PUT",
    headers: adminHeaders(),
    body: JSON.stringify({ homePageId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee" }),
  });
  step("H-cross-store-or-missing-home", missingHome.body, missingHome.status === 404 && missingHome.body?.errorCode === "landing.page.missing");

  const cleared = await json(`${HOST}/v1/admin/pages/home`, {
    method: "PUT",
    headers: adminHeaders(),
    body: JSON.stringify({ homePageId: null }),
  });
  const draftAgain = await json(`${HOST}/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: adminHeaders(),
    body: JSON.stringify({ status: "Draft" }),
  });
  const gone = await json(`${HOST}/v1/storefront/pages/summer-sale?locale=fa`, { headers: headers() });
  const homeRestored = await json(`${HOST}/v1/storefront/home-selection`, { headers: headers() });
  const homeRestoredFe = await fetch(`${FE}/fa`, { headers: { Accept: "text/html" }, cache: "no-store" });
  step("I-restore", {
    cleared: cleared.body,
    draft: draftAgain.body?.status,
    publicGone: gone.status,
    home: homeRestored.body,
    fe: homeRestoredFe.status,
  }, cleared.status === 200 && draftAgain.status === 200 && gone.status === 404 && homeRestored.body?.usesCanonicalHome === true && !homeRestored.body?.homePageId && homeRestoredFe.status === 200);
} catch (error) {
  report.ok = false;
  report.errors.push(String(error));
}

writeFileSync(OUT, JSON.stringify(report, null, 2));
console.log(JSON.stringify({ ok: report.ok, errors: report.errors, passed: report.steps.filter((s) => s.pass).length, total: report.steps.length }, null, 2));
if (!report.ok) process.exit(1);
