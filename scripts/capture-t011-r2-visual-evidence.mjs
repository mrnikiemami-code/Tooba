/**
 * TB-P06-T011-R2 — mandatory Returns visual parity captures (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T011-R2/captures");
const motionPath = path.resolve("docs/evidence/TB-P06-T011-R2/motion-proof.json");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
const sellerPartyId = process.env.TOOBA_SELLER_PARTY_ID || "01a030d1-40cb-7000-8abe-6d31739956c5";
const actorUserId = process.env.TOOBA_ACTOR_USER_ID || "01a03628-3f68-7000-844d-99f1cadb54b0";
const checkoutId = process.env.TOOBA_CHECKOUT_ID || "01a03ef2-4d7c-7000-a47a-deee181523cd";
let port = Number(process.env.TOOBA_CDP_PORT || 9300);

fs.mkdirSync(outDir, { recursive: true });
const motion = { capturedAt: new Date().toISOString(), interactions: [] };

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t011r2-chrome-"));
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
  await delay(1500);
}

async function shot(send, name, full = true) {
  const result = await send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: full === true,
  });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", name, fs.statSync(file).size);
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
        boxShadow: cs.boxShadow,
        transitionProperty: cs.transitionProperty,
        transitionDuration: cs.transitionDuration,
        opacity: cs.opacity,
        transform: cs.transform,
      };
    })()`,
    returnByValue: true,
  });
  return result.result.value;
}

async function clickText(send, text) {
  await send("Runtime.evaluate", {
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
  await delay(800);
}

async function primeToobaSellerStorage(send) {
  await send("Runtime.evaluate", {
    expression: `(() => {
      localStorage.setItem('tooba.sellerPartyId', ${JSON.stringify(sellerPartyId)});
      localStorage.setItem('tooba.devActorUserId', ${JSON.stringify(actorUserId)});
      localStorage.setItem('tooba.adminActorUserId', '01a036c2-970e-7000-8eb7-94bf5cc2d8db');
      return true;
    })()`,
    returnByValue: true,
  });
}

async function main() {
  // --- Desktop Shopeiva customer return modal ---
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigate(send, `${shopeiva}/user-panel/orders`);
    await shot(send, "01-shopeiva-customer-orders-desktop.png");
    await clickText(send, "مرجوع");
    await shot(send, "02-shopeiva-customer-return-modal-desktop.png");
    const before = await evalStyle(send, "button");
    await send("Runtime.evaluate", {
      expression: `(() => { const b=document.querySelector('button.bg-emerald-500,button.bg-\\[\\#2563EB\\],button'); if(b){b.dispatchEvent(new MouseEvent('mouseover',{bubbles:true}));} })()`,
    });
    await delay(300);
    const after = await evalStyle(send, "button");
    motion.interactions.push({ surface: "shopeiva-customer-modal", before, after });

    await navigate(send, `${shopeiva}/vendor-panel/orders/1`);
    await shot(send, "03-shopeiva-seller-order-detail-desktop.png");
    await clickText(send, "بررسی درخواست");
    await shot(send, "04-shopeiva-seller-return-review-modal-desktop.png");
  });

  // --- Desktop Tooba ---
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await primeToobaSellerStorage(send);
    await navigate(send, `${tooba}/customer-panel/orders/${checkoutId}`);
    await shot(send, "05-tooba-customer-order-detail-desktop.png");
    await navigate(send, `${tooba}/customer-panel/orders`);
    await shot(send, "06-tooba-customer-orders-desktop.png");

    await navigate(send, `${tooba}/vendor-panel/returns?sellerPartyId=${sellerPartyId}`);
    await shot(send, "07-tooba-seller-returns-list-desktop.png");
    const rowLinkBefore = await evalStyle(send, "a.font-semibold");
    await send("Runtime.evaluate", {
      expression: `(() => { const a=document.querySelector('a.font-semibold'); if(a){a.dispatchEvent(new MouseEvent('mouseover',{bubbles:true}));} })()`,
    });
    await delay(300);
    motion.interactions.push({
      surface: "tooba-seller-grid",
      before: rowLinkBefore,
      after: await evalStyle(send, "a.font-semibold"),
    });

    await navigate(send, `${tooba}/admin/returns`);
    await shot(send, "08-tooba-admin-returns-list-desktop.png");
    await navigate(send, `${tooba}/`);
    await shot(send, "09-tooba-home-desktop.png");
    await navigate(send, `${tooba}/products/demo-game-2`);
    await shot(send, "10-tooba-pdp-desktop.png");
  });

  // --- Mobile ---
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, `${shopeiva}/user-panel/orders`);
    await clickText(send, "مرجوع");
    await shot(send, "11-shopeiva-customer-return-modal-mobile.png", false);
    await navigate(send, `${shopeiva}/vendor-panel/orders/1`);
    await clickText(send, "بررسی درخواست");
    await shot(send, "12-shopeiva-seller-return-review-mobile.png", false);

    await primeToobaSellerStorage(send);
    await navigate(send, `${tooba}/vendor-panel/returns?sellerPartyId=${sellerPartyId}`);
    await shot(send, "13-tooba-seller-returns-mobile.png", false);
    await navigate(send, `${tooba}/admin/returns`);
    await shot(send, "14-tooba-admin-returns-mobile.png", false);
    await navigate(send, `${tooba}/customer-panel/orders/${checkoutId}`);
    await shot(send, "15-tooba-customer-order-mobile.png", false);
  });

  fs.writeFileSync(motionPath, JSON.stringify(motion, null, 2));
  console.log("t011-r2 captures complete ->", outDir);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
