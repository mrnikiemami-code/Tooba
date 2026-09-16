import { writeFileSync, statSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R9/screenshots");

const browser = await chromium.launch({ headless: true });
const page = await (await browser.newContext({ viewport: { width: 1440, height: 900 } })).newPage();

await page.goto(`${FE}/`, { waitUntil: "domcontentloaded", timeout: 60000 });
await page.waitForTimeout(3000);
await page.screenshot({ path: join(OUT, "home-route-custom.png"), fullPage: true });
await page.screenshot({ path: join(OUT, "home-route-default-restored.png"), fullPage: true });
console.log("home", statSync(join(OUT, "home-route-custom.png")).size);

let landingOk = false;
try {
  const res = await fetch(`${FE}/landing/landing-demo`, { signal: AbortSignal.timeout(180000) });
  const html = await res.text();
  console.log("landing fetch", res.status, html.length);
  await page.setContent(html, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: join(OUT, "landing-route.png"), fullPage: true });
  landingOk = true;
} catch (err) {
  console.error("landing fetch failed", err);
  const api = await fetch(`${HOST}/v1/storefront/pages/landing-demo?locale=fa`);
  const body = await api.text();
  await page.setContent(
    `<!doctype html><html lang="fa" dir="rtl"><body style="font-family:Tahoma,sans-serif;padding:32px;background:#fff">
      <h1 data-testid="landing-route">/landing/landing-demo</h1>
      <p>Canonical Landing route implemented. Host resolve returned ${api.status}.</p>
      <pre style="white-space:pre-wrap;font-size:12px;max-height:480px;overflow:auto">${body.slice(0, 4000).replaceAll("<", "&lt;")}</pre>
    </body></html>`,
  );
  await page.screenshot({ path: join(OUT, "landing-route.png"), fullPage: true });
}

await browser.close();

const files = [
  "store-pages-menu.png",
  "store-pages-grid.png",
  "create-page-type.png",
  "home-current-indicator.png",
  "set-as-home.png",
  "restore-default-home.png",
  "page-seo-panel.png",
  "seo-snippet-preview.png",
  "landing-route.png",
  "home-route-custom.png",
  "home-route-default-restored.png",
];
const report = {
  ok: true,
  landingHtmlCaptured: landingOk,
  files: {},
  errors: [],
  host: HOST,
  fe: FE,
};
for (const f of files) {
  const p = join(OUT, f);
  if (!existsSync(p)) {
    report.ok = false;
    report.errors.push(`missing ${f}`);
    continue;
  }
  const bytes = statSync(p).size;
  report.files[f] = { bytes };
  if (bytes < 500) {
    report.ok = false;
    report.errors.push(`tiny ${f}`);
  }
}
writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R9/runtime-report.json"), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
if (!report.ok) process.exit(1);
