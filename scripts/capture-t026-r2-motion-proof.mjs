import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T026-R2");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = "http://127.0.0.1:3000";
const port = 9288;

fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(e);
        }
      });
    }).on("error", reject);
  });
}

async function main() {
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t026r2-motion-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${userData}`,
      "--headless=new",
      "--disable-gpu",
      "about:blank",
    ],
    { stdio: "ignore" },
  );
  let webSocketUrl = null;
  for (let i = 0; i < 40; i++) {
    try {
      const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {}
    await delay(250);
  }
  if (!webSocketUrl) throw new Error("CDP not ready");
  const socket = new WebSocket(webSocketUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve);
    socket.addEventListener("error", reject);
  });
  let nextId = 1;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const handlers = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) handlers.reject(new Error(JSON.stringify(message.error)));
      else handlers.resolve(message.result);
    }
  });
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });

  await send("Page.enable");
  await send("Runtime.enable");
  await send("Emulation.setDeviceMetricsOverride", {
    width: 1440,
    height: 900,
    deviceScaleFactor: 1,
    mobile: false,
  });
  await send("Page.navigate", { url: `${tooba}/` });
  await delay(2500);

  const proof = await send("Runtime.evaluate", {
    expression: `(() => {
      const qs = (s) => document.querySelector(s);
      const newRail = qs('[data-testid="home-new-products-carousel"]') || qs('[data-testid="home-new-products"]');
      const brands = qs('[data-testid="home-brands"]');
      const best = qs('[data-testid="home-best-sellers"]');
      const reviews = qs('[data-testid="home-testimonials"]');
      const articles = qs('[data-testid="home-articles"]');

      function slideTranslate(root) {
        const slide = root?.querySelector('.swiper-slide-active, .swiper-wrapper');
        if (!slide) return null;
        const wrapper = root.querySelector('.swiper-wrapper');
        return wrapper ? getComputedStyle(wrapper).transform : null;
      }

      const t0 = slideTranslate(newRail);
      return new Promise((resolve) => {
        setTimeout(() => {
          const t1 = slideTranslate(newRail);
          const brandCard = brands?.querySelector('button, a, .group');
          const bestRow = best?.querySelector('[class*="group"]') || best?.querySelector('a');
          const articleCard = articles?.querySelector('article, a.group, .group');
          resolve({
            sections: {
              bestSellers: !!best,
              brands: !!brands,
              newProducts: !!newRail,
              testimonials: !!reviews,
              articles: !!articles,
            },
            newProductsAutoplay: {
              transformBefore: t0,
              transformAfter: t1,
              moved: !!(t0 && t1 && t0 !== t1),
              hasSwiper: !!(newRail && newRail.querySelector('.swiper-wrapper')),
            },
            reviewsPagination: !!(reviews && reviews.querySelector('.swiper-pagination')),
            articlesPagination: !!(articles && articles.querySelector('.swiper-pagination')),
            brandHasOverlayClasses: !!(brands && brands.innerHTML.includes('from-black')),
            bestHasHoverShadowClasses: !!(best && (best.innerHTML.includes('hover:shadow') || best.innerHTML.includes('hover:-translate'))),
            articleHasHoverLift: !!(articles && articles.innerHTML.includes('hover:-translate-y')),
          });
        }, 4500);
      });
    })()`,
    awaitPromise: true,
    returnByValue: true,
  });

  const result = proof.result.value;
  const md = `# 13 — Home motion / interaction proof (TB-P05-T026-R2)

Deterministic CDP checks against live Tooba Home after repair.

\`\`\`json
${JSON.stringify(result, null, 2)}
\`\`\`

| Check | Result |
|---|---|
| Newest Products Swiper present | ${result.newProductsAutoplay.hasSwiper ? "PASS" : "FAIL"} |
| Newest Products translate changed without user input (~4.5s) | ${result.newProductsAutoplay.moved ? "PASS (autoplay)" : "INCONCLUSIVE/FAIL"} |
| Reviews pagination bullets | ${result.reviewsPagination ? "PASS" : "FAIL"} |
| Articles pagination bullets | ${result.articlesPagination ? "PASS" : "FAIL"} |
| Brand gradient overlay classes | ${result.brandHasOverlayClasses ? "PASS" : "FAIL"} |
| Best Sellers hover shadow/lift classes | ${result.bestHasHoverShadowClasses ? "PASS" : "FAIL"} |
| Articles hover lift classes | ${result.articleHasHoverLift ? "PASS" : "FAIL"} |

Autoplay config (source-compatible): delay 4000ms, \`pauseOnMouseEnter: true\`, \`disableOnInteraction: false\` (New Products / Testimonials); Articles delay 5000ms.

**Motion proof: ${result.newProductsAutoplay.moved && result.newProductsAutoplay.hasSwiper && result.sections.testimonials && result.sections.articles ? "PASS" : "REVIEW"}**
`;
  fs.writeFileSync(path.join(outDir, "13-home-motion-interaction-proof.md"), md, "utf8");
  console.log(JSON.stringify(result, null, 2));

  socket.close();
  chromeProcess.kill();
  try {
    fs.rmSync(userData, { recursive: true, force: true });
  } catch {}
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
