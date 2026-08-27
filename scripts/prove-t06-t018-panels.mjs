/**
 * TB-P06-T018 — panel wave1 nav honesty + settings browser proof.
 */
import fs from "node:fs";
import path from "node:path";
import http from "node:http";
import { spawn } from "node:child_process";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T018");
const capDir = path.join(outDir, "captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const base = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
let port = Number(process.env.TOOBA_CDP_PORT || 9740);
fs.mkdirSync(capDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (res) => {
      let data = "";
      res.on("data", (c) => (data += c));
      res.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(e);
        }
      });
    }).on("error", reject);
  });
}

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t018-chrome-"));
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
  for (let i = 0; i < 40; i++) {
    try {
      const pages = await getJson(`http://127.0.0.1:${sessionPort}/json/list`);
      const page = pages.find((p) => p.type === "page") || pages[0];
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
  let id = 0;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const h = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) h.reject(new Error(JSON.stringify(message.error)));
      else h.resolve(message.result);
    }
  });
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const messageId = ++id;
      pending.set(messageId, { resolve, reject });
      socket.send(JSON.stringify({ id: messageId, method, params }));
    });
  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: viewport.width < 800,
    });
    return await work({ send });
  } finally {
    try {
      socket.close();
    } catch {}
    try {
      chromeProcess.kill();
    } catch {}
    await delay(200);
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {}
  }
}

async function evalValue(send, expression) {
  const result = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
  return result?.result?.value;
}

async function shot(send, name) {
  const s = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(capDir, name), Buffer.from(s.data, "base64"));
  return name;
}

const proof = { taskId: "TB-P06-T018", recordedAtUtc: new Date().toISOString(), checks: {}, browser: {}, captures: [] };

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await send("Page.navigate", { url: `${base}/customer-panel` });
  await delay(3500);
  proof.browser.customerNavLabels = await evalValue(
    send,
    `[...document.querySelectorAll('[data-testid^="customer-nav-"]')].map(el => el.getAttribute('data-testid') + ':' + el.getAttribute('data-live'))`,
  );
  proof.browser.customerHasWalletNav = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="customer-nav-wallet"]')`),
  );
  proof.browser.customerHasSettingsNav = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="customer-nav-settings"]')`),
  );
  proof.captures.push(await shot(send, "01-customer-dashboard-live-nav.png"));

  await send("Page.navigate", { url: `${base}/customer-panel/settings` });
  await delay(2500);
  proof.browser.customerSettingsLocale = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="customer-settings-locale"]')`),
  );
  proof.captures.push(await shot(send, "02-customer-settings-locale.png"));

  await send("Page.navigate", { url: `${base}/vendor-panel` });
  await delay(4000);
  proof.browser.vendorHasCustomersNav = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="vendor-nav-customers"]')`),
  );
  proof.browser.vendorHasSettingsNav = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="vendor-nav-settings"]')`),
  );
  proof.captures.push(await shot(send, "03-vendor-dashboard-live-nav.png"));

  await send("Page.navigate", { url: `${base}/vendor-panel/settings` });
  await delay(3500);
  proof.browser.vendorSettingsPage = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="vendor-settings-page"]')`),
  );
  proof.captures.push(await shot(send, "04-vendor-settings-live.png"));

  await send("Page.navigate", { url: `${base}/admin` });
  await delay(3500);
  proof.browser.adminHasSettingsNav = Boolean(
    await evalValue(send, `!!document.querySelector('[data-testid="admin-nav-settings"]')`),
  );
  proof.captures.push(await shot(send, "05-admin-dashboard-no-settings-nav.png"));
});

proof.checks.customerNoWalletNav = proof.browser.customerHasWalletNav === false;
proof.checks.customerSettingsNav = proof.browser.customerHasSettingsNav === true;
proof.checks.customerLocaleSection = proof.browser.customerSettingsLocale === true;
proof.checks.vendorNoCustomersNav = proof.browser.vendorHasCustomersNav === false;
proof.checks.vendorSettingsNav = proof.browser.vendorHasSettingsNav === true;
proof.checks.vendorSettingsLive = proof.browser.vendorSettingsPage === true;
proof.checks.adminNoSettingsNav = proof.browser.adminHasSettingsNav === false;
proof.pass = Object.values(proof.checks).every(Boolean);

fs.writeFileSync(path.join(outDir, "_acceptance-proof.json"), JSON.stringify(proof, null, 2));
console.log(JSON.stringify({ pass: proof.pass, checks: proof.checks, browser: proof.browser, captures: proof.captures }, null, 2));
process.exit(proof.pass ? 0 : 2);
