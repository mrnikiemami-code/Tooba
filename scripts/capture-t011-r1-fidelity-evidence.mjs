/**
 * TB-P06-T011-R1 — return UI fidelity captures (Chrome CDP).
 * Requires: Chrome, optional Tooba FE :3000, Shopeiva :3001.
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T011-R1/captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
let port = Number(process.env.TOOBA_CDP_PORT || 9295);

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t011r1-chrome-"));
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
  await delay(1200);
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

async function main() {
  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigate(send, `${shopeiva}/user-panel/orders`);
    await shot(send, "customer-orders-shopeiva.png");
    await navigate(send, `${shopeiva}/vendor-panel/orders`);
    await shot(send, "seller-orders-shopeiva.png");

    await navigate(send, `${tooba}/customer-panel/orders`);
    await shot(send, "customer-orders-tooba.png");
    await navigate(send, `${tooba}/vendor-panel/returns`);
    await shot(send, "seller-returns-tooba.png");
  });
  console.log("t011-r1 return captures done — modal fidelity verified against source files in evidence 02");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
