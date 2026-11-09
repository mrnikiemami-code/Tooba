/**
 * TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2 — bounded real-SAVE cases.
 *
 * Task-owned headless Chrome (fresh profile) on the EXISTING task-owned draft. Each case performs ONE
 * reversible UI edit and ONE real Save button press, then reloads the route and reports the exact
 * rehydrated calendars. No screenshot, no user profile/tab, no publication.
 *
 * TB256R2_CASE:
 *   customer-only      change ONLY the customer weekday-3 start, the carrier week must stay untouched
 *   customer-restore   revert that customer start to 09:00
 *   copy-then-edit     press «کپی از ساعات تحویل مشتری», then edit ONLY carrier weekday-3 close
 *   restore-carrier    put the original carrier week back (weekday 3 = 10:00–13:00, weekday 5 closed)
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const scenario = process.env.TB256R2_CASE || "customer-only";
const port = Number(process.env.TB256R2_CASE_PORT || 9753);
const outPath = process.env.TB256R2_CASE_OUT || "";
const draftId = "01a1beed-d7df-7000-bfc8-0185ea61afe3";
const route = `http://127.0.0.1:3000/admin/shipping-services/authoring/${draftId}?mode=edit`;
const userData = fs.mkdtempSync(path.join(os.tmpdir(), `tb256r2-case-${scenario}-`));

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
socket.addEventListener("message", (event) => {
  const message = JSON.parse(String(event.data));
  if (message.method === "Network.requestWillBeSent") {
    if (message.params.request.url.includes("/editor-document") && message.params.request.method === "PUT") {
      puts.push({ requestId: message.params.requestId, body: message.params.request.postData ?? null, status: null });
    }
  }
  if (message.method === "Network.responseReceived") {
    const entry = puts.find((item) => item.requestId === message.params.requestId);
    if (entry) entry.status = message.params.response.status;
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

const READY =
  "!!document.querySelector('[data-testid=\"shipping-authoring-apply\"]') && !!document.getElementById('shipping-authoring-timing-tab-carrier')";

/** One page-side helper: real typing through React's own input handling. */
const HELPERS = `window.__tb = {
  set(id, value) {
    const input = document.querySelector('[data-testid="' + id + '"]');
    if (!input) return false;
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
    input.focus();
    setter.call(input, value);
    input.dispatchEvent(new Event('input', { bubbles: true }));
    return true;
  },
  click(id) {
    const el = document.querySelector('[data-testid="' + id + '"]');
    if (!el) return false;
    el.click();
    return true;
  },
  read(id) {
    const el = document.querySelector('[data-testid="' + id + '"]');
    return el ? el.value : null;
  },
  select(id) {
    const el = document.getElementById(id);
    if (el) el.click();
    return !!el;
  },
  state() {
    const tabs = (prefix) => Array.from(document.querySelectorAll('[data-testid^="' + prefix + '"]'))
      .map((node) => node.getAttribute('data-testid'));
    return {
      applyDisabled: document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled ?? null,
      unsavedHint: !!document.querySelector('[data-testid="shipping-authoring-unsaved-hint"]'),
      status: document.querySelector('[role="status"]')?.textContent?.trim() ?? null,
      alerts: Array.from(document.querySelectorAll('[role="alert"]')).map((n) => (n.textContent || '').trim()).filter(Boolean),
      carrierRows: tabs('shipping-authoring-carrier-day-').length,
      customerRows: tabs('shipping-authoring-week-day-').length,
      carrier: [1,2,3,4,5,6,7].map((d) => ({
        weekday: d,
        accepting: document.querySelector('[data-testid="shipping-authoring-carrier-accepting-' + d + '"]')?.getAttribute('aria-pressed') ?? null,
        open: document.querySelector('[data-testid="shipping-authoring-carrier-open-' + d + '"]')?.value ?? null,
        close: document.querySelector('[data-testid="shipping-authoring-carrier-close-' + d + '"]')?.value ?? null,
      })),
      customer: [1,2,3,4,5].map((d) => ({
        weekday: d,
        open: document.querySelector('[data-testid="shipping-authoring-week-start-' + d + '"]')?.value ?? null,
        close: document.querySelector('[data-testid="shipping-authoring-week-end-' + d + '"]')?.value ?? null,
      })),
    };
  },
}; true`;

await send("Page.enable");
await send("Runtime.enable");
await send("Network.enable");
await send("Page.navigate", { url: route });
const ready = await waitFor(READY);
await evaluate(HELPERS);

const readCalendars = async () => {
  await evaluate(`window.__tb.select('shipping-authoring-timing-tab-carrier')`);
  await delay(200);
  return evaluate(`window.__tb.state()`);
};

const before = await readCalendars();

