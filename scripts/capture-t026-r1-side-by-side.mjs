import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T026-R1");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
let port = Number(process.env.TOOBA_CDP_PORT || 9270);
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3017";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeivaHome = `${shopeiva}/`;
const shopeivaPdp =
  process.env.SHOPEIVA_PDP_URL ||
  `${shopeiva}/product/1/${encodeURIComponent("گوشی-موبایل-اپل-آیفون-۱۵-پرو-مکس")}`;
const toobaHome = `${tooba}/`;
const toobaPdp = process.env.TOOBA_PDP_URL || `${tooba}/products/demo-game-2`;

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t026r1-chrome-"));
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
      // retry
    }
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
    } catch {
      // ignore
    }
  }
}

async function navigate(send, url) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 80; attempt += 1) {
    await delay(300);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: !!(document.body && document.body.innerText && document.body.innerText.length > 80), title: document.title })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) return result.result.value;
  }
  throw new Error(`not ready ${url}`);
}

async function screenshot(send, name) {
  await delay(500);
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true, captureBeyondViewport: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function capturePair({ send }, url, file) {
  await navigate(send, url);
  await screenshot(send, file);
}

async function main() {
  await withSession({ width: 1440, height: 900, mobile: false }, async (session) => {
    await capturePair(session, shopeivaHome, "03-original-shopeiva-home-live.png");
    await capturePair(session, toobaHome, "04-current-tooba-home-live.png");
    await capturePair(session, shopeivaPdp, "05-original-shopeiva-pdp-live.png");
    await capturePair(session, toobaPdp, "06-current-tooba-pdp-live.png");
  });
  console.log(
    JSON.stringify({ shopeivaHome, toobaHome, shopeivaPdp, toobaPdp }, null, 2),
  );
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
