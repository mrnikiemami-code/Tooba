// TB-205-R2 harness: the committed 205-R1 CDP harness (finite timeouts, /v1-only fail-closed
// interception) wired to the Architect-prepared save scenario. No generic mock engine.
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, existsSync, mkdirSync, rmSync } from "node:fs";
import {
  initialResponseText, savedResponseText, assertScenarioRequest, IDS as SIDS
} from "../../TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2/scripts/save-scenario.mjs";
import { routes, typesEnvelope, devContextEnvelope, capabilitiesEnvelope } from "../../TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1/scripts/fixtures.mjs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const ROOT = process.env.TB_ROOT ?? "D:\\temp\\tb205-r3";
const PROFILE = `${ROOT}\\chrome-profile`;
const SHOTS = `${ROOT}\\shots`;
const CMD = `${ROOT}\\cmd.json`;
const RES = `${ROOT}\\result.json`;
const PORT = 9555;

mkdirSync(SHOTS, { recursive: true });
try { rmSync(CMD, { force: true }); } catch {}
try { rmSync(RES, { force: true }); } catch {}

const draftId = process.argv[2] ?? SIDS.draft;
const url = `http://127.0.0.1:3517/admin/shipping-services/authoring/${draftId}?mode=edit`;

const chrome = spawn(CHROME, [
  "--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=${PROFILE}`,
  "--no-first-run", "--no-default-browser-check", "--disable-gpu", "--window-size=1440,900",
  "--lang=fa-IR", "about:blank"
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function version() {
  for (let i = 0; i < 80; i++) {
    try { return await (await fetch(`http://127.0.0.1:${PORT}/json/version`)).json(); }
    catch { await sleep(250); }
  }
  throw new Error("chrome-cdp-unavailable");
}

const v = await version();
const ws = new WebSocket(v.webSocketDebuggerUrl);
await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });

let seq = 0;
const pending = new Map();
const netlog = [];
let savedPhase = false;
let putCount = 0, putBody = null, putAssert = null, heldPut = null;
const events = [];

ws.onmessage = (msg) => {
  const data = JSON.parse(msg.data);
  if (data.id && pending.has(data.id)) {
    const { resolve } = pending.get(data.id);
    pending.delete(data.id);
    resolve(data);
    return;
  }
  if (data.method === "Fetch.requestPaused") handlePaused(data.sessionId, data.params).catch((e) => {
    netlog.push({ url: data.params?.request?.url, result: `HANDLER_ERROR:${String(e?.message ?? e)}` });
  });
  else if (data.method === "Runtime.consoleAPICalled" || data.method === "Runtime.exceptionThrown" || data.method === "Log.entryAdded")
    events.push({ method: data.method, params: data.params });
};

function send(method, params, sessionId, timeoutMs = 15000) {
  const id = ++seq;
  return new Promise((resolve) => {
    const timer = setTimeout(() => { pending.delete(id); resolve({ __timeout: true, __method: method }); }, timeoutMs);
    pending.set(id, { resolve: (data) => { clearTimeout(timer); resolve(data); } });
    ws.send(JSON.stringify({ id, method, params, sessionId }));
  });
}

async function cmd(method, params, sessionId, timeoutMs) {
  const r = await send(method, params, sessionId, timeoutMs);
  if (r.__timeout) return { error: `timeout:${method}` };
  if (r.error) return { error: `protocol:${method}:${JSON.stringify(r.error)}` };
  return { result: r.result };
}

const COUNTRY2 = "11111111-1111-4111-8111-222222222222";

function routeFor(u) {
  if (!u.includes("/v1/")) return null;
  if (u.includes("/editor-document")) return { kind: "editor" };
  if (u.includes("/authoring-types")) return { kind: "types", body: typesEnvelope };
  if (u.includes("/admin/dev-context")) return { kind: "devContext", body: devContextEnvelope };
  if (u.includes("/access-control/me/capabilities")) return { kind: "capabilities", body: capabilitiesEnvelope };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/administrative-areas/.test(u))
    return { kind: "areas", body: u.toLowerCase().includes(COUNTRY2) ? "[]" : routes.areas };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/cities/.test(u)) {
    if (u.toLowerCase().includes(COUNTRY2)) return { kind: "citiesEmpty", body: "[]" };
    const filtered = /administrativeAreaId=/.test(u);
    return { kind: filtered ? "citiesFiltered" : "citiesCountryWide", body: filtered ? routes.citiesFiltered : routes.citiesCountryWide };
  }
  if (/\/v1\/admin\/geography\/cities\/[0-9a-f-]+\/service-areas/.test(u)) return { kind: "serviceAreas", body: routes.serviceAreas };
  if (/\/v1\/admin\/geography\/countries(\?|$)/.test(u)) return { kind: "countries", body: routes.countries };
  return { kind: "unhandled" };
}

