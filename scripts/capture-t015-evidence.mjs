import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T015");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9242;
const host = "http://127.0.0.1:5088";
const web = "http://127.0.0.1:3000";
const seedActor = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const emptyActor = "cccccccc-cccc-4ccc-8ccc-000000000014";
const savedDisplayName = "سارا احمدی نمایشی";

fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (error) {
          reject(error);
        }
      });
    }).on("error", reject);
  });
}

async function hostFetch(pathname, { method = "GET", actor = seedActor, body } = {}) {
  const headers = { Accept: "application/json" };
  if (actor) headers["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (body) headers["Content-Type"] = "application/json";
  const response = await fetch(`${host}${pathname}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json, text };
}

async function withSession(viewport, work) {
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t015-chrome-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${userData}`,
      "--headless=new",
      "--disable-gpu",
      "--hide-scrollbars",
      `--window-size=${viewport.width},${viewport.height}`,
      "about:blank",
    ],
    { stdio: "ignore" },
  );

  let webSocketUrl = null;
  for (let attempt = 0; attempt < 40; attempt += 1) {
    try {
      const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // Chrome هنوز آماده نیست.
    }
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("Chrome CDP not ready");
  }

  const socket = new WebSocket(webSocketUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve);
    socket.addEventListener("error", reject);
  });
  let nextId = 1;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const handlers = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) handlers.reject(new Error(JSON.stringify(message.error)));
      else handlers.resolve(message.result);
    }
  });
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });

  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: viewport.mobile === true,
    });
    await work({ send });
  } finally {
    socket.close();
    chromeProcess.kill();
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {
      // پاک‌سازی best-effort پروفایل موقت.
    }
  }
}

async function navigate(send, url, readyText) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 60; attempt += 1) {
    await delay(200);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body && document.body.innerText.includes(${JSON.stringify(readyText)}), overflow: document.documentElement.scrollWidth > innerWidth + 2, text: (document.body?.innerText || '').slice(0, 240) })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) {
      if (result.result.value.overflow) throw new Error(`horizontal overflow at ${url}`);
      return;
    }
  }
  const last = await send("Runtime.evaluate", {
    expression: `(document.body?.innerText || '').slice(0, 500)`,
    returnByValue: true,
  });
  throw new Error(`page did not become ready: ${url} :: ${last.result.value}`);
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function setActor(send, actorId) {
  await send("Runtime.evaluate", {
    expression: `localStorage.setItem('tooba.customerActorUserId', ${JSON.stringify(actorId)}); true`,
    returnByValue: true,
  });
}

async function setFieldByLabel(send, labelText, value) {
  await send("Runtime.evaluate", {
    expression: `(() => {
      const labelText = ${JSON.stringify(labelText)};
      const value = ${JSON.stringify(value)};
      const label = [...document.querySelectorAll('label')].find((el) => (el.innerText || '').trim().startsWith(labelText));
      if (!label) return false;
      const field = label.parentElement?.querySelector('input, textarea') || label.querySelector('input, textarea');
      if (!field) return false;
      const proto = field.tagName === 'TEXTAREA' ? window.HTMLTextAreaElement.prototype : window.HTMLInputElement.prototype;
      const setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
      setter?.call(field, value);
      field.dispatchEvent(new Event('input', { bubbles: true }));
      field.dispatchEvent(new Event('change', { bubbles: true }));
      return true;
    })()`,
    returnByValue: true,
  });
}

async function clickText(send, text) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const needle = ${JSON.stringify(text)};
      const nodes = [...document.querySelectorAll('button, a, [role="button"]')];
      const match = nodes.find((node) => (node.innerText || node.textContent || '').includes(needle));
      if (!match) return false;
      match.click();
      return true;
    })()`,
    returnByValue: true,
  });
  if (!result.result.value) throw new Error(`click target missing: ${text}`);
}

// API probes run after visual capture (see bottom).

await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
  await hostFetch("/v1/customer/profile", {
    method: "PUT",
    body: {
      displayName: "مشتری نمایشی توبا",
      birthDate: "1403/06/04",
      bio: "پروفایل آزمایشی Development برای اتصال UI Shopeiva.",
    },
  });

  await send("Page.navigate", { url: web });
  await delay(500);
  await setActor(send, seedActor);

  await navigate(send, `${web}/customer-panel/profile`, "اطلاعات پروفایل");
  await delay(400);
  await screenshot(send, "03-profile-desktop-before-save.png");

  await setFieldByLabel(send, "نام و نام خانوادگی", "John");
  await send("Runtime.evaluate", {
    expression: `(() => {
      const label = [...document.querySelectorAll('label')].find((el) => (el.innerText || '').trim().startsWith('نام و نام خانوادگی'));
      const field = label?.parentElement?.querySelector('input');
      field?.focus();
      field?.blur();
      return true;
    })()`,
    returnByValue: true,
  });
  await delay(400);
  await clickText(send, "ذخیره اطلاعات");
  await delay(500);
  await screenshot(send, "05-profile-validation.png");

  await send("Page.navigate", { url: `${web}/customer-panel/profile` });
  await delay(800);
  await setActor(send, seedActor);
  await navigate(send, `${web}/customer-panel/profile`, "اطلاعات پروفایل");
  await setFieldByLabel(send, "نام و نام خانوادگی", savedDisplayName);
  await setFieldByLabel(send, "بیوگرافی", "پروفایل آزمون R1 — ذخیرهٔ زنده برای شواهد.");
  await clickText(send, "ذخیره اطلاعات");
  await delay(900);
  await screenshot(send, "04-profile-desktop-after-save.png");

  await delay(300);
  await screenshot(send, "06-profile-readonly-identity-fields.png");

  await send("Page.navigate", { url: `${web}/customer-panel` });
  await delay(800);
  await setActor(send, seedActor);
  await navigate(send, `${web}/customer-panel`, "خوش آمدی");
  await delay(500);
  await screenshot(send, "08-profile-dashboard-reflection.png");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await send("Page.navigate", { url: web });
  await delay(400);
  await setActor(send, seedActor);
  await navigate(send, `${web}/customer-panel/profile`, "اطلاعات پروفایل");
  await delay(400);
  await screenshot(send, "09-profile-mobile-390x844.png");
});

const seedGet = await hostFetch("/v1/customer/profile");
const seedDashboard = await hostFetch("/v1/customer/dashboard");
const anonGet = await hostFetch("/v1/customer/profile", { actor: null });
const emptyGet = await hostFetch("/v1/customer/profile", { actor: emptyActor });
const emptyPut = await hostFetch("/v1/customer/profile", {
  method: "PUT",
  actor: emptyActor,
  body: { displayName: "نفوذ", birthDate: "", bio: "" },
});

const probePath = path.join(outDir, "_api-probe-r1.json");
fs.writeFileSync(
  probePath,
  JSON.stringify(
    {
      seedGetStatus: seedGet.status,
      seedDisplayName: seedGet.json?.displayName || seedGet.json?.DisplayName,
      dashboardStatus: seedDashboard.status,
      dashboardDisplayName: seedDashboard.json?.displayName || seedDashboard.json?.DisplayName,
      anonGetStatus: anonGet.status,
      emptyGetStatus: emptyGet.status,
      emptyPutStatus: emptyPut.status,
    },
    null,
    2,
  ),
);
console.log("wrote", probePath);
console.log("capture complete");
