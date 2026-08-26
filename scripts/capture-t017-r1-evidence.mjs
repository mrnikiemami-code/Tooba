import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T017");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
let port = Number(process.env.TOOBA_CDP_PORT || 9251);
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3017";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeivaPdp =
  process.env.SHOPEIVA_PDP_URL ||
  `${shopeiva}/product/1/${encodeURIComponent("گوشی-موبایل-اپل-آیفون-۱۵-پرو-مکس")}`;
const toobaPdp = process.env.TOOBA_PDP_URL || `${tooba}/products/demo-mobile-1`;

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

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t017r1-chrome-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${sessionPort}`,
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
      const pages = await getJson(`http://127.0.0.1:${sessionPort}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // waiting
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
      // ignore
    }
  }
}

async function navigate(send, url, readyText) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 80; attempt += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body && document.body.innerText.includes(${JSON.stringify(readyText)}), text: (document.body?.innerText || '').slice(0, 200) })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) return;
  }
  const last = await send("Runtime.evaluate", {
    expression: `(document.body?.innerText || '').slice(0, 400)`,
    returnByValue: true,
  });
  throw new Error(`not ready ${url} :: ${last.result.value}`);
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function clickTab(send, label) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const needle = ${JSON.stringify(label)};
      const match = [...document.querySelectorAll('button')].find((node) => (node.innerText || '').includes(needle));
      if (!match) return false;
      match.click();
      const card = match.closest('.rounded-2xl');
      card?.scrollIntoView({ block: 'start' });
      return true;
    })()`,
    returnByValue: true,
  });
  if (!result.result.value) throw new Error(`tab missing: ${label}`);
  await delay(600);
}

async function captureShopeiva() {
  await withSession({ width: 1440, height: 1100, mobile: false }, async ({ send }) => {
    await navigate(send, shopeivaPdp, "معرفی اجمالی");
    await screenshot(send, "02-original-shopeiva-pdp-top.png");
    await clickTab(send, "معرفی اجمالی");
    await screenshot(send, "03-original-shopeiva-tab-overview.png");
    await clickTab(send, "معرفی تکمیلی");
    await screenshot(send, "04-original-shopeiva-tab-details.png");
    await clickTab(send, "مشخصات فنی");
    await screenshot(send, "05-original-shopeiva-tab-specifications.png");
    await clickTab(send, "نظرات");
    await screenshot(send, "06-original-shopeiva-tab-reviews.png");
    await clickTab(send, "پرسش و پاسخ");
    await screenshot(send, "07-original-shopeiva-tab-qa.png");
    await clickTab(send, "خرید عمده");
    await screenshot(send, "08-original-shopeiva-tab-wholesale.png");
  });
}

async function alignTabsBelowHeader(send, extraScroll = 0) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const card = document.querySelector('[data-testid="pdp-tabs-card"]');
      const strip = card?.querySelector('.sticky');
      if (!card || !strip) return { ok: false, reason: 'missing-tabs' };
      const header = document.querySelector('.sticky.top-0.z-50') || document.querySelector('header');
      const headerHeight = header ? Math.ceil(header.getBoundingClientRect().height) : 0;
      const absoluteTop = card.getBoundingClientRect().top + window.scrollY;
      window.scrollTo({ top: Math.max(0, absoluteTop - headerHeight - 8 + ${Number(extraScroll)}), behavior: 'instant' });
      const rect = strip.getBoundingClientRect();
      return {
        ok: true,
        headerHeight,
        stripTop: Math.round(rect.top),
        stripHeight: Math.round(rect.height),
        labels: [...strip.querySelectorAll('button')].map((node) => (node.innerText || '').replace(/\\s+/g, ' ').trim()),
      };
    })()`,
    returnByValue: true,
  });
  if (!result.result.value?.ok) throw new Error(`tabs align failed: ${JSON.stringify(result.result.value)}`);
  await delay(450);
  return result.result.value;
}

async function captureStickyTooba() {
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigate(send, toobaPdp, "معرفی اجمالی");
    const desktop = await alignTabsBelowHeader(send, 0);
    console.log("sticky-desktop-align", desktop);
    await screenshot(send, "26-sticky-tab-desktop.png");
    await clickTab(send, "مشخصات فنی");
    // Keep strip fully below sticky site header so active tab + section stay visible.
    const active = await alignTabsBelowHeader(send, 0);
    console.log("sticky-active-align", active);
    await screenshot(send, "27-sticky-tab-active-section.png");
    // Prove stickiness: after further scroll, strip should pin near viewport top (under header).
    const stuck = await send("Runtime.evaluate", {
      expression: `(() => {
        const strip = document.querySelector('[data-testid="pdp-tabs-card"] .sticky');
        const before = strip.getBoundingClientRect().top;
        window.scrollBy(0, 220);
        const after = strip.getBoundingClientRect().top;
        return { before: Math.round(before), after: Math.round(after), stuck: after <= 1 };
      })()`,
      returnByValue: true,
    });
    console.log("sticky-pin-check", stuck.result.value);
  });
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, toobaPdp, "معرفی اجمالی");
    const mobile = await alignTabsBelowHeader(send, 0);
    console.log("sticky-mobile-align", mobile);
    await screenshot(send, "28-sticky-tab-mobile.png");
  });
}

const mode = process.argv[2] || "all";
async function main() {
  if (mode === "shopeiva" || mode === "all") await captureShopeiva();
  if (mode === "sticky" || mode === "all") await captureStickyTooba();
  console.log("capture-t017-r1 complete", mode);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
