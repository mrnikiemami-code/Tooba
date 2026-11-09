/**
 * TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2 — authorized SAVE proof.
 *
 * Task-owned headless Chrome (fresh temporary profile, no user cookies/session) renders the real
 * authoring route of the EXISTING task-owned draft, makes ONE reversible carrier-calendar edit in the
 * real UI and presses the real Save button. It reports the actual request/response facts, the
 * post-save document and the reloaded values. No screenshot artifact, no user tab, no publication.
 *
 * Env:
 *   TB256R2_PHASE = before | after        (label only, written into the report)
 *   TB256R2_TARGET = "<HH:mm>"            new closing time for carrier weekday 3 (default 14:00)
 *   TB256R2_OUT = path of the JSON report
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = Number(process.env.TB256R2_PORT || 9752);
const phase = process.env.TB256R2_PHASE || "before";
const target = process.env.TB256R2_TARGET || "14:00";
const outPath = process.env.TB256R2_OUT || "";
const draftId = "01a1beed-d7df-7000-bfc8-0185ea61afe3";
const route = `http://127.0.0.1:3000/admin/shipping-services/authoring/${draftId}?mode=edit`;
const userData = fs.mkdtempSync(path.join(os.tmpdir(), `tb256r2-save-${phase}-`));

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
for (let i = 0; i < 80; i += 1) {
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
  console.log(JSON.stringify({ ok: false, reason: "no_debug_target" }, null, 2));
  process.exit(2);
}

const socket = new WebSocket(wsUrl);
await new Promise((resolve, reject) => {
  socket.addEventListener("open", resolve);
  socket.addEventListener("error", reject);
});

let nextId = 1;
const pending = new Map();
const puts = [];
const getDocs = [];
const requests = new Map();
const consoleErrors = [];
socket.addEventListener("message", (event) => {
  const message = JSON.parse(String(event.data));
  if (message.method === "Network.requestWillBeSent") {
    const url = message.params.request.url;
    const method = message.params.request.method;
    requests.set(message.params.requestId, { url, method, postData: message.params.request.postData ?? null });
    if (url.includes("/editor-document") && method === "PUT") {
      puts.push({ requestId: message.params.requestId, at: Date.now(), body: message.params.request.postData ?? null, status: null, responseBody: null });
    }
    if (url.includes("/editor-document") && method === "GET") {
      getDocs.push({ requestId: message.params.requestId, at: Date.now() });
    }
  }
  if (message.method === "Network.responseReceived") {
    const entry = puts.find((item) => item.requestId === message.params.requestId);
    if (entry) entry.status = message.params.response.status;
  }
  if (message.method === "Network.loadingFinished") {
    const entry = puts.find((item) => item.requestId === message.params.requestId);
    if (entry && !entry.responseBody) {
      send("Network.getResponseBody", { requestId: message.params.requestId })
        .then((result) => {
          entry.responseBody = typeof result.body === "string" ? result.body.slice(0, 1200) : null;
        })
        .catch(() => {});
    }
  }
  if (message.method === "Runtime.exceptionThrown") {
    consoleErrors.push(message.params.exceptionDetails?.exception?.description ?? "exception");
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

const waitFor = async (expression, timeoutMs = 45000) => {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      if (await evaluate(expression)) return true;
    } catch {}
    await delay(300);
  }
  return false;
};

await send("Page.enable");
await send("Runtime.enable");
await send("Network.enable");
await send("Page.navigate", { url: route });

const READY =
  "!!document.querySelector('[data-testid=\"shipping-authoring-apply\"]') && !!document.getElementById('shipping-authoring-timing-tab-carrier') && !document.querySelector('[data-testid=\"shipping-authoring-entry-loading\"]')";
const ready = await waitFor(READY);

const loaded = await evaluate(`(() => {
  const apply = document.querySelector('[data-testid="shipping-authoring-apply"]');
  return {
    applyPresent: !!apply,
    applyDisabled: apply ? apply.disabled : null,
    applyLabel: apply ? (apply.textContent || '').trim() : null,
    unsavedHint: !!document.querySelector('[data-testid="shipping-authoring-unsaved-hint"]'),
    loadError: document.querySelector('[data-testid="shipping-authoring-load-error"]')?.textContent?.trim() ?? null,
    modeBanner: document.querySelector('[data-testid="shipping-authoring-mode-banner"]')?.textContent?.trim() ?? null,
  };
})()`);

const before = await evaluate(`(async () => {
  document.getElementById('shipping-authoring-timing-tab-carrier').click();
  await new Promise((r) => setTimeout(r, 250));
  const closeInput = document.querySelector('[data-testid="shipping-authoring-carrier-close-3"]');
  const openInput = document.querySelector('[data-testid="shipping-authoring-carrier-open-3"]');
  return {
    tabSelected: document.getElementById('shipping-authoring-timing-tab-carrier')?.getAttribute('aria-selected'),
    open: openInput ? openInput.value : null,
    close: closeInput ? closeInput.value : null,
    dayRows: document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"]').length,
    timeZone: document.querySelector('[data-testid="shipping-authoring-carrier-timezone"]')?.value ?? null,
  };
})()`);

const edited = await evaluate(`(async () => {
  const input = document.querySelector('[data-testid="shipping-authoring-carrier-close-3"]');
  if (!input) return { supported: false };
  const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
  input.focus();
  setter.call(input, ${JSON.stringify(target)});
  input.dispatchEvent(new Event('input', { bubbles: true }));
  await new Promise((r) => setTimeout(r, 400));
  const apply = document.querySelector('[data-testid="shipping-authoring-apply"]');
  return {
    supported: true,
    inputValue: input.value,
    applyDisabled: apply ? apply.disabled : null,
    unsavedHint: !!document.querySelector('[data-testid="shipping-authoring-unsaved-hint"]'),
    carrierIssues: Array.from(document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"] [role="alert"]')).map((n) => (n.textContent || '').trim()),
  };
})()`);

const putCountBefore = puts.length;
const clicked = await evaluate(`(async () => {
  const apply = document.querySelector('[data-testid="shipping-authoring-apply"]');
  if (!apply) return { clicked: false };
  apply.click();
  await new Promise((r) => setTimeout(r, 2500));
  return {
    clicked: true,
    disabledAfterClick: apply.disabled,
    labelAfterClick: (apply.textContent || '').trim(),
  };
})()`);

await delay(1500);

const afterClick = await evaluate(`(() => {
  const apply = document.querySelector('[data-testid="shipping-authoring-apply"]');
  const notice = document.querySelector('[role="status"]');
  return {
    applyDisabled: apply ? apply.disabled : null,
    applyLabel: apply ? (apply.textContent || '').trim() : null,
    anyStatusNotice: notice ? (notice.textContent || '').trim().slice(0, 220) : null,
    errorTexts: Array.from(document.querySelectorAll('[role="alert"]')).map((n) => (n.textContent || '').trim()).filter(Boolean).slice(0, 6),
    unsavedHint: !!document.querySelector('[data-testid="shipping-authoring-unsaved-hint"]'),
    modeBanner: document.querySelector('[data-testid="shipping-authoring-mode-banner"]')?.textContent?.trim() ?? null,
    closeValue: document.querySelector('[data-testid="shipping-authoring-carrier-close-3"]')?.value ?? null,
  };
})()`);

// Fresh reload of the same route: the rehydrated calendar is the only accepted proof.
await send("Page.navigate", { url: route });
const reloadedReady = await waitFor(READY);
await delay(600);
const afterReload = reloadedReady
  ? await evaluate(`(async () => {
  document.getElementById('shipping-authoring-timing-tab-carrier').click();
  await new Promise((r) => setTimeout(r, 350));
  const closeInput = document.querySelector('[data-testid="shipping-authoring-carrier-close-3"]');
  const openInput = document.querySelector('[data-testid="shipping-authoring-carrier-open-3"]');
  return {
    open: openInput ? openInput.value : null,
    close: closeInput ? closeInput.value : null,
    dayRows: document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"]').length,
    applyDisabled: document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled ?? null,
  };
})()`)
  : null;

const report = {
  phase,
  route,
  ready,
  target,
  loaded,
  before,
  edited,
  clicked,
  afterClick,
  putCountBefore,
  putCountAfter: puts.length,
  puts: puts.map((item) => ({
    status: item.status,
    bodyKeys: item.body ? Object.keys(JSON.parse(item.body)) : [],
    bodyCarrier: item.body ? JSON.parse(item.body).carrierSchedule ?? null : null,
    expectedRevision: item.body ? JSON.parse(item.body).expectedRevision ?? null : null,
    hasWeeklySchedule: item.body ? Object.prototype.hasOwnProperty.call(JSON.parse(item.body), "weeklySchedule") : null,
    responseSnippet: item.responseBody ? item.responseBody.slice(0, 400) : null,
  })),
  documentGets: getDocs.length,
  consoleErrors: consoleErrors.slice(0, 6),
  afterReload,
};

if (outPath) fs.writeFileSync(outPath, JSON.stringify(report, null, 2), "utf8");
console.log(JSON.stringify(report, null, 2));

socket.close();
await delay(150);
chromeProcess.kill();
try {
  fs.rmSync(userData, { recursive: true, force: true });
} catch {}
