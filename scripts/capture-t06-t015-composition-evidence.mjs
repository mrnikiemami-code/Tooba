/**
 * TB-P06-T015 composition visual + admin captures (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T015/captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
const actor = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
let port = Number(process.env.TOOBA_CDP_PORT || 9715);
fs.mkdirSync(outDir, { recursive: true });

async function hostFetch(pathname, { method = "GET", body } = {}) {
  const headers = { Accept: "application/json", "X-Tooba-Dev-Actor-User-Id": actor };
  if (body) headers["Content-Type"] = "application/json";
  const response = await fetch(`${host}${pathname}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  return { status: response.status, json: await response.json() };
}

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t015-chrome-"));
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

async function capture(send, file, url, cookies = [], actorId = null) {
  if (cookies.length) await send("Network.setCookies", { cookies });
  await send("Page.navigate", { url: `${tooba}/` });
  await delay(300);
  if (actorId) {
    await send("Runtime.evaluate", {
      expression: `localStorage.setItem('tooba.adminActorUserId', ${JSON.stringify(actorId)}); true`,
      returnByValue: true,
    });
  }
  await send("Page.navigate", { url });
  await delay(4500);
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(outDir, file), Buffer.from(shot.data, "base64"));
  console.log(`wrote ${file}`);
}

await hostFetch("/v1/admin/page-composition/home/restore-default", { method: "POST" });

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "01-tooba-home-default-desktop.png", `${tooba}/`);
  await capture(send, "02-shopeiva-home-desktop.png", `${shopeiva}/`);
  await capture(send, "03-admin-page-composition-desktop.png", `${tooba}/admin/page-composition`, [], actor);
});

const adminHome = await hostFetch("/v1/admin/page-composition/home");
const ids = adminHome.json?.sections?.map((s) => s.pageSectionId) ?? [];
if (ids.length >= 2) {
  const swapped = ids.slice();
  [swapped[0], swapped[1]] = [swapped[1], swapped[0]];
  await hostFetch("/v1/admin/page-composition/home/reorder", { method: "PUT", body: { sectionIds: swapped } });
}

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "04-tooba-home-reordered-desktop.png", `${tooba}/`);
});

const brands = adminHome.json?.sections?.find((s) => s.sectionType === "brands");
if (brands) {
  await hostFetch(`/v1/admin/page-composition/home/sections/${brands.pageSectionId}`, {
    method: "PUT",
    body: { isVisible: false },
  });
}

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "05-tooba-home-brands-hidden-desktop.png", `${tooba}/`);
  await capture(send, "06-admin-page-composition-hidden-state.png", `${tooba}/admin/page-composition`, [], actor);
});

await hostFetch("/v1/admin/page-composition/home/restore-default", { method: "POST" });

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await capture(send, "07-tooba-home-default-mobile.png", `${tooba}/`);
  await capture(send, "08-admin-page-composition-mobile.png", `${tooba}/admin/page-composition`, [], actor);
});

console.log(`captures written to ${outDir}`);
