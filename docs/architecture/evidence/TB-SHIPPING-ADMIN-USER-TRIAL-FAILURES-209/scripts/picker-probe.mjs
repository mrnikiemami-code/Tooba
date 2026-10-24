// TB-209 probe: reads the picker's own state AFTER a successful Add, so the evidence states whether
// the selector is cleared deliberately or keeps the chosen branch. Real traffic, no interception.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = "D:\\temp\\tb209-probe";
const PORT = 9559;
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const URL_ARG = process.argv[2];
fs.mkdirSync(ROOT, { recursive: true });
const child = spawn(
  CHROME,
  ["--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=${path.join(ROOT, "profile")}`, "--no-first-run", "--disable-extensions", "--window-size=1440,1000", "about:blank"],
  { stdio: "ignore" },
);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let ws;
let seq = 0;
const pending = new Map();
function send(method, params = {}) {
  const id = ++seq;
  ws.send(JSON.stringify({ id, method, params }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}
async function evaluate(expression) {
  const r = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
  if (r.exceptionDetails) throw new Error(r.exceptionDetails.text);
  return r.result?.value;
}
const setter = (testId, value) => `(() => { const el = document.querySelector('[data-testid="${testId}"]'); if (!el) return "missing"; const p = el instanceof HTMLSelectElement ? HTMLSelectElement.prototype : HTMLInputElement.prototype; Object.getOwnPropertyDescriptor(p, "value").set.call(el, ${JSON.stringify(value)}); el.dispatchEvent(new Event(el instanceof HTMLSelectElement ? "change" : "input", { bubbles: true })); return "ok"; })()`;
const click = (testId) => `(() => { const el = document.querySelector('[data-testid="${testId}"]'); if (!el) return "missing"; el.click(); return "ok"; })()`;
async function waitFor(expr) {
  for (let i = 0; i < 80; i++) {
    if (await evaluate(`Boolean(${expr})`)) return;
    await sleep(200);
  }
  throw new Error("timeout");
}
async function main() {
  for (let i = 0; i < 80; i++) {
    try {
      if ((await fetch(`http://127.0.0.1:${PORT}/json/version`)).ok) break;
    } catch {}
    await sleep(250);
  }
  const targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
  ws = new WebSocket(targets.find((t) => t.type === "page").webSocketDebuggerUrl);
  await new Promise((r, j) => { ws.onopen = r; ws.onerror = j; });
  ws.onmessage = (event) => {
    const m = JSON.parse(event.data);
    if (m.id && pending.has(m.id)) {
      const p = pending.get(m.id);
      pending.delete(m.id);
      m.error ? p.reject(new Error(m.error.message)) : p.resolve(m.result);
    }
  };
  await send("Page.enable");
  await send("Runtime.enable");
  await send("Page.navigate", { url: URL_ARG });
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-edit"]')`);
  await evaluate(click("shipping-authoring-edit"));
  await sleep(600);
  await evaluate(click("shipping-authoring-tab-regions"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-group-add"]')`);
  await evaluate(click("shipping-authoring-group-add"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-destination-country"]')`);
  await sleep(900);
  const country = await evaluate(`(() => { const s = document.querySelector('[data-testid="shipping-authoring-destination-country"]'); const o = Array.from(s.options).find((x) => x.value); return o ? o.value : ""; })()`);
  await evaluate(setter("shipping-authoring-destination-country", country));
  await sleep(700);
  const beforeAdd = await evaluate(`(() => { const s = document.querySelector('[data-testid="shipping-authoring-destination-country"]'); return { value: s.value, text: s.selectedOptions[0]?.textContent, cityDisabled: document.querySelector('[data-testid="shipping-authoring-destination-city"]').disabled }; })()`);
  await evaluate(click("shipping-authoring-destination-add"));
  await sleep(400);
  const afterAdd = await evaluate(`(() => { const s = document.querySelector('[data-testid="shipping-authoring-destination-country"]'); return { value: s.value, text: s.selectedOptions[0]?.textContent, addDisabled: document.querySelector('[data-testid="shipping-authoring-destination-add"]').disabled, chips: Array.from(document.querySelectorAll('[data-testid^="shipping-authoring-area-"]')).map((n) => n.innerText), placeholder: document.querySelector('[data-testid="shipping-authoring-destination-city"]')?.disabled }; })()`);
  const shot = await send("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(ROOT, "picker-after-add.png"), Buffer.from(shot.data, "base64"));
  console.log(JSON.stringify({ country, beforeAdd, afterAdd }, null, 2));
}
main()
  .catch((e) => console.log(JSON.stringify({ error: String(e) })))
  .finally(() => { try { child.kill(); } catch {} setTimeout(() => process.exit(0), 300); });