const fulf = async (sessionId, requestId, code, body, ctype = "application/json") =>
  cmd("Fetch.fulfillRequest", {
    requestId, responseCode: code,
    responseHeaders: [{ name: "Content-Type", value: ctype }],
    body: Buffer.from(body).toString("base64")
  }, sessionId);

async function handlePaused(sessionId, p) {
  const u = p.request?.url ?? "";
  const method = p.request?.method ?? "GET";
  const hit = routeFor(u);
  if (method === "OPTIONS") return cmd("Fetch.fulfillRequest", { requestId: p.requestId, responseCode: 204, responseHeaders: [] }, sessionId);
  if (!hit || hit.kind === "unhandled") {
    netlog.push({ url: u, method, result: "BLOCKED_LOCAL" });
    return cmd("Fetch.failRequest", { requestId: p.requestId, errorReason: "Failed" }, sessionId);
  }
  if (hit.kind === "editor" && method === "PUT") {
    putCount++;
    putBody = p.request.postData ?? "";
    let assertion;
    try { assertScenarioRequest(putBody); assertion = { ok: true }; }
    catch (e) { assertion = { ok: false, error: String(e?.message ?? e) }; }
    putAssert = assertion;
    netlog.push({ url: u, method, result: `PUT#${putCount}`, bodyLength: putBody.length, assertionOk: assertion.ok });
    // HOLD the one paused PUT so a pending duplicate click can be measured.
    heldPut = { sessionId, requestId: p.requestId };
    return;
  }
  if (hit.kind === "editor" && method === "GET") {
    netlog.push({ url: u, method, result: savedPhase ? "GET+saved" : "GET+initial" });
    return fulf(sessionId, p.requestId, 200, savedPhase ? savedResponseText : initialResponseText);
  }
  netlog.push({ url: u, method, result: `FULFILLED:${hit.kind}` });
  return fulf(sessionId, p.requestId, 200, hit.body);
}

const t = await cmd("Target.createTarget", { url: "about:blank" });
if (t.error) throw new Error(t.error);
const att = await cmd("Target.attachToTarget", { targetId: t.result.targetId, flatten: true });
const session = att.result.sessionId;

