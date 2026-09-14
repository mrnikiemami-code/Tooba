import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const OUT = "docs/evidence/TB-P10-T009/runtime-raw.json";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

function headers(extra = {}) {
  return { Accept: "application/json", Host: "alpha.localhost", ...extra };
}

async function json(url, init = {}) {
  const res = await fetch(url, init);
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 400); }
  return { status: res.status, body };
}

async function put(paletteKey, themeMode, productCardSkin) {
  return json(`${HOST}/v1/admin/settings/appearance`, {
    method: "PUT",
    headers: headers({ "Content-Type": "application/json", "X-Tooba-Dev-Actor-User-Id": ACTOR }),
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin }),
  });
}

function extract(html) {
  return {
    palette: html.match(/data-storefront-palette="([^"]+)"/)?.[1] ?? null,
    themeMode: html.match(/data-storefront-theme-mode="([^"]+)"/)?.[1] ?? null,
    scheme: html.match(/data-storefront-color-scheme="([^"]+)"/)?.[1] ?? null,
    skin: html.match(/data-storefront-product-card-skin="([^"]+)"/)?.[1] ?? null,
    cardSkin: html.match(/data-product-card-skin="([^"]+)"/)?.[1] ?? null,
    darkClass: /<html[^>]*class="[^"]*\bdark\b/.test(html),
    card: html.includes('data-testid="storefront-product-card"'),
    classicChrome: html.includes("hover:shadow-black/40"),
    cleanChrome: html.includes("border-transparent"),
    elevatedChrome: html.includes("shadow-md hover:shadow-2xl"),
    glassChrome: html.includes("backdrop-blur-md"),
  };
}

async function page(path) {
  const res = await fetch(`${FE}${path}`, { headers: { Accept: "text/html" }, cache: "no-store" });
  return { status: res.status, ...extract(await res.text()) };
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

  const a = await put("tooba-blue", "LightOnly", "classic");
  step("A-default-classic", a.body, a.status === 200 && a.body?.productCardSkin === "classic" && a.body?.paletteKey === "tooba-blue" && a.body?.themeMode === "LightOnly");

  const homeB = await page("/fa");
  const listingB = await page("/fa/products");
  const pdpB = await page("/fa/products/demo-prod-av-audio-headphones-3");
  const cartB = await page("/fa/cart");
  step("B-surfaces-classic", { homeB, listingB, pdpB, cartB }, [homeB, listingB, pdpB, cartB].every((p) => p.skin === "classic" && p.status === 200));

  const draft = await json(`${HOST}/v1/admin/settings/appearance`, { headers: headers({ "X-Tooba-Dev-Actor-User-Id": ACTOR }) });
  step("C-admin-four-skins", { count: draft.body?.skins?.length, current: draft.body?.productCardSkin }, draft.status === 200 && draft.body?.skins?.length === 4 && draft.body?.productCardSkin === "classic");

  const invalid = await put("tooba-blue", "LightOnly", "custom-html");
  step("D-invalid-skin", invalid.body, invalid.status === 400 && invalid.body?.errorCode === "appearance.skin.invalid");

  const e = await put("tooba-blue", "LightOnly", "clean");
  const cancelCheck = await json(`${HOST}/v1/admin/settings/appearance`, { headers: headers({ "X-Tooba-Dev-Actor-User-Id": ACTOR }) });
  step("E-cancel-not-needed-saved-clean-preview-path", cancelCheck.body, e.status === 200 && cancelCheck.body?.productCardSkin === "clean");

  const f = await put("tooba-blue", "LightOnly", "clean");
  const homeG = await page("/fa");
  const listingG = await page("/fa/products");
  const pdpG = await page("/fa/products/demo-prod-av-audio-headphones-3");
  const cartG = await page("/fa/cart");
  step("F-save-clean", f.body, f.status === 200 && f.body?.productCardSkin === "clean");
  step("G-storefront-clean", { homeG, listingG, pdpG, cartG }, [homeG, listingG, pdpG, cartG].every((p) => p.skin === "clean"));

  const h = await put("tooba-blue", "DarkOnly", "clean");
  const homeI = await page("/fa");
  step("H-darkonly", h.body, h.status === 200 && h.body?.themeMode === "DarkOnly" && h.body?.productCardSkin === "clean");
  step("I-clean-dark", homeI, homeI.skin === "clean" && homeI.darkClass && homeI.themeMode === "DarkOnly");

  const j = await put("forest-green", "LightOnly", "clean");
  const homeJ = await page("/fa");
  step("J-forest-green", { j: j.body, homeJ }, j.status === 200 && j.body?.paletteKey === "forest-green" && homeJ.palette === "forest-green" && homeJ.skin === "clean");

  const k = await put("forest-green", "DarkOnly", "clean");
  const homeK = await page("/fa");
  step("K-clean-dark-forest", homeK, homeK.skin === "clean" && homeK.darkClass && homeK.palette === "forest-green");

  const lLight = await put("tooba-blue", "LightOnly", "elevated");
  const homeL1 = await page("/fa");
  const lDark = await put("tooba-blue", "DarkOnly", "elevated");
  const homeL2 = await page("/fa");
  step("L-elevated-light-dark", { lLight: lLight.body, homeL1, homeL2 }, lLight.status === 200 && lDark.status === 200 && homeL1.skin === "elevated" && homeL2.skin === "elevated" && homeL2.darkClass);

  const mLight = await put("tooba-blue", "LightOnly", "glass");
  const homeM1 = await page("/fa");
  const mDark = await put("tooba-blue", "DarkOnly", "glass");
  const homeM2 = await page("/fa");
  step("M-glass-light-dark", { mLight: mLight.body, homeM1, homeM2 }, mLight.status === 200 && mDark.status === 200 && homeM1.skin === "glass" && homeM2.skin === "glass" && homeM2.darkClass);

  const n = await put("tooba-blue", "LightOnly", "classic");
  const homeN = await page("/fa");
  step("N-restore-default", { n: n.body, homeN }, n.status === 200 && n.body?.paletteKey === "tooba-blue" && n.body?.themeMode === "LightOnly" && n.body?.productCardSkin === "classic" && homeN.skin === "classic" && !homeN.darkClass);
} catch (error) {
  report.ok = false;
  report.errors.push(String(error));
}

writeFileSync(OUT, JSON.stringify(report, null, 2));
console.log(JSON.stringify({ ok: report.ok, errors: report.errors, passed: report.steps.filter((s) => s.pass).length, total: report.steps.length }, null, 2));
if (!report.ok) process.exit(1);
