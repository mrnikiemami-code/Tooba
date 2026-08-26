import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T026-R2");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
let port = Number(process.env.TOOBA_CDP_PORT || 9280);
const shopeiva = "http://127.0.0.1:3017";
const tooba = "http://127.0.0.1:3000";

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

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t026r2-chrome-"));
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
      mobile: false,
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
  for (let i = 0; i < 80; i++) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: `!!(document.body && document.body.innerText && document.body.innerText.length > 40)`,
      returnByValue: true,
    });
    if (result.result.value) return;
  }
  throw new Error(`not ready ${url}`);
}

async function shot(send, name) {
  await delay(400);
  const result = await send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: true,
  });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", name, fs.statSync(file).size);
}

async function scrollToTestId(send, testId) {
  await send("Runtime.evaluate", {
    expression: `(() => { const el = document.querySelector('[data-testid="${testId}"]'); if (el) { el.scrollIntoView({ block: 'center' }); return true; } return false; })()`,
    returnByValue: true,
  });
  await delay(600);
}

async function main() {
  await withSession({ width: 1440, height: 900 }, async ({ send }) => {
    await navigate(send, `${shopeiva}/`);
    await scrollToTestId(send, "home-best-sellers").catch(() => {});
    // Shopeiva may not have same testids — scroll by text
    await send("Runtime.evaluate", {
      expression: `(() => { const h = [...document.querySelectorAll('h2,h3')].find(n => (n.innerText||'').includes('پرفروش')); h?.scrollIntoView({block:'center'}); return !!h; })()`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "02-original-best-sellers.png");

    await send("Runtime.evaluate", {
      expression: `(() => { const h = [...document.querySelectorAll('h2,h3')].find(n => (n.innerText||'').includes('برند')); h?.scrollIntoView({block:'center'}); return !!h; })()`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "04-original-brands.png");

    await send("Runtime.evaluate", {
      expression: `(() => { const h = [...document.querySelectorAll('h2,h3')].find(n => (n.innerText||'').includes('جدیدترین')); h?.scrollIntoView({block:'center'}); return !!h; })()`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "06-original-newest-products.png");

    await send("Runtime.evaluate", {
      expression: `(() => { const h = [...document.querySelectorAll('h2,h3')].find(n => (n.innerText||'').includes('نظرات') || (n.innerText||'').includes('مشتری')); h?.scrollIntoView({block:'center'}); return !!h; })()`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "08-original-customer-reviews.png");

    await send("Runtime.evaluate", {
      expression: `(() => { const h = [...document.querySelectorAll('h2,h3')].find(n => (n.innerText||'').includes('مقال') || (n.innerText||'').includes('بلاگ')); h?.scrollIntoView({block:'center'}); return !!h; })()`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "10-original-latest-articles.png");

    await shot(send, "14-original-shopeiva-home-final.png");
  });

  await withSession({ width: 1440, height: 900 }, async ({ send }) => {
    await navigate(send, `${tooba}/`);
    await delay(1500);

    for (const [tid, file] of [
      ["home-best-sellers", "03-tooba-best-sellers-after.png"],
      ["home-brands", "05-tooba-brands-after.png"],
      ["home-new-products", "07-tooba-newest-products-after.png"],
      ["home-testimonials", "09-tooba-customer-reviews-after.png"],
      ["home-articles", "11-tooba-latest-articles-after.png"],
    ]) {
      const found = await send("Runtime.evaluate", {
        expression: `(() => { const el = document.querySelector('[data-testid="${tid}"]'); if (el) { el.scrollIntoView({ block: 'center' }); return true; } return false; })()`,
        returnByValue: true,
      });
      console.log(tid, found.result.value);
      await delay(700);
      await shot(send, file);
    }

    await send("Runtime.evaluate", {
      expression: `window.scrollTo(0,0)`,
      returnByValue: true,
    });
    await delay(500);
    await shot(send, "15-tooba-home-after-repair-final.png");
  });
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
