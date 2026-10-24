// TB-209 probe: mobile 390px check of the repaired General + Regions sections (real traffic).
// Emulation is used ONLY for layout measurement; nothing about the responses is faked.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = "D:\\temp\\tb209-mobile";
const PORT = 9561;
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const URL_ARG = process.argv[2];
fs.mkdirSync(ROOT, { recursive: true });
const child = spawn(
  CHROME,
  ["--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=${path.join(ROOT, "profile")}`, "--no-first-run", "--disable-extensions", "--window-size=390,844", "about:blank"],
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
async function shot(name) {
  const png = await send("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(ROOT, name), Buffer.from(png.data, "base64"));
}
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
  await send("Emulation.setDeviceMetricsOverride", { width: 390, height: 844, deviceScaleFactor: 2, mobile: true });
  await send("Page.navigate", { url: URL_ARG });
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-edit"]')`);
  await sleep(1200);
  const overflow = () =>
    evaluate(`(() => {
      const root = document.querySelector('[data-testid="shipping-authoring-screen"]');
      const wide = Array.from(root.querySelectorAll('*')).filter((n) => n.getBoundingClientRect().width > 390.5).map((n) => n.getAttribute('data-testid') || n.tagName).slice(0, 8);
      return { scrollWidth: document.documentElement.scrollWidth, viewport: 390, overflowing: wide };
    })()`);
  const general = await overflow();
  await shot("mobile-general.png");
  await evaluate(`document.querySelector('[data-testid="shipping-authoring-tab-regions"]').click()`);
  await sleep(900);
  const regions = await overflow();
  await shot("mobile-regions.png");
  await send("Emulation.clearDeviceMetricsOverride");
  console.log(JSON.stringify({ general, regions }, null, 2));
}
main()
  .catch((e) => console.log(JSON.stringify({ error: String(e) })))
  .finally(() => { try { child.kill(); } catch {} setTimeout(() => process.exit(0), 300); });
