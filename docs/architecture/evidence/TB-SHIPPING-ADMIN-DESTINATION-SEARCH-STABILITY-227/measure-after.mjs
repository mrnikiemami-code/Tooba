// TB-227 step 2 — measure + screenshot the REPAIRED modal. Task-owned Chrome only (port 9335, fresh profile,
// target created here). Real API traffic, no interception.
import fs from "node:fs";
import path from "node:path";

const CDP = "http://127.0.0.1:9335";
const URL_EDIT =
  "http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit";
const OUT = "D:/Users/User/source/repos/SarvNewVer/docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227";
fs.mkdirSync(path.join(OUT, "screens"), { recursive: true });

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
console.log(JSON.stringify({ targetId, browser: version.Browser }));

const evaluate = async (expression) =>
  (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true }, sid)).result?.value;
const shot = async (name) => {
  const png = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, sid);
  fs.writeFileSync(path.join(OUT, "screens", name), Buffer.from(png.data, "base64"));
  return name;
};

const box = async (selector) =>
  evaluate(`(() => {
    const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { x: Math.round(r.x + r.width / 2), y: Math.round(r.y + r.height / 2), w: Math.round(r.width), h: Math.round(r.height) };
  })()`);

const click = async (selector, { scroll = false } = {}) => {
  if (scroll) await evaluate(`document.querySelector(${JSON.stringify(selector)})?.scrollIntoView({ block: "center" })`);
  if (scroll) await sleep(150);
  const b = await box(selector);
  if (!b || b.w === 0) return false;
  for (const type of ["mousePressed", "mouseReleased"]) {
    await send("Input.dispatchMouseEvent", { type, x: b.x, y: b.y, button: "left", clickCount: 1 }, sid);
  }
  await sleep(320);
  return true;
};

const key = async (k, code, vk) => {
  await send("Input.dispatchKeyEvent", { type: "rawKeyDown", key: k, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk }, sid);
  await send("Input.dispatchKeyEvent", { type: "keyUp", key: k, code, windowsVirtualKeyCode: vk }, sid);
  await sleep(80);
};
const typeChars = async (text) => {
  for (const ch of text) {
    await send("Input.dispatchKeyEvent", { type: "keyDown", text: ch, unmodifiedText: ch, key: ch }, sid);
    await send("Input.dispatchKeyEvent", { type: "keyUp", key: ch }, sid);
  }
  await sleep(150);
};

const START = `window.__tb227After = { samples: [], running: true };
(() => {
  const t0 = performance.now();
  const tick = () => {
    if (!window.__tb227After.running) return;
    const d = document.querySelector("dialog[open]");
    const i = d?.querySelector("div");
    const r = d?.getBoundingClientRect();
    window.__tb227After.samples.push({
      t: Math.round(performance.now() - t0),
      top: r ? Math.round(r.top) : null,
      h: r ? Math.round(r.height) : null,
      left: r ? Math.round(r.left) : null,
      w: r ? Math.round(r.width) : null,
      pageY: Math.round(document.scrollingElement.scrollTop),
      innerScrollTop: i ? Math.round(i.scrollTop) : null,
      active: document.activeElement?.getAttribute("data-testid") ?? document.activeElement?.tagName ?? null,
      status: document.querySelector('[data-testid="shipping-authoring-destination-status"]')?.textContent?.trim().slice(0, 40) ?? null,
      panels: document.querySelectorAll('[data-testid^="shipping-authoring-destination-"][data-testid$="-panel"]').length,
    });
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
})();`;
const STOP = `(() => { window.__tb227After.running = false; return window.__tb227After.samples; })()`;

const summarize = (samples) => {
  const tops = samples.map((s) => s.top).filter((v) => v !== null);
  const heights = samples.map((s) => s.h).filter((v) => v !== null);
  return {
    frames: samples.length,
    topDistinct: [...new Set(tops)],
    heightDistinct: [...new Set(heights)],
    topTravel: tops.length ? Math.max(...tops) - Math.min(...tops) : null,
    heightTravel: heights.length ? Math.max(...heights) - Math.min(...heights) : null,
    pageYDistinct: [...new Set(samples.map((s) => s.pageY))],
    innerScrollDistinct: [...new Set(samples.map((s) => s.innerScrollTop))],
    activeSequence: [...new Set(samples.map((s) => s.active))],
    statuses: [...new Set(samples.map((s) => s.status))],
  };
};

