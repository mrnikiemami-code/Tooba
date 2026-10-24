// Task-owned CDP harness: launches installed Chrome, intercepts ALL /v1 requests with synthetic
// fixtures (unhandled /v1 fails locally, never passes through), and serves commands from files.
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, existsSync, mkdirSync, rmSync } from "node:fs";
import { routes, editorDocument, IDS } from "./fixtures.mjs";

const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const ROOT = "D:\\temp\\tb205-runtime";
const PROFILE = `${ROOT}\\chrome-profile`;
const SHOTS = `${ROOT}\\shots`;
const CMD = `${ROOT}\\harness\\cmd.json`;
const RES = `${ROOT}\\harness\\result.json`;
const PORT = 9555;

mkdirSync(SHOTS, { recursive: true });
try { rmSync(CMD, { force: true }); } catch {}

const mode = process.argv[2] ?? "view";
const draftId = process.argv[3] ?? IDS.draft;
const url = `http://127.0.0.1:3517/admin/shipping-services/authoring/${draftId}?mode=${mode}`;

const chrome = spawn(CHROME, [
  "--headless=new",
  `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${PROFILE}`,
  "--no-first-run",
  "--no-default-browser-check",
  "--disable-gpu",
  "--window-size=1440,900",
  "--lang=fa-IR",
  "about:blank"
], { stdio: "ignore", detached: false });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function version() {
  for (let i = 0; i < 60; i++) {
    try {
      const r = await fetch(`http://127.0.0.1:${PORT}/json/version`);
      return await r.json();
    } catch { await sleep(250); }
  }
  throw new Error("chrome-cdp-unavailable");
}

const v = await version();
const ws = new WebSocket(v.webSocketDebuggerUrl);
await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });

let seq = 0;
const pending = new Map();
const log = [];
let putCount = 0;
let putBody = null;
let revision = 7;
let putPlan = "ok"; // ok | conflict409 | transportFail
let events = [];

ws.onmessage = (msg) => {
  const data = JSON.parse(msg.data);
  if (data.id && pending.has(data.id)) {
    const { resolve, reject } = pending.get(data.id);
    pending.delete(data.id);
    if (data.error) reject(new Error(JSON.stringify(data.error))); else resolve(data.result);
    return;
  }
  if (data.method === "Fetch.requestPaused") handlePaused(data.sessionId, data.params).catch(() => {});
  else if (data.method === "Runtime.consoleAPICalled" || data.method === "Runtime.exceptionThrown" || data.method === "Log.entryAdded") {
    events.push({ method: data.method, params: data.params });
  }
};

function send(method, params, sessionId) {
  const id = ++seq;
  return new Promise((resolve, reject) => {
    pending.set(id, { resolve, reject });
    ws.send(JSON.stringify({ id, method, params, sessionId }));
  }).catch((e) => ({ __error: e.message }));
}

function routeFor(u) {
  if (!u.includes("/v1/")) return null;
  if (u.includes("/editor-document")) return { kind: "editor" };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/administrative-areas/.test(u)) return { kind: "areas", body: routes.areas };
  if (/\/v1\/admin\/geography\/countries\/[0-9a-f-]+\/cities/.test(u)) return { kind: "cities", body: routes.cities };
  if (/\/v1\/admin\/geography\/cities\/[0-9a-f-]+\/service-areas/.test(u)) return { kind: "serviceAreas", body: routes.serviceAreas };
  if (/\/v1\/admin\/geography\/countries(\?|$)/.test(u)) return { kind: "countries", body: routes.countries };
  return { kind: "unhandled" };
}

