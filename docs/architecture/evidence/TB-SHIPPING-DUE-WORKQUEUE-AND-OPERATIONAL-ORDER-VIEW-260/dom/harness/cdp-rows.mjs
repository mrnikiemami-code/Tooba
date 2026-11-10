// Enumerate the queue page-1 operational hrefs and probe each order detail (read-only GET).
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9347;
const OUT = "D:\\temp\\tb260-evidence\\dom\\tb260-rows.json";
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
  const out = {};
  try {
    await waitCdp();
    const t = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(t.webSocketDebuggerUrl); await cdp.open();
    await cdp.send("Page.enable"); await cdp.send("Runtime.enable");
    await cdp.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
    await cdp.send("Page.navigate", { url: "http://127.0.0.1:3000/fa/admin/fulfillments" });
    await sleep(5000);
    out.hrefs = JSON.parse(await cdp.eval(`JSON.stringify([...document.querySelectorAll('[data-testid^="admin-fulfillment-open-operations-"]')].map(a => a.getAttribute('href')))`));
    const orders = [...new Set(out.hrefs.map(h => h.split('/admin/orders/')[1].split('/')[0]))].slice(0, 5);
    out.probes = [];
    for (const id of orders) {
      await cdp.send("Page.navigate", { url: `http://127.0.0.1:3000/fa/admin/orders/${id}` });
      await sleep(3500);
      const state = await cdp.eval(`(() => {
        const err = document.querySelector('[data-testid="admin-order-operations-error"]');
        const body = document.body.innerText;
        return JSON.stringify({ id: '${id}', detailErrored: body.includes('admin.http.500') || body.includes('سفارش خوانده نشد'), hasShipping: !!document.querySelector('[data-testid="admin-order-items-shipping"]'), hasOps: !!document.querySelector('[data-testid="admin-order-operational-delivery"], [data-testid="admin-order-operational-delivery-empty"]') });
      })()`);
      out.probes.push(JSON.parse(state));
    }
    out.ok = true;
  } catch (e) { out.ok = false; out.error = String(e && e.stack ? e.stack : e); }
  finally { try { writeFileSync(OUT, JSON.stringify(out, null, 2), "utf8"); } catch {} try { chrome.kill(); } catch {} console.log(JSON.stringify({ ok: out.ok, out: OUT })); process.exit(0); }
})();
