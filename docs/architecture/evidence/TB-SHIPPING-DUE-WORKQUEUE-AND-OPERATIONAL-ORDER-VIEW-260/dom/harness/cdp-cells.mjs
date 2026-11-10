// TB-260 case D3: dump the exact outerHTML of the timing cells to attribute their text truthfully.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9343;
const PROFILE = "D:\\temp\\tb260-chrome";
const OUT = "D:\\temp\\tb260-evidence\\dom\\tb260-cell-dump.json";
const chrome = spawn(CHROME, [`--remote-debugging-port=${PORT}`, `--user-data-dir=${PROFILE}`, "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
async function waitCdp() { for (let i = 0; i < 80; i++) { try { const r = await fetch(`http://127.0.0.1:${PORT}/json/version`); if (r.ok) return; } catch {} await sleep(250); } throw new Error("cdp down"); }
class Cdp {
  constructor(u) { this.url = u; this.id = 0; this.pending = new Map(); }
  async open() { this.ws = new WebSocket(this.url); await new Promise((res, rej) => { this.ws.addEventListener("open", () => res()); this.ws.addEventListener("error", () => rej(new Error("ws"))); }); this.ws.addEventListener("message", (ev) => { const m = JSON.parse(ev.data); if (m.id && this.pending.has(m.id)) { const p = this.pending.get(m.id); this.pending.delete(m.id); m.error ? p.rej(new Error(JSON.stringify(m.error))) : p.res(m.result); } }); }
  send(method, params = {}) { const id = ++this.id; return new Promise((res, rej) => { this.pending.set(id, { res, rej }); this.ws.send(JSON.stringify({ id, method, params })); setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error("timeout " + method)); } }, 40000); }); }
  async eval(e) { const r = await this.send("Runtime.evaluate", { expression: e, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 500)); return r.result.value; }
}
const expr = `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  for (let i = 0; i < 40; i++) { if (document.querySelectorAll('.ag-cell').length > 0) break; await wait(300); }
  const scroll = document.querySelector('.ag-body-horizontal-scroll-viewport');
  if (scroll) { scroll.scrollLeft = -700; await wait(600); }
  const first = (id) => document.querySelector('.ag-cell[col-id="' + id + '"]');
  const dump = (id) => { const e = first(id); return e ? e.outerHTML.slice(0, 900) : null; };
  return JSON.stringify({
    buildHint: [...document.scripts].map(s => s.src).filter(s => s.includes('/_next/static/chunks/')).slice(0, 4),
    bucket: dump('dispatchBucket'),
    plan: dump('dispatchDeadline'),
    receiving: dump('customerReceiving'),
    nextStep: dump('nextStep'),
  });
})()`;
(async () => {
  const out = {};
  try {
    await waitCdp();
    const t = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(t.webSocketDebuggerUrl); await cdp.open();
    await cdp.send("Page.enable"); await cdp.send("Runtime.enable");
    await cdp.send("Emulation.setDeviceMetricsOverride", { width: 1600, height: 900, deviceScaleFactor: 1, mobile: false });
    await cdp.send("Page.navigate", { url: "http://127.0.0.1:3000/fa/admin/fulfillments" });
    await sleep(3500);
    Object.assign(out, JSON.parse(await cdp.eval(expr)));
    await cdp.send("Emulation.clearDeviceMetricsOverride");
    out.ok = true;
  } catch (e) { out.ok = false; out.error = String(e && e.stack ? e.stack : e); }
  finally { try { writeFileSync(OUT, JSON.stringify(out, null, 2), "utf8"); } catch {} try { chrome.kill(); } catch {} console.log(JSON.stringify({ ok: out.ok, error: out.error ?? null, out: OUT })); process.exit(0); }
})();
