import { writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const routes = ["/fa", "/fa/products", "/fa/products/demo-prod-fashion-men-men-pants-1", "/fa/landing-campaign"];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  const page = await context.newPage();
  const rows = [];
  for (const path of routes) {
    const console = [];
    const onMsg = (msg) => {
      const text = msg.text();
      if (msg.type() === "error" || /hydrat|didn't match|did not match|mismatch/i.test(text)) {
        console.push({ type: msg.type(), text: text.slice(0, 4000) });
      }
    };
    page.on("console", onMsg);
    const res = await page.goto(`${FE}${path}`, { waitUntil: "load" });
    await page.waitForTimeout(1200);
    const overlay = await page.evaluate(() => {
      const portals = [...document.querySelectorAll("nextjs-portal")];
      const texts = portals.map((p) => (p.shadowRoot ? p.shadowRoot.textContent : p.textContent) || "").slice(0, 1);
      return {
        statusNote: document.querySelector("[data-next-badge-root]")?.textContent?.slice(0, 200) || null,
        portal: texts[0]?.slice(0, 1500) || null,
      };
    });
    page.off("console", onMsg);
    rows.push({ path, status: res?.status(), console, overlay });
  }
  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r2-hydration-probe.json"), JSON.stringify(rows, null, 2));
})();
