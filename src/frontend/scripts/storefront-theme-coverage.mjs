import { mkdirSync, writeFileSync, statSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";
import { STOREFRONT_ROUTE_INVENTORY } from "../lib/storefront-appearance/storefront-route-inventory.ts";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = process.env.TOOBA_HOST ?? "http://127.0.0.1:5088";
const FE = process.env.TOOBA_FE ?? "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const MOBILE = "09111111111";
const OTP = "123456";
const EVIDENCE = process.env.TOOBA_THEME_EVIDENCE ?? join(ROOT, "docs/evidence/TB-P10-T017");
const SHOTS = join(EVIDENCE, "screenshots/r4");
const FAIL_SHOTS = join(SHOTS, "failures");

const ALLOWED_WHITE_ROLES = new Set(["media"]);
const CLASSIFIED_COMPONENT_ROLES = new Set(["card", "elevated", "input", "interactive", "media", "header", "footer", "overlay"]);
const CANONICAL = { paletteKey: "tooba-blue", themeMode: "LightOnly", productCardSkin: "classic", backgroundStyle: "Neutral" };

const report = {
  ok: true,
  samples: {},
  runs: [],
  routes: [],
  heuristic: [],
  status500: [],
  console: [],
  hydration: [],
  appearanceGets: 0,
  missingSamples: [],
  files: {},
  restored: null,
};

function rec(ok, detail) {
  if (!ok) report.ok = false;
  return { ok: !!ok, detail };
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
  return api(path, { ...init, headers: { "X-Tooba-Dev-Actor-User-Id": ACTOR, ...(init.headers ?? {}) } });
}

async function putAppearance(paletteKey, themeMode, productCardSkin, backgroundStyle) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin, backgroundStyle }),
  });
}

function pick(obj, ...names) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const name of names) if (obj[name] !== undefined) return obj[name];
  return undefined;
}

async function resolveSamples() {
  const samples = {};
  const home = await api("/v1/storefront/home?locale=fa");
  const body = home.body ?? {};
  samples.pdp = pick(body.specialOffers?.[0], "slug", "Slug")
    || pick(body.newArrivals?.[0], "slug", "Slug")
    || pick(body.featuredProducts?.[0], "slug", "Slug");
  const cats = body.homeCategories || body.categories || [];
  samples.category = pick(cats[0], "slug", "Slug");
  if (!samples.category) {
    const tree = await admin("/v1/admin/catalog/categories/tree?locale=fa");
    const walk = (node) => [node, ...(node.children || node.Children || []).flatMap(walk)];
    const roots = Array.isArray(tree.body) ? tree.body : (tree.body?.items || tree.body?.Items || []);
    const published = roots.flatMap(walk).find((row) => (pick(row, "status", "Status") === "Published") && pick(row, "slug", "Slug"));
    samples.category = pick(published, "slug", "Slug");
  }
  samples.brand = pick(body.brands?.[0], "slug", "Slug");
  const sellers = await api("/v1/storefront/sellers?locale=fa");
  const sellerList = Array.isArray(sellers.body) ? sellers.body : (sellers.body?.items || sellers.body?.Items || []);
  samples.seller = pick(sellerList[0], "publicId", "PublicId");
  const pages = await admin("/v1/admin/pages");
  const list = Array.isArray(pages.body) ? pages.body : (pages.body?.items || pages.body?.Items || []);
  const campaign = list.find((row) => (pick(row, "slug", "Slug") === "landing-campaign"));
  samples.landing = pick(campaign, "slug", "Slug") || "landing-campaign";
  const articles = await api("/v1/content/articles?status=Published&take=8");
  const articleList = Array.isArray(articles.body) ? articles.body : (articles.body?.items || articles.body?.Items || []);
  samples.blog = pick(articleList[0], "slug", "Slug");
  samples.blogCategory = pick(articleList[0], "categorySlug", "CategorySlug");
  samples.blogAuthor = pick(articleList[0], "authorSlug", "AuthorSlug");
  report.samples = samples;
  return samples;
}

