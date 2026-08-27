/**
 * TB-P06-T012 — browser side-by-side settlement captures (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T012/captures");
const motionPath = path.resolve("docs/evidence/TB-P06-T012/motion-proof.json");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
const sellerParty = process.env.TOOBA_SELLER_PARTY || "01a030d1-40cb-7000-8abe-6d31739956c5";
const sellerActor = process.env.TOOBA_SELLER_ACTOR || "01a03628-3f68-7000-844d-99f1cadb54b0";
let port = Number(process.env.TOOBA_CDP_PORT || 9500);

fs.mkdirSync(outDir, { recursive: true });
const motion = { capturedAt: new Date().toISOString(), interactions: [] };

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t012-chrome-"));
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
    await send("Emulation.setDeviceMetricsOverride", { width: viewport.width, height: viewport.height, deviceScaleFactor: 1, mobile: viewport.mobile || false });
    return await work({ send });
  } finally {
    try { socket.close(); } catch {}
    try { chromeProcess.kill(); } catch {}
    await delay(500);
    try { fs.rmSync(userData, { recursive: true, force: true }); } catch {}
  }
}

async function capture(send, file, url, label) {
  await send("Page.navigate", { url });
  await delay(2500);
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(outDir, file), Buffer.from(shot.data, "base64"));
  motion.interactions.push({ label, url, file, viewport: label.includes("mobile") ? "mobile" : "desktop" });
}

const sellerWallet = `${tooba}/vendor-panel/wallet?sellerPartyId=${sellerParty}&actorUserId=${sellerActor}`;
const adminSettlement = `${tooba}/admin/settlement`;
const adminPayouts = `${tooba}/admin/payouts`;
const shopeivaWallet = `${shopeiva}/vendor-panel/wallet`;

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "01-shopeiva-vendor-wallet-desktop.png", shopeivaWallet, "shopeiva-wallet-desktop");
  await capture(send, "02-tooba-vendor-wallet-desktop.png", sellerWallet, "tooba-wallet-desktop");
  await capture(send, "03-tooba-admin-settlement-desktop.png", adminSettlement, "tooba-admin-settlement-desktop");
  await capture(send, "04-tooba-admin-payouts-desktop.png", adminPayouts, "tooba-admin-payouts-desktop");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await capture(send, "05-tooba-vendor-wallet-mobile.png", sellerWallet, "tooba-wallet-mobile");
  await capture(send, "06-tooba-admin-settlement-mobile.png", adminSettlement, "tooba-admin-settlement-mobile");
});

fs.writeFileSync(motionPath, JSON.stringify(motion, null, 2));
console.log(`captures written to ${outDir}`);
