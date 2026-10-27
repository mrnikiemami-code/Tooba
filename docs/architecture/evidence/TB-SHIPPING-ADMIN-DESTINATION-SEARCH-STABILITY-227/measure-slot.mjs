// TB-227 — how tall must the reserved status slot be? Measured from the REAL localized strings at each
// viewport by injecting each message into the existing slot (DOM-only measurement, no source change).
const CDP = "http://127.0.0.1:9335";
const URL_EDIT =
  "http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const version = JSON.parse(await (await fetch(`${CDP}/json/version`)).text());
const ws = new WebSocket(version.webSocketDebuggerUrl);
await new Promise((res) => ws.addEventListener("open", res, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (e) => {
  const m = JSON.parse(e.data);
  if (m.id && pending.has(m.id)) {
    pending.get(m.id)(m.result ?? {});
    pending.delete(m.id);
  }
});
const send = (method, params = {}, sessionId) =>
  new Promise((r) => {
    const n = ++id;
    pending.set(n, r);
    ws.send(JSON.stringify({ id: n, method, params, ...(sessionId ? { sessionId } : {}) }));
  });
const targetId = (await send("Target.createTarget", { url: "about:blank" })).targetId;
const sid = (await send("Target.attachToTarget", { targetId, flatten: true })).sessionId;
await send("Page.enable", {}, sid);
await send("Runtime.enable", {}, sid);
const ev = async (expression) => (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true }, sid)).result?.value;
const click = async (sel) => {
  const b = await ev(`(() => { const el = document.querySelector(${JSON.stringify(sel)}); if(!el) return null; const r = el.getBoundingClientRect(); return {x: Math.round(r.x+r.width/2), y: Math.round(r.y+r.height/2)}; })()`);
  if (!b) return "missing";
  for (const type of ["mousePressed", "mouseReleased"]) await send("Input.dispatchMouseEvent", { type, x: b.x, y: b.y, button: "left", clickCount: 1 }, sid);
  await sleep(350);
  return b;
};

const MEASURE = (messages) => `(() => {
  const d = document.querySelector("dialog[open]");
  const dialogRect = d?.getBoundingClientRect();
  const slot = document.querySelector('[data-testid="shipping-authoring-destination-status"]');
  const pathLine = slot?.querySelector('[data-testid="shipping-authoring-destination-path"]');
  const statusLine = slot ? [...slot.children].filter((c) => c !== pathLine)[0] : null;
  const messages = ${JSON.stringify(messages)};
  const out = {};
  const write = (path, text) => {
    if (pathLine) pathLine.style.display = path ? "" : "none";
    if (text === null) { if (statusLine) statusLine.style.display = "none"; return; }
    if (statusLine) { statusLine.style.display = ""; statusLine.textContent = text; }
  };
  const slotH = () => Math.round(slot.getBoundingClientRect().height);
  const dialogH = () => Math.round(document.querySelector("dialog[open]").getBoundingClientRect().height);
  write(true, null); out.pathOnly = { slot: slotH(), dialog: dialogH() };
  write(false, null); out.empty = { slot: slotH(), dialog: dialogH() };
  for (const [key, text] of Object.entries(messages)) {
    write(true, text); out["path+" + key] = { slot: slotH(), dialog: dialogH() };
    write(false, text); out[key] = { slot: slotH(), dialog: dialogH() };
  }
  return { viewport: { w: window.innerWidth, h: window.innerHeight }, slotMinHeight: slot ? getComputedStyle(slot).minHeight : null, out };
})()`;

await send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
await send("Page.navigate", { url: URL_EDIT }, sid);
for (let i = 0; i < 80; i += 1) {
  await sleep(400);
  if (await ev(`!!document.querySelector('[data-testid="shipping-authoring-tab-regions"]')`)) break;
}
await ev(`document.querySelector('[data-testid="shipping-authoring-tab-regions"]')?.scrollIntoView({block:"center"})`);
await sleep(200);
await click('[data-testid="shipping-authoring-tab-regions"]');
await ev(`document.querySelector('[data-testid="shipping-authoring-area-add"]')?.scrollIntoView({block:"center"})`);
await sleep(200);
await click('[data-testid="shipping-authoring-area-add"]');
await sleep(900);
await click('[data-testid="shipping-authoring-destination-country"]');
const optionId = await ev(`document.querySelector('[data-testid^="shipping-authoring-destination-country-option-"]')?.getAttribute("data-testid")`);
await click(`[data-testid="${optionId}"]`);
await sleep(900);

const messages = {
  loading: "در حال خواندن…",
  deepest: "برای این شاخه سطح عمیق‌تری ثبت نشده؛ همان سطح انتخاب‌شده افزوده می‌شود.",
  added: "منطقه افزوده شد؛ می‌توانید ناحیهٔ بعدی را انتخاب کنید.",
  noArea:
    "برای این کشور استان ثبت نشده است؛ در این نصب نمی‌توان استان/شهر را انتخاب کرد. این یک شکاف بازنمایی داده است، نه نبود مقصد.",
  noCity: "برای این استان/کشور شهری ثبت نشده است.",
  noServiceArea: "برای این شهر ناحیه‌ای ثبت نشده است.",
  denied: "اجازهٔ خواندن جغرافیا را ندارید.",
  network: "ارتباط با جغرافیا برقرار نشد.",
  untrusted: "پاسخ جغرافیا قابل اعتماد نبود و پذیرفته نشد.",
};
const report = { slotMinHeightInline: "3.25rem (52px)", viewports: [] };
for (const [w, h] of [[1440, 900], [390, 844], [360, 780]]) {
  await send("Emulation.setDeviceMetricsOverride", { width: w, height: h, deviceScaleFactor: 1, mobile: w < 800 }, sid);
  await sleep(700);
  report.viewports.push(await ev(MEASURE(messages)));
}
console.log(JSON.stringify(report, null, 1));
await send("Emulation.clearDeviceMetricsOverride", {}, sid);
ws.close();
