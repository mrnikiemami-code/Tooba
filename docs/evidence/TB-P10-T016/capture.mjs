import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T016/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], console: [], appearance: {} };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, data });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function api(path, init = {}) {
  const res = await fetch(`${HOST}${path}`, {
    ...init,
    headers: { Accept: "application/json", Host: "alpha.localhost", ...(init.headers ?? {}) },
  });
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 240); }
  return { status: res.status, body };
}

async function admin(path, init = {}) {
  return api(path, {
    ...init,
    headers: { "X-Tooba-Dev-Actor-User-Id": ACTOR, ...(init.headers ?? {}) },
  });
}

async function putAppearance(paletteKey, themeMode, productCardSkin, backgroundStyle) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin, backgroundStyle }),
  });
}

async function waitMedia(page) {
  await page.waitForTimeout(500);
  await page.evaluate(async () => {
    const imgs = [...document.querySelectorAll("img")].slice(0, 16);
    await Promise.all(imgs.map((img) => {
      if (img.complete && img.naturalWidth > 0) return undefined;
      return new Promise((resolve) => {
        img.addEventListener("load", resolve, { once: true });
        img.addEventListener("error", resolve, { once: true });
        setTimeout(resolve, 4000);
      });
    }));
  });
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

async function open(page, path, expectedStyle) {
  for (let i = 0; i < 4; i += 1) {
    await page.goto(`${FE}${path}`, { waitUntil: "load" });
    const style = await page.locator("html").getAttribute("data-storefront-background-style");
    if (!expectedStyle || style === expectedStyle) {
      await waitMedia(page);
      return style;
    }
    await page.waitForTimeout(800);
  }
  await waitMedia(page);
  return page.locator("html").getAttribute("data-storefront-background-style");
}

(async () => {
  const invalid = await putAppearance("tooba-blue", "LightOnly", "classic", "NeonWash");
  rec("invalid-background-400", invalid.status === 400 && invalid.body?.errorCode === "appearance.background.invalid");

  const pub = await api("/v1/storefront/appearance");
  rec("public-four-roles", pub.status === 200
    && pub.body?.tint?.sectionAlternateRgb
    && pub.body?.tint?.sectionAccentRgb
    && pub.body?.tint?.sectionAlternateRgb !== pub.body?.tint?.pageBackgroundRgb
    && pub.body?.tint?.sectionAccentRgb !== pub.body?.tokens?.primaryRgb);

  const tintBlue = await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");
  rec("A-blue-tint", tintBlue.status === 200 && tintBlue.body?.backgroundStyle === "PaletteTint");
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });

  const homeFeed = await api("/v1/storefront/home?locale=fa");
  const slug = homeFeed.body?.specialOffers?.[0]?.slug
    || homeFeed.body?.newArrivals?.[0]?.slug
    || homeFeed.body?.featuredProducts?.[0]?.slug
    || homeFeed.body?.productRail?.[0]?.slug
    || null;
  rec("pdp-slug", Boolean(slug), { slug });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));
  page.on("console", (msg) => {
    if (msg.type() === "error") report.console.push({ type: "console-error", text: msg.text().slice(0, 240) });
  });

  rec("B-home-style", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  rec("B-home-roles", await page.locator("[data-storefront-surface-role]").count() > 0);
  await shot(page, "home-four-surfaces.png");

  rec("C-plp", (await open(page, "/fa/products", "PaletteTint")) === "PaletteTint");
  await shot(page, "plp-four-surfaces.png");

  if (slug) {
    rec("D-pdp", (await open(page, `/fa/products/${slug}`, "PaletteTint")) === "PaletteTint");
    rec("D-pdp-marker", await page.locator("[data-testid=storefront-pdp]").count() > 0);
  } else {
    rec("D-pdp", false);
  }
  await shot(page, "pdp-four-surfaces.png");

  rec("E-content", (await open(page, "/fa/blogs", "PaletteTint")) === "PaletteTint");
  await shot(page, "content-four-surfaces.png");

  rec("F-cart", (await open(page, "/fa/cart", "PaletteTint")) === "PaletteTint");
  await shot(page, "cart-four-surfaces.png");

  rec("G-checkout", (await open(page, "/fa/shipping", "PaletteTint")) === "PaletteTint");
  await shot(page, "checkout-four-surfaces.png");

  rec("H-account", (await open(page, "/fa/customer-panel", "PaletteTint")) === "PaletteTint");
  rec("H-wishlist", (await open(page, "/fa/customer-panel/wishlist", "PaletteTint")) === "PaletteTint");

  rec("I-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  rec("I-landing-roles", await page.locator("[data-landing-section-type]").count() > 0);
  await shot(page, "landing-four-surfaces.png");

  const forest = await putAppearance("forest-green", "LightOnly", "classic", "PaletteTint");
  rec("J-forest", forest.status === 200 && forest.body?.paletteKey === "forest-green");
  rec("J-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  await shot(page, "forest-four-surfaces.png");

  const dark = await putAppearance("forest-green", "DarkOnly", "classic", "PaletteTint");
  rec("K-dark", dark.status === 200 && dark.body?.themeMode === "DarkOnly");
  rec("K-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  rec("K-dark-class", await page.locator("html").evaluate((el) => el.classList.contains("dark")));
  await shot(page, "dark-four-surfaces.png");

  const end = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("L-restore", end.status === 200 && end.body?.backgroundStyle === "Neutral" && end.body?.paletteKey === "tooba-blue" && end.body?.themeMode === "LightOnly" && end.body?.productCardSkin === "classic");
  const homeClear = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("L-home", homeClear.status === 200 && (homeClear.body?.usesCanonicalHome === true || homeClear.body?.UsesCanonicalHome === true || !homeClear.body?.homePageId));

  report.appearance = {
    paletteKey: end.body?.paletteKey,
    themeMode: end.body?.themeMode,
    productCardSkin: end.body?.productCardSkin,
    backgroundStyle: end.body?.backgroundStyle,
  };

  const loops = report.console.filter((row) => /Maximum update depth/i.test(row.text));
  rec("no-console-loop", loops.length === 0, { loops });

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T016/runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T016/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