const state = () =>
  evaluate(`(() => {
    const d = document.querySelector("dialog[open]");
    const r = d?.getBoundingClientRect();
    const fields = ["country", "area", "city", "service-area"].map((name) => {
      const t = document.querySelector('[data-testid="shipping-authoring-destination-' + name + '"]');
      return { name, exists: !!t, disabled: t ? t.disabled : null, text: t ? t.textContent.trim() : null, expanded: t?.getAttribute("aria-expanded") ?? null };
    });
    const panel = document.querySelector('[data-testid^="shipping-authoring-destination-"][data-testid$="-panel"]');
    const pr = panel?.getBoundingClientRect();
    const search = panel?.querySelector('input');
    return {
      dialog: r ? { top: Math.round(r.top), left: Math.round(r.left), w: Math.round(r.width), h: Math.round(r.height) } : null,
      pageY: Math.round(document.scrollingElement.scrollTop),
      innerScrollTop: d?.querySelector("div") ? Math.round(d.querySelector("div").scrollTop) : null,
      fields,
      panel: pr ? { top: Math.round(pr.top), bottom: Math.round(pr.bottom), h: Math.round(pr.height), w: Math.round(pr.width), left: Math.round(pr.left), inViewport: pr.top >= 0 && pr.bottom <= window.innerHeight } : null,
      panelHasSearchInside: !!search,
      searchFocused: search ? document.activeElement === search : false,
      options: panel ? [...panel.querySelectorAll('[role="option"]')].map((o) => o.textContent.trim()) : [],
      count: panel?.querySelector('[data-testid$="-count"]')?.textContent?.trim() ?? null,
      empty: panel?.querySelector('[data-testid$="-empty"]')?.textContent?.trim() ?? null,
      status: document.querySelector('[data-testid="shipping-authoring-destination-status"]')?.textContent?.trim() ?? null,
      dialogOpen: !!d,
      viewport: { w: window.innerWidth, h: window.innerHeight },
      docScrollW: document.documentElement.scrollWidth,
      docClientW: document.documentElement.clientWidth,
    };
  })()`);

const receipts = { baseline: null, steps: [], shots: [] };

await send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
await send("Page.navigate", { url: URL_EDIT }, sid);
for (let i = 0; i < 80; i += 1) {
  await sleep(400);
  if (await evaluate(`!!document.querySelector('[data-testid="shipping-authoring-tab-regions"]')`)) break;
}
await click('[data-testid="shipping-authoring-tab-regions"]', { scroll: true });
await click('[data-testid="shipping-authoring-area-add"]', { scroll: true });
await sleep(700);
receipts.baseline = await state();
await evaluate(START);

/* 1 — country popup: search lives INSIDE the popup, no separate external input. */
await click('[data-testid="shipping-authoring-destination-country"]');
receipts.steps.push({ step: "country-popup-open", state: await state(), shots: [await shot("227-1440-country-popup.png")] });

/* 2 — a query that matches nothing must say so instead of showing a stale list. */
await typeChars("zz");
receipts.steps.push({ step: "country-popup-no-result", state: await state() });

/* 3 — clear the query with a real select-all + Backspace, then keyboard-select the first row. */
await send("Input.dispatchKeyEvent", { type: "rawKeyDown", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, modifiers: 2 }, sid);
await send("Input.dispatchKeyEvent", { type: "keyUp", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, modifiers: 2 }, sid);
await sleep(80);
await key("Backspace", "Backspace", 8);
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(900);
receipts.steps.push({ step: "country-keyboard-picked", state: await state() });

/* 4 — province popup: type a Persian query, filter, Enter picks the first match. */
await click('[data-testid="shipping-authoring-destination-area"]');
receipts.steps.push({ step: "province-popup-open", state: await state() });
await send("Input.insertText", { text: "ته" }, sid);
await sleep(250);
receipts.steps.push({ step: "province-filtered", state: await state() });
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(900);
receipts.steps.push({ step: "province-keyboard-picked", state: await state() });

