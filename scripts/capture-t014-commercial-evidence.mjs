/**
 * TB-P06-T014 side-by-side captures (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T014/captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
let port = Number(process.env.TOOBA_CDP_PORT || 9700);
fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try { resolve(JSON.parse(data)); } catch (error) { reject(error); }
      });
    }).on("error", reject);
  });
}

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t014-chrome-"));
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
  await new Promise((resolve, reject) => { socket.addEventListener("open", resolve); socket.addEventListener("error", reject); });
  let id = 0;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(event.data);
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) reject(new Error(JSON.stringify(message.error))); else resolve(message.result);
    }
  });
  const send = (method, params = {}) => new Promise((resolve, reject) => {
    const messageId = ++id;
    pending.set(messageId, { resolve, reject });
    socket.send(JSON.stringify({ id: messageId, method, params }));
  });
  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Network.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width, height: viewport.height, deviceScaleFactor: 1, mobile: !!viewport.mobile,
    });
    return await work({ send });
  } finally {
    try { socket.close(); } catch {}
    try { chromeProcess.kill(); } catch {}
    await delay(400);
    try { fs.rmSync(userData, { recursive: true, force: true }); } catch {}
  }
}

async function capture(send, file, url, cookies = []) {
  if (cookies.length) {
    await send("Network.setCookies", { cookies });
  }
  await send("Page.navigate", { url });
  await delay(4500);
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(outDir, file), Buffer.from(shot.data, "base64"));
  console.log(`wrote ${file}`);
}

const faCookie = [{ name: "tooba_locale", value: "fa", domain: "127.0.0.1", path: "/" }];
const enCookie = [{ name: "tooba_locale", value: "en", domain: "127.0.0.1", path: "/" }];

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "01-tooba-home-desktop-fa.png", `${tooba}/`, faCookie);
  await capture(send, "02-tooba-home-desktop-en.png", `${tooba}/`, enCookie);
  await capture(send, "03-tooba-blogs-desktop.png", `${tooba}/blogs`, faCookie);
  await capture(send, "04-tooba-customer-dashboard.png", `${tooba}/customer-panel`, faCookie);
  await capture(send, "05-tooba-customer-settings.png", `${tooba}/customer-panel/settings`, faCookie);
  await capture(send, "06-tooba-vendor-dashboard.png", `${tooba}/vendor-panel`, faCookie);
  await capture(send, "07-tooba-vendor-analytics.png", `${tooba}/vendor-panel/analytics`, faCookie);
  await capture(send, "08-tooba-admin-dashboard.png", `${tooba}/admin`, faCookie);
  await capture(send, "09-shopeiva-home-desktop.png", `${shopeiva}/`);
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await capture(send, "10-tooba-home-mobile-fa.png", `${tooba}/`, faCookie);
  await capture(send, "11-tooba-customer-settings-mobile.png", `${tooba}/customer-panel/settings`, faCookie);
});

console.log(`captures written to ${outDir}`);
