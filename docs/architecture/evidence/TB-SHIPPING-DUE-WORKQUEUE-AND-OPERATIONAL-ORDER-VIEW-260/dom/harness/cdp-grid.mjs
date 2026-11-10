// TB-260 case D2: prove the dispatch-timing columns actually render their values.
// Read-only: navigate, scroll the grid horizontally, read DOM text. No interaction with records.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9341;
const PROFILE = "D:\\temp\\tb260-chrome";
const OUT = "D:\\temp\\tb260-evidence\\dom\\tb260-dom-grid.json";

const chrome = spawn(CHROME, [
  `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${PROFILE}`,
  "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank",
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function waitCdp() {
  for (let i = 0; i < 80; i++) {
    try { const r = await fetch(`http://127.0.0.1:${PORT}/json/version`); if (r.ok) return; } catch {}
    await sleep(250);
  }
  throw new Error("cdp down");
}

class Cdp {
  constructor(url) { this.url = url; this.id = 0; this.pending = new Map(); }
  async open() {
    this.ws = new WebSocket(this.url);
    await new Promise((res, rej) => { this.ws.addEventListener("open", () => res()); this.ws.addEventListener("error", () => rej(new Error("ws"))); });
    this.ws.addEventListener("message", (ev) => {
      const m = JSON.parse(ev.data);
      if (m.id && this.pending.has(m.id)) { const p = this.pending.get(m.id); this.pending.delete(m.id); m.error ? p.rej(new Error(JSON.stringify(m.error))) : p.res(m.result); }
    });
  }
  send(method, params = {}) {
    const id = ++this.id;
    return new Promise((res, rej) => { this.pending.set(id, { res, rej }); this.ws.send(JSON.stringify({ id, method, params })); setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error("timeout " + method)); } }, 40000); });
  }
  async eval(expression) {
    const r = await this.send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
    if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 500));
    return r.result.value;
  }
}

const script = (left) => `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  const sels = ['.ag-body-horizontal-scroll-viewport', '.ag-center-cols-viewport', '.ag-body-viewport'];
  for (const s of sels) document.querySelectorAll(s).forEach(e => { try { e.scrollLeft = ${left}; } catch {} });
  await wait(500);
  const txt = (e) => (e.textContent || '').trim();
  const headers = [...document.querySelectorAll('.ag-header-cell')].map(h => h.getAttribute('col-id')).filter(Boolean);
  const cell = (id) => [...document.querySelectorAll('.ag-cell[col-id="' + id + '"]')].map(txt);
  return JSON.stringify({
    moved: ${left},
    headers,
    plan: cell('dispatchDeadline').slice(0, 3),
    receiving: cell('customerReceiving').slice(0, 3),
    nextStep: cell('nextStep').slice(0, 3),
    bucket: cell('dispatchBucket').slice(0, 3),
    scrollLefts: sels.map(s => { const e = document.querySelector(s); return e ? Math.round(e.scrollLeft) : null; }),
  });
})()`;

(async () => {
  const out = { at: new Date().toISOString(), samples: [] };
  try {
    await waitCdp();
    const t = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(t.webSocketDebuggerUrl);
    await cdp.open();
    await cdp.send("Page.enable");
    await cdp.send("Runtime.enable");
    await cdp.send("Emulation.setDeviceMetricsOverride", { width: 1600, height: 900, deviceScaleFactor: 1, mobile: false });
    await cdp.send("Page.navigate", { url: "http://127.0.0.1:3000/fa/admin/fulfillments" });
    await sleep(3000);
    for (let i = 0; i < 20; i++) {
      const n = await cdp.eval("document.querySelectorAll('.ag-cell').length");
      if (n > 0) break;
      await sleep(500);
    }
    for (const left of [0, -400, -900, 400, 900, -1600]) out.samples.push(JSON.parse(await cdp.eval(script(left))));
    await cdp.send("Emulation.clearDeviceMetricsOverride");
    out.ok = true;
  } catch (e) {
    out.ok = false; out.error = String(e && e.stack ? e.stack : e);
  } finally {
    try { writeFileSync(OUT, JSON.stringify(out, null, 2), "utf8"); } catch {}
    try { chrome.kill(); } catch {}
    console.log(JSON.stringify({ ok: out.ok, error: out.error ?? null, samples: out.samples.length, out: OUT }));
    process.exit(0);
  }
})();
