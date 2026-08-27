import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";
import http from "node:http";

const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9739;
const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "acc-probe3-"));
const chromeProcess = spawn(
  chrome,
  [
    `--remote-debugging-port=${port}`,
    `--user-data-dir=${userData}`,
    "--headless=new",
    "--disable-gpu",
    "--window-size=1440,900",
    "about:blank",
  ],
  { stdio: "ignore" },
);

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => resolve(JSON.parse(data)));
    }).on("error", reject);
  });
}

let wsUrl = null;
for (let i = 0; i < 40; i += 1) {
  try {
    const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
    const page = pages.find((item) => item.type === "page") || pages[0];
    if (page?.webSocketDebuggerUrl) {
      wsUrl = page.webSocketDebuggerUrl;
      break;
    }
  } catch {}
  await delay(250);
}

const socket = new WebSocket(wsUrl);
await new Promise((resolve, reject) => {
  socket.addEventListener("open", resolve);
  socket.addEventListener("error", reject);
});

let nextId = 1;
const pending = new Map();
const failed = [];
const reqs = new Map();
socket.addEventListener("message", (event) => {
  const message = JSON.parse(String(event.data));
  if (message.method === "Network.requestWillBeSent") {
    reqs.set(message.params.requestId, message.params.request.url);
  }
  if (message.method === "Network.responseReceived") {
    const status = message.params.response.status;
    if (status >= 400) {
      failed.push({ status, url: message.params.response.url });
    }
  }
  if (message.method === "Network.loadingFailed") {
    failed.push({
      status: "fail",
      url: reqs.get(message.params.requestId) || message.params.errorText,
      error: message.params.errorText,
    });
  }
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

await send("Page.enable");
await send("Runtime.enable");
await send("Network.enable");
await send("Page.navigate", { url: "http://127.0.0.1:3000/admin/access-control" });
await delay(12000);
console.log(JSON.stringify(failed.slice(0, 30), null, 2));
socket.close();
chromeProcess.kill();
