import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T018");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
let port = 9260;
fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try { resolve(JSON.parse(data)); } catch (e) { reject(e); }
      });
    }).on("error", reject);
  });
}

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t018-chrome-"));
  const chromeProcess = spawn(chrome, [
    `--remote-debugging-port=${sessionPort}`,
    `--user-data-dir=${userData}`,
    "--headless=new",
    "--disable-gpu",
    "--hide-scrollbars",
    `--window-size=${viewport.width},${viewport.height}`,
    "about:blank",
  ], { stdio: "ignore" });
  let webSocketUrl = null;
  for (let attempt = 0; attempt < 40; attempt += 1) {
    try {
      const pages = await getJson(`http://127.0.0.1:${sessionPort}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) { webSocketUrl = page.webSocketDebuggerUrl; break; }
    } catch {}
    await delay(250);
  }
  if (!webSocketUrl) { chromeProcess.kill(); throw new Error("CDP not ready"); }
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
  const send = (method, params = {}) => new Promise((resolve, reject) => {
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
    try { fs.rmSync(userData, { recursive: true, force: true }); } catch {}
  }
}

async function navigate(send, url) {
  await send("Page.navigate", { url });
  for (let i = 0; i < 60; i += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: "document.readyState",
      returnByValue: true,
    });
    if (result.result.value === "complete") break;
  }
  await delay(800);
}

async function shot(send, name, full = false) {
  const result = await send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: full === true,
  });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", name, fs.statSync(file).size);
}

async function scroll(send, y) {
  await send("Runtime.evaluate", { expression: `window.scrollTo(0, ${Number(y)}); true`, returnByValue: true });
  await delay(400);
}

const mode = process.argv[2] || "before";

async function captureShopeiva() {
  await withSession({ width: 1440, height: 2200, mobile: false }, async ({ send }) => {
    await navigate(send, "http://127.0.0.1:3017/");
    await shot(send, "02-original-shopeiva-home-full.png", true);
    await scroll(send, 0);
    await shot(send, "03-original-shopeiva-home-top.png");
    await scroll(send, 700);
    await shot(send, "04-original-shopeiva-home-categories.png");
    await scroll(send, 1400);
    await shot(send, "05-original-shopeiva-home-mid-sections.png");
    await scroll(send, 2400);
    await shot(send, "06-original-shopeiva-home-product-rails.png");
    await scroll(send, 3600);
    await shot(send, "07-original-shopeiva-home-bottom.png");
  });
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, "http://127.0.0.1:3017/");
    await shot(send, "08-original-shopeiva-home-mobile-390x844.png", true);
  });
}

async function captureToobaBefore() {
  await withSession({ width: 1440, height: 2200, mobile: false }, async ({ send }) => {
    await navigate(send, "http://127.0.0.1:3000/");
    await shot(send, "09-tooba-home-before-full.png", true);
    await scroll(send, 900);
    await shot(send, "10-tooba-home-before-categories.png");
  });
}

async function captureToobaAfter() {
  await withSession({ width: 1440, height: 2200, mobile: false }, async ({ send }) => {
    await navigate(send, "http://127.0.0.1:3000/");
    await shot(send, "14-tooba-home-after-full.png", true);
    await scroll(send, 0);
    await shot(send, "15-tooba-home-after-top.png");
    await scroll(send, 700);
    await shot(send, "16-tooba-home-after-categories.png");
    await scroll(send, 1400);
    await shot(send, "17-tooba-home-after-mid.png");
    await scroll(send, 2400);
    await shot(send, "18-tooba-home-after-product-rails.png");
    await scroll(send, 3600);
    await shot(send, "19-tooba-home-after-bottom.png");
  });
  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigate(send, "http://127.0.0.1:3000/");
    await shot(send, "20-tooba-home-after-mobile-390x844.png", true);
  });
}

async function main() {
  if (mode === "shopeiva" || mode === "all") await captureShopeiva();
  if (mode === "before" || mode === "all") await captureToobaBefore();
  if (mode === "after") await captureToobaAfter();
  console.log("t018 capture done", mode);
}
main().catch((e) => { console.error(e); process.exit(1); });
