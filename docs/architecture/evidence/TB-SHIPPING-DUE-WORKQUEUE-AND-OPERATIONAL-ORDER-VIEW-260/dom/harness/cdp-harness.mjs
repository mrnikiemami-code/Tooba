// TB-260 case D harness (final): read-only CDP verification of the canonical queue and the shared
// operational order view at three widths. GET only: no click, no business mutation, no POST.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9339;
const PROFILE = "D:\\temp\\tb260-chrome";
const OUT = "D:\\temp\\tb260-evidence\\dom\\tb260-dom.json";
const BASE = "http://127.0.0.1:3000";
const LEGACY = "01a1b92d-e97e-7000-999a-143350d0edaf";

const chrome = spawn(CHROME, [
  `--remote-debugging-port=${PORT}`, `--user-data-dir=${PROFILE}`,
  "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank",
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function waitCdp() {
  for (let i = 0; i < 80; i++) {
    try { const r = await fetch(`http://127.0.0.1:${PORT}/json/version`); if (r.ok) return; } catch {}
    await sleep(250);
  }
  throw new Error("cdp not reachable");
}

class Cdp {
  constructor(url) { this.url = url; this.id = 0; this.pending = new Map(); }
  async open() {
    this.ws = new WebSocket(this.url);
    await new Promise((res, rej) => { this.ws.addEventListener("open", () => res()); this.ws.addEventListener("error", () => rej(new Error("ws error"))); });
    this.ws.addEventListener("message", (ev) => {
      const m = JSON.parse(ev.data);
      if (m.id && this.pending.has(m.id)) { const p = this.pending.get(m.id); this.pending.delete(m.id); m.error ? p.rej(new Error(JSON.stringify(m.error))) : p.res(m.result); }
    });
  }
  send(method, params = {}) {
    const id = ++this.id;
    return new Promise((res, rej) => {
      this.pending.set(id, { res, rej });
      this.ws.send(JSON.stringify({ id, method, params }));
      setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error(`timeout ${method}`)); } }, 40000);
    });
  }
  async eval(expression) {
    const r = await this.send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
    if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 800));
    return r.result.value;
  }
}

const queueExpr = `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  for (let i = 0; i < 80; i++) {
    if (document.querySelector('[data-testid^="admin-fulfillment-open-operations-"]')) break;
    await wait(250);
  }
  const hv = document.querySelector('.ag-body-horizontal-scroll-viewport');
  if (hv) { hv.scrollLeft = -700; await wait(700); }
  const byPrefix = (p) => [...document.querySelectorAll('[data-testid^="' + p + '"]')];
  const txt = (e) => (e.textContent || '').trim();
  const tabs = [...document.querySelectorAll('[data-testid^="admin-fulfillment-queue-filter-dispatch_"]')];
  const counts = [...document.querySelectorAll('[data-testid^="admin-fulfillment-queue-count-"]')];
  const actions = byPrefix('admin-fulfillment-open-operations-');
  const actionsH = actions.map(a => Math.round(a.getBoundingClientRect().height));
  const statusTabs = [...document.querySelectorAll('[data-testid^="admin-fulfillment-queue-filter-"]')].filter(t => !t.getAttribute('data-testid').includes('dispatch_'));
  const cells = (p) => byPrefix(p).map(txt).filter(Boolean);
  const bucketBadges = [...document.querySelectorAll('.ag-cell[col-id="dispatchBucket"]')].map(txt).slice(0, 4);
  const planCells = [...document.querySelectorAll('[data-testid^="admin-fulfillment-plan-"]')].map(txt).slice(0, 4);
  const selectedTab = tabs.find(t => t.getAttribute('aria-selected') === 'true');
  return JSON.stringify({
    url: location.href,
    dir: getComputedStyle(document.documentElement).direction,
    innerWidth, docScrollWidth: document.documentElement.scrollWidth, bodyScrollWidth: document.body.scrollWidth,
    selectedDispatchTab: selectedTab ? txt(selectedTab.querySelector('span') || selectedTab) : null,
    dispatchTabs: tabs.map(t => ({ label: txt(t.querySelector('span') || t), count: t.getAttribute('data-count'), h: Math.round(t.getBoundingClientRect().height) })),
    counts: counts.map(c => txt(c)),
    statusTabCount: statusTabs.length,
    rowCount: actions.length,
    rowIconMinHeight: actionsH.length ? Math.min(...actionsH) : null,
    tabMinHeight: tabs.length ? Math.min(...tabs.map(t => Math.round(t.getBoundingClientRect().height))) : null,
    bucketBadges, planCells,
    receivings: cells('admin-fulfillment-receiving-').slice(0, 4),
    nextSteps: cells('admin-fulfillment-next-step-').slice(0, 4),
    hrefs: actions.slice(0, 2).map(a => a.getAttribute('href')),
    latinLeak: /\\b(Today|Overdue|Upcoming|Unspecified|InTransit|TodayNotOpen|Closed|None|NotQueued)\\b/.test(document.querySelector('[data-testid="admin-fulfillment-work-queue"]').innerText),
    hasEmptyState: !!document.querySelector('[data-testid="admin-fulfillment-work-queue"]').innerText.includes('موردی'),
  });
})()`;

