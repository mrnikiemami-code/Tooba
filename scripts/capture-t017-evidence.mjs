import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T017");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9244;
const host = "http://127.0.0.1:5088";
const web = "http://127.0.0.1:3000";
const slug = process.env.TOOBA_PDP_SLUG || "demo-mobile-1";

fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (error) {
          reject(error);
        }
      });
    }).on("error", reject);
  });
}

async function hostFetch(pathname) {
  const response = await fetch(`${host}${pathname}`, {
    headers: { Accept: "application/json", Host: "localhost" },
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json, text };
}

async function withSession(viewport, work) {
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t017-chrome-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${userData}`,
      "--headless=new",
      "--disable-gpu",
      "--hide-scrollbars",
      `--window-size=${viewport.width},${viewport.height}`,
      "about:blank",
    ],
    { stdio: "ignore" },
  );

  let webSocketUrl = null;
  for (let attempt = 0; attempt < 40; attempt += 1) {
    try {
      const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // Chrome هنوز آماده نیست.
    }
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("Chrome CDP not ready");
  }

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

  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: viewport.mobile === true,
    });
    await work({ send });
  } finally {
    socket.close();
    chromeProcess.kill();
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {
      // پاک‌سازی best-effort.
    }
  }
}

async function navigate(send, url, readyText) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 80; attempt += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body && document.body.innerText.includes(${JSON.stringify(readyText)}), overflow: document.documentElement.scrollWidth > innerWidth + 2 })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) {
      if (result.result.value.overflow) throw new Error(`horizontal overflow at ${url}`);
      return;
    }
  }
  const last = await send("Runtime.evaluate", {
    expression: `(document.body?.innerText || '').slice(0, 500)`,
    returnByValue: true,
  });
  throw new Error(`page did not become ready: ${url} :: ${last.result.value}`);
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function clickTabAndShot(send, label, filename, testId, readyNeedle) {
  const clicked = await send("Runtime.evaluate", {
    expression: `(() => {
      const needle = ${JSON.stringify(label)};
      const buttons = [...document.querySelectorAll('button')];
      const match = buttons.find((node) => (node.innerText || '').includes(needle));
      if (!match) return false;
      match.click();
      // related rail را موقتاً مخفی کن تا اسکرین‌شات تب آلوده نشود
      const related = document.getElementById('related-products-title')?.closest('section');
      if (related) related.style.display = 'none';
      return true;
    })()`,
    returnByValue: true,
  });
  if (!clicked.result.value) throw new Error(`tab missing: ${label}`);

  let readyOk = false;
  for (let attempt = 0; attempt < 50; attempt += 1) {
    await delay(120);
    const ready = await send("Runtime.evaluate", {
      expression: `(() => {
        const testId = ${JSON.stringify(testId)};
        const needle = ${JSON.stringify(readyNeedle)};
        const panel = document.querySelector('[data-testid="' + testId + '"]');
        if (!panel) return { ok: false };
        const text = panel.innerText || '';
        if (!text.includes(needle)) return { ok: false, text: text.slice(0, 80) };
        const tabBtn = [...document.querySelectorAll('button')].find((node) => (node.innerText || '').includes(${JSON.stringify(label)}));
        const card = tabBtn?.closest('.rounded-2xl');
        if (card) {
          card.scrollIntoView({ block: 'start' });
          window.scrollBy(0, -12);
        }
        return { ok: true, preview: text.slice(0, 100) };
      })()`,
      returnByValue: true,
    });
    if (ready.result.value?.ok) {
      readyOk = true;
      console.log("ready", label, ready.result.value.preview?.replace(/\s+/g, " "));
      break;
    }
  }
  if (!readyOk) throw new Error(`tab content not ready: ${label} / ${testId}`);
  await delay(250);
  await screenshot(send, filename);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const related = document.getElementById('related-products-title')?.closest('section');
      if (related) related.style.display = '';
      return true;
    })()`,
    returnByValue: true,
  });
}

async function scrollToTabs(send) {
  await send("Runtime.evaluate", {
    expression: `(() => {
      const tabs = [...document.querySelectorAll('button')].find((node) => (node.innerText || '').includes('معرفی اجمالی'));
      tabs?.scrollIntoView({ block: 'start' });
      return !!tabs;
    })()`,
    returnByValue: true,
  });
  await delay(200);
}

async function main() {
  const detail = await hostFetch(`/v1/storefront/products/${slug}`);
  if (detail.status !== 200) {
    throw new Error(`PDP API failed ${detail.status}: ${detail.text?.slice?.(0, 300)}`);
  }
  fs.writeFileSync(path.join(outDir, "_api-pdp.json"), JSON.stringify(detail.json, null, 2));
  const questions = await hostFetch(`/v1/storefront/products/${slug}/questions`);
  fs.writeFileSync(path.join(outDir, "_api-questions.json"), JSON.stringify({ status: questions.status, body: questions.json }, null, 2));

  const pdpUrl = `${web}/products/${slug}`;
  const title = detail.json?.title || detail.json?.Title || "demo";

  await withSession({ width: 1440, height: 1100, mobile: false }, async ({ send }) => {
    await navigate(send, pdpUrl, title);
    await screenshot(send, "11-tooba-pdp-top-1440x900.png");

    await clickTabAndShot(send, "معرفی اجمالی", "12-tooba-tab-overview.png", "pdp-intro", "ارسال از فروشنده");
    await clickTabAndShot(send, "معرفی تکمیلی", "13-tooba-tab-details.png", "pdp-full", "ماژول‌های مالک");
    await clickTabAndShot(send, "مشخصات فنی", "14-tooba-tab-specifications.png", "pdp-specs", "استاندارد");
    await clickTabAndShot(send, "نظرات", "15-tooba-tab-reviews.png", "pdp-reviews", "نوشتن نظر");
    await clickTabAndShot(send, "پرسش و پاسخ", "16-tooba-tab-qa.png", "pdp-qa", "رضا");
    await clickTabAndShot(send, "خرید عمده", "17-tooba-tab-wholesale.png", "pdp-bulk", "ثبت درخواست عمده");

    await send("Runtime.evaluate", {
      expression: `(() => {
        const el = [...document.querySelectorAll('strong, h2, p')].find((node) => (node.innerText || '').includes('فروشندگان دیگر'));
        el?.scrollIntoView({ block: 'center' });
        return !!el;
      })()`,
      returnByValue: true,
    });
    await delay(300);
    await screenshot(send, "18-tooba-other-sellers.png");

    await send("Runtime.evaluate", {
      expression: `(() => {
        const el = document.getElementById('related-products-title') || [...document.querySelectorAll('h2')].find((node) => (node.innerText || '').includes('محصولات مرتبط'));
        el?.scrollIntoView({ block: 'start' });
        return !!el;
      })()`,
      returnByValue: true,
    });
    await delay(300);
    await screenshot(send, "19-tooba-related-products.png");
  });

  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, pdpUrl, title);
    await delay(400);
    await screenshot(send, "20-tooba-pdp-mobile-390x844.png");
  });

  console.log("capture-t017 complete");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
