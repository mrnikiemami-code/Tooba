// TB-264-R3 read-only geometry check (GET only, no click, no save, no mutation).
import { spawn } from "node:child_process";
import { writeFileSync, mkdirSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9351;
const PROFILE = "D:\\temp\\tb264r3-chrome";
const OUTDIR = "D:\\temp\\tb264r3-evidence";
const OUT = `${OUTDIR}\\measure.json`;
const BASE = "http://127.0.0.1:3000";
const DRAFT = "01a1cd43-b98d-7000-b2a9-a35ab39d8058";
const URL_ = `${BASE}/fa/admin/shipping-services/authoring/${DRAFT}`;

mkdirSync(OUTDIR, { recursive: true });

const chrome = spawn(CHROME, [
  `--remote-debugging-port=${PORT}`, `--user-data-dir=${PROFILE}`,
  "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank",
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function waitCdp() {
  for (let i = 0; i < 100; i++) {
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
      setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error(`timeout ${method}`)); } }, 45000);
    });
  }
  async eval(expression) {
    const r = await this.send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
    if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 900));
    return r.result.value;
  }
}

const expr = `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  for (let i = 0; i < 100; i++) {
    if (document.querySelector('[data-testid="shipping-authoring-screen"]')) break;
    await wait(250);
  }
  for (let i = 0; i < 60; i++) {
    if (document.querySelector('[data-testid="shipping-authoring-page-header"]')) break;
    await wait(250);
  }
  await wait(900);
  const q = (s) => document.querySelector(s);
  const qa = (s) => [...document.querySelectorAll(s)];
  const R = (e) => { if (!e) return null; const r = e.getBoundingClientRect(); return { x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom), left: Math.round(r.left), right: Math.round(r.right) }; };
  const T = (e) => (e ? (e.textContent || '').trim() : null);
  const vis = (e) => e && e.offsetParent !== null;

  const header = q('[data-testid="shipping-authoring-page-header"]');
  const screen = q('[data-testid="shipping-authoring-screen"]');
  const back = q('[data-testid="shipping-authoring-back"]');
  const title = header ? header.querySelector('h1') : null;
  const badge = q('[data-testid="shipping-authoring-status-badge"]');
  const method = q('[data-testid="shipping-authoring-method-name"]');
  const toolbar = q('[data-testid="shipping-authoring-toolbar"]');
  const identity = title ? title.parentElement.parentElement : null;

  const tcs = title ? getComputedStyle(title) : null;
  const lh = tcs ? parseFloat(tcs.lineHeight) || 0 : 0;
  const titleRect = title ? title.getBoundingClientRect() : null;
  const titleLines = (titleRect && lh) ? Math.round(titleRect.height / lh) : null;
  const methodRect = method ? method.getBoundingClientRect() : null;
  const methodClipped = method ? (method.scrollWidth > method.clientWidth + 1) : null;

  const clickable = qa('a,button,[role="button"],input,select,textarea').filter(vis);
  const tooSmall = clickable.filter(e => { const h = e.getBoundingClientRect().height; return h > 0 && h < 44; }).length;
  const headerButtons = header ? [...header.querySelectorAll('a,button')].filter(vis) : [];
  const headerBtnHeights = headerButtons.map(e => Math.round(e.getBoundingClientRect().height));

  const vw = document.documentElement.clientWidth;
  const offscreen = clickable.filter(e => { const r = e.getBoundingClientRect(); return r.width > 0 && (r.left < -1 || r.right > vw + 1); }).length;

  const inputs = qa('[data-testid="shipping-authoring-screen"] input, [data-testid="shipping-authoring-screen"] select').filter(vis);
  const inputWidths = inputs.map(e => Math.round(e.getBoundingClientRect().width));
  const narrowInputs = inputWidths.filter(w => w > 0 && w < 90).length;
  const clippedInputs = inputs.filter(e => e.getBoundingClientRect().height > 0 && e.getBoundingClientRect().height < 28).length;

  const tabs = qa('[data-testid^="shipping-authoring-tab-"]');
  const timingTabs = qa('[data-testid^="shipping-authoring-timing-tab-"]');

  // day rows across whichever week editor is mounted
  const dayRows = qa('[data-testid*="-day-"]').filter(vis);
  const dayRowRects = dayRows.map(e => { const r = R(e); return { id: e.getAttribute('data-testid'), w: r.w, h: r.h, right: r.right }; });
  const dayRowOverflow = dayRows.filter(e => e.scrollWidth > e.clientWidth + 1).map(e => e.getAttribute('data-testid'));
  const dayRowClippedH = dayRows.filter(e => e.getBoundingClientRect().height < 32).map(e => e.getAttribute('data-testid'));

  const labels = qa('[data-testid="shipping-authoring-screen"] label').filter(vis);
  const boundLabels = labels.filter(l => { const f = l.getAttribute('for'); return f && document.getElementById(f); }).length;

  const toolbarRects = toolbar ? [...toolbar.children].filter(vis).map(e => ({ t: T(e), r: R(e) })) : null;
  const collide = (toolbarRects || []).some(c => { if (!header) return false; const hr = header.getBoundingClientRect(); return c.r.left < hr.left || c.r.right > hr.right; });

  const activeTab = tabs.find(t => t.getAttribute('aria-selected') === 'true' || t.dataset.active === 'true');

  return JSON.stringify({
    url: location.href,
    title_text: T(title),
    dir: getComputedStyle(document.documentElement).direction,
    viewport: { innerWidth, clientWidth: vw },
    shell: { screen: R(screen), screenScrollWidth: screen ? screen.scrollWidth : null },
    header: { rect: R(header), position: header ? getComputedStyle(header).position : null },
    identity: R(identity),
    title: { rect: R(title), lines: titleLines, lineHeight: lh, scrollWidth: title ? title.scrollWidth : null, clientWidth: title ? title.clientWidth : null },
    badge: badge ? { text: T(badge), rect: R(badge) } : null,
    method: method ? { text: T(method), rect: R(method), clipped: methodClipped, hasTitleAttr: !!method.getAttribute('title') } : null,
    toolbar: { rect: R(toolbar), children: toolbarRects, collidesWithHeader: collide },
    headerButtons: headerButtons.map(e => ({ t: T(e), h: Math.round(e.getBoundingClientRect().height), w: Math.round(e.getBoundingClientRect().width) })),
    headerBtnMinHeight: headerBtnHeights.length ? Math.min(...headerBtnHeights) : null,
    doc: { scrollWidth: document.documentElement.scrollWidth, clientWidth: vw, overflow: document.documentElement.scrollWidth - vw, bodyScrollWidth: document.body.scrollWidth },
    clickable: { count: clickable.length, below44: tooSmall, offscreen },
    inputs: { count: inputs.length, minWidth: inputWidths.length ? Math.min(...inputWidths) : null, narrow: narrowInputs, shortH: clippedInputs },
    tabs: { count: tabs.length, active: activeTab ? T(activeTab) : null, heights: tabs.map(t => Math.round(t.getBoundingClientRect().height)) },
    timingTabs: { count: timingTabs.length, labels: timingTabs.map(T) },
    dayRows: { count: dayRows.length, overflow: dayRowOverflow, clippedH: dayRowClippedH, first: dayRowRects.slice(0, 7) },
    labels: { count: labels.length, bound: boundLabels },
    unsavedHint: T(q('[data-testid="shipping-authoring-unsaved-hint"]')),
    applyDisabled: (() => { const a = q('[data-testid="shipping-authoring-apply"]'); return a ? a.disabled : null; })(),
    editVisible: !!q('[data-testid="shipping-authoring-edit"]'),
  });
})()`;

