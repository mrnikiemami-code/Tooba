// Probe one exact locator URL and report the page state + message (read-only GET).
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9345;
const URL_ = process.argv[2];
const OUT = process.argv[3] ?? "D:\\temp\\tb260-evidence\\dom\\tb260-probe.json";
const chrome = spawn(CHROME, [`--remote-debugging-port=${PORT}`, `--user-data-dir=D:\\temp\\tb260-chrome`, "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
async function waitCdp() { for (let i = 0; i < 80; i++) { try { const r = await fetch(`http://127.0.0.1:${PORT}/json/version`); if (r.ok) return; } catch {} await sleep(250); } throw new Error("cdp down"); }
class Cdp {
  constructor(u) { this.url = u; this.id = 0; this.pending = new Map(); }
  async open() { this.ws = new WebSocket(this.url); await new Promise((res, rej) => { this.ws.addEventListener("open", () => res()); this.ws.addEventListener("error", () => rej(new Error("ws"))); }); this.ws.addEventListener("message", (ev) => { const m = JSON.parse(ev.data); if (m.id && this.pending.has(m.id)) { const p = this.pending.get(m.id); this.pending.delete(m.id); m.error ? p.rej(new Error(JSON.stringify(m.error))) : p.res(m.result); } }); }
  send(method, params = {}) { const id = ++this.id; return new Promise((res, rej) => { this.pending.set(id, { res, rej }); this.ws.send(JSON.stringify({ id, method, params })); setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error("timeout " + method)); } }, 40000); }); }
  async eval(e) { const r = await this.send("Runtime.evaluate", { expression: e, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 400)); return r.result.value; }
}
(async () => {
  const out = { url: URL_ };
  try {
    await waitCdp();
    const t = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(t.webSocketDebuggerUrl); await cdp.open();
    await cdp.send("Page.enable"); await cdp.send("Runtime.enable");
    await cdp.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
    await cdp.send("Page.navigate", { url: URL_ });
    await sleep(6000);
    out.snapshot = JSON.parse(await cdp.eval(`JSON.stringify({
      main: !!document.querySelector('[data-testid="admin-order-operations-view"]'),
      state: document.querySelector('[data-testid="admin-order-operations-denied"]') ? 'denied' : document.querySelector('[data-testid="admin-order-operations-error"]') ? 'error' : 'ready',
      errorText: (document.querySelector('[data-testid="admin-order-operations-error"]')?.innerText || '').slice(0, 300),
      missingTarget: (document.querySelector('[data-testid="admin-order-operations-target-missing"]')?.innerText || '').slice(0, 300),
      targets: document.querySelectorAll('[data-testid^="admin-order-operations-target-"]').length,
      panel: !!document.querySelector('[data-testid="admin-order-items-shipping"]'),
      back: document.querySelector('[data-testid="admin-order-operations-back"]')?.getAttribute('href') ?? null,
      status: performance.getEntriesByType('resource').filter(r => r.name.includes('/v1/admin/orders/')).map(r => r.name.slice(0, 160)).slice(0, 3)
    })`));
    out.ok = true;
  } catch (e) { out.ok = false; out.error = String(e && e.stack ? e.stack : e); }
  finally { try { writeFileSync(OUT, JSON.stringify(out, null, 2), "utf8"); } catch {} try { chrome.kill(); } catch {} console.log(JSON.stringify({ ok: out.ok, out: OUT })); process.exit(0); }
})();
