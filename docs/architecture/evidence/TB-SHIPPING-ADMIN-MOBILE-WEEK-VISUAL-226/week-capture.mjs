// TB-226 final pass: one Closed text everywhere (desktop dash in the bound columns), re-captured.
import fs from "node:fs";
import path from "node:path";

const CDP = "http://127.0.0.1:9334";
const FA = "http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496";
const EN = "http://127.0.0.1:3000/en/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496";
const OUT = "D:/Users/User/source/repos/SarvNewVer/docs/architecture/evidence/TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226";
const SCREENS = path.join(OUT, "screens");

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

const receipts = [];
const record = (name, value) => {
  receipts.push({ name, ...value });
  console.log(name, JSON.stringify(value));
};

const targetId = (await send("Target.createTarget", { url: "about:blank" })).targetId;
const sid = (await send("Target.attachToTarget", { targetId, flatten: true })).sessionId;
await send("Page.enable", {}, sid);
await send("Runtime.enable", {}, sid);
await send("Network.enable", {}, sid);
record("target", { targetId, browser: version.Browser, port: 9334, profile: "task-owned fresh profile" });

const evaluate = async (expression) =>
  (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true }, sid)).result?.value;

const isMobile = (w) => w < 640;
async function open(width, height, url, mobile = isMobile(width)) {
  await send("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile }, sid);
  await send("Page.navigate", { url }, sid);
  for (let i = 0; i < 60; i += 1) {
    await sleep(400);
    if (await evaluate(`!!document.querySelector('[data-testid="shipping-authoring-week"]')`)) break;
  }
  await evaluate(`document.querySelector('[data-testid="shipping-authoring-week"]').scrollIntoView({ block: "start" })`);
  await sleep(700);
  return evaluate(`({ lang: document.documentElement.lang, dir: document.documentElement.getAttribute("dir") })`);
}

async function capture(name) {
  const shot = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false, fromSurface: true }, sid);
  const file = path.join(SCREENS, name);
  fs.writeFileSync(file, Buffer.from(shot.data, "base64"));
  const buffer = fs.readFileSync(file);
  record(`png:${name}`, {
    bytes: buffer.length,
    width: buffer.readUInt32BE(16),
    height: buffer.readUInt32BE(20),
    viewport: await evaluate(`({ w: innerWidth, h: innerHeight, scrollWidth: document.documentElement.scrollWidth, lang: document.documentElement.lang, dir: document.documentElement.getAttribute("dir") })`),
  });
}

const audit = (closedWord) =>
  evaluate(`(() => {
    const rows = [...document.querySelectorAll('[data-testid="shipping-authoring-week-days"] li')].map((li) => ({
      testid: li.getAttribute("data-testid"),
      h: Math.round(li.getBoundingClientRect().height),
      text: li.innerText.replace(/\\s+/g, " ").trim(),
      closedCount: li.innerText.split("${closedWord}").length - 1,
      dash: (li.innerText.match(/—/g) ?? []).length,
      range: li.querySelector('[data-testid^="shipping-authoring-week-range-"]')?.innerText.replace(/\\s+/g, " ").trim() ?? null,
      timeInputs: li.querySelectorAll('input[type="time"]').length,
      checkbox: !!li.querySelector('input[type="checkbox"]'),
    }));
    return { rows, overflowX: document.documentElement.scrollWidth > innerWidth, dir: document.documentElement.getAttribute("dir"), lang: document.documentElement.lang };
  })()`);

const clearCookies = () => send("Network.clearBrowserCookies", {}, sid);

/* FA / RTL 390 VIEW + EDIT */
await clearCookies();
record("fa390view", await open(390, 844, `${FA}?mode=view`));
record("fa390view:audit", await audit("تعطیل"));
await capture("226-390-view.png");
record("fa390edit", await open(390, 844, `${FA}?mode=edit`));
record("fa390edit:audit", await audit("تعطیل"));
await capture("226-390-edit.png");
await evaluate(`document.querySelector('[data-testid="shipping-authoring-week-day-1"] input[type="checkbox"]').click()`);
await sleep(400);
await capture("226-390-edit-day1-closed.png");
await evaluate(`document.querySelector('[data-testid="shipping-authoring-week-day-1"] input[type="checkbox"]').click()`);
await sleep(400);
record("fa390edit:roundtrip", await evaluate(`(() => {
  const li = document.querySelector('[data-testid="shipping-authoring-week-day-1"]');
  return { times: [...li.querySelectorAll('input[type="time"]')].map((i) => i.value), applyDisabled: !!document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled };
})()`));

/* FA / RTL 360 VIEW + EDIT */
await open(360, 780, `${FA}?mode=view`);
record("fa360view:audit", await audit("تعطیل"));
await capture("226-360-view.png");
await open(360, 780, `${FA}?mode=edit`);
await capture("226-360-edit.png");

/* EN / LTR 390 VIEW + EDIT */
await clearCookies();
record("en390view", await open(390, 844, `${EN}?mode=view`));
record("en390view:audit", await audit("Closed"));
await capture("226-390-view-en.png");
record("en390edit", await open(390, 844, `${EN}?mode=edit`));
record("en390edit:audit", await audit("Closed"));
await capture("226-390-edit-en.png");
await clearCookies();

/* Desktop 1440 VIEW: same shared column contract, ONE Closed text, dash in the bound columns */
await open(1440, 900, `${FA}?mode=view`, false);
await evaluate(`document.querySelector('[data-testid="shipping-authoring-week"]').scrollIntoView({ block: "center" })`);
await sleep(500);
record("view1440:audit", await evaluate(`(() => {
  const head = document.querySelector('[data-testid="shipping-authoring-week-head"]');
  const cells = [...head.querySelectorAll("span")].map((s) => { const r = s.getBoundingClientRect(); return { text: s.textContent, x: Math.round(r.x), w: Math.round(r.width) }; });
  const rows = [...document.querySelectorAll('[data-testid="shipping-authoring-week-days"] li')].map((li) => {
    const spans = [...li.querySelectorAll(":scope > span")].map((s) => ({ t: s.textContent.trim(), x: Math.round(s.getBoundingClientRect().x), w: Math.round(s.getBoundingClientRect().width) }));
    return { text: li.innerText.replace(/\\s+/g, " ").trim(), closedCount: li.innerText.split("تعطیل").length - 1, h: Math.round(li.getBoundingClientRect().height), spans };
  });
  return { cells, rows, overflowX: document.documentElement.scrollWidth > innerWidth };
})()`));
await capture("226-1440-view.png");

await send("Emulation.clearDeviceMetricsOverride", {}, sid);
fs.writeFileSync(path.join(OUT, "mount-receipts.json"), JSON.stringify(receipts, null, 2));
ws.close();