async function handlePaused(sessionId, p) {
  const u = p.request.url;
  const method = p.request.method;
  const hit = routeFor(u);
  if (method === "OPTIONS") {
    await send("Fetch.fulfillRequest", { requestId: p.requestId, responseCode: 204, responseHeaders: [] }, sessionId);
    return;
  }
  if (!hit || hit.kind === "unhandled") {
    log.push({ url: u, method, result: "BLOCKED_LOCAL", at: Date.now() });
    await send("Fetch.failRequest", { requestId: p.requestId, errorReason: "Failed" }, sessionId);
    return;
  }
  if (hit.kind === "editor" && method === "PUT") {
    putCount++;
    putBody = p.request.postData ?? null;
    log.push({ url: u, method, result: `PUT#${putCount}`, bodyLength: putBody?.length ?? 0, at: Date.now() });
    if (putPlan === "transportFail") {
      await send("Fetch.failRequest", { requestId: p.requestId, errorReason: "ConnectionAborted" }, sessionId);
      return;
    }
    if (putPlan === "conflict409") {
      const body = JSON.stringify({ title: "Conflict", errorCode: "RevisionMismatch", errors: { revision: ["Stale"] } });
      await send("Fetch.fulfillRequest", {
        requestId: p.requestId, responseCode: 409,
        responseHeaders: [{ name: "Content-Type", value: "application/problem+json" }],
        body: Buffer.from(body).toString("base64")
      }, sessionId);
      return;
    }
    revision += 1;
    const body = editorDocument.replace(/"revision": 7/g, `"revision": ${revision}`);
    await send("Fetch.fulfillRequest", {
      requestId: p.requestId, responseCode: 200,
      responseHeaders: [{ name: "Content-Type", value: "application/json" }],
      body: Buffer.from(body).toString("base64")
    }, sessionId);
    return;
  }
  if (hit.kind === "editor" && method === "GET") {
    log.push({ url: u, method, result: "GET", at: Date.now() });
    const body = editorDocument.replace(/"revision": 7/g, `"revision": ${revision}`);
    await send("Fetch.fulfillRequest", {
      requestId: p.requestId, responseCode: 200,
      responseHeaders: [{ name: "Content-Type", value: "application/json" }],
      body: Buffer.from(body).toString("base64")
    }, sessionId);
    return;
  }
  log.push({ url: u, method, result: `FULFILLED:${hit.kind}`, at: Date.now() });
  await send("Fetch.fulfillRequest", {
    requestId: p.requestId, responseCode: 200,
    responseHeaders: [{ name: "Content-Type", value: "application/json" }],
    body: Buffer.from(hit.body).toString("base64")
  }, sessionId);
}

const t = await send("Target.createTarget", { url: "about:blank" });
const targetId = t.targetId;
const att = await send("Target.attachToTarget", { targetId, flatten: true });
const session = att.sessionId;

await send("Page.enable", {}, session);
await send("Runtime.enable", {}, session);
await send("Log.enable", {}, session);
await send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false }, session);
await send("Fetch.enable", { patterns: [{ urlPattern: "*/v1/*", requestStage: "Request" }] }, session);

async function evaluate(expr) {
  const r = await send("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true }, session);
  if (r.__error) return { error: r.__error };
  if (r.exceptionDetails) return { error: r.exceptionDetails.text ?? "exception", detail: r.exceptionDetails.exception?.description };
  return { value: r.result?.value };
}

async function shot(name) {
  const r = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false }, session);
  if (r.__error || !r.data) return { error: r.__error ?? "no-data" };
  const path = `${SHOTS}\\${name}.png`;
  writeFileSync(path, Buffer.from(r.data, "base64"));
  return { path };
}

await send("Page.navigate", { url }, session);
await sleep(4500);

let running = true;
process.on("SIGTERM", () => { running = false; });
process.on("exit", () => { try { chrome.kill(); } catch {} });

// Command loop.
for (;;) {
  await sleep(200);
  if (!existsSync(CMD)) continue;
  let cmd;
  try { cmd = JSON.parse(readFileSync(CMD, "utf8").replace(/^\uFEFF/, "")); }
  catch { rmSync(CMD, { force: true }); continue; }
  if (!cmd || cmd.__done) { rmSync(CMD, { force: true }); continue; }
  rmSync(CMD, { force: true });
  let out;
  try {
    switch (cmd.op) {
      case "eval": out = await evaluate(cmd.expr); break;
      case "shot": out = await shot(cmd.name); break;
      case "nav": await send("Page.navigate", { url: cmd.url }, session); await sleep(cmd.wait ?? 4000); out = { ok: true }; break;
      case "log": out = { log, putCount, putBody, revision, unhandled: log.filter((x) => x.result === "BLOCKED_LOCAL").length }; break;
      case "plan": putPlan = cmd.plan; out = { plan: putPlan }; break;
      case "revision": revision = cmd.value; out = { revision }; break;
      case "events": out = { count: events.length, events: events.slice(-40) }; break;
      case "metrics": await send("Emulation.setDeviceMetricsOverride", cmd.metrics, session); out = { ok: true }; break;
      case "exit": out = { ok: true }; writeFileSync(RES, JSON.stringify({ exit: true })); chrome.kill(); process.exit(0); break;
      default: out = { error: "unknown-op" };
    }
  } catch (e) {
    out = { error: String(e?.message ?? e) };
  }
  writeFileSync(RES, JSON.stringify({ id: cmd.id, op: cmd.op, out }));
}
