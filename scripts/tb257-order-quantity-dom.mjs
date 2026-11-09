/**
 * TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257 — read-only operational order DOM proof.
 * Task-owned headless Chrome (fresh profile); no click, no mutation, no screenshot artifact.
 * Usage: node scripts/tb257-order-quantity-dom.mjs [label]
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const label = process.argv[2] || "snapshot";
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9761;
const route =
  process.env.TB257_ORDER_ROUTE || "http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf";
const userData = fs.mkdtempSync(path.join(os.tmpdir(), "tb257-order-"));
const chromeProcess = spawn(
  chrome,
  [
    `--remote-debugging-port=${port}`,
    `--user-data-dir=${userData}`,
    "--headless=new",
    "--disable-gpu",
    "--no-first-run",
    "--no-default-browser-check",
    "--window-size=1440,900",
    "about:blank",
  ],
  { stdio: "ignore" },
);

const getJson = (url) =>
  new Promise((resolve, reject) => {
    http
      .get(url, (response) => {
        let data = "";
        response.on("data", (chunk) => (data += chunk));
        response.on("end", () => {
          try {
            resolve(JSON.parse(data));
          } catch (error) {
            reject(error);
          }
        });
      })
      .on("error", reject);
  });

let wsUrl = null;
for (let i = 0; i < 60; i += 1) {
  try {
    const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
    const page = pages.find((item) => item.type === "page") || pages[0];
    if (page?.webSocketDebuggerUrl) {
      wsUrl = page.webSocketDebuggerUrl;
      break;
    }
  } catch {}
  await delay(250);
}
if (!wsUrl) {
  chromeProcess.kill();
  console.log(JSON.stringify({ ok: false, reason: "no_debug_target" }));
  process.exit(2);
}

const socket = new WebSocket(wsUrl);
await new Promise((resolve, reject) => {
  socket.addEventListener("open", resolve);
  socket.addEventListener("error", reject);
});

let nextId = 1;
const pending = new Map();
const failed = [];
socket.addEventListener("message", (event) => {
  const message = JSON.parse(String(event.data));
  if (message.method === "Network.responseReceived" && message.params.response.status >= 400) {
    failed.push({ status: message.params.response.status, url: message.params.response.url });
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
const evaluate = async (expression) => {
  const result = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
  if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
  return result.result.value;
};

await send("Page.enable");
await send("Runtime.enable");
await send("Network.enable");
await send("Page.navigate", { url: route });

const started = Date.now();
let ready = false;
while (Date.now() - started < 45000) {
  try {
    ready = await evaluate(
      "document.querySelectorAll('[data-testid^=\"admin-order-operational-group-\"]').length > 0",
    );
    if (ready) break;
  } catch {}
  await delay(400);
}
// The group rows and their parcels hydrate after the section shell; let them settle.
await delay(1500);

const facts = ready
  ? await evaluate(`(() => {
    const groups = Array.from(document.querySelectorAll('[data-testid^="admin-order-operational-group-"]'));
    return {
      groupCount: groups.length,
      groups: groups.map((group) => {
        const key = (group.getAttribute('data-testid') || '').replace('admin-order-operational-group-', '');
        const createEntries = Array.from(group.querySelectorAll('[data-testid^="admin-order-operational-create-"]'))
          .map((node) => node.getAttribute('data-testid'));
        const prepareEntries = Array.from(group.querySelectorAll('[data-testid^="admin-order-operational-prepare-"]'))
          .map((node) => node.getAttribute('data-testid'));
        const items = Array.from(group.querySelectorAll('[data-testid^="admin-order-operational-item-"]'))
          .map((node) => (node.innerText || '').replace(/\\s+/g, ' ').trim());
        const members = Array.from(group.querySelectorAll('[data-testid^="admin-order-operational-member-"]'))
          .map((node) => (node.innerText || '').replace(/\\s+/g, ' ').trim().slice(0, 200));
        const parcels = Array.from(group.querySelectorAll('[data-testid^="admin-order-operational-parcel-"]'))
          .filter((node) => /^admin-order-operational-parcel-[0-9a-f-]{36}$/.test(node.getAttribute('data-testid') || ''))
          .map((node) => (node.innerText || '').replace(/\\s+/g, ' ').trim().slice(0, 200));
        return { key, createEntries, prepareEntries, items, members, parcels };
      }),
      overflowX: document.documentElement.scrollWidth - window.innerWidth,
      dir: (document.querySelector('[data-testid="admin-order-operational-delivery"]') || document.body).getAttribute('dir'),
    };
  })()`)
  : null;

console.log(JSON.stringify({ label, route, ready, failedRequests: failed.slice(0, 10), facts }, null, 2));
socket.close();
await delay(150);
chromeProcess.kill();
try {
  fs.rmSync(userData, { recursive: true, force: true, maxRetries: 3 });
} catch {}
