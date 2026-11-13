// TB-264-R3 tab/day-row probe at mobile widths: switches the authoring tabs (no save, no publication,
// no mode change, no data write) and records day-row/field geometry for each timing editor.
import { spawn } from "node:child_process";
import { writeFileSync, mkdirSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9355;
const PROFILE = "D:\\temp\\tb264r3-chrome";
const OUTDIR = "D:\\temp\\tb264r3-evidence";
const OUT = `${OUTDIR}\\tabs.json`;
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
  const sc = document.querySelector('[data-testid="shipping-authoring-screen"]');
  if (!sc) return JSON.stringify({ missing: true });
  const qa = (s) => [...sc.querySelectorAll(s)];
  const rows = qa('[data-testid*="-day-"]').filter(e => e.offsetParent !== null);
  const inputs = qa('input, select').filter(e => e.offsetParent !== null);
  const R = (e) => { const r = e.getBoundingClientRect(); return { w: Math.round(r.width), h: Math.round(r.height), right: Math.round(r.right), left: Math.round(r.left) }; };
  const vw = document.documentElement.clientWidth;
  const labels = qa('label').filter(e => e.offsetParent !== null);
  return JSON.stringify({
    activeMainTab: (qa('[data-testid^="shipping-authoring-tab-"]').find(t => t.getAttribute('aria-selected') === 'true') || {}).textContent?.trim() ?? null,
    activeTimingTab: (qa('[data-testid^="shipping-authoring-timing-tab-"]').find(t => t.getAttribute('aria-selected') === 'true') || {}).textContent?.trim() ?? null,
    timingTabHeights: qa('[data-testid^="shipping-authoring-timing-tab-"]').map(t => Math.round(t.getBoundingClientRect().height)),
    rows: rows.length,
    rowIds: rows.map(r => r.getAttribute('data-testid')),
    rowWidths: rows.map(r => Math.round(r.getBoundingClientRect().width)),
    rowOverflow: rows.filter(r => r.scrollWidth > r.clientWidth + 1).map(r => r.getAttribute('data-testid')),
    rowOffscreen: rows.filter(r => { const b = r.getBoundingClientRect(); return b.right > vw + 1 || b.left < -1; }).length,
    inputs: inputs.length,
    inputKinds: inputs.map(i => i.type + ':' + Math.round(i.getBoundingClientRect().width) + 'x' + Math.round(i.getBoundingClientRect().height)).slice(0, 24),
    textInputsNarrow: inputs.filter(i => i.type !== 'radio' && i.type !== 'checkbox' && i.getBoundingClientRect().width < 90).length,
    textInputsClippedH: inputs.filter(i => i.type !== 'radio' && i.type !== 'checkbox' && i.getBoundingClientRect().height < 28).length,
    labels: labels.length,
    labelsBound: labels.filter(l => { const f = l.getAttribute('for'); return f && document.getElementById(f); }).length,
    docOverflow: document.documentElement.scrollWidth - vw,
    buttonMinHeight: (() => { const b = qa('button').filter(e => e.offsetParent !== null); return b.length ? Math.min(...b.map(x => Math.round(x.getBoundingClientRect().height))) : null; })(),
  });
})()`;

const clickExpr = (testid) => `(() => { const e = document.querySelector('[data-testid="${testid}"]'); if (!e) return 'missing:${testid}'; if (e.disabled) return 'disabled:${testid}'; e.click(); return 'clicked:${testid}'; })()`;

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
        if (!JSON.parse(await cdp.eval(measureExpr)).missing) break;
        await sleep(1500);
      }
      const steps = [];
      steps.push({ step: "general", state: JSON.parse(await cdp.eval(measureExpr)) });

      // timing tabs inside the general panel
      for (const t of ["customer", "carrier", "processing"]) {
        const clicked = await cdp.eval(clickExpr(`shipping-authoring-timing-tab-${t}`));
        await sleep(1100);
        steps.push({ step: `timing-${t}`, clicked, state: JSON.parse(await cdp.eval(measureExpr)) });
      }

      // regions tab
      const r = await cdp.eval(clickExpr("shipping-authoring-tab-regions"));
      await sleep(1300);
      steps.push({ step: "regions", clicked: r, state: JSON.parse(await cdp.eval(measureExpr)) });
      result.cases.push({ width: w, steps });
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
