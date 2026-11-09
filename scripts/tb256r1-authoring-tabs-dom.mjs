/**
 * TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1 — authorized read-only DOM proof.
 *
 * Task-owned headless Chrome (fresh temporary profile, no user cookies/session) renders the real
 * authoring route for the review draft and reports facts only. It never clicks Save/Publish, never
 * mutates a draft, and produces no screenshot artifact.
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9751;
const route =
  process.env.TB256R1_ROUTE ||
  "http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit";
const userData = fs.mkdtempSync(path.join(os.tmpdir(), "tb256r1-dom-"));

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
const failed = [];
const consoleErrors = [];
const reqs = new Map();
socket.addEventListener("message", (event) => {
  const message = JSON.parse(String(event.data));
  if (message.method === "Network.requestWillBeSent") reqs.set(message.params.requestId, { url: message.params.request.url, method: message.params.request.method, initiatorType: message.params.initiator?.type ?? null });
  if (message.method === "Network.responseReceived" && message.params.response.status >= 400) {
    const sent = reqs.get(message.params.requestId);
    failed.push({
      status: message.params.response.status,
      method: sent?.method,
      type: message.params.type,
      url: message.params.response.url,
      mime: message.params.response.mimeType,
      initiator: sent?.initiatorType ?? null,
    });
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

const TIMING_READY =
  "!!document.querySelector(\"#shipping-authoring-timing-panel-customer\") && !!document.querySelector('[data-testid=\"shipping-authoring-tab-general\"]')";
const ready = await waitFor(TIMING_READY);

const collect = () =>
  evaluate(`(() => {
    const tabs = Array.from(document.querySelectorAll('[role="tab"]'));
    const timingTabs = tabs.filter((t) => t.id.startsWith('shipping-authoring-timing-tab-'));
    const pageTabs = tabs.filter((t) => t.id.startsWith('shipping-authoring-tab-') && !t.id.startsWith('shipping-authoring-timing'));
    const describe = (t) => {
      const style = getComputedStyle(t);
      const rect = t.getBoundingClientRect();
      const panel = t.getAttribute('aria-controls') ? document.getElementById(t.getAttribute('aria-controls')) : null;
      return {
        id: t.id,
        testid: t.getAttribute('data-testid'),
        text: (t.textContent || '').trim(),
        ariaSelected: t.getAttribute('aria-selected'),
        tabIndex: t.tabIndex,
        ariaControls: t.getAttribute('aria-controls'),
        panelExists: !!panel,
        panelHidden: panel ? panel.hasAttribute('hidden') : null,
        panelLabelledBy: panel ? panel.getAttribute('aria-labelledby') : null,
        role: t.getAttribute('role'),
        height: Math.round(rect.height),
        borderBottomWidth: style.borderBottomWidth,
        borderBottomColor: style.borderBottomColor,
        backgroundColor: style.backgroundColor,
        borderRadius: style.borderRadius,
        fontWeight: style.fontWeight,
      };
    };
    const timingSection = document.querySelector('[role="tablist"][aria-label="زمان‌بندی ارسال"]')?.parentElement ?? null;
    const timingText = timingSection ? timingSection.textContent || '' : '';
    const forbiddenNeedles = ['UTC', 'revision', 'نسخه', 'شناسه منبع', 'مستقل از', 'منطقه زمانی', 'نیمه‌باز', 'سیاست', 'اعتبار زمانی', 'داده‌های ثبت‌شده'];
    const forbiddenHits = [];
    if (timingSection) {
      const nodes = Array.from(timingSection.querySelectorAll('p, span, li, h2, h3, h4, label'));
      for (const node of nodes) {
        const text = (node.textContent || '').trim();
        if (!text || node.children.length > 0) continue;
        const needle = forbiddenNeedles.find((n) => text.includes(n));
        if (needle) forbiddenHits.push({ needle, testid: node.getAttribute('data-testid'), text: text.slice(0, 180) });
      }
    }
    return {
      listCount: document.querySelectorAll('[role="tablist"]').length,
      listLabels: Array.from(document.querySelectorAll('[role="tablist"]')).map((l) => l.getAttribute('aria-label')),
      pageTabs: pageTabs.map(describe),
      timingTabs: timingTabs.map(describe),
      panelCount: document.querySelectorAll('[role="tabpanel"]').length,
      customerDayRows: document.querySelectorAll('[data-testid^="shipping-authoring-week-day-"]').length,
      carrierDayRows: document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"]').length,
      carrierUnconfigured: !!document.querySelector('[data-testid="shipping-authoring-carrier-unconfigured"]'),
      copyActionCount: document.querySelectorAll('[data-testid="shipping-authoring-carrier-copy"]').length,
      copyActionVisible: Array.from(document.querySelectorAll('[data-testid="shipping-authoring-carrier-copy"]')).some((b) => b.getBoundingClientRect().height > 0),
      timingSectionTextLength: timingText.length,
      forbiddenCopyHits: forbiddenHits.slice(0, 8),
      hasEmojiOrIconOnlyTab: timingTabs.some((t) => (t.textContent || '').trim().length === 0),
      overflowX: document.documentElement.scrollWidth - window.innerWidth,
    };
  })()`);

const initial = ready ? await collect() : null;

// Read the customer day state, flip one customer day to closed (local unsaved edit only).
const editStep = await evaluate(`(() => {
  const working = document.querySelector('[data-testid="shipping-authoring-week-working-1"]');
  const closed = document.querySelector('[data-testid="shipping-authoring-week-closed-1"]');
  if (!working || !closed) return { supported: false };
  const beforePressed = closed.getAttribute('aria-pressed');
  closed.click();
  return {
    supported: true,
    beforeClosedPressed: beforePressed,
    afterClosedPressed: closed.getAttribute('aria-pressed'),
    afterWorkingPressed: working.getAttribute('aria-pressed'),
  };
})()`);

await delay(250);

const switchToCarrier = await evaluate(`(async () => {
  const tab = document.getElementById('shipping-authoring-timing-tab-carrier');
  tab.click();
  await new Promise((r) => setTimeout(r, 250));
  const customer = document.getElementById('shipping-authoring-timing-panel-customer');
  const carrier = document.getElementById('shipping-authoring-timing-panel-carrier');
  return {
    customerSelected: tab.getAttribute('aria-selected'),
    customerPanelHidden: customer.hasAttribute('hidden'),
    carrierPanelHidden: carrier ? carrier.hasAttribute('hidden') : null,
    carrierRows: document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"]').length,
    customerRowsStillMounted: document.querySelectorAll('[data-testid^="shipping-authoring-week-day-"]').length,
    copyVisible: (() => { const b = document.querySelector('[data-testid="shipping-authoring-carrier-copy"]'); if (!b) return null; const r = b.getBoundingClientRect(); return { height: Math.round(r.height), width: Math.round(r.width), disabled: b.disabled, text: (b.textContent || '').trim() }; })(),
    copyHint: (() => { const b = document.querySelector('[data-testid="shipping-authoring-carrier-copy"]'); return b?.parentElement ? (b.parentElement.textContent || '').trim().slice(0, 160) : null; })(),
    carrierRowsVisible: Array.from(document.querySelectorAll('[data-testid^="shipping-authoring-carrier-day-"]')).filter((n) => n.getBoundingClientRect().height > 0).length,
    activeTabTabIndexes: Array.from(document.querySelectorAll('[role="tab"][id^="shipping-authoring-timing-tab-"]')).map((t) => ({ id: t.id, selected: t.getAttribute('aria-selected'), tabIndex: t.tabIndex })),
  };
})()`);

const backToCustomer = await evaluate(`(async () => {
  document.getElementById('shipping-authoring-timing-tab-customer').click();
  await new Promise((r) => setTimeout(r, 250));
  const working = document.querySelector('[data-testid="shipping-authoring-week-working-1"]');
  const closed = document.querySelector('[data-testid="shipping-authoring-week-closed-1"]');
  const preserved = closed.getAttribute('aria-pressed') === 'true';
  // restore the untouched baseline (this day was ACTIVE before the probe) so no edit remains
  if (preserved) working.click();
  await new Promise((r) => setTimeout(r, 150));
  return {
    editedStatePreserved: preserved,
    restoredBaseline: working.getAttribute('aria-pressed') === 'true' && closed.getAttribute('aria-pressed') === 'false',
    customerPanelHidden: document.getElementById('shipping-authoring-timing-panel-customer').hasAttribute('hidden'),
  };
})()`);

// Keyboard: ArrowLeft (RTL => forward in reading order) then Home.
const keyboard = await evaluate(`(async () => {
  const first = document.getElementById('shipping-authoring-timing-tab-customer');
  first.focus();
  const dispatch = (key) => first.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
  const rtl = document.documentElement.getAttribute('dir') === 'rtl' || !!document.querySelector('[dir="rtl"]');
  dispatch('ArrowLeft');
  await new Promise((r) => setTimeout(r, 200));
  const afterArrow = document.activeElement?.id || null;
  dispatch('Home');
  await new Promise((r) => setTimeout(r, 200));
  const afterHome = document.activeElement?.id || null;
  return { rtl, afterArrow, afterHome, selected: Array.from(document.querySelectorAll('[role="tab"][id^="shipping-authoring-timing-tab-"]')).map((t) => t.getAttribute('aria-selected')) };
})()`);

const responsive = {};
for (const width of [1024, 390]) {
  await send("Emulation.setDeviceMetricsOverride", {
    width,
    height: width === 390 ? 844 : 900,
    deviceScaleFactor: 1,
    mobile: width === 390,
  });
  await delay(400);
  responsive[width] = await evaluate(`(() => {
    const list = document.querySelector('[role="tablist"][aria-label]');
    const boxes = Array.from(document.querySelectorAll('[role="tab"][id^="shipping-authoring-timing-tab-"]')).map((t) => {
      const r = t.getBoundingClientRect();
      return { text: (t.textContent || '').trim(), height: Math.round(r.height), width: Math.round(r.width) };
    });
    return {
      overflowX: document.documentElement.scrollWidth - window.innerWidth,
      tabs: boxes,
      pageWidth: window.innerWidth,
      docWidth: document.documentElement.scrollWidth,
    };
  })()`);
}
await send("Emulation.clearDeviceMetricsOverride");

console.log(
  JSON.stringify(
    {
      route,
      ready,
      failedRequests: failed.slice(0, 20),
      consoleErrors: consoleErrors.slice(0, 10),
      initial,
      editStep,
      switchToCarrier,
      backToCustomer,
      keyboard,
      responsive,
    },
    null,
    2,
  ),
);

socket.close();
await delay(150);
chromeProcess.kill();
fs.rmSync(userData, { recursive: true, force: true });
