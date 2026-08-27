/**
 * TB-P06-T024-R2 — Access Control browser proof (Chrome CDP headless).
 * Resolves demo IDs from Host, injects dev actor localStorage, captures ACC + orders + Shopeiva reference.
 */
import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const evidenceDir = path.resolve("docs/evidence/TB-P06-T024-R2");
const outDir = path.join(evidenceDir, "captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const host = (process.env.TOOBA_HOST || "http://127.0.0.1:5088").replace(/\/$/, "");
const tooba = (process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000").replace(/\/$/, "");
const shopeiva = (process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001").replace(/\/$/, "");
let port = Number(process.env.TOOBA_CDP_PORT || 9724);

fs.mkdirSync(outDir, { recursive: true });

const runtimeErrors = [];

function pick(obj, ...keys) {
  for (const key of keys) {
    if (obj && obj[key] != null) return obj[key];
  }
  return null;
}

async function hostJson(pathname) {
  const response = await fetch(`${host}${pathname}`, { headers: { Accept: "application/json" } });
  const text = await response.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }
  if (!response.ok) {
    throw new Error(`${response.status} ${pathname} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  }
  return body;
}

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

async function resolvePreviewContext() {
  const adminCtx = await hostJson("/v1/admin/dev-context");
  const demo = await hostJson("/v1/admin/access-control/demo-preview");
  const devContexts = await hostJson("/v1/seller/dev-contexts");

  const adminActorUserId = String(
    pick(adminCtx, "actorUserId", "ActorUserId")
      ?? pick(demo, "platformAdminActorId", "PlatformAdminActorId")
      ?? "",
  );
  const sellerPartyId = String(pick(demo, "sellerPartyId", "SellerPartyId") ?? "");
  const sellerOwnerActorUserId = String(
    pick(demo, "sellerOwnerActorId", "SellerOwnerActorId") ?? "",
  );
  const employeeActorUserId = String(pick(demo, "employeeActorId", "EmployeeActorId") ?? "");
  const mobileSellerOrderId = String(
    pick(demo, "mobileSellerOrderId", "MobileSellerOrderId") ?? "",
  );

  const contextList = Array.isArray(devContexts?.actors)
    ? devContexts.actors
    : Array.isArray(devContexts)
      ? devContexts
      : devContexts?.contexts ?? [];

  const ownerContext = contextList.find(
    (row) =>
      pick(row, "sellerPartyId", "SellerPartyId") === sellerPartyId
      && pick(row, "actorUserId", "ActorUserId") === sellerOwnerActorUserId,
  );
  const employeeContext = contextList.find(
    (row) =>
      pick(row, "sellerPartyId", "SellerPartyId") === sellerPartyId
      && pick(row, "actorUserId", "ActorUserId") === employeeActorUserId,
  );

  const missing = [];
  if (!adminActorUserId) missing.push("adminActorUserId");
  if (!sellerPartyId) missing.push("sellerPartyId");
  if (!sellerOwnerActorUserId) missing.push("sellerOwnerActorUserId");
  if (!employeeActorUserId) missing.push("employeeActorUserId");
  if (!mobileSellerOrderId) missing.push("mobileSellerOrderId");
  if (missing.length) {
    throw new Error(`demo preview incomplete: ${missing.join(", ")}`);
  }

  return {
    adminActorUserId,
    sellerPartyId,
    sellerOwnerActorUserId,
    employeeActorUserId,
    mobileSellerOrderId,
    sellerOwnerLabel: pick(ownerContext, "actorLabel", "ActorLabel")
      ?? pick(demo, "sellerOwnerLabel", "SellerOwnerLabel")
      ?? "Seller Owner",
    employeeLabel: pick(employeeContext, "actorLabel", "ActorLabel")
      ?? pick(demo, "employeeLabel", "EmployeeLabel")
      ?? "Seller Employee",
    sellerDisplayName: pick(demo, "sellerDisplayName", "SellerDisplayName") ?? "",
    mobileOrderNumber: pick(demo, "mobileOrderNumber", "MobileOrderNumber") ?? "",
    devContextCount: contextList.length,
  };
}

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t024-r2-chrome-"));
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
    if (message.method === "Runtime.exceptionThrown") {
      runtimeErrors.push({
        text: message.params?.exceptionDetails?.text || "exception",
        url: message.params?.exceptionDetails?.url,
      });
    }
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

async function waitForReady(send) {
  for (let i = 0; i < 80; i += 1) {
    await delay(250);
    const result = await send("Runtime.evaluate", {
      expression: "document.readyState",
      returnByValue: true,
    });
    if (result.result.value === "complete") break;
  }
  // Wait out Admin/Vendor shell bootstrap ("آماده‌سازی …") until panel chrome appears.
  for (let i = 0; i < 60; i += 1) {
    await delay(500);
    const probe = await send("Runtime.evaluate", {
      expression: `(() => {
        const t = document.body?.innerText || '';
        const loading = t.includes('آماده‌سازی');
        const readyHint =
          t.includes('کنترل دسترسی') ||
          t.includes('سفارش') ||
          t.includes('نقش') ||
          t.includes('داشبورد') ||
          !!document.querySelector('[data-testid="access-control-center"],[data-testid="admin-panel-nav-live-only"],nav');
        return { loading, readyHint, len: t.length };
      })()`,
      returnByValue: true,
    });
    const v = probe.result?.value;
    if (v && !v.loading && (v.readyHint || v.len > 80)) break;
  }
  await delay(1200);
}

async function injectLocalStorage(send, keys) {
  const entries = Object.entries(keys).filter(([, value]) => value);
  if (!entries.length) return;
  const lines = entries.map(
    ([key, value]) => `localStorage.setItem(${JSON.stringify(key)}, ${JSON.stringify(String(value))});`,
  );
  await send("Runtime.evaluate", {
    expression: `(() => { ${lines.join(" ")} return true; })()`,
    returnByValue: true,
  });
}

async function navigateTooba(send, url, storageKeys = {}) {
  await send("Page.navigate", { url: `${tooba}/` });
  await waitForReady(send);
  await injectLocalStorage(send, storageKeys);
  await send("Page.navigate", { url });
  await waitForReady(send);
  // Force one more settle after client effects (ACC caps / seller contexts).
  await delay(4000);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const loading = (document.body?.innerText || '').includes('آماده‌سازی');
      if (loading) {
        // kick a soft reload once storage is present
        location.reload();
      }
      return loading;
    })()`,
    returnByValue: true,
  });
  await waitForReady(send);
  await delay(3000);
}

async function navigateRaw(send, url) {
  await send("Page.navigate", { url });
  await waitForReady(send);
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
  const ctx = await resolvePreviewContext();
  const adminStorage = { "tooba.adminActorUserId": ctx.adminActorUserId };
  const ownerStorage = {
    "tooba.sellerActorUserId": ctx.sellerOwnerActorUserId,
    "tooba.sellerPartyId": ctx.sellerPartyId,
  };
  const employeeStorage = {
    "tooba.sellerActorUserId": ctx.employeeActorUserId,
    "tooba.sellerPartyId": ctx.sellerPartyId,
  };

  const manifest = {
    taskId: "TB-P06-T024-R2",
    host,
    tooba,
    shopeiva,
    capturedAt: new Date().toISOString(),
    context: ctx,
    captures: [],
  };

  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigateTooba(send, `${tooba}/admin/access-control`, adminStorage);
    await shot(send, "01-admin-access-control.png");
    manifest.captures.push({
      file: "01-admin-access-control.png",
      viewport: "1440x900",
      url: `${tooba}/admin/access-control`,
      actor: "Platform Admin",
      storage: adminStorage,
    });

    await navigateTooba(
      send,
      `${tooba}/admin/sellers/${ctx.sellerPartyId}/access-control`,
      adminStorage,
    );
    await shot(send, "02-admin-seller-access-control.png");
    manifest.captures.push({
      file: "02-admin-seller-access-control.png",
      viewport: "1440x900",
      url: `${tooba}/admin/sellers/${ctx.sellerPartyId}/access-control`,
      actor: "Platform Admin",
      storage: adminStorage,
    });

    await navigateTooba(send, `${tooba}/vendor-panel/access-control`, ownerStorage);
    await shot(send, "03-seller-access-control-desktop.png");
    manifest.captures.push({
      file: "03-seller-access-control-desktop.png",
      viewport: "1440x900",
      url: `${tooba}/vendor-panel/access-control`,
      actor: ctx.sellerOwnerLabel,
      storage: ownerStorage,
    });

    await navigateTooba(send, `${tooba}/vendor-panel/orders`, ownerStorage);
    await shot(send, "05-seller-orders-owner.png");
    manifest.captures.push({
      file: "05-seller-orders-owner.png",
      viewport: "1440x900",
      url: `${tooba}/vendor-panel/orders`,
      actor: ctx.sellerOwnerLabel,
      storage: ownerStorage,
    });

    await navigateTooba(send, `${tooba}/vendor-panel/orders`, employeeStorage);
    await shot(send, "06-seller-orders-employee.png");
    manifest.captures.push({
      file: "06-seller-orders-employee.png",
      viewport: "1440x900",
      url: `${tooba}/vendor-panel/orders`,
      actor: ctx.employeeLabel,
      storage: employeeStorage,
      note: "Scoped employee — Mobile visible, Books absent",
    });

    await navigateTooba(
      send,
      `${tooba}/vendor-panel/orders/${ctx.mobileSellerOrderId}`,
      employeeStorage,
    );
    await shot(send, "07-seller-order-mobile-detail.png");
    manifest.captures.push({
      file: "07-seller-order-mobile-detail.png",
      viewport: "1440x900",
      url: `${tooba}/vendor-panel/orders/${ctx.mobileSellerOrderId}`,
      actor: ctx.employeeLabel,
      storage: employeeStorage,
      orderNumber: ctx.mobileOrderNumber,
    });
  });

  await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
    await navigateTooba(send, `${tooba}/vendor-panel/access-control`, ownerStorage);
    await shot(send, "04-seller-access-control-mobile.png");
    manifest.captures.push({
      file: "04-seller-access-control-mobile.png",
      viewport: "390x844",
      url: `${tooba}/vendor-panel/access-control`,
      actor: ctx.sellerOwnerLabel,
      storage: ownerStorage,
    });
  });

  await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
    await navigateRaw(send, `${shopeiva}/vendor-panel/settings`);
    await shot(send, "08-shopeiva-vendor-settings.png");
    manifest.captures.push({
      file: "08-shopeiva-vendor-settings.png",
      viewport: "1440x900",
      url: `${shopeiva}/vendor-panel/settings`,
      actor: "Shopeiva reference (no Tooba storage)",
    });
  });

  fs.writeFileSync(path.join(evidenceDir, "browser-proof.json"), JSON.stringify(manifest, null, 2), "utf8");
  fs.writeFileSync(
    path.join(evidenceDir, "browser-runtime-errors.json"),
    JSON.stringify({ runtimeErrors }, null, 2),
    "utf8",
  );

  console.log("context", ctx);
  console.log("runtimeErrors", runtimeErrors.length);
  console.log("captures written to", outDir);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
