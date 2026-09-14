import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const OUT = "docs/evidence/TB-P10-T008/r1-runtime-raw.json";
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

async function put(paletteKey, themeMode) {
  return json(`${HOST}/v1/admin/settings/appearance`, {
    method: "PUT",
    headers: headers({ "Content-Type": "application/json", "X-Tooba-Dev-Actor-User-Id": ACTOR }),
    body: JSON.stringify({ paletteKey, themeMode }),
  });
}

function extract(html) {
  return {
    palette: html.match(/data-storefront-palette="([^"]+)"/)?.[1] ?? null,
    themeMode: html.match(/data-storefront-theme-mode="([^"]+)"/)?.[1] ?? null,
    scheme: html.match(/data-storefront-color-scheme="([^"]+)"/)?.[1] ?? null,
    darkClass: /<html[^>]*class="[^"]*\bdark\b/.test(html),
    primary: html.match(/--color-primary:([^;"]+)/)?.[1]?.trim() ?? null,
    onDark: html.match(/--color-primary-on-dark:([^;"]+)/)?.[1]?.trim() ?? null,
    bootstrap: html.includes("tooba.storefront.color-scheme") || html.includes("prefers-color-scheme"),
    mediaWell: html.includes("data-storefront-media-well"),
  };
}

async function page(path, extraHeaders = {}) {
  const res = await fetch(`${FE}${path}`, { headers: { Accept: "text/html", ...extraHeaders }, cache: "no-store" });
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

  const a = await put("wine-burgundy", "DarkOnly");
  step("A-wine-burgundy-darkonly", a.body, a.status === 200 && a.body?.themeMode === "DarkOnly" && a.body?.paletteKey === "wine-burgundy" && a.body?.tokens?.primaryOnDarkRgb === "189 91 118");
  const homeA = await page("/fa");
  const pdpB = await page("/fa/products/demo-prod-av-audio-headphones-3");
  const cartB = await page("/fa/cart");
  const shipB = await page("/fa/shipping");
  step("B-surfaces-dark", { homeA, pdpB, cartB, shipB }, [homeA, pdpB, cartB, shipB].every((p) => p.darkClass && p.themeMode === "DarkOnly" && p.scheme === "dark"));
  step("C-brand-text-tokens", homeA, homeA.primary === "159 18 57" && homeA.onDark === "189 91 118");
  step("D-product-card-dark", homeA, homeA.mediaWell === true || homeA.darkClass === true);

  const e = await put("forest-green", "DarkOnly");
  const homeE = await page("/fa");
  step("E-forest-green-dark", { e: e.body, homeE }, e.status === 200 && homeE.primary === "21 128 61" && homeE.onDark === "42 139 78" && homeE.darkClass);

  const f = await put("amber-gold", "DarkOnly");
  const homeF = await page("/fa");
  step("F-amber-gold-dark", { f: f.body, homeF }, f.status === 200 && homeF.primary === "180 83 9" && homeF.onDark === "187 98 31" && homeF.darkClass);

  const g = await put("violet-royal", "DarkOnly");
  const homeG = await page("/fa");
  step("G-violet-royal-dark", { g: g.body, homeG }, g.status === 200 && homeG.primary === "124 58 237" && homeG.onDark === "144 88 240" && homeG.darkClass);

  const h = await put("tooba-blue", "UserChoice");
  const userDark = await page("/fa", { Cookie: "tooba-storefront-color-scheme=dark" });
  const userLight = await page("/fa", { Cookie: "tooba-storefront-color-scheme=light" });
  step("H-userchoice-persist", { h: h.body, userDark, userLight }, h.status === 200 && userDark.darkClass && userDark.scheme === "dark" && !userLight.darkClass && userLight.scheme === "light");

  const i = await put("tooba-blue", "LightOnly");
  const homeI = await page("/fa");
  step("I-restore-tooba-blue-light", { i: i.body, homeI }, i.status === 200 && homeI.palette === "tooba-blue" && homeI.themeMode === "LightOnly" && !homeI.darkClass && homeI.primary === "37 99 235");
} catch (error) {
  report.ok = false;
  report.errors.push(String(error));
}

writeFileSync(OUT, JSON.stringify(report, null, 2));
if (!report.ok) {
  process.exitCode = 1;
}
