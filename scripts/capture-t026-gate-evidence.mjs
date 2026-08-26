import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T026");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const pdp = process.env.TOOBA_PDP_SLUG || "demo-game-2";
let port = Number(process.env.TOOBA_CDP_PORT || 9320);
fs.mkdirSync(outDir, { recursive: true });
const runtimeErrors = [];

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => resolve(JSON.parse(data)));
    }).on("error", reject);
  });
}

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t026-chrome-"));
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
    if (message.method === "Log.entryAdded" && message.params?.entry?.level === "error") {
      runtimeErrors.push({ text: message.params.entry.text, url: message.params.entry.url });
    }
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
    await send("Log.enable");
    await send("Network.enable");
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
  for (let i = 0; i < 80; i += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: "document.readyState",
      returnByValue: true,
    });
    if (result.result.value === "complete") break;
  }
  await delay(1600);
}

async function shot(send, name) {
  const result = await send("Page.captureScreenshot", {
    format: "png",
    fromSurface: true,
    captureBeyondViewport: true,
  });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", name, fs.statSync(file).size);
}

async function main() {
  const e2e = JSON.parse(fs.readFileSync(path.join(outDir, "03-commerce-e2e-result.json"), "utf8"));
  await withSession({ width: 1440, height: 900 }, async ({ send }) => {
    await navigate(send, `${tooba}/`);
    await shot(send, "12-home-final-gate.png");
    await navigate(send, `${tooba}/products/${pdp}`);
    await shot(send, "13-pdp-final-gate.png");
    await navigate(send, `${tooba}/products`);
    await shot(send, "14-listing-final-gate.png");
    await navigate(send, `${tooba}/cart`);
    await shot(send, "04-commerce-e2e-cart.png");
    await navigate(send, `${tooba}/checkout`);
    await shot(send, "05-commerce-e2e-checkout.png");
    if (e2e.checkoutId) {
      await navigate(send, `${tooba}/order/confirmation?checkoutId=${e2e.checkoutId}`);
      await shot(send, "06-commerce-e2e-confirmation.png");
    }
    await navigate(send, `${tooba}/customer-panel`);
    await shot(send, "15-customer-final-gate.png");
    await navigate(send, `${tooba}/vendor-panel`);
    await shot(send, "16-seller-final-gate.png");
    await navigate(send, `${tooba}/admin`);
    await shot(send, "17-admin-final-gate.png");
    // favicon probe
    const fav = await send("Runtime.evaluate", {
      expression: `fetch('/favicon.ico',{method:'HEAD'}).then(r=>r.status).catch(()=>-1)`,
      awaitPromise: true,
      returnByValue: true,
    });
    console.log("faviconStatus", fav.result?.value);
  });
  fs.writeFileSync(path.join(outDir, "19-browser-network-errors.json"), JSON.stringify({ runtimeErrors }, null, 2));
  console.log("errors", runtimeErrors.length);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
