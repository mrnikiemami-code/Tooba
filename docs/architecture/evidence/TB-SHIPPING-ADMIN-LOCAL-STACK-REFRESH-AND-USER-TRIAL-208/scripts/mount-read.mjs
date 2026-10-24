// TB-208 task-owned mounted read harness — REAL traffic only.
// Launches an isolated headless Chrome profile, navigates the actual admin route, and reports
// DOM text, console exceptions, failed requests and screenshots. It installs NO Fetch interception,
// injects no responses and rewrites no request: whatever the page asks the real backend, it gets.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = "D:\\temp\\tb208-browser";
const PORT = 9555;
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const URL_ARG = process.argv[2];
const SHOT = process.argv[3] ?? path.join(ROOT, "view.png");
fs.mkdirSync(ROOT, { recursive: true });

const profile = path.join(ROOT, "profile");
const child = spawn(CHROME, [
  "--headless=new",
  `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${profile}`,
  "--no-first-run",
  "--no-default-browser-check",
  "--disable-extensions",
  "--disable-background-networking",
  "--window-size=1440,900",
  "about:blank",
], { detached: false, stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function versionInfo() {
  for (let i = 0; i < 60; i++) {
    try {
      const r = await fetch(`http://127.0.0.1:${PORT}/json/version`);
      if (r.ok) return await r.json();
    } catch { /* not up yet */ }
    await sleep(250);
  }
  throw new Error("cdp-not-ready");
}

let ws;
let seq = 0;
const pending = new Map();
const events = [];

function send(method, params = {}, sessionId) {
  const id = ++seq;
  const payload = { id, method, params };
  if (sessionId) payload.sessionId = sessionId;
  ws.send(JSON.stringify(payload));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}

async function main() {
  await versionInfo();
  const targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
  const page = targets.find((t) => t.type === "page");
  ws = new WebSocket(page.webSocketDebuggerUrl);
  await new Promise((r, j) => { ws.onopen = r; ws.onerror = j; });
  ws.onmessage = (m) => {
    const msg = JSON.parse(m.data);
    if (msg.id && pending.has(msg.id)) {
      const p = pending.get(msg.id);
      pending.delete(msg.id);
      msg.error ? p.reject(new Error(msg.method + ":" + msg.error.message)) : p.resolve(msg.result);
      return;
    }
    if (msg.method === "Runtime.exceptionThrown") {
      events.push({ kind: "exception", text: msg.params.exceptionDetails?.exception?.description ?? msg.params.exceptionDetails?.text });
    }
    if (msg.method === "Runtime.consoleAPICalled" && ["error", "warning"].includes(msg.params.type)) {
      events.push({ kind: "console." + msg.params.type, text: msg.params.args.map((a) => a.value ?? a.description ?? "").join(" ").slice(0, 300) });
    }
    if (msg.method === "Network.loadingFailed") {
      events.push({ kind: "request.failed", text: (msg.params.errorText ?? "") + " " + (msg.params.blockedReason ?? "") });
    }
    if (msg.method === "Network.responseReceived") {
      const r = msg.params.response;
      if (r.url.includes("/v1/")) events.push({ kind: "http." + r.status, text: r.url.replace(/^https?:\/\/[^/]+/, "") });
    }
  };
  await send("Runtime.enable");
  await send("Network.enable");
  await send("Page.enable");
  await send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
  await send("Page.navigate", { url: URL_ARG });
  await sleep(9000);

  const probe = await send("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const t = (s) => { const e = document.querySelector(s); return e ? e.value : null; };
      const btn = (label) => [...document.querySelectorAll('button')].find((b) => (b.innerText || '').trim() === label) || null;
      return {
        url: location.href,
        title: document.title,
        text: document.body.innerText.replace(/\\s+/g, ' ').slice(0, 1600),
        viewMode: document.body.innerText.includes('حالت مشاهده'),
        editMode: document.body.innerText.includes('حالت ویرایش'),
        firstRtlInput: t('input[dir="rtl"]'),
        buttons: [...document.querySelectorAll('button')].filter((b) => b.offsetParent).map((b) => ({ t: (b.innerText || '').trim().slice(0, 40), disabled: b.disabled })),
        groupText: (document.body.innerText.match(/گروه \\d+/g) || []).join('|'),
        revision: (document.body.innerText.match(/بازبینی[^\\d]{0,4}(\\d+)/) || [])[1] ?? null,
      };
    })()`,
  });

  const shot = await send("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(SHOT, Buffer.from(shot.data, "base64"));

  // Ground truth of what the browser itself receives for the document read (read-only GET, same
  // origin, no header invention beyond the documented dev-actor value already used by the app).
  const inPage = await send("Runtime.evaluate", {
    returnByValue: true,
    awaitPromise: true,
    expression: `(async () => {
      const key = 'tooba.adminActorUserId';
      const stored = localStorage.getItem(key);
      const actor = '01a036c2-970e-7000-8eb7-94bf5cc2d8db';
      const url = '/v1/admin/shipping-services/authoring-drafts/01a16eeb-70b3-7000-87c2-564c82835496/editor-document';
      const r = await fetch(url, { headers: { Accept: 'application/json', 'Accept-Language': 'fa', 'X-Tooba-Dev-Actor-User-Id': stored || actor } });
      const body = await r.text();
      return { storedActor: stored, status: r.status, bodyStart: body.slice(0, 700) };
    })()`,
  });

  console.log(JSON.stringify({ probe: probe.result.value, inPage: inPage.result.value, events: events.slice(0, 30), screenshot: SHOT }, null, 2));

  if (process.argv[4] === "edit-clean") {
    const clickText = async (label) => {
      await send("Runtime.evaluate", {
        returnByValue: true,
        expression: `(() => { const b = [...document.querySelectorAll('button')].find((x) => (x.innerText || '').trim().startsWith(${JSON.stringify(label)}) && x.offsetParent); if (!b) return 'missing'; b.click(); return 'clicked'; })()`,
      });
      await sleep(1500);
    };
    await clickText("ویرایش");
    const inEdit = await send("Runtime.evaluate", {
      returnByValue: true,
      expression: `JSON.stringify({ edit: document.body.innerText.includes('حالت ویرایش'), applyDisabled: !!([...document.querySelectorAll('button')].find((b) => (b.innerText || '').trim() === 'اعمال') || {}).disabled, nameValue: (document.querySelector('input[dir="rtl"]') || {}).value ?? null, dirty: document.body.innerText.includes('تغییر اعمال‌نشده'), loadError: document.body.innerText.includes('خواندن پیش‌نویس ناموفق بود') })`,
    });
    await clickText("انصراف از ویرایش");
    const afterCancel = await send("Runtime.evaluate", {
      returnByValue: true,
      expression: `JSON.stringify({ dialog: [...document.querySelectorAll('button')].some((b) => (b.innerText || '').trim() === 'خروج بدون ذخیره'), stillEdit: document.body.innerText.includes('حالت ویرایش') })`,
    });
    const dialog = JSON.parse(afterCancel.result.value);
    let afterConfirm = null;
    if (dialog.dialog) {
      await clickText("خروج بدون ذخیره");
      const r = await send("Runtime.evaluate", {
        returnByValue: true,
        expression: `JSON.stringify({ view: document.body.innerText.includes('حالت مشاهده'), loadError: document.body.innerText.includes('خواندن پیش‌نویس ناموفق بود') })`,
      });
      afterConfirm = JSON.parse(r.result.value);
    }
    console.log(JSON.stringify({ edit: JSON.parse(inEdit.result.value), cancelDialog: dialog, afterConfirm }, null, 2));
  }
  ws.close();
  child.kill();
  process.exit(0);
}

main().catch((e) => { console.log(JSON.stringify({ fatal: String(e) })); try { child.kill(); } catch {} process.exit(1); });