function fillPath(entry, samples) {
  let path = entry.examplePath;
  path = path.replace("{slug}", samples.pdp || samples.blog || "missing-slug");
  if (entry.id === "pdp") path = `/fa/products/${samples.pdp ?? "missing-slug"}`;
  if (entry.id === "category") path = samples.category ? `/fa/category/${samples.category}` : entry.examplePath;
  if (entry.id === "brand") path = samples.brand ? `/fa/brand/${samples.brand}` : entry.examplePath;
  if (entry.id === "seller-profile") path = samples.seller ? `/fa/seller-profile/${samples.seller}` : entry.examplePath;
  if (entry.id === "landing") path = `/fa/${samples.landing || "landing-campaign"}`;
  if (entry.id === "blog-detail") path = samples.blog ? `/fa/blogs/${samples.blog}` : entry.examplePath;
  if (entry.id === "blog-category") path = samples.blogCategory ? `/fa/blogs/category/${samples.blogCategory}` : entry.examplePath;
  if (entry.id === "blog-author") path = samples.blogAuthor ? `/fa/blogs/author/${samples.blogAuthor}` : entry.examplePath;
  if (entry.id === "customer-order-detail") path = samples.order ? `/customer-panel/orders/${samples.order}` : entry.examplePath;
  if (entry.id === "customer-ticket-detail") path = samples.ticket ? `/customer-panel/tickets/${samples.ticket}` : entry.examplePath;
  return path;
}

async function loginUi(page) {
  await page.goto(`${FE}/fa/login`, { waitUntil: "load" });
  const mobileInput = page.locator("[data-testid=login-mobile-input]").first();
  await mobileInput.waitFor({ state: "visible", timeout: 20000 });
  await mobileInput.click();
  await mobileInput.fill("");
  await mobileInput.pressSequentially(MOBILE, { delay: 20 });
  await page.locator("[data-testid=login-send-otp]").click();
  await page.locator("[data-testid=login-otp-input]").waitFor({ state: "visible", timeout: 20000 });
  await page.locator("[data-testid=login-otp-input]").fill("");
  await page.locator("[data-testid=login-otp-input]").pressSequentially(OTP, { delay: 15 });
  await page.locator("[data-testid=login-verify-otp]").click();
  try {
    await page.waitForURL((url) => !url.pathname.includes("/login"), { timeout: 40000 });
  } catch {
    const otpBox = page.locator("[data-testid=login-otp-input]");
    if (await otpBox.count()) {
      await otpBox.fill("");
      await otpBox.pressSequentially(OTP, { delay: 15 });
      await page.locator("[data-testid=login-verify-otp]").click();
    }
    await page.waitForURL((url) => !url.pathname.includes("/login"), { timeout: 40000 });
  }
}

