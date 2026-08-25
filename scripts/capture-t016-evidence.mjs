import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T016");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9243;
const host = "http://127.0.0.1:5088";
const web = "http://127.0.0.1:3000";

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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t016-chrome-"));
  const chromeProcess = spawn(
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

  let webSocketUrl = null;
  for (let attempt = 0; attempt < 40; attempt += 1) {
    try {
      const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // Chrome not ready.
    }
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("Chrome CDP not ready");
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
      // best-effort cleanup
    }
  }
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function openMegaMenu(send) {
  await send("Page.navigate", { url: web });
  await delay(2500);
  const click = await send("Runtime.evaluate", {
    expression: `(() => {
      const btn = document.querySelector('button[aria-controls="storefront-mega-menu"]')
        || [...document.querySelectorAll('button')].find((b) => /دسته/.test(b.innerText || ''));
      if (!btn) return false;
      btn.click();
      return true;
    })()`,
    returnByValue: true,
  });
  if (!click.result.value) throw new Error("mega menu trigger missing");
  await delay(500);
}

async function hoverCategoryByName(send, name) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const needle = ${JSON.stringify(name)};
      const btn = [...document.querySelectorAll('#storefront-mega-menu button')].find((b) => (b.innerText || '').includes(needle));
      if (!btn) return false;
      btn.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }));
      btn.click();
      return true;
    })()`,
    returnByValue: true,
  });
  if (!result.result.value) throw new Error(`category rail missing: ${name}`);
  await delay(600);
}

async function hoverCategoryByIndex(send, index) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const grid = document.querySelector('#storefront-mega-menu .grid-cols-12');
      const firstCol = grid?.children?.[0];
      const buttons = firstCol ? [...firstCol.querySelectorAll('button')] : [];
      const btn = buttons[${index}];
      if (!btn) return buttons.length;
      btn.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }));
      btn.click();
      return buttons.length;
    })()`,
    returnByValue: true,
  });
  const count = result.result.value ?? 0;
  if (count <= index) {
    console.warn("category rail index unavailable", index, "count", count);
  }
  await delay(600);
}

const categories = await fetch(`${host}/v1/storefront/categories`).then((r) => r.json());
const roots = categories.filter((item) => !item.parentCategoryId && !item.ParentCategoryId);
const rootIds = new Set(roots.map((item) => item.categoryId || item.CategoryId));
const secondLevel = categories.filter((item) => {
  const parent = item.parentCategoryId || item.ParentCategoryId;
  return parent && rootIds.has(parent);
});
const secondIds = new Set(secondLevel.map((item) => item.categoryId || item.CategoryId));
const thirdLevel = categories.filter((item) => {
  const parent = item.parentCategoryId || item.ParentCategoryId;
  return parent && secondIds.has(parent);
});

await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
  await openMegaMenu(send);
  await screenshot(send, "06-tooba-after-level3-1440x900.png");

  await hoverCategoryByIndex(send, 1);
  await screenshot(send, "07-tooba-level1-switching.png");

  await hoverCategoryByIndex(send, 0);
  await screenshot(send, "08-tooba-level2-level3-structure.png");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await send("Page.navigate", { url: web });
  await delay(2000);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const menu = document.querySelector('button[aria-label="منوی موبایل"]');
      menu?.click();
      return !!menu;
    })()`,
    returnByValue: true,
  });
  await delay(600);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const btn = [...document.querySelectorAll('button')].find((b) => /دسته/.test(b.innerText || ''));
      btn?.click();
      return !!btn;
    })()`,
    returnByValue: true,
  });
  await delay(500);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const root = [...document.querySelectorAll('button')].find((b) => (b.innerText || '').includes('محصولات دیجیتال'));
      root?.click();
      return !!root;
    })()`,
    returnByValue: true,
  });
  await delay(500);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const child = [...document.querySelectorAll('button')].find((b) => (b.innerText || '').includes('گوشی موبایل'));
      child?.click();
      return !!child;
    })()`,
    returnByValue: true,
  });
  await delay(700);
  await screenshot(send, "10-tooba-mobile-level3-390x844.png");
});

const sampleThird = thirdLevel[0];
const sampleLink = sampleThird
  ? `/products?categoryId=${sampleThird.categoryId || sampleThird.CategoryId}`
  : null;
const probe = {
  rootCount: roots.length,
  secondLevelCount: secondLevel.length,
  thirdLevelCount: thirdLevel.length,
  sampleThirdLevelName: sampleThird?.name || sampleThird?.Name || null,
  sampleThirdLevelLink: sampleLink,
  containsPrice: JSON.stringify(categories).toLowerCase().includes("price"),
  containsStock: JSON.stringify(categories).toLowerCase().includes("stock"),
};
fs.writeFileSync(path.join(outDir, "_api-probe.json"), JSON.stringify(probe, null, 2));
console.log("capture complete", probe);