(async () => {
  const result = { generatedAt: new Date().toISOString(), url: URL_, widths: [], ok: false };
  try {
    await waitCdp();
    const target = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(target.webSocketDebuggerUrl);
    await cdp.open();
    await cdp.send("Page.enable");
    await cdp.send("Runtime.enable");
    for (const w of [360, 390, 1440]) {
      await cdp.send("Emulation.setDeviceMetricsOverride", { width: w, height: 900, deviceScaleFactor: 1, mobile: false });
      await cdp.send("Page.navigate", { url: URL_ });
      await sleep(3200);
      let value = await cdp.eval(expr);
      for (let i = 0; i < 6; i++) {
        if (JSON.parse(value).header && JSON.parse(value).header.rect) break;
        await sleep(1500);
        value = await cdp.eval(expr);
      }
      result.widths.push({ width: w, metrics: JSON.parse(value) });
    }
    await cdp.send("Emulation.clearDeviceMetricsOverride");
    result.ok = true;
  } catch (err) {
    result.error = String(err && err.stack ? err.stack : err);
  } finally {
    try { writeFileSync(OUT, JSON.stringify(result, null, 2), "utf8"); } catch {}
    try { chrome.kill(); } catch {}
    console.log(JSON.stringify({ ok: result.ok, error: result.error ?? null, out: OUT }));
    process.exit(0);
  }
})();