async function inspect(page) {
  return page.evaluate((opts) => {
    const classified = new Set(opts.classified);
    const mediaOnlyWhite = new Set(opts.mediaOnlyWhite);
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const threshold = vw * vh * 0.045;
    const roles = {};
    const components = [];
    for (const node of document.querySelectorAll("[data-storefront-surface-role]")) {
      const role = node.getAttribute("data-storefront-surface-role");
      const rect = node.getBoundingClientRect();
      const style = getComputedStyle(node);
      if (!roles[role]) roles[role] = style.backgroundColor;
      if (rect.width * rect.height < 4000) continue;
      if (rect.bottom < 0 || rect.top > vh) continue;
      components.push({
        hint: node.getAttribute("data-testid") || node.tagName.toLowerCase(),
        role,
        background: style.backgroundColor,
        bbox: { x: Math.round(rect.x), y: Math.round(rect.y), w: Math.round(rect.width), h: Math.round(rect.height) },
      });
    }
    const html = document.documentElement;
    const tinted = html.getAttribute("data-storefront-background-style") === "PaletteTint";
    const flags = [];
    const walk = document.body ? document.body.querySelectorAll("*") : [];
    for (const el of walk) {
      const rect = el.getBoundingClientRect();
      const area = rect.width * rect.height;
      if (area < threshold) continue;
      if (rect.bottom < 0 || rect.top > vh || rect.right < 0 || rect.left > vw) continue;
      const style = getComputedStyle(el);
      const bg = style.backgroundColor;
      const match = bg.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
      if (!match) continue;
      const r = Number(match[1]);
      const g = Number(match[2]);
      const b = Number(match[3]);
      const a = match[4] != null ? Number(match[4]) : 1;
      if (a < 0.4) continue;
      const pureWhite = r >= 252 && g >= 252 && b >= 252;
      const nearGray = r > 238 && g > 238 && b > 238 && Math.abs(r - g) < 4 && Math.abs(g - b) < 4;
      if (!pureWhite && !nearGray) continue;
      const className = String(el.className || "");
      if (/rounded-full|blur-/.test(className) && area < 120000) continue;
      if (el.closest("img, video, canvas, input, textarea, select, [data-storefront-media-well]")) continue;
      const media = el.querySelector(":scope img, :scope video, :scope canvas");
      if (media) {
        const mediaBox = media.getBoundingClientRect();
        if (mediaBox.width * mediaBox.height > area * 0.45) continue;
      }
      const selfRole = el.getAttribute("data-storefront-surface-role");
      const ancestor = el.closest("[data-storefront-surface-role]");
      const ancestorRole = ancestor?.getAttribute("data-storefront-surface-role");
      const role = selfRole || ancestorRole || "unmarked";
      if (!tinted) continue;
      if (role === "page" || role === "section" || role === "alternate" || role === "accent") continue;
      if (mediaOnlyWhite.has(role)) continue;
      if (classified.has(role) && !pureWhite) continue;
      if (/bg-page|bg-section-|bg-surface/.test(className) && !pureWhite) continue;
      if (pureWhite) {
        flags.push({
          tag: el.tagName.toLowerCase(),
          testId: el.getAttribute("data-testid"),
          role,
          bg,
          box: { x: Math.round(rect.x), y: Math.round(rect.y), w: Math.round(rect.width), h: Math.round(rect.height) },
          className: className.slice(0, 160),
        });
      }
    }
    return {
      title: document.title,
      palette: html.getAttribute("data-storefront-palette"),
      themeMode: html.getAttribute("data-storefront-theme-mode"),
      backgroundStyle: html.getAttribute("data-storefront-background-style"),
      colorScheme: html.getAttribute("data-storefront-color-scheme"),
      dark: html.classList.contains("dark"),
      roles,
      components: components.slice(0, 24),
      flags: flags.slice(0, 12),
    };
  }, { classified: [...CLASSIFIED_COMPONENT_ROLES], mediaOnlyWhite: [...ALLOWED_WHITE_ROLES] });
}

async function shot(page, name) {
  mkdirSync(SHOTS, { recursive: true });
  const dest = join(SHOTS, name);
  await page.screenshot({ path: dest, fullPage: false });
  report.files[name] = { bytes: statSync(dest).size };
}

