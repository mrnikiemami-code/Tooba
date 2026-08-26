import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T024");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const tooba = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
let port = Number(process.env.TOOBA_CDP_PORT || 9300);
fs.mkdirSync(outDir, { recursive: true });

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t024-chrome-"));
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
  const phase = process.argv[2] || "all";
  if (phase === "before" || phase === "all") {
    await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
      await navigate(send, `${tooba}/admin`);
      await shot(send, "04-admin-before-dashboard.png");
      await navigate(send, `${tooba}/admin/products`);
      await shot(send, "05-admin-before-catalog.png");
      await navigate(send, `${tooba}/admin/orders`);
      await shot(send, "06-admin-before-orders.png");
    });
    await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
      await navigate(send, `${tooba}/admin`);
      await shot(send, "07-admin-before-mobile.png");
    });
  }
  if (phase === "after" || phase === "all") {
    await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
      await navigate(send, `${tooba}/admin`);
      await shot(send, "08-admin-after-dashboard.png");
      await navigate(send, `${tooba}/admin/products`);
      await shot(send, "09-admin-after-catalog.png");
      const href = await send("Runtime.evaluate", {
        expression:
          "(() => { const a=[...document.querySelectorAll('a')].find((x)=>/\\/admin\\/products\\//.test(x.getAttribute('href')||'')); return a?a.href:null; })()",
        returnByValue: true,
      });
      await navigate(send, href.result.value || `${tooba}/admin/products`);
      await shot(send, "10-admin-after-product-workspace.png");
      await navigate(send, `${tooba}/admin/orders`);
      await shot(send, "11-admin-after-orders.png");
      await navigate(send, `${tooba}/admin/reviews`);
      await shot(send, "12-admin-after-moderation.png");
    });
    await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
      await navigate(send, `${tooba}/admin`);
      await shot(send, "13-admin-after-mobile-390x844.png");
    });
  }
  console.log("t024 captures done:", phase);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
