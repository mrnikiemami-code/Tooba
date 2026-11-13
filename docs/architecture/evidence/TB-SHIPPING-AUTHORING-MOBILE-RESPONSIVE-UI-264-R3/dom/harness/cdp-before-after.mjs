// TB-264-R3 A/B geometry check: measures the SAME served page with the pre-fix classes re-applied
// in the live DOM only (no source edit, no save, no data mutation) to record the real before/after pair.
import { spawn } from "node:child_process";
import { writeFileSync, mkdirSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9353;
const PROFILE = "D:\\temp\\tb264r3-chrome";
const OUTDIR = "D:\\temp\\tb264r3-evidence";
const OUT = `${OUTDIR}\\ab.json`;
const URL_ = "http://127.0.0.1:3000/fa/admin/shipping-services/authoring/01a1cd43-b98d-7000-b2a9-a35ab39d8058";

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

const measureExpr = `(() => {
  const q = (s) => document.querySelector(s);
  const header = q('[data-testid="shipping-authoring-page-header"]');
  const title = header ? header.querySelector('h1') : null;
  const method = q('[data-testid="shipping-authoring-method-name"]');
  const identity = title ? title.parentElement.parentElement : null;
  const R = (e) => { if (!e) return null; const r = e.getBoundingClientRect(); return { x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height), left: Math.round(r.left), right: Math.round(r.right) }; };
  const lh = title ? parseFloat(getComputedStyle(title).lineHeight) || 24 : 24;
  const tr = title ? title.getBoundingClientRect() : null;
  return JSON.stringify({
    header: R(header), position: header ? getComputedStyle(header).position : null,
    identity: R(identity),
    title: { rect: R(title), lines: tr ? Math.round(tr.height / lh) : null, text: title ? title.textContent.trim() : null },
    method: method ? { rect: R(method), text: method.textContent.trim(), clipped: method.scrollWidth > method.clientWidth + 1 } : null,
    docOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    subActions: [...document.querySelectorAll('[data-testid="shipping-authoring-screen"] a, [data-testid="shipping-authoring-screen"] button')]
      .filter(e => e.offsetParent !== null)
      .map(e => ({ tag: e.tagName, h: Math.round(e.getBoundingClientRect().height), w: Math.round(e.getBoundingClientRect().width), t: (e.textContent || '').trim().slice(0, 24) }))
      .filter(e => e.h > 0 && e.h < 44),
    smallInputs: [...document.querySelectorAll('[data-testid="shipping-authoring-screen"] input')]
      .map(e => ({ type: e.type, w: Math.round(e.getBoundingClientRect().width), h: Math.round(e.getBoundingClientRect().height) }))
      .filter(e => e.w < 90),
  });
})()`;

const applyOldExpr = `(() => {
  const q = (s) => document.querySelector(s);
  const header = q('[data-testid="shipping-authoring-page-header"]');
  const title = header.querySelector('h1');
  const identity = title.parentElement.parentElement;
  const inner = title.parentElement;
  const method = q('[data-testid="shipping-authoring-method-name"]');
  header.className = 'sticky top-0 z-20 space-y-2 rounded-xl border border-border bg-surface px-3 py-2 shadow-sm';
  identity.className = 'min-w-0 flex-1';
  inner.className = 'flex flex-wrap items-center gap-2';
  title.className = 'text-base font-black text-gray-900';
  if (method) method.className = 'truncate text-sm font-semibold text-gray-700';
  return 'old-classes-applied';
})()`;

(async () => {
  const result = { generatedAt: new Date().toISOString(), url: URL_, cases: [], ok: false };
  try {
    await waitCdp();
    const target = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(target.webSocketDebuggerUrl);
    await cdp.open();
    await cdp.send("Page.enable");
    await cdp.send("Runtime.enable");
    for (const w of [360, 390]) {
      await cdp.send("Emulation.setDeviceMetricsOverride", { width: w, height: 900, deviceScaleFactor: 1, mobile: false });
      await cdp.send("Page.navigate", { url: URL_ });
      await sleep(3400);
      for (let i = 0; i < 6; i++) {
        if (JSON.parse(await cdp.eval(measureExpr)).header) break;
        await sleep(1500);
      }
      const after = JSON.parse(await cdp.eval(measureExpr));
      await cdp.eval(applyOldExpr);
      await sleep(700);
      const before = JSON.parse(await cdp.eval(measureExpr));
      result.cases.push({ width: w, after, before });
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