/* 5 — city popup with the popup OPEN in the desktop screenshot, then pick. */
await click('[data-testid="shipping-authoring-destination-city"]');
receipts.steps.push({ step: "city-popup-open", state: await state(), shots: [await shot("227-1440-city-popup.png")] });
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(900);
receipts.steps.push({ step: "city-keyboard-picked", state: await state() });

/* 6 — Escape inside an OPEN dropdown closes only the dropdown, never the whole dialog. */
await click('[data-testid="shipping-authoring-destination-service-area"]');
const beforeEscape = await state();
await key("Escape", "Escape", 27);
await sleep(300);
receipts.steps.push({ step: "escape-in-popup", beforeEscape, state: await state() });

/* 7 — clear the country: ALL descendants are emptied and disabled immediately. */
await click('[data-testid="shipping-authoring-destination-country"]');
await click('[data-testid="shipping-authoring-destination-country-clear"]');
await sleep(700);
receipts.steps.push({ step: "country-cleared-descendants-reset", state: await state() });

/* 8 — the fourth stage is honestly empty in this installation (Tehran has no service areas). */
await click('[data-testid="shipping-authoring-destination-country"]');
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(1200);
await click('[data-testid="shipping-authoring-destination-area"]');
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(1200);
await click('[data-testid="shipping-authoring-destination-city"]');
await key("ArrowDown", "ArrowDown", 40);
await key("Enter", "Enter", 13);
await sleep(1200);
await click('[data-testid="shipping-authoring-destination-service-area"]');
await sleep(400);
receipts.steps.push({ step: "service-area-branch", state: await state(), shots: [await shot("227-1440-service-area-empty.png")] });
await key("Escape", "Escape", 27);
await sleep(300);

const desktopSamples = await evaluate(STOP);
receipts.desktop = summarize(desktopSamples);
receipts.shots.push(await shot("227-1440-modal-final.png"));

/* 9 — native 390 viewport: the popup must fit and scroll internally; the dialog must not move. */
await send("Emulation.setDeviceMetricsOverride", { width: 390, height: 844, deviceScaleFactor: 1, mobile: true }, sid);
await sleep(600);
await evaluate(START);
await click('[data-testid="shipping-authoring-destination-city"]');
receipts.steps.push({ step: "390-popup", state: await state(), shots: [await shot("227-390-popup.png")] });
await key("Escape", "Escape", 27);
await sleep(300);
receipts.mobile390 = summarize(await evaluate(STOP));

/* 10 — native 360 viewport with the popup open (fit check). */
await send("Emulation.setDeviceMetricsOverride", { width: 360, height: 780, deviceScaleFactor: 1, mobile: true }, sid);
await sleep(600);
await evaluate(START);
await click('[data-testid="shipping-authoring-destination-city"]');
receipts.steps.push({ step: "360-popup", state: await state(), shots: [await shot("227-360-popup.png")] });
await key("Escape", "Escape", 27);
await sleep(300);
receipts.mobile360 = summarize(await evaluate(STOP));

/* 11 — the destination dialog itself still closes on Escape when no dropdown is open. */
await key("Escape", "Escape", 27);
await sleep(400);
receipts.dialogClosedByEscape = await evaluate(`!document.querySelector("dialog[open]")`);

fs.writeFileSync(path.join(OUT, "measure-after.json"), JSON.stringify(receipts, null, 2));
console.log(JSON.stringify({ desktop: receipts.desktop, mobile390: receipts.mobile390, mobile360: receipts.mobile360, dialogClosedByEscape: receipts.dialogClosedByEscape, steps: receipts.steps.map((s) => ({ step: s.step, status: s.state?.status?.slice(0, 60), panel: s.state?.panel, count: s.state?.count, empty: s.state?.empty, options: s.state?.options, fields: s.state?.fields?.map((f) => `${f.name}:${f.disabled ? "X" : "o"}:${f.text}`), dialog: s.state?.dialog, pageY: s.state?.pageY })), shots: receipts.shots }, null, 2));
await send("Emulation.clearDeviceMetricsOverride", {}, sid);
ws.close();
