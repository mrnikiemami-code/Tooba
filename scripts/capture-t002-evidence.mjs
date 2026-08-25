import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T002");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9229;
fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (res) => {
      let data = "";
      res.on("data", (chunk) => (data += chunk));
      res.on("end", () => {
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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-chrome-"));
  const proc = spawn(
    chrome,
    [
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${userData}`,
      "--headless=new",
      "--disable-gpu",
      "--hide-scrollbars",
      `--window-size=${viewport.width},${viewport.height}`,
      "about:blank",
    ],
    { stdio: "ignore" },
  );

  let wsUrl = null;
  for (let i = 0; i < 40; i += 1) {
    try {
      const list = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = list.find((item) => item.type === "page") || list[0];
      if (page?.webSocketDebuggerUrl) {
        wsUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // waiting for chrome
    }
    await delay(250);
  }
  if (!wsUrl) {
    proc.kill();
    throw new Error("Chrome CDP not ready");
  }

  const ws = new WebSocket(wsUrl);
  await new Promise((resolve, reject) => {
    ws.addEventListener("open", resolve);
    ws.addEventListener("error", reject);
  });

  let nextId = 1;
  const pending = new Map();
  ws.addEventListener("message", (event) => {
    const msg = JSON.parse(String(event.data));
    if (msg.id && pending.has(msg.id)) {
      const { resolve, reject } = pending.get(msg.id);
      pending.delete(msg.id);
      if (msg.error) reject(new Error(JSON.stringify(msg.error)));
      else resolve(msg.result);
    }
  });

  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      ws.send(JSON.stringify({ id, method, params }));
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
    ws.close();
    proc.kill();
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {
      // ignore cleanup
    }
  }
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
  await send("Page.navigate", { url: "http://localhost:3000/" });
  await delay(1500);
  await screenshot(send, "01-home-desktop.png");

  await send("Runtime.evaluate", {
    expression: `(() => {
      const btn = [...document.querySelectorAll('button')].find((el) => el.textContent?.includes('دسته‌بندی‌ها'));
      btn?.click();
      return !!document.getElementById('storefront-mega-menu');
    })()`,
  });
  await delay(500);
  await screenshot(send, "03-mega-menu.png");

  await send("Runtime.evaluate", {
    expression: `(() => {
      const btn = [...document.querySelectorAll('button')].find((el) => el.textContent?.includes('دسته‌بندی‌ها'));
      if (document.getElementById('storefront-mega-menu')) btn?.click();
      return !document.getElementById('storefront-mega-menu');
    })()`,
  });
  await delay(300);
  const sections = [
    ["home-special-offers", "04-special-offers.png"],
    ["home-sale", "05-sale-section.png"],
    ["home-new-arrivals", "06-new-arrivals.png"],
    ["home-product-rail", "07-product-rail.png"],
  ];
  for (const [id, name] of sections) {
    await send("Runtime.evaluate", {
      expression: `document.getElementById('${id}')?.scrollIntoView({block:'center'}); true`,
    });
    await delay(350);
    await screenshot(send, name);
  }

  await send("Runtime.evaluate", {
    expression: `document.getElementById('home-categories-heading')?.scrollIntoView({block:'center'}); true`,
  });
  await delay(350);
  await screenshot(send, "08-category-section.png");

  await send("Runtime.evaluate", {
    expression: `window.scrollTo(0, document.body.scrollHeight); true`,
  });
  await delay(400);
  await screenshot(send, "09-footer-trust.png");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await send("Page.navigate", { url: "http://localhost:3000/" });
  await delay(1200);
  await screenshot(send, "02-home-mobile-390x844.png");
});
