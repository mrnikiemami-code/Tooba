import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const url = process.argv[2];
const ssr = await (await fetch(url)).text();
console.log("ssr sunny", ssr.includes('data-product-layout="sunny"'));
console.log("ssr slide-active true", ssr.includes('data-slide-active="true"'));
console.log("ssr showcase", ssr.includes("product-showcase-sunny"));

const b = await chromium.launch({ headless: true });
const p = await (await b.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" })).newPage();
p.on("pageerror", (e) => console.log("pageerror", e.message));
p.on("console", (m) => {
  if (m.type() === "error") console.log("console", m.text());
});
await p.goto(url, { waitUntil: "domcontentloaded", timeout: 90000 });
await p.waitForTimeout(4000);
const info = await p.evaluate(() => ({
  layout: [...document.querySelectorAll("[data-product-layout]")].map((e) => e.getAttribute("data-product-layout")),
  active: [...document.querySelectorAll("[data-slide-active]")].slice(0, 5).map((e) => e.getAttribute("data-slide-active")),
  htmlHasSunny: document.documentElement.innerHTML.includes("product-showcase-sunny"),
  bodySnippet: (document.body?.innerText || "").replace(/\s+/g, " ").slice(0, 240),
}));
console.log(JSON.stringify(info, null, 2));
await b.close();
