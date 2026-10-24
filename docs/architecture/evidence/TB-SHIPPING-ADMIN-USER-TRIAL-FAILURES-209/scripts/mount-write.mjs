// TB-209 task-owned mounted WRITE harness — real traffic only.
// Launches an isolated headless Chrome profile, drives the ACTUAL admin authoring route against the
// live local Host, performs ONE real Apply, reloads, and reports DOM text/screenshots.
// It installs NO Fetch interception, injects no response and rewrites no request.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = "D:\\temp\\tb209-browser";
const PORT = 9557;
const CHROME = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const URL_ARG = process.argv[2];
const OUT = process.argv[3] ?? ROOT;
fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(ROOT, { recursive: true });

const profile = path.join(ROOT, "profile");
const child = spawn(
  CHROME,
  [
    "--headless=new",
    `--remote-debugging-port=${PORT}`,
    `--user-data-dir=${profile}`,
    "--no-first-run",
    "--no-default-browser-check",
    "--disable-extensions",
    "--window-size=1440,1000",
    "about:blank",
  ],
  { detached: false, stdio: "ignore" },
);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function versionInfo() {
  for (let i = 0; i < 80; i++) {
    try {
      const r = await fetch(`http://127.0.0.1:${PORT}/json/version`);
      if (r.ok) return await r.json();
    } catch {
      /* not up yet */
    }
    await sleep(250);
  }
  throw new Error("cdp-not-ready");
}

let ws;
let seq = 0;
const pending = new Map();
const netFailures = [];
const consoleErrors = [];

