import { writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const out = { console: [], hydration: [], cards: [], overlay: null };

function css(el, props) {
  const s = getComputedStyle(el);
  const box = el.getBoundingClientRect();
  const result = { tag: el.tagName, className: String(el.className).slice(0, 180), w: box.width, h: box.height, x: box.x, y: box.y };
  for (const p of props) result[p] = s.getPropertyValue(p);
  return result;
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" })).newPage();
  page.on("console", (msg) => {
    const text = msg.text();
    out.console.push({ type: msg.type(), text: text.slice(0, 800) });
    if (/hydrat/i.test(text) || /didn.t match/i.test(text)) out.hydration.push(text.slice(0, 2000));
  });
  page.on("pageerror", (err) => out.console.push({ type: "pageerror", text: String(err).slice(0, 800) }));

  await page.goto(`${FE}/fa/products`, { waitUntil: "load" });
  out.cards = await page.evaluate(() => {
    const cards = [...document.querySelectorAll("[data-testid=storefront-product-card]")].slice(0, 3);
    return cards.map((card) => {
      const well = card.querySelector("[data-storefront-media-well]");
      const img = well?.querySelector("img");
      const pick = (el, props) => {
        if (!el) return null;
        const s = getComputedStyle(el);
        const box = el.getBoundingClientRect();
        const r = {
          tag: el.tagName,
          className: String(el.className).slice(0, 220),
          w: Math.round(box.width),
          h: Math.round(box.height),
          x: Math.round(box.x),
          y: Math.round(box.y),
        };
        for (const p of props) r[p] = s.getPropertyValue(p);
        return r;
      };
      return {
        skin: card.getAttribute("data-product-card-skin"),
        card: pick(card, ["display", "overflow", "position"]),
        well: pick(well, ["display", "visibility", "opacity", "overflow", "position", "aspect-ratio", "z-index", "background-color"]),
        img: img ? {
          ...pick(img, ["display", "visibility", "opacity", "object-fit", "object-position", "position", "z-index"]),
          src: img.currentSrc || img.src,
          naturalWidth: img.naturalWidth,
          naturalHeight: img.naturalHeight,
          complete: img.complete,
        } : null,
      };
    });
  });
  out.overlay = await page.evaluate(() => {
    const n = document.querySelector("nextjs-portal") || document.querySelector("[data-nextjs-dialog]") || document.querySelector("button");
    const issue = [...document.querySelectorAll("button, a, div")].find((el) => /Issue/i.test(el.textContent || ""));
    return {
      issueText: issue ? (issue.textContent || "").slice(0, 80) : null,
      htmlAttrs: {
        className: document.documentElement.className,
        colorScheme: document.documentElement.getAttribute("data-storefront-color-scheme"),
        themeMode: document.documentElement.getAttribute("data-storefront-theme-mode"),
        dir: document.documentElement.getAttribute("dir"),
        lang: document.documentElement.lang,
      },
    };
  });
  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r2-probe.json"), JSON.stringify(out, null, 2));
})();