await cmd("Page.enable", {}, session);
await cmd("Runtime.enable", {}, session);
await cmd("Log.enable", {}, session);
await cmd("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false }, session);
await cmd("Fetch.enable", { patterns: [{ urlPattern: "*/v1/*", requestStage: "Request" }] }, session);
await cmd("Page.navigate", { url }, session);
await sleep(5000);

async function evaluate(expr) {
  const r = await cmd("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true }, session, 20000);
  if (r.error) return { error: r.error };
  const res = r.result;
  if (res.exceptionDetails) return { error: "exception", detail: res.exceptionDetails.exception?.description ?? res.exceptionDetails.text };
  return { value: res.result?.value };
}

/** Real mouse click on the LAST visible element whose text starts with `text`. */
async function clickText(text, exact = false) {
  const q = await evaluate(`(() => {
    const want = ${JSON.stringify(text)};
    const els = [...document.querySelectorAll("button,a,[role=tab],[role=button],label,li,span,div")]
      .filter(e => {
        const s = (e.innerText || e.textContent || "").trim();
        return (${exact} ? s === want : s.startsWith(want)) && e.offsetParent !== null;
      })
      .filter(e => !(e.querySelector && e.querySelector("button") && e.tagName !== "BUTTON"));
    const el = els[els.length - 1];
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    if (r.width === 0 || r.height === 0) return null;
    return { x: r.x + r.width / 2, y: r.y + r.height / 2, tag: el.tagName, text: (el.innerText||"").slice(0,30) };
  })()`);
  if (q.error || !q.value) return { error: q.error ?? "not-found", text };
  const { x, y } = q.value;
  await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  const up = await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  if (up.error) return { error: up.error };
  return { clicked: q.value.tag, at: [Math.round(x), Math.round(y)] };
}

async function clickSelector(selector, index = 0) {
  const q = await evaluate(`(() => {
    const els = [...document.querySelectorAll(${JSON.stringify(selector)})].filter(e => e.offsetParent !== null);
    const el = els[${index}];
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    return { x: r.x + r.width / 2, y: r.y + r.height / 2, value: el.value ?? null };
  })()`);
  if (q.error || !q.value) return { error: q.error ?? "not-found", selector, index };
  const { x, y } = q.value;
  await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  const up = await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  return up.error ? { error: up.error } : { clicked: selector, index, was: q.value.value };
}

async function setInput(selector, index, text) {
  const q = await evaluate(`(() => {
    const els = [...document.querySelectorAll(${JSON.stringify(selector)})].filter(e => e.offsetParent !== null);
    const el = els[${index}];
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    el.focus();
    const r = el.getBoundingClientRect();
    return { x: r.x + r.width / 2, y: r.y + r.height / 2, value: el.value };
  })()`);
  if (q.error || !q.value) return { error: q.error ?? "not-found", selector, index };
  const { x, y } = q.value;
  await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  await cmd("Input.dispatchKeyEvent", { type: "keyDown", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, nativeVirtualKeyCode: 65, modifiers: 2 }, session);
  await cmd("Input.dispatchKeyEvent", { type: "keyUp", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, nativeVirtualKeyCode: 65, modifiers: 2 }, session);
  await cmd("Input.insertText", { text }, session);
  const after = await evaluate(`(() => { const els=[...document.querySelectorAll(${JSON.stringify(selector)})].filter(e=>e.offsetParent!==null); return els[${index}]?.value ?? null; })()`);
  return { before: q.value.value, after: after.value };
}

/** Real <select> choice: set the matching option and fire the same input/change events a user does. */
async function selectByLabel(selector, index, label) {
  const q = await evaluate(`(() => {
    const els = [...document.querySelectorAll(${JSON.stringify(selector)})].filter(e => e.offsetParent !== null);
    const el = els[${index}];
    if (!el) return { error: "not-found" };
    const opt = [...el.options].find(o => (o.textContent || "").trim() === ${JSON.stringify(label)});
    if (!opt) return { error: "option-not-found", options: [...el.options].map(o => (o.textContent||"").trim()) };
    el.focus();
    const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value").set;
    setter.call(el, opt.value);
    el.dispatchEvent(new Event("input", { bubbles: true }));
    el.dispatchEvent(new Event("change", { bubbles: true }));
    return { value: el.value, label: (opt.textContent||"").trim() };
  })()`);
  if (q.error) return { error: q.error };
  if (q.value?.error) return q.value;
  return q.value;
}

async function typeText(text) {
  const r = await cmd("Input.insertText", { text }, session);
  return r.error ? { error: r.error } : { typed: text.length };
}

async function pressTab() {
  const a = await cmd("Input.dispatchKeyEvent", { type: "keyDown", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9, nativeVirtualKeyCode: 9 }, session);
  return a.error ? { error: a.error } : { ok: true };
}

async function setTimeValue(selector, index, value) {
  // A real time entry ends as a value + input/change event; headless cannot type into type=time,
  // so set the same value through the native setter and fire the same events the component listens to.
  const q = await evaluate(`(() => {
    const els = [...document.querySelectorAll(${JSON.stringify(selector)})].filter(e => e.offsetParent !== null);
    const el = els[${index}];
    if (!el) return { error: "not-found" };
    const before = el.value;
    el.focus();
    const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value").set;
    setter.call(el, ${JSON.stringify(value)});
    el.dispatchEvent(new Event("input", { bubbles: true }));
    el.dispatchEvent(new Event("change", { bubbles: true }));
    return { before, after: el.value };
  })()`);
  return q.error ? { error: q.error } : (q.value ?? { error: "no-value" });
}

async function shot(name) {
  const r = await cmd("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, session, 25000);
  if (r.error) return { error: r.error };
  writeFileSync(`${SHOTS}\\${name}.png`, Buffer.from(r.result.data, "base64"));
  return { path: `${SHOTS}\\${name}.png` };
}

/** Structural snapshot: groups, their titles/area chips/rows, plus picker and week state. */
const SURVEY = `(() => {
  const txt = (e) => (e.innerText || e.textContent || "").trim();
  const groups = [...document.querySelectorAll("fieldset,[data-group],section")].filter(g => /گروه|گروه\\s/.test(txt(g)) && g.offsetParent !== null).slice(0, 12)
    .map((g, i) => ({
      i,
      head: txt(g).slice(0, 60),
      titles: [...g.querySelectorAll("input[type=text]")].map(e => ({ v: e.value, dir: e.dir })),
      chips: [...g.querySelectorAll("button,[role=button],span")].map(txt).filter(t => /[:：]/.test(t) || /×/.test(t)).slice(0, 12),
      rows: [...g.querySelectorAll("input[type=number],input[inputmode=numeric]")].length,
      selects: [...g.querySelectorAll("select")].map(s => ({ value: s.value, options: [...s.options].map(o => txt(o)) }))
    }));
  return JSON.stringify({
    buttons: [...document.querySelectorAll("button")].filter(b => b.offsetParent).map(txt).filter(Boolean),
    groups,
    allSelects: [...document.querySelectorAll("select")].filter(s => s.offsetParent).map(s => ({ value: s.value, options: [...s.options].map(o => ({ t: txt(o), v: o.value })) })),
    allNumberInputs: [...document.querySelectorAll("input[type=number],input[inputmode=numeric],input[type=text]")].filter(e => e.offsetParent).map(e => ({ t: e.type, v: e.value, ph: e.placeholder, dir: e.dir })),
    times: [...document.querySelectorAll("input[type=time]")].filter(e => e.offsetParent).map(e => e.value),
    tabs: [...document.querySelectorAll("[role=tab]")].map(t => ({ t: txt(t), sel: t.getAttribute("aria-selected") })),
    tableRows: document.querySelectorAll("table tbody tr").length,
    bodyHead: document.body.innerText.slice(0, 300)
  });
})()`;

let done = false;
for (;;) {
  if (done) break;
  await sleep(150);
  if (!existsSync(CMD)) continue;
  let c;
  try { c = JSON.parse(readFileSync(CMD, "utf8").replace(/^\uFEFF/, "")); }
  catch { rmSync(CMD, { force: true }); continue; }
  rmSync(CMD, { force: true });
  let out;
  try {
    switch (c.op) {
      case "eval": out = await evaluate(c.expr); break;
      case "survey": out = await evaluate(SURVEY); break;
      case "shot": out = await shot(c.name); break;
      case "click": out = await clickText(c.text, c.exact === true); break;
      case "clicksel": out = await clickSelector(c.sel, c.index ?? 0); break;
      case "setinput": out = await setInput(c.sel, c.index ?? 0, c.text); break;
      case "select": out = await selectByLabel(c.sel ?? "select", c.index ?? 0, c.label); break;
      case "settime": out = await setTimeValue(c.sel ?? "input[type=time]", c.index ?? 0, c.text); break;
      case "type": out = await typeText(c.text); break;
      case "tab": out = await pressTab(); break;
      case "wait": await sleep(c.ms ?? 500); out = { ok: true }; break;
      case "cookie": {
        const r = await cmd("Network.setCookie", { name: "tooba_locale", value: c.value, url: "http://127.0.0.1:3517/", path: "/" }, session, 8000);
        out = r.error ? { error: r.error } : { cookie: c.value };
        break;
      }
      case "reload": await cmd("Page.reload", {}, session); await sleep(c.wait ?? 5000); out = { ok: true }; break;
      case "nav": await cmd("Page.navigate", { url: c.url }, session); await sleep(c.wait ?? 5000); out = { ok: true }; break;
      case "release": {
        if (!heldPut) { out = { error: "no-held-put" }; break; }
        const h = heldPut;
        heldPut = null;
        const putOk = putAssert?.ok === true;
        const r = putOk
          ? await fulf(h.sessionId, h.requestId, 200, savedResponseText)
          : await fulf(h.sessionId, h.requestId, 400, JSON.stringify({ title: "ScenarioMismatch", errorCode: "ScenarioMismatch" }), "application/problem+json");
        if (putOk) savedPhase = true;
        out = r.error ? { error: r.error } : { released: true, assertionOk: putOk };
        break;
      }
      case "reject": {
        if (!heldPut) { out = { error: "no-held-put" }; break; }
        const h = heldPut; heldPut = null;
        // Canonical conflict envelope: ShippingErrorCodes.AuthoringDraftRevisionConflict plus the
        // adapter's 409 mapping (errorCode + errors{property:[codes]}), not an invented code.
        const r = await fulf(h.sessionId, h.requestId, 409,
          JSON.stringify({ errorCode: "shipping.authoring.draft_revision_conflict", errors: { expectedRevision: ["stale"] } }),
          "application/problem+json");
        out = r.error ? { error: r.error } : { rejected: 409 };
        break;
      }
      case "failput": {
        if (!heldPut) { out = { error: "no-held-put" }; break; }
        const h = heldPut; heldPut = null;
        const r = await cmd("Fetch.failRequest", { requestId: h.requestId, errorReason: "ConnectionAborted" }, h.sessionId);
        out = r.error ? { error: r.error } : { failedAsTransport: true };
        break;
      }
      case "resettrial": {
        heldPut = null; savedPhase = false;
        await cmd("Page.navigate", { url }, session); await sleep(c.wait ?? 5500);
        out = { ok: true };
        break;
      }
      case "log": out = { netlog, putCount, putAssert, held: heldPut !== null, savedPhase, gets: netlog.filter((x) => x.result?.startsWith("GET")).length, blocked: netlog.filter((x) => x.result === "BLOCKED_LOCAL").length, events: events.length }; break;
      case "metrics": await cmd("Emulation.setDeviceMetricsOverride", { width: c.w, height: c.h, deviceScaleFactor: 1, mobile: c.mobile === true }, session); out = { ok: true }; break;
      case "exit": out = { ok: true }; done = true; break;
      default: out = { error: "unknown-op" };
    }
  } catch (e) { out = { error: String(e?.message ?? e) }; }
  writeFileSync(RES, JSON.stringify({ id: c.id, op: c.op, out }));
}

try { chrome.kill(); } catch {}
process.exit(0);