function send(method, params = {}) {
  const id = ++seq;
  ws.send(JSON.stringify({ id, method, params }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}

async function evaluate(expression) {
  const result = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
  if (result.exceptionDetails) throw new Error(`eval:${result.exceptionDetails.text}:${expression.slice(0, 120)}`);
  return result.result?.value;
}

async function shot(name) {
  const result = await send("Page.captureScreenshot", { format: "png" });
  const file = path.join(OUT, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  return file;
}

const setValue = `
(function setValue(testId, value) {
  const el = document.querySelector('[data-testid="' + testId + '"]');
  if (!el) return "missing:" + testId;
  const proto = el instanceof HTMLSelectElement ? HTMLSelectElement.prototype : el instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
  const setter = Object.getOwnPropertyDescriptor(proto, "value").set;
  setter.call(el, value);
  el.dispatchEvent(new Event(el instanceof HTMLSelectElement ? "change" : "input", { bubbles: true }));
  return "ok";
})`;

const clickIt = `
(function clickIt(testId) {
  const el = document.querySelector('[data-testid="' + testId + '"]');
  if (!el) return "missing:" + testId;
  if (el.disabled) return "disabled:" + testId;
  el.click();
  return "ok";
})`;

async function set(testId, value) {
  const r = await evaluate(`${setValue}(${JSON.stringify(testId)}, ${JSON.stringify(value)})`);
  await sleep(90);
  return r;
}

async function click(testId) {
  const r = await evaluate(`${clickIt}(${JSON.stringify(testId)})`);
  await sleep(160);
  return r;
}

async function text(testIdOrNull) {
  return evaluate(
    testIdOrNull
      ? `(document.querySelector('[data-testid="${testIdOrNull}"]')?.innerText ?? null)`
      : `document.body.innerText`,
  );
}

async function waitFor(expression, label, attempts = 80) {
  for (let i = 0; i < attempts; i++) {
    const value = await evaluate(`Boolean(${expression})`);
    if (value) return true;
    await sleep(200);
  }
  throw new Error(`timeout:${label}`);
}

async function main() {
  await versionInfo();
  const targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
  const page = targets.find((t) => t.type === "page");
  ws = new WebSocket(page.webSocketDebuggerUrl);
  await new Promise((r, j) => {
    ws.onopen = r;
    ws.onerror = j;
  });
  ws.onmessage = (event) => {
    const message = JSON.parse(event.data);
    if (message.id && pending.has(message.id)) {
      const entry = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) entry.reject(new Error(message.error.message));
      else entry.resolve(message.result);
      return;
    }
    if (message.method === "Network.loadingFailed") {
      netFailures.push(message.params);
    }
    if (message.method === "Runtime.consoleAPICalled" && message.params.type === "error") {
      consoleErrors.push(message.params.args.map((a) => a.value ?? a.description ?? "").join(" "));
    }
  };

  await send("Page.enable");
  await send("Runtime.enable");
  await send("Network.enable");

  const report = { url: URL_ARG, steps: [] };
  const step = (name, value) => report.steps.push({ name, value });

  await send("Page.navigate", { url: URL_ARG });
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-edit"]')`, "load");
  step("opened", "view");

  step("edit", await click("shipping-authoring-edit"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-type"]')`, "edit-mode");
  await sleep(700);

  // Real active catalog state (must offer the real "عادی" type, not an empty list).
  step(
    "typeOptions",
    await evaluate(
      `Array.from(document.querySelectorAll('[data-testid="shipping-authoring-type"] option')).map((o) => o.textContent)`,
    ),
  );
  step("typeState", await text("shipping-authoring-type-state"));

  const typeId = await evaluate(
    `(() => { const s = document.querySelector('[data-testid="shipping-authoring-type"]'); const o = Array.from(s.options).find((x) => x.value && x.value !== "__none__"); return o ? o.value : ""; })()`,
  );
  step("typeId", typeId);
  if (typeId) {
    step("typeSelect", await set("shipping-authoring-type", typeId));
    await sleep(200);
  }

  step("name", await set("shipping-authoring-name", "پیشتاز"));
  await sleep(200);
  step(
    "settings",
    [await click("shipping-authoring-freight-Prepaid"), await click("shipping-authoring-calculation-Manual")],
  );
  await sleep(250);
  await shot("01-general-edit.png");

  // Regions: one real delivery area with a real destination and one lawful fee row.
  step("tabRegions", await click("shipping-authoring-tab-regions"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-group-add"]')`, "regions");
  step("addArea", await click("shipping-authoring-group-add"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-destination-country"]')`, "picker");
  await sleep(900);

  const countryId = await evaluate(
    `(() => { const s = document.querySelector('[data-testid="shipping-authoring-destination-country"]'); const o = Array.from(s.options).find((x) => x.value); return o ? o.value : ""; })()`,
  );
  step("countryId", countryId);
  step("country", await set("shipping-authoring-destination-country", countryId));
  await sleep(700);
  step("addDestination", await click("shipping-authoring-destination-add"));
  await sleep(300);
  step(
    "chips",
    await evaluate(
      `Array.from(document.querySelectorAll('[data-testid^="shipping-authoring-area-"]')).map((n) => n.innerText)`,
    ),
  );
  step("title", await set("shipping-authoring-group-title", "تست"));
  await sleep(200);

  step("addRow", await click("shipping-authoring-rate-add"));
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-rate-freight"]')`, "rate-row");
  step(
    "row",
    [
      await set("shipping-authoring-rate-min-weight", "1000"),
      await set("shipping-authoring-rate-max-weight", "2000"),
      await set("shipping-authoring-rate-freight", "120000"),
      await set("shipping-authoring-rate-packaging", "5000"),
      await set("shipping-authoring-rate-min-days", "1"),
      await set("shipping-authoring-rate-max-days", "3"),
    ],
  );
  await sleep(250);
  await shot("02-regions-edit.png");

  // Schedule: lawful time-zone identifier chosen by the operator, one working interval.
  step("tabGeneral", await click("shipping-authoring-tab-general"));
  await sleep(300);
  step("timeZoneSelect", await set("shipping-authoring-week-configure-timezone", "Asia/Tehran"));
  await sleep(250);
  step("configure", await click("shipping-authoring-week-configure"));
  await sleep(400);
  step("working1", await click("shipping-authoring-week-working-1"));
  await sleep(250);
  step("start", await set("shipping-authoring-week-start-1", "09:00"));
  step("end", await set("shipping-authoring-week-end-1", "17:00"));
  await sleep(300);

  step("applyDisabled", await evaluate(`document.querySelector('[data-testid="shipping-authoring-apply"]').disabled`));
  step("localIssues", await text("shipping-authoring-local-issues"));
  step("blocked", await text("shipping-authoring-blocked"));
  await shot("03-ready-to-apply.png");

  step("apply", await click("shipping-authoring-apply"));
  for (let i = 0; i < 60; i++) {
    await sleep(250);
    const failed = await evaluate(
      `Boolean(document.querySelector('[data-testid^="shipping-authoring-save-"]'))`,
    );
    if (failed) break;
    const saved = await evaluate(
      `document.body.innerText.includes("تغییرات ذخیره شد") || !!document.querySelector('[data-testid="shipping-authoring-edit"]') && !document.querySelector('[data-testid="shipping-authoring-apply"]').disabled`,
    );
    if (saved) break;
  }
  await sleep(800);
  step("saveAlert", await text("shipping-authoring-save-httpError"));
  step("saveFields", await text("shipping-authoring-save-fields"));
  step("saveCode", await text("shipping-authoring-save-code"));
  step("modeBanner", await text("shipping-authoring-mode-banner"));
  step("bodyAfterApply", (await text(null)).slice(0, 1600));
  await shot("04-after-apply.png");

  // Fresh reload: the exact saved values must come back from the server.
  await send("Page.reload", { ignoreCache: true });
  await waitFor(`document.querySelector('[data-testid="shipping-authoring-edit"]')`, "reload");
  await sleep(1200);
  step("reloadedBody", (await text(null)).slice(0, 1800));
  await shot("05-reloaded.png");
  step("tabRegionsReload", await click("shipping-authoring-tab-regions"));
  await sleep(700);
  step(
    "reloadedChips",
    await evaluate(
      `Array.from(document.querySelectorAll('[data-testid^="shipping-authoring-area-"]')).map((n) => n.innerText)`,
    ),
  );
  await shot("06-reloaded-regions.png");

  report.netFailures = netFailures.map((f) => f.errorText ?? f.blockedReason ?? "failed");
  report.consoleErrors = consoleErrors;
  console.log(JSON.stringify(report, null, 2));
}

main()
  .catch((error) => {
    console.log(JSON.stringify({ error: String(error) }, null, 2));
    process.exitCode = 1;
  })
  .finally(() => {
    try {
      child.kill();
    } catch {
      /* ignore */
    }
    setTimeout(() => process.exit(process.exitCode ?? 0), 400);
  });