const opsExpr = `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  for (let i = 0; i < 80; i++) {
    if (document.querySelector('[data-testid="admin-order-operations-view"]') &&
        (document.querySelector('[data-testid="admin-order-items-shipping"]') || document.querySelector('[data-testid="admin-order-operations-error"]') || document.querySelector('[data-testid="admin-order-operations-denied"]'))) break;
    await wait(250);
  }
  await wait(600);
  const main = document.querySelector('[data-testid="admin-order-operations-view"]');
  if (!main) return JSON.stringify({ url: location.href, missingView: true });
  const txt = (e) => (e.textContent || '').trim();
  const cards = [...document.querySelectorAll('[data-testid^="admin-order-operations-target-"]')].filter(e => e.tagName === 'LI');
  const interactive = [...main.querySelectorAll('a,button')].filter(e => e.offsetParent !== null);
  const heights = interactive.map(e => Math.round(e.getBoundingClientRect().height));
  const body = txt(main);
  const finance = ['تسویه', 'کیف پول', 'بازگشت وجه', 'دریافت‌ها', 'سهم فروشنده', 'پرداخت به فروشنده'];
  return JSON.stringify({
    url: location.href,
    dir: getComputedStyle(main).direction,
    innerWidth, docScrollWidth: document.documentElement.scrollWidth, mainScrollWidth: main.scrollWidth,
    hasOperationalPanel: !!document.querySelector('[data-testid="admin-order-operational-delivery"], [data-testid="admin-order-operational-delivery-empty"]'),
    hasShippingPanel: !!document.querySelector('[data-testid="admin-order-items-shipping"]'),
    state: document.querySelector('[data-testid="admin-order-operations-denied"]') ? 'denied'
      : document.querySelector('[data-testid="admin-order-operations-error"]') ? 'error' : 'ready',
    targetCards: cards.length,
    targetTexts: cards.map(txt).slice(0, 3),
    nullPlanCards: cards.filter(c => txt(c).includes('موعد ارسال مشخص نشده')).length,
    missingTarget: !!document.querySelector('[data-testid="admin-order-operations-target-missing"]'),
    financeHits: finance.filter(f => body.includes(f)),
    backHref: document.querySelector('[data-testid="admin-order-operations-back"]')?.getAttribute('href') ?? null,
    fullOrderLink: document.querySelector('[data-testid="admin-order-operations-full-order"]')?.getAttribute('href') ?? null,
    actionCount: heights.length,
    actionMinHeight: heights.length ? Math.min(...heights) : null,
    below44: heights.filter(h => h < 44).length,
    targetWrap: (() => { const el = cards[0]?.querySelector('dd'); if (!el) return null; const cs = getComputedStyle(el); return { overflowWrap: cs.overflowWrap, wordBreak: cs.wordBreak }; })(),
  });
})()`;

(async () => {
  const result = { generatedAt: new Date().toISOString(), widths: [1440, 1024, 390], pages: [] };
  try {
    await waitCdp();
    const target = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(target.webSocketDebuggerUrl);
    await cdp.open();
    await cdp.send("Page.enable");
    await cdp.send("Runtime.enable");

    const run = async (name, url, width, expr, ready) => {
      await cdp.send("Emulation.setDeviceMetricsOverride", { width, height: 900, deviceScaleFactor: 1, mobile: false });
      await cdp.send("Page.navigate", { url });
      await sleep(2600);
      let value = await cdp.eval(expr);
      for (let i = 0; i < 8; i++) {
        if (ready(JSON.parse(value))) break;
        await sleep(1400);
        value = await cdp.eval(expr);
      }
      const metrics = JSON.parse(value);
      result.pages.push({ name, width, metrics });
      return metrics;
    };

    const queueReady = (m) => m.rowCount > 0;
    let firstHref = null;
    for (const w of [1440, 1024, 390]) {
      const m = await run("queue", `${BASE}/fa/admin/fulfillments`, w, queueExpr, queueReady);
      if (!firstHref && m.hrefs?.length) firstHref = m.hrefs[0];
    }

    // The active quick filter must travel with the row link so «بازگشت به صف کار» restores the same view.
    await run("queue-filtered", `${BASE}/fa/admin/fulfillments?filter=dispatch_today`, 1440, queueExpr, queueReady);

    // Exact locator taken from a real queue row (fulfillmentId + shippingGroupId + filter).
    if (firstHref) {
      const exact = `${BASE}/fa${firstHref}${firstHref.includes("filter=") ? "" : "&filter=dispatch_today"}`;
      result.exactLocatorUrl = exact;
      await run("queue-exact-locator", exact, 1440, opsExpr, (m) => m.hasShippingPanel === true);
    }

    for (const w of [1440, 1024, 390]) {
      await run("legacy-operations-all", `${BASE}/fa/admin/orders/${LEGACY}/operations?from=queue`, w, opsExpr, (m) => m.hasShippingPanel === true);
    }

    await cdp.send("Emulation.clearDeviceMetricsOverride");
    result.ok = true;
  } catch (err) {
    result.ok = false;
    result.error = String(err && err.stack ? err.stack : err);
  } finally {
    try { writeFileSync(OUT, JSON.stringify(result, null, 2), "utf8"); } catch {}
    try { chrome.kill(); } catch {}
    console.log(JSON.stringify({ ok: result.ok, error: result.error ?? null, pages: result.pages.length, out: OUT }));
    process.exit(0);
  }
})();
