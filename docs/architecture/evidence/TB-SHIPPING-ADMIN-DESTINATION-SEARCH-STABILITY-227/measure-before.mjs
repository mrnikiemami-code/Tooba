// TB-227 step 1 — REPRODUCE and MEASURE the destination-modal shake in the CURRENT source.
// Task-owned Chrome only (fresh profile, port 9335, target created here). Real API traffic.
import fs from "node:fs";
import path from "node:path";

const CDP = "http://127.0.0.1:9335";
const URL_EDIT =
  "http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit";
const OUT = "D:/Users/User/source/repos/SarvNewVer/docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227";
fs.mkdirSync(path.join(OUT, "before"), { recursive: true });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const version = JSON.parse(await (await fetch(`${CDP}/json/version`)).text());
const ws = new WebSocket(version.webSocketDebuggerUrl);
await new Promise((res) => ws.addEventListener("open", res, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (event) => {
  const m = JSON.parse(event.data);
  if (m.id && pending.has(m.id)) {
    pending.get(m.id)(m.result ?? { error: m.error });
    pending.delete(m.id);
  }
});
const send = (method, params = {}, sessionId) =>
  new Promise((resolve) => {
    const n = ++id;
    pending.set(n, resolve);
    ws.send(JSON.stringify({ id: n, method, params, ...(sessionId ? { sessionId } : {}) }));
  });

const targetId = (await send("Target.createTarget", { url: "about:blank" })).targetId;
const sid = (await send("Target.attachToTarget", { targetId, flatten: true })).sessionId;
await send("Page.enable", {}, sid);
await send("Runtime.enable", {}, sid);
await send("Network.enable", {}, sid);
await send("Network.clearBrowserCookies", {}, sid);
console.log(JSON.stringify({ targetId, browser: version.Browser }));

const evaluate = async (expression) =>
  (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true }, sid)).result?.value;

const click = async (testId) => {
  const box = await evaluate(`(() => {
    const el = document.querySelector('[data-testid="${testId}"]');
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    return { x: Math.round(r.x + r.width / 2), y: Math.round(r.y + r.height / 2), w: Math.round(r.width) };
  })()`);
  if (!box || box.w === 0) return { testId, clicked: false };
  await sleep(200);
  for (const type of ["mousePressed", "mouseReleased"]) {
    await send("Input.dispatchMouseEvent", { type, x: box.x, y: box.y, button: "left", clickCount: 1 }, sid);
  }
  await sleep(350);
  return { testId, clicked: true, box };
};

const key = async (k, code, vk) => {
  await send("Input.dispatchKeyEvent", { type: "rawKeyDown", key: k, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk }, sid);
  await send("Input.dispatchKeyEvent", { type: "keyUp", key: k, code, windowsVirtualKeyCode: vk }, sid);
  await sleep(90);
};

/** Frame-accurate sampler of the dialog geometry, page/internal scroll and focus. */
const START = `window.__tb227 = { samples: [], running: true };
window.__tb227Start = () => {
  const dialog = document.querySelector("dialog[open]");
  const inner = dialog?.querySelector("div");
  const t0 = performance.now();
  const tick = () => {
    if (!window.__tb227.running) return;
    const d = document.querySelector("dialog[open]");
    const i = d?.querySelector("div");
    const r = d?.getBoundingClientRect();
    window.__tb227.samples.push({
      t: Math.round(performance.now() - t0),
      top: r ? Math.round(r.top) : null,
      h: r ? Math.round(r.height) : null,
      left: r ? Math.round(r.left) : null,
      w: r ? Math.round(r.width) : null,
      pageY: Math.round(document.scrollingElement.scrollTop),
      innerScrollTop: i ? Math.round(i.scrollTop) : null,
      innerScrollH: i ? Math.round(i.scrollHeight) : null,
      innerClientH: i ? Math.round(i.clientHeight) : null,
      active: document.activeElement?.getAttribute("data-testid") ?? document.activeElement?.tagName ?? null,
      rows: document.querySelectorAll('[data-testid="shipping-authoring-destination-path"], [data-testid="shipping-authoring-destination-branch-note"], [data-testid="shipping-authoring-destination-deepest"]').length,
    });
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
};`;

const STOP = `(() => { window.__tb227.running = false; return window.__tb227.samples; })()`;

