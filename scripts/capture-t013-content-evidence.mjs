/**
 * TB-P06-T013 — blog/content side-by-side captures (Chrome CDP).
 */
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T013/captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
const slug = process.env.TOOBA_ARTICLE_SLUG || "guide-online-shopping";
let port = Number(process.env.TOOBA_CDP_PORT || 9600);

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t013-chrome-"));
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

async function capture(send, file, url, scrollY = 0) {
  await send("Page.navigate", { url });
  await delay(2800);
  if (scrollY > 0) {
    await send("Runtime.evaluate", { expression: `window.scrollTo(0, ${scrollY})` });
    await delay(900);
  }
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(outDir, file), Buffer.from(shot.data, "base64"));
  console.log(`wrote ${file}`);
}

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await capture(send, "01-shopeiva-blogs-list-desktop.png", `${shopeiva}/blogs`);
  await capture(send, "02-tooba-blogs-list-desktop.png", `${tooba}/blogs`);
  await capture(send, "03-shopeiva-blog-detail-desktop.png", `${shopeiva}/blogs/${slug}`);
  await capture(send, "04-tooba-blog-detail-desktop.png", `${tooba}/blogs/${slug}`);
  await capture(send, "05-tooba-admin-content-desktop.png", `${tooba}/admin/content`);
  await capture(send, "06-tooba-home-articles-desktop.png", `${tooba}/`, 4210);
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await capture(send, "07-tooba-blogs-list-mobile.png", `${tooba}/blogs`);
  await capture(send, "08-tooba-blog-detail-mobile.png", `${tooba}/blogs/${slug}`);
});

console.log(`captures written to ${outDir}`);
