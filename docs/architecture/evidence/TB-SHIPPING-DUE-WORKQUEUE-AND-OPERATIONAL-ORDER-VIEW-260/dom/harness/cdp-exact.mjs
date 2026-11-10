// TB-260 final exact-target proof: queue row locator (working order) and legacy order group locator.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const PORT = 9349;
const OUT = "D:\\temp\\tb260-evidence\\dom\\tb260-exact.json";
const BASE = "http://127.0.0.1:3000";
const chrome = spawn(CHROME, [`--remote-debugging-port=${PORT}`, `--user-data-dir=D:\\temp\\tb260-chrome`, "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-gpu", "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
async function waitCdp() { for (let i = 0; i < 80; i++) { try { const r = await fetch(`http://127.0.0.1:${PORT}/json/version`); if (r.ok) return; } catch {} await sleep(250); } throw new Error("cdp down"); }
class Cdp {
  constructor(u) { this.url = u; this.id = 0; this.pending = new Map(); }
  async open() { this.ws = new WebSocket(this.url); await new Promise((res, rej) => { this.ws.addEventListener("open", () => res()); this.ws.addEventListener("error", () => rej(new Error("ws"))); }); this.ws.addEventListener("message", (ev) => { const m = JSON.parse(ev.data); if (m.id && this.pending.has(m.id)) { const p = this.pending.get(m.id); this.pending.delete(m.id); m.error ? p.rej(new Error(JSON.stringify(m.error))) : p.res(m.result); } }); }
  send(method, params = {}) { const id = ++this.id; return new Promise((res, rej) => { this.pending.set(id, { res, rej }); this.ws.send(JSON.stringify({ id, method, params })); setTimeout(() => { if (this.pending.has(id)) { this.pending.delete(id); rej(new Error("timeout " + method)); } }, 40000); }); }
  async eval(e) { const r = await this.send("Runtime.evaluate", { expression: e, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 400)); return r.result.value; }
}
const opsExpr = `(async () => {
  const wait = (ms) => new Promise(r => setTimeout(r, ms));
  for (let i = 0; i < 80; i++) {
    if (document.querySelector('[data-testid="admin-order-operations-view"]') &&
        (document.querySelector('[data-testid="admin-order-items-shipping"]') || document.querySelector('[data-testid="admin-order-operations-error"]') || document.querySelector('[data-testid="admin-order-operations-denied"]'))) break;
    await wait(250);
  }
  await wait(700);
  const main = document.querySelector('[data-testid="admin-order-operations-view"]');
  if (!main) return JSON.stringify({ missing: true });
  const txt = (e) => (e.textContent || '').trim();
  const cards = [...document.querySelectorAll('[data-testid^="admin-order-operations-target-"]')].filter(e => e.tagName === 'LI');
  const interactive = [...main.querySelectorAll('a,button')].filter(e => e.offsetParent !== null);
  const heights = interactive.map(e => Math.round(e.getBoundingClientRect().height));
  const body = txt(main);
  return JSON.stringify({
    url: location.href,
    dir: getComputedStyle(main).direction,
    innerWidth, docScrollWidth: document.documentElement.scrollWidth,
    state: document.querySelector('[data-testid="admin-order-operations-denied"]') ? 'denied' : document.querySelector('[data-testid="admin-order-operations-error"]') ? 'error' : 'ready',
    targetCards: cards.length,
    targetKeys: cards.map(c => (c.getAttribute('data-testid') || '').slice(-40)),
    targetTexts: cards.map(txt).slice(0, 2),
    nullPlan: cards.filter(c => txt(c).includes('موعد ارسال مشخص نشده')).length,
    financeHits: ['تسویه','کیف پول','بازگشت وجه','دریافت‌ها','سهم فروشنده','پرداخت به فروشنده'].filter(f => body.includes(f)),
    hasOperationalPanel: !!document.querySelector('[data-testid="admin-order-operational-delivery"], [data-testid="admin-order-operational-delivery-empty"]'),
    hasShippingPanel: !!document.querySelector('[data-testid="admin-order-items-shipping"]'),
    backHref: document.querySelector('[data-testid="admin-order-operations-back"]')?.getAttribute('href') ?? null,
    minAction: heights.length ? Math.min(...heights) : null,
    below44: heights.filter(h => h < 44).length,
  });
})()`;
(async () => {
  const out = { pages: [] };
  try {
    await waitCdp();
    const t = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: "PUT" })).json();
    const cdp = new Cdp(t.webSocketDebuggerUrl); await cdp.open();
    await cdp.send("Page.enable"); await cdp.send("Runtime.enable");
    const urls = [
      { name: "queue-row-locator", url: `${BASE}/fa/admin/orders/64677682-ccd0-43ce-b3c8-20b804a2e273/operations?fulfillmentId=01a1c5c2-ee27-7000-9809-a00147d07c82&from=queue&filter=dispatch_unspecified` },
      { name: "legacy-group-locator", url: `${BASE}/fa/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf/operations?from=queue&shippingGroupId=01a1b92db7e1700097a193f3bc3bcf0c&filter=dispatch_unspecified` },
    ];
    for (const u of urls) {
      for (const w of [1440, 1024, 390]) {
        await cdp.send("Emulation.setDeviceMetricsOverride", { width: w, height: 900, deviceScaleFactor: 1, mobile: false });
        await cdp.send("Page.navigate", { url: u.url });
        await sleep(3000);
        let m = JSON.parse(await cdp.eval(opsExpr));
        for (let i = 0; i < 8 && m.state !== "ready"; i++) { await sleep(1400); m = JSON.parse(await cdp.eval(opsExpr)); }
        out.pages.push({ name: u.name, width: w, metrics: m });
      }
    }
    await cdp.send("Emulation.clearDeviceMetricsOverride");
    out.ok = true;
  } catch (e) { out.ok = false; out.error = String(e && e.stack ? e.stack : e); }
  finally { try { writeFileSync(OUT, JSON.stringify(out, null, 2), "utf8"); } catch {} try { chrome.kill(); } catch {} console.log(JSON.stringify({ ok: out.ok, error: out.error ?? null, out: OUT })); process.exit(0); }
})();