const window4 = (samples) => {
  if (!samples.length) return null;
  const tops = samples.map((s) => s.top).filter((v) => v !== null);
  const heights = samples.map((s) => s.h).filter((v) => v !== null);
  const distinctTops = [...new Set(tops)];
  return {
    frames: samples.length,
    topMin: Math.min(...tops),
    topMax: Math.max(...tops),
    travel: Math.max(...tops) - Math.min(...tops),
    distinctTops,
    heightMin: Math.min(...heights),
    heightMax: Math.max(...heights),
    heightTravel: Math.max(...heights) - Math.min(...heights),
    pageYDistinct: [...new Set(samples.map((s) => s.pageY))],
    innerScrollDistinct: [...new Set(samples.map((s) => s.innerScrollTop))],
    activeDistinct: [...new Set(samples.map((s) => s.active))],
  };
};

await send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
await send("Page.navigate", { url: URL_EDIT }, sid);
for (let i = 0; i < 80; i += 1) {
  await sleep(400);
  if (await evaluate(`!!document.querySelector('[data-testid="shipping-authoring-tab-regions"]')`)) break;
}
await evaluate(START);
await click("shipping-authoring-tab-regions");
await click("shipping-authoring-area-add");
await sleep(600);

const baseline = await evaluate(`(() => {
  const d = document.querySelector("dialog[open]");
  const r = d?.getBoundingClientRect();
  const selects = [...document.querySelectorAll('[data-testid^="shipping-authoring-destination-"]')]
    .filter((e) => e.tagName === "SELECT")
    .map((s) => ({ testid: s.getAttribute("data-testid"), options: s.options.length, disabled: s.disabled, value: s.value }));
  return { rect: r ? { top: Math.round(r.top), left: Math.round(r.left), w: Math.round(r.width), h: Math.round(r.height) } : null, selects, pageY: Math.round(document.scrollingElement.scrollTop), active: document.activeElement?.tagName };
})()`);
console.log("baseline", JSON.stringify(baseline));

const shot1 = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, sid);
fs.writeFileSync(path.join(OUT, "before", "227-before-1440-modal.png"), Buffer.from(shot1.data, "base64"));

/** One branch step: reset the sampler, perform the choice with real keys, collect the trace. */
async function step(label, testId, downs) {
  await evaluate(`window.__tb227.samples = []; window.__tb227.running = true; window.__tb227Start();`);
  await click(testId);
  const focused = await evaluate(`document.activeElement?.getAttribute("data-testid") ?? null`);
  for (let i = 0; i < downs; i += 1) await key("ArrowDown", "ArrowDown", 40);
  await key("Enter", "Enter", 13);
  await sleep(1400);
  const samples = await evaluate(STOP);
  const summary = window4(samples);
  const after = await evaluate(`(() => {
    const d = document.querySelector("dialog[open]");
    const r = d?.getBoundingClientRect();
    const selects = [...document.querySelectorAll('[data-testid^="shipping-authoring-destination-"]')]
      .filter((e) => e.tagName === "SELECT")
      .map((s) => ({ testid: s.getAttribute("data-testid"), options: s.options.length, disabled: s.disabled, value: s.value }));
    const inner = d?.querySelector("div");
    return { rect: r ? { top: Math.round(r.top), h: Math.round(r.height) } : null, selects, pageY: Math.round(document.scrollingElement.scrollTop), innerScrollTop: inner ? Math.round(inner.scrollTop) : null, innerScrollH: inner ? Math.round(inner.scrollHeight) : null, innerClientH: inner ? Math.round(inner.clientHeight) : null };
  })()`);
  console.log(`STEP ${label}`, JSON.stringify({ testId, focused, downs, before: summary, after }));
  return { label, testId, focused, downs, summary, after };
}

const steps = [];
steps.push(await step("country-first-option", "shipping-authoring-destination-country", 1));
const shot2 = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, sid);
fs.writeFileSync(path.join(OUT, "before", "227-before-1440-country.png"), Buffer.from(shot2.data, "base64"));
steps.push(await step("province-first-option", "shipping-authoring-destination-area", 1));
steps.push(await step("city-first-option", "shipping-authoring-destination-city", 1));
const shot3 = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, sid);
fs.writeFileSync(path.join(OUT, "before", "227-before-1440-city.png"), Buffer.from(shot3.data, "base64"));

fs.writeFileSync(path.join(OUT, "before", "measure-before.json"), JSON.stringify({ baseline, steps }, null, 2));
await send("Emulation.clearDeviceMetricsOverride", {}, sid);
ws.close();