const editStep = await evaluate(`(async () => {
  const scenario = ${JSON.stringify(scenario)};
  const set = (id, value) => window.__tb.set(id, value);
  const click = (id) => window.__tb.click(id);
  await new Promise((r) => setTimeout(r, 150));
  if (scenario === 'customer-only' || scenario === 'customer-restore') {
    window.__tb.select('shipping-authoring-timing-tab-customer');
    await new Promise((r) => setTimeout(r, 200));
    const value = scenario === 'customer-only' ? '10:00' : '09:00';
    const ok = set('shipping-authoring-week-start-3', value);
    await new Promise((r) => setTimeout(r, 300));
    return { scenario, edited: 'customer.weekday3.open=' + value, ok, readBack: window.__tb.read('shipping-authoring-week-start-3') };
  }
  if (scenario === 'copy-then-edit') {
    window.__tb.select('shipping-authoring-timing-tab-carrier');
    await new Promise((r) => setTimeout(r, 200));
    const copied = click('shipping-authoring-carrier-copy');
    await new Promise((r) => setTimeout(r, 350));
    const carrierAfterCopy = [1,3,5,6].map((d) => ({
      weekday: d,
      open: window.__tb.read('shipping-authoring-carrier-open-' + d),
      close: window.__tb.read('shipping-authoring-carrier-close-' + d),
    }));
    const ok = set('shipping-authoring-carrier-close-3', '16:00');
    await new Promise((r) => setTimeout(r, 300));
    return { scenario, copied, carrierAfterCopy, ok, carrier33: window.__tb.read('shipping-authoring-carrier-close-3') };
  }
  if (scenario === 'restore-carrier') {
    window.__tb.select('shipping-authoring-timing-tab-carrier');
    await new Promise((r) => setTimeout(r, 200));
    set('shipping-authoring-carrier-open-3', '10:00');
    await new Promise((r) => setTimeout(r, 120));
    set('shipping-authoring-carrier-close-3', '13:00');
    await new Promise((r) => setTimeout(r, 120));
    click('shipping-authoring-carrier-notaccepting-5');
    await new Promise((r) => setTimeout(r, 200));
    for (const day of [6, 7]) {
      click('shipping-authoring-carrier-accepting-' + day);
      await new Promise((r) => setTimeout(r, 250));
      set('shipping-authoring-carrier-open-' + day, '09:00');
      await new Promise((r) => setTimeout(r, 120));
      set('shipping-authoring-carrier-close-' + day, '17:00');
      await new Promise((r) => setTimeout(r, 120));
    }
    return {
      scenario,
      day3: { open: window.__tb.read('shipping-authoring-carrier-open-3'), close: window.__tb.read('shipping-authoring-carrier-close-3') },
      day5: document.querySelector('[data-testid="shipping-authoring-carrier-notaccepting-5"]')?.getAttribute('aria-pressed') ?? null,
      day6: { open: window.__tb.read('shipping-authoring-carrier-open-6'), close: window.__tb.read('shipping-authoring-carrier-close-6') },
      day7: { open: window.__tb.read('shipping-authoring-carrier-open-7'), close: window.__tb.read('shipping-authoring-carrier-close-7') },
    };
  }
  return { scenario, unsupported: true };
})()`);

await delay(400);
const beforeClick = await evaluate(`(() => ({
  applyDisabled: document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled ?? null,
  unsavedHint: !!document.querySelector('[data-testid="shipping-authoring-unsaved-hint"]'),
  alerts: Array.from(document.querySelectorAll('[role="alert"]')).map((n) => (n.textContent || '').trim()).filter(Boolean),
}))()`);

const putCountBefore = puts.length;
await evaluate(`(async () => { document.querySelector('[data-testid="shipping-authoring-apply"]').click(); await new Promise((r) => setTimeout(r, 2200)); return true; })()`);
await delay(1200);
const afterClick = await evaluate(`(() => ({
  applyDisabled: document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled ?? null,
  status: document.querySelector('[role="status"]')?.textContent?.trim() ?? null,
  alerts: Array.from(document.querySelectorAll('[role="alert"]')).map((n) => (n.textContent || '').trim()).filter(Boolean),
  modeBanner: document.querySelector('[data-testid="shipping-authoring-mode-banner"]')?.textContent?.trim() ?? null,
}))()`);

await send("Page.navigate", { url: route });
const reloadedReady = await waitFor(READY);
await evaluate(HELPERS);
await delay(500);
const afterReload = reloadedReady ? await readCalendars() : null;

const report = {
  scenario,
  route,
  ready,
  before,
  editStep,
  beforeClick,
  afterClick,
  putCount: puts.length - putCountBefore,
  puts: puts.map((item) => {
    const body = item.body ? JSON.parse(item.body) : null;
    return {
      status: item.status,
      bodyKeys: body ? Object.keys(body) : [],
      expectedRevision: body?.expectedRevision ?? null,
      carrierDay3: body?.carrierSchedule?.days?.find((day) => day.weekday === 3) ?? null,
      carrierDay5: body?.carrierSchedule?.days?.find((day) => day.weekday === 5) ?? null,
      hasWeeklySchedule: body ? Object.prototype.hasOwnProperty.call(body, 'weeklySchedule') : null,
      weeklyDay3: body?.weeklySchedule?.days?.find((day) => day.weekday === 3) ?? null,
    };
  }),
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

