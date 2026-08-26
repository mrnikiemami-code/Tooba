import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T023");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9292;
const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "t023-edit-"));
const chromeProcess = spawn(
  chrome,
  [
    `--remote-debugging-port=${port}`,
    `--user-data-dir=${userData}`,
    "--headless=new",
    "--disable-gpu",
    "--window-size=1440,900",
    "about:blank",
  ],
  { stdio: "ignore" },
);

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => resolve(JSON.parse(data)));
    }).on("error", reject);
  });
}

let webSocketUrl = null;
for (let i = 0; i < 40; i += 1) {
  try {
    const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
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

await send("Page.enable");
await send("Runtime.enable");
await send("Emulation.setDeviceMetricsOverride", {
  width: 1440,
  height: 900,
  deviceScaleFactor: 1,
  mobile: false,
});
await send("Page.navigate", { url: "http://127.0.0.1:3000/vendor-panel/products" });
await delay(2500);
const offerHref = await send("Runtime.evaluate", {
  expression:
    "(() => { const a = [...document.querySelectorAll('a')].find((x) => /\\/vendor-panel\\/products\\//.test(x.getAttribute('href') || '')); return a ? a.href : null; })()",
  returnByValue: true,
});
const href = offerHref.result.value || "http://127.0.0.1:3000/vendor-panel/products";
await send("Page.navigate", { url: href });
await delay(2500);
const shot = await send("Page.captureScreenshot", {
  format: "png",
  fromSurface: true,
  captureBeyondViewport: true,
});
const file = path.join(outDir, "16-tooba-seller-product-edit-after.png");
fs.writeFileSync(file, Buffer.from(shot.data, "base64"));
console.log("wrote", file, "from", href);
socket.close();
chromeProcess.kill();
