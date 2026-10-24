// 205-R1 CDP harness (continuation of the committed 205 harness).
// Corrections required by review: finite command timeouts, protocol/__error/exception are errors,
// BOM fix retained, /v1-only Fetch interception retained, real input via CDP Input domain,
// PUT asserts the expected request first and returns a scenario-specific prepared response.
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, existsSync, mkdirSync, rmSync } from "node:fs";
import { routes, editorDocument, typesEnvelope, devContextEnvelope, capabilitiesEnvelope, IDS } from "./fixtures.mjs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const ROOT = "D:\\temp\\tb205-r1";
const PROFILE = `${ROOT}\\chrome-profile`;
const SHOTS = `${ROOT}\\shots`;
const CMD = `${ROOT}\\harness\\cmd.json`;
const RES = `${ROOT}\\harness\\result.json`;
const PORT = 9555;

mkdirSync(SHOTS, { recursive: true });
try { rmSync(CMD, { force: true }); } catch {}
try { rmSync(RES, { force: true }); } catch {}

const draftId = process.argv[2] ?? IDS.draft;
const locale = process.argv[3] ?? "fa-IR";
const url = `http://127.0.0.1:3517/admin/shipping-services/authoring/${draftId}?mode=edit`;

const chrome = spawn(CHROME, [
  "--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=${PROFILE}`,
  "--no-first-run", "--no-default-browser-check", "--disable-gpu", "--window-size=1440,900",
  `--lang=${locale}`, "about:blank"
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
let putCount = 0, putBody = null, putAssert = null, revision = 7, putPlan = "ok";
const events = [];

ws.onmessage = (msg) => {
  const data = JSON.parse(msg.data);
  if (data.id && pending.has(data.id)) {
    const { resolve } = pending.get(data.id);
    pending.delete(data.id);
    resolve(data);            // full envelope: caller classifies error/result (no silent success)
    return;
  }
  if (data.method === "Fetch.requestPaused") handlePaused(data.sessionId, data.params).catch(() => {});
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

function routeFor(u) {
  if (!u.includes("/v1/")) return null;
  if (u.includes("/editor-document")) return { kind: "editor" };
  if (u.includes("/authoring-types")) return { kind: "types", body: typesEnvelope };
  if (u.includes("/admin/dev-context")) return { kind: "devContext", body: devContextEnvelope };
  if (u.includes("/access-control/me/capabilities")) return { kind: "capabilities", body: capabilitiesEnvelope };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/administrative-areas/.test(u)) return { kind: "areas", body: routes.areas };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/cities/.test(u)) {
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

// Assertions on the ONE expected request; response is prepared for THIS scenario only.
function block(text, key) {
  const at = text.indexOf(`"${key}":`);
  if (at < 0) return null;
  const start = text.indexOf("{", at);
  let depth = 0, end = -1;
  for (let i = start; i < text.length; i++) {
    if (text[i] === "{") depth++;
    else if (text[i] === "}") { depth--; if (depth === 0) { end = i + 1; break; } }
  }
  return end < 0 ? null : { start, end, text: text.slice(start, end) };
}

function arrayBlock(text, key) {
  const at = text.indexOf(`"${key}":`);
  if (at < 0) return null;
  const start = text.indexOf("[", at);
  let depth = 0, end = -1;
  for (let i = start; i < text.length; i++) {
    if (text[i] === "[") depth++;
    else if (text[i] === "]") { depth--; if (depth === 0) { end = i + 1; break; } }
  }
  return end < 0 ? null : { start, end, text: text.slice(start, end) };
}

function assertAndShape(requestText) {
  const checks = [];
  const notes = {};
  const parsed = JSON.parse(requestText);
  notes.rootKeys = Object.keys(parsed).sort().join(",");
  notes.expectedRevision = parsed.expectedRevision;
  checks.push(["expectedRevision-matches-current", parsed.expectedRevision === revision]);
  checks.push(["two-retained-groups", Array.isArray(parsed.regionGroups) && parsed.regionGroups.length === 2]);
  const grpText = JSON.stringify(parsed.regionGroups ?? []);
  checks.push(["retained-group1-id", grpText.includes(IDS.group1)]);
  checks.push(["retained-group2-id", grpText.includes(IDS.group2)]);
  checks.push(["retained-row1-id", grpText.includes(IDS.row1)]);
  checks.push(["retained-currency", grpText.includes("IRR")]);
  checks.push(["exact-decimal-token-0.50", requestText.includes("0.50")]);
  checks.push(["exact-decimal-token-1850000.00", requestText.includes("1850000.00")]);
  checks.push(["week-peer-end-intact", requestText.includes("17:30:00")]);
  checks.push(["fractional-start-present", /09:00:00\.\d+/.test(requestText)]);
  checks.push(["one-root-week", (requestText.match(/"timeZoneId"/g) ?? []).length === 1]);
  notes.day1 = JSON.stringify((parsed.weeklySchedule?.days ?? []).find((d) => d.weekday === 1) ?? null);
  notes.methodTranslations = JSON.stringify(parsed.methodTranslations ?? null);
  putAssert = { checks, notes };

  // Prepared full-document response for THIS scenario: adopted request blocks + new revision.
  const newRevision = revision + 1;
  let resp = editorDocument.replace(/"revision":\s*7/g, `"revision": ${newRevision}`);
  for (const [key, kind] of [["weeklySchedule", "o"], ["regionGroups", "a"], ["methodTranslations", "a"]]) {
    const reqBlock = kind === "o" ? block(requestText, key) : arrayBlock(requestText, key);
    if (!reqBlock) continue;
    const docBlock = kind === "o" ? block(resp, key) : arrayBlock(resp, key);
    if (!docBlock) continue;
    resp = resp.slice(0, docBlock.start) + reqBlock.text + resp.slice(docBlock.end);
  }
  resp = resp.replace(/"updatedAt":\s*"[^"]*"/, `"updatedAt": "2026-10-24T16:00:00Z"`);
  revision = newRevision;
  notes.returnedRevision = revision;
  try { JSON.parse(resp); notes.responseParses = true; } catch { notes.responseParses = false; }
  return resp;
}

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
    netlog.push({ url: u, method, result: `PUT#${putCount}`, bodyLength: putBody.length });
    if (putPlan === "transportFail") return cmd("Fetch.failRequest", { requestId: p.requestId, errorReason: "ConnectionAborted" }, sessionId);
    if (putPlan === "conflict409") {
      return fulf(sessionId, p.requestId, 409,
        JSON.stringify({ title: "Conflict", errorCode: "RevisionMismatch", errors: { revision: ["StaleRevision"] } }),
        "application/problem+json");
    }
    try {
      return fulf(sessionId, p.requestId, 200, assertAndShape(putBody));
    } catch (e) {
      putAssert = { checks: [["body-parse", false]], error: String(e?.message ?? e) };
      return fulf(sessionId, p.requestId, 400, JSON.stringify({ title: "Bad", errorCode: "Malformed" }), "application/problem+json");
    }
  }
  if (hit.kind === "editor" && method === "GET") {
    netlog.push({ url: u, method, result: "GET" });
    return fulf(sessionId, p.requestId, 200, editorDocument.replace(/"revision":\s*7/g, `"revision": ${revision}`));
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

/** Real mouse click at the centre of the first element matching text (or selector). */
async function clickText(text, tag = "*") {
  const q = await evaluate(`(() => {
    const els = [...document.querySelectorAll(${JSON.stringify(tag)})]
      .filter(e => (e.innerText || e.textContent || "").trim().startsWith(${JSON.stringify(text)}) && e.offsetParent !== null);
    const el = els[els.length - 1] || els[0];
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    return { x: r.x + r.width / 2, y: r.y + r.height / 2, w: r.width, h: r.height };
  })()`);
  if (q.error || !q.value) return { error: q.error ?? "not-found", text };
  const { x, y } = q.value;
  const down = await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  const up = await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  if (down.error || up.error) return { error: down.error ?? up.error };
  return { clicked: text, at: [Math.round(x), Math.round(y)] };
}

async function clickSelector(selector) {
  const q = await evaluate(`(() => {
    const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
  })()`);
  if (q.error || !q.value) return { error: q.error ?? "not-found", selector };
  const { x, y } = q.value;
  await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  const up = await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  return up.error ? { error: up.error } : { clicked: selector };
}

async function typeText(text) {
  const r = await cmd("Input.insertText", { text }, session);
  return r.error ? { error: r.error } : { typed: text.length };
}

async function key(name, code, keyCode) {
  const a = await cmd("Input.dispatchKeyEvent", { type: "keyDown", key: name, windowsVirtualKeyCode: keyCode, nativeVirtualKeyCode: keyCode, code }, session);
  const b = await cmd("Input.dispatchKeyEvent", { type: "keyUp", key: name, windowsVirtualKeyCode: keyCode, nativeVirtualKeyCode: keyCode, code }, session);
  return a.error || b.error ? { error: a.error ?? b.error } : { key: name };
}

async function shot(name) {
  const r = await cmd("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, session, 25000);
  if (r.error) return { error: r.error };
  writeFileSync(`${SHOTS}\\${name}.png`, Buffer.from(r.result.data, "base64"));
  return { path: `${SHOTS}\\${name}.png` };
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
  if (q.error || !q.value) return { error: q.error ?? "not-found" };
  const { x, y } = q.value;
  await cmd("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 }, session);
  await cmd("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 }, session);
  // select-all then replace, so the stored value is fully overwritten
  await cmd("Input.dispatchKeyEvent", { type: "keyDown", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, nativeVirtualKeyCode: 65, modifiers: 2 }, session);
  await cmd("Input.dispatchKeyEvent", { type: "keyUp", key: "a", code: "KeyA", windowsVirtualKeyCode: 65, nativeVirtualKeyCode: 65, modifiers: 2 }, session);
  await cmd("Input.insertText", { text }, session);
  const after = await evaluate(`(() => { const els=[...document.querySelectorAll(${JSON.stringify(selector)})].filter(e=>e.offsetParent!==null); return els[${index}]?.value ?? null; })()`);
  return { before: q.value.value, after: after.value };
}

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
      case "shot": out = await shot(c.name); break;
      case "click": out = await clickText(c.text, c.tag ?? "*"); break;
      case "clicksel": out = await clickSelector(c.sel); break;
      case "type": out = await typeText(c.text); break;
      case "key": out = await key(c.key, c.code ?? "Enter", c.keyCode ?? 13); break;
      case "wait": await sleep(c.ms ?? 500); out = { ok: true }; break;
      case "cookie": {
        const r = await cmd("Network.setCookie", { name: "tooba_locale", value: c.value, url: "http://127.0.0.1:3517/" }, session, 8000);
        out = r.error ? { error: r.error } : { cookie: c.value };
        break;
      }
      case "reload": await cmd("Page.reload", {}, session); await sleep(c.wait ?? 5000); out = { ok: true }; break;
      case "nav": await cmd("Page.navigate", { url: c.url }, session); await sleep(c.wait ?? 5000); out = { ok: true }; break;
      case "log": out = { netlog, putCount, putAssert, revision, blocked: netlog.filter((x) => x.result === "BLOCKED_LOCAL").length, gets: netlog.filter((x) => x.result === "GET").length }; break;
      case "plan": putPlan = c.plan; out = { plan: putPlan }; break;
      case "reset-revision": revision = c.value; out = { revision }; break;
      case "setinput": out = await setInput(c.sel, c.index ?? 0, c.text); break;
      case "metrics": await cmd("Emulation.setDeviceMetricsOverride", { width: c.w, height: c.h, deviceScaleFactor: 1, mobile: c.mobile === true }, session); out = { ok: true }; break;
      case "events": out = { count: events.length, tail: events.slice(-25) }; break;
      case "script": {
        const results = [];
        for (const step of c.steps ?? []) {
          let r;
          switch (step.op) {
            case "eval": r = await evaluate(step.expr); break;
            case "shot": r = await shot(step.name); break;
            case "click": r = await clickText(step.text, step.tag ?? "*"); break;
            case "clicksel": r = await clickSelector(step.sel); break;
            case "type": r = await typeText(step.text); break;
            case "key": r = await key(step.key, step.code ?? "Enter", step.keyCode ?? 13); break;
            case "wait": await sleep(step.ms ?? 400); r = { ok: true }; break;
            case "cookie": {
              const cr = await cmd("Network.setCookie", { name: "tooba_locale", value: step.value, url: "http://127.0.0.1:3517/" }, session, 8000);
              r = cr.error ? { error: cr.error } : { cookie: step.value };
              break;
            }
            case "reload": await cmd("Page.reload", {}, session); await sleep(step.wait ?? 5000); r = { ok: true }; break;
            case "nav": await cmd("Page.navigate", { url: step.url }, session); await sleep(step.wait ?? 5000); r = { ok: true }; break;
            case "plan": putPlan = step.plan; r = { plan: putPlan }; break;
            case "reset-revision": revision = step.value; r = { revision }; break;
            case "metrics": await cmd("Emulation.setDeviceMetricsOverride", { width: step.w, height: step.h, deviceScaleFactor: 1, mobile: step.mobile === true }, session); r = { ok: true }; break;
            case "setinput": r = await setInput(step.sel, step.index ?? 0, step.text); break;
            default: r = { error: `unknown-step:${step.op}` };
          }
          results.push({ label: step.label ?? step.op, r });
        }
        out = { steps: results, putCount, putAssert, revision, netlog, blocked: netlog.filter((x) => x.result === "BLOCKED_LOCAL").length, gets: netlog.filter((x) => x.result === "GET").length };
        break;
      }
      case "exit": out = { ok: true }; done = true; break;
      default: out = { error: "unknown-op" };
    }
  } catch (e) { out = { error: String(e?.message ?? e) }; }
  writeFileSync(RES, JSON.stringify({ id: c.id, op: c.op, out }));
}

try { chrome.kill(); } catch {}
process.exit(0);
