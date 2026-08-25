import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T003");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9230;
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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-customer-chrome-"));
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
      // پاک‌سازی best-effort پروفایل موقت.
    }
  }
}

async function navigate(send, url, readyText) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 40; attempt += 1) {
    await delay(150);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body.innerText.includes(${JSON.stringify(readyText)}), overflow: document.documentElement.scrollWidth > innerWidth })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) {
      if (result.result.value.overflow) throw new Error(`horizontal overflow at ${url}`);
      return;
    }
  }
  throw new Error(`page did not become ready: ${url}`);
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
  await navigate(send, "http://127.0.0.1:3000/customer-panel", "آخرین سفارش‌ها");
  await screenshot(send, "01-customer-dashboard-desktop-1440x900.png");

  await navigate(send, "http://127.0.0.1:3000/customer-panel/orders", "کل سفارش‌ها");
  for (let attempt = 0; attempt < 40; attempt += 1) {
    const probe = await send("Runtime.evaluate", {
      expression: `!!document.querySelector('a[href^="/customer-panel/orders/"]')`,
      returnByValue: true,
    });
    if (probe.result.value) break;
    await delay(150);
  }
  await screenshot(send, "02-customer-orders-desktop-1440x900.png");
  const firstLink = await send("Runtime.evaluate", {
    expression: `[...document.querySelectorAll('a[href^="/customer-panel/orders/"]')][0]?.getAttribute('href') ?? null`,
    returnByValue: true,
  });
  const detailPath = firstLink.result.value;
  if (!detailPath) throw new Error("live customer order detail link missing");
  await navigate(send, `http://127.0.0.1:3000${detailPath}`, "نشانی تحویل");
  await screenshot(send, "03-customer-order-detail-desktop-1440x900.png");

  await navigate(send, "http://127.0.0.1:3000/customer-panel/addresses", "هنوز به backend متصل نیست");
  await screenshot(send, "05-address-capability-empty-state.png");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await navigate(send, "http://127.0.0.1:3000/customer-panel", "آخرین سفارش‌ها");
  await screenshot(send, "04-customer-dashboard-mobile-390x844.png");
});
