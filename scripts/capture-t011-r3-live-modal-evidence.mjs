/**
 * TB-P06-T011-R3 — capture real Tooba customer/seller return modals (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";
import { submitReturnRequest } from "./t011-r3-return-scenario.mjs";

const outDir = path.resolve("docs/evidence/TB-P06-T011-R3/captures");
const motionPath = path.resolve("docs/evidence/TB-P06-T011-R3/motion-proof.json");
const scenarioPath = path.resolve("docs/evidence/TB-P06-T011-R3/dev-scenario.json");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
let port = Number(process.env.TOOBA_CDP_PORT || 9400);

fs.mkdirSync(outDir, { recursive: true });
const motion = { capturedAt: new Date().toISOString(), interactions: [] };

function loadScenario() {
  if (!fs.existsSync(scenarioPath)) throw new Error(`missing ${scenarioPath}`);
  return JSON.parse(fs.readFileSync(scenarioPath, "utf8"));
}

function saveScenario(scenario) {
  fs.writeFileSync(scenarioPath, JSON.stringify(scenario, null, 2));
}

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t011r3-chrome-"));
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
    } catch {}
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("CDP not ready");
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
    } catch {}
  }
}

async function navigate(send, url) {
  await send("Page.navigate", { url });
  for (let i = 0; i < 80; i += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: "document.readyState",
      returnByValue: true,
    });
    if (result.result.value === "complete") break;
  }
  await delay(2000);
}

async function shot(send, name, full = true) {
  const result = await send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: full === true,
  });
  fs.writeFileSync(path.join(outDir, name), Buffer.from(result.data, "base64"));
  console.log("wrote", name);
}

async function evalStyle(send, selector) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const el = document.querySelector(${JSON.stringify(selector)});
      if (!el) return null;
      const cs = getComputedStyle(el);
      return {
        backgroundColor: cs.backgroundColor,
        color: cs.color,
        borderRadius: cs.borderRadius,
        opacity: cs.opacity,
        transitionProperty: cs.transitionProperty,
        transitionDuration: cs.transitionDuration,
      };
    })()`,
    returnByValue: true,
  });
  return result.result.value;
}

async function clickText(send, text) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const nodes = [...document.querySelectorAll('button,a,span')];
      const hit = nodes.find(n => (n.textContent || '').includes(${JSON.stringify(text)}));
      if (!hit) return false;
      hit.scrollIntoView({block:'center'});
      hit.click();
      return true;
    })()`,
    returnByValue: true,
  });
  await delay(1200);
  return result.result.value === true;
}

async function waitForReturnButton(send, attempts = 60) {
  for (let i = 0; i < attempts; i += 1) {
    const result = await send("Runtime.evaluate", {
      expression: `[...document.querySelectorAll('button')].some(b => (b.textContent||'').includes('درخواست مرجوعی'))`,
      returnByValue: true,
    });
    if (result.result.value) return true;
    await delay(500);
  }
  return false;
}

async function waitForSelector(send, selector, attempts = 40) {
  for (let i = 0; i < attempts; i += 1) {
    const result = await send("Runtime.evaluate", {
      expression: `Boolean(document.querySelector(${JSON.stringify(selector)}))`,
      returnByValue: true,
    });
    if (result.result.value) return true;
    await delay(500);
  }
  return false;
}

async function primeSellerStorage(send, scenario) {
  await send("Runtime.evaluate", {
    expression: `(() => {
      localStorage.setItem('tooba.sellerPartyId', ${JSON.stringify(scenario.sellerPartyId)});
      localStorage.setItem('tooba.devActorUserId', ${JSON.stringify(scenario.sellerActor)});
      return true;
    })()`,
    returnByValue: true,
  });
}

async function main() {
  const scenario = loadScenario();

  // --- Customer desktop: order page + live return request modal ---
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigate(send, scenario.customerOrderUrl);
    await shot(send, "01-tooba-customer-order-before-modal-desktop.png");
    const ready = await waitForReturnButton(send);
    if (!ready) throw new Error("customer return button not found — fulfillment not Delivered?");
    const clicked = await clickText(send, "درخواست مرجوعی");
    if (!clicked) throw new Error("customer return button click failed");
    await waitForSelector(send, "h3");
    await shot(send, "02-tooba-customer-return-modal-open-desktop.png");
    const before = await evalStyle(send, "button.bg-\\[\\#2563EB\\], button");
    await send("Runtime.evaluate", {
      expression: `(() => { const b=document.querySelector('button.bg-[#2563EB],button'); if(b){b.dispatchEvent(new MouseEvent('mouseover',{bubbles:true}));} })()`,
    });
    await delay(300);
    motion.interactions.push({
      surface: "tooba-customer-return-modal",
      before,
      after: await evalStyle(send, "button.bg-\\[\\#2563EB\\], button"),
    });
    await shot(send, "03-tooba-customer-return-modal-hover-desktop.png");
  });

  // --- Customer mobile ---
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, scenario.customerOrderUrl);
    await clickText(send, "درخواست مرجوعی");
    await waitForSelector(send, "h3");
    await shot(send, "04-tooba-customer-return-modal-open-mobile.png", false);
  });

  // Submit return via Host API (after customer modal captured)
  if (!scenario.returnRequestId) {
    const created = await submitReturnRequest(scenario);
    scenario.returnRequestId = created.returnRequestId;
    scenario.returnStatus = created.status;
    scenario.sellerReturnDetailUrl = `${tooba}/vendor-panel/returns/${created.returnRequestId}?sellerPartyId=${scenario.sellerPartyId}`;
    saveScenario(scenario);
    console.log("return submitted", created.returnRequestId);
  }

  // --- Seller desktop: list + auto-open review modal ---
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await primeSellerStorage(send, scenario);
    await navigate(send, scenario.sellerReturnsUrl);
    await shot(send, "05-tooba-seller-returns-list-with-row-desktop.png");
    await navigate(send, scenario.sellerReturnDetailUrl);
    await waitForSelector(send, "h3");
    await shot(send, "06-tooba-seller-return-review-modal-open-desktop.png");
    const before = await evalStyle(send, "button.bg-emerald-500");
    await send("Runtime.evaluate", {
      expression: `(() => { const b=document.querySelector('button.bg-emerald-500'); if(b){b.dispatchEvent(new MouseEvent('mouseover',{bubbles:true}));} })()`,
    });
    await delay(300);
    motion.interactions.push({
      surface: "tooba-seller-review-modal",
      before,
      after: await evalStyle(send, "button.bg-emerald-500"),
    });
    await shot(send, "07-tooba-seller-return-review-modal-hover-desktop.png");
  });

  // --- Seller mobile ---
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await primeSellerStorage(send, scenario);
    await navigate(send, scenario.sellerReturnDetailUrl);
    await waitForSelector(send, "h3");
    await shot(send, "08-tooba-seller-return-review-modal-open-mobile.png", false);
  });

  // Reference Shopeiva modals (from R2 captures path for side-by-side doc)
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigate(send, `${shopeiva}/user-panel/orders`);
    await clickText(send, "مرجوع");
    await shot(send, "09-shopeiva-customer-return-modal-ref-desktop.png");
    await navigate(send, `${shopeiva}/vendor-panel/orders/1`);
    await clickText(send, "بررسی درخواست");
    await shot(send, "10-shopeiva-seller-return-review-ref-desktop.png");
  });

  fs.writeFileSync(motionPath, JSON.stringify(motion, null, 2));
  console.log("t011-r3 captures complete ->", outDir);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