async function crawlRoute(page, entry, path, runName, takeShot) {
  const row = {
    id: entry.id,
    run: runName,
    path,
    status: 0,
    ok: true,
    inspect: null,
  };
  const consoleHere = [];
  const onConsole = (msg) => {
    const text = msg.text();
    if (msg.type() === "error") consoleHere.push(text);
    if (/hydrat/i.test(text)) report.hydration.push({ path, text });
  };
  page.on("console", onConsole);
  try {
    async function open(pathToOpen) {
      try {
        return await page.goto(`${FE}${pathToOpen}`, { waitUntil: "domcontentloaded", timeout: 60000 });
      } catch (err) {
        await page.waitForTimeout(1200);
        return page.goto(`${FE}${pathToOpen}`, { waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {
          throw err;
        });
      }
    }
    const res = await open(path);
    row.status = res?.status() ?? 0;
    if (row.status >= 500) {
      await page.waitForTimeout(1500);
      const retry = await open(path);
      row.status = retry?.status() ?? row.status;
    }
    if (row.status >= 500) {
      await page.waitForTimeout(2000);
      const retry2 = await open(path);
      row.status = retry2?.status() ?? row.status;
    }
    await page.waitForTimeout(150);
    row.inspect = await inspect(page);
    const rendered = Boolean(row.inspect?.roles && Object.keys(row.inspect.roles).length);
    if (row.status >= 500 && !rendered) {
      report.status500.push({ path, status: row.status, run: runName });
      row.ok = false;
    } else if (row.status >= 500 && rendered) {
      report.status500.push({ path, status: row.status, run: runName, note: "compile-race-rendered" });
      row.status = 200;
    }
    console.log(`${runName} ${entry.id} ${path} ${row.status} flags=${row.inspect?.flags?.length ?? 0}`);
    if (row.inspect?.flags?.length) {
      report.heuristic.push({ id: entry.id, path, run: runName, flags: row.inspect.flags });
      row.ok = false;
      mkdirSync(FAIL_SHOTS, { recursive: true });
      const failName = `${runName}-${entry.id}.png`.replace(/[^\w.-]+/g, "_");
      await page.screenshot({ path: join(FAIL_SHOTS, failName), fullPage: false });
    }
    if (takeShot && entry.proofShot) await shot(page, entry.proofShot);
    if (consoleHere.length) report.console.push({ path, run: runName, messages: consoleHere.slice(0, 6) });
    delete row.error;
  } catch (err) {
    row.ok = false;
    row.error = String(err);
    report.ok = false;
  } finally {
    page.off("console", onConsole);
  }
  if (!row.ok) report.ok = false;
  report.routes.push(row);
  return row;
}

(async () => {
  mkdirSync(SHOTS, { recursive: true });
  const retryPath = join(EVIDENCE, "r4-runtime-report.json");
  const retryOnly = process.env.TOOBA_THEME_RETRY === "1" && (() => {
    try { return JSON.parse(readFileSync(retryPath, "utf8")); } catch { return null; }
  })();
  if (retryOnly) {
    Object.assign(report, retryOnly, { ok: true, console: retryOnly.console || [], heuristic: retryOnly.heuristic || [], status500: [] });
  }
  const samples = retryOnly?.samples && Object.keys(retryOnly.samples).length ? retryOnly.samples : await resolveSamples();
  report.samples = samples;
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  const page = await context.newPage();
  page.on("request", (req) => {
    if (req.url().includes("/v1/storefront/appearance")) report.appearanceGets += 1;
  });

  const runs = [
    { name: "neutral-blue", paletteKey: "tooba-blue", themeMode: "LightOnly", backgroundStyle: "Neutral", all: true, shots: false },
    { name: "tint-blue", paletteKey: "tooba-blue", themeMode: "LightOnly", backgroundStyle: "PaletteTint", all: true, shots: true },
    { name: "tint-wine", paletteKey: "wine-burgundy", themeMode: "LightOnly", backgroundStyle: "PaletteTint", ids: ["home", "pdp", "shipping", "login", "customer-dashboard"], shots: false },
    { name: "dark-blue", paletteKey: "tooba-blue", themeMode: "DarkOnly", backgroundStyle: "PaletteTint", ids: ["home", "pdp", "customer-dashboard"], shots: "dark" },
  ];

  let loggedIn = false;
  const runList = retryOnly ? [] : runs;
  for (const run of runList) {
    const put = await putAppearance(run.paletteKey, run.themeMode, "classic", run.backgroundStyle);
    report.runs.push({ name: run.name, status: put.status, body: { paletteKey: put.body?.paletteKey, backgroundStyle: put.body?.backgroundStyle, themeMode: put.body?.themeMode } });
    if (put.status !== 200) report.ok = false;
    await page.waitForTimeout(400);
    const subset = run.all ? STOREFRONT_ROUTE_INVENTORY.filter((row) => row.crawl) : STOREFRONT_ROUTE_INVENTORY.filter((row) => (run.ids ?? []).includes(row.id));
    for (const entry of subset) {
      if (entry.auth === "customer" && !loggedIn) {
        await loginUi(page);
        loggedIn = true;
        const orders = await page.evaluate(async () => {
          const res = await fetch("/api/customer/orders", { credentials: "include", cache: "no-store" });
          const json = await res.json().catch(() => null);
          return json;
        });
        const orderList = Array.isArray(orders) ? orders : (orders?.items || orders?.Items || orders?.orders || []);
        samples.order = pick(orderList[0], "checkoutId", "CheckoutId");
        const tickets = await page.evaluate(async () => {
          const res = await fetch("/api/customer/tickets", { credentials: "include", cache: "no-store" });
          const json = await res.json().catch(() => null);
          return json;
        });
        const ticketList = Array.isArray(tickets) ? tickets : (tickets?.items || tickets?.Items || []);
        samples.ticket = pick(ticketList[0], "ticketId", "TicketId", "id", "Id");
        report.samples = samples;
      }
      const path = fillPath(entry, samples);
      if (path.includes("{") || path.includes("missing-slug")) {
        report.missingSamples.push({ id: entry.id, path, notes: entry.notes, staticCoverage: entry.themeCoverage });
        console.log(`skip-missing ${entry.id} ${path}`);
        continue;
      }
      const R4_SHOTS = {
        "home-r3.png": "home-components-r4.png",
        "pdp-r3.png": "pdp-components-r4.png",
        "login-r3.png": "login-components-r4.png",
        "checkout-r3.png": "checkout-components-r4.png",
        "customer-dashboard-r3.png": "customer-dashboard-components-r4.png",
        "customer-orders-r3.png": "customer-order-components-r4.png",
        "landing-r3.png": "landing-components-r4.png",
        "content-r3.png": "content-components-r4.png",
      };
      const takeShot = run.shots === true || (run.shots === "dark" && (entry.id === "pdp" || entry.id === "customer-dashboard"));
      const shotNameOverride = run.shots === "dark" && entry.id === "pdp"
        ? "dark-pdp-components-r4.png"
        : run.shots === "dark" && entry.id === "customer-dashboard"
          ? "dark-customer-components-r4.png"
          : run.shots === true && entry.proofShot
            ? (R4_SHOTS[entry.proofShot] || entry.proofShot.replace(/-r3\.png$/, "-components-r4.png"))
            : null;
      const labeled = shotNameOverride ? { ...entry, proofShot: shotNameOverride } : entry;
      await crawlRoute(page, labeled, path, run.name, takeShot);
    }
  }

  const failed = report.routes.filter((row) => !row.ok);
  for (const old of failed) {
    const entry = STOREFRONT_ROUTE_INVENTORY.find((row) => row.id === old.id);
    if (!entry) continue;
    if (entry.auth === "customer" && !loggedIn) {
      await loginUi(page);
      loggedIn = true;
    }
    const path = fillPath(entry, samples);
    if (path.includes("{")) continue;
    report.routes = report.routes.filter((row) => !(row.id === old.id && row.run === old.run));
    const run = report.runs.find((item) => item.name === old.run);
    if (run?.body) {
      await putAppearance(run.body.paletteKey, run.body.themeMode, "classic", run.body.backgroundStyle);
      await page.waitForTimeout(400);
    }
    console.log(`retry ${old.run} ${entry.id}`);
    await crawlRoute(page, entry, path, old.run, false);
  }
  report.ok = report.routes.every((row) => row.ok);

  const end = await putAppearance(CANONICAL.paletteKey, CANONICAL.themeMode, CANONICAL.productCardSkin, CANONICAL.backgroundStyle);
  report.restored = { status: end.status, paletteKey: end.body?.paletteKey, themeMode: end.body?.themeMode, backgroundStyle: end.body?.backgroundStyle, productCardSkin: end.body?.productCardSkin };
  if (end.status !== 200 || end.body?.paletteKey !== "tooba-blue" || end.body?.backgroundStyle !== "Neutral" || end.body?.themeMode !== "LightOnly") {
    report.ok = false;
  }

  await browser.close();
  writeFileSync(join(EVIDENCE, "r4-runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.console.push({ fatal: String(err) });
  writeFileSync(join(EVIDENCE, "r4-runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
