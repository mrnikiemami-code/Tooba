import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const base = process.env.TOOBA_FE || "http://127.0.0.1:3000";
const port = Number(process.env.TOOBA_CDP_PORT || 9560);
const outFile = path.join(path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1")), "browser-proof.json");
const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-r22-proof-"));
const proc = spawn(chrome, [`--remote-debugging-port=${port}`, `--user-data-dir=${userData}`, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--window-size=1440,900", "about:blank"], { stdio: "ignore" });
const getJson = (url) => new Promise((resolve, reject) => { http.get(url, (r) => { let d = ""; r.on("data", (c) => d += c); r.on("end", () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } }); }).on("error", reject); });
let wsUrl;
for (let i = 0; i < 60; i++) { try { const pages = await getJson(`http://127.0.0.1:${port}/json/list`); const page = pages.find((p) => p.type === "page"); if (page?.webSocketDebuggerUrl) { wsUrl = page.webSocketDebuggerUrl; break; } } catch {} await delay(200); }
if (!wsUrl) { proc.kill(); throw new Error("Chrome CDP not ready"); }
const socket = new WebSocket(wsUrl);
await new Promise((resolve, reject) => { socket.addEventListener("open", resolve); socket.addEventListener("error", reject); });
let id = 1; const pending = new Map();
socket.addEventListener("message", (e) => { const m = JSON.parse(String(e.data)); if (m.id && pending.has(m.id)) { const h = pending.get(m.id); pending.delete(m.id); m.error ? h.reject(new Error(JSON.stringify(m.error))) : h.resolve(m.result); } });
const send = (method, params = {}) => new Promise((resolve, reject) => { const n = id++; pending.set(n, { resolve, reject }); socket.send(JSON.stringify({ id: n, method, params })); });
const evalv = async (expression) => (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true })).result.value;
await send("Page.enable"); await send("Runtime.enable");
await send("Page.navigate", { url: `${base}/fa/admin/stories` }); await delay(5000);
await evalv("document.querySelector('[data-testid=\"story-row-title\"]')?.click()"); await delay(2500);
const storyId = await evalv("location.pathname.split('/').pop()");
const original = await evalv("document.querySelector('[data-testid=\"story-edit-title\"]')?.value");
const setTitle = async (value) => evalv(`(()=>{const i=document.querySelector('[data-testid="story-edit-title"]');const s=Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set;s.call(i,${JSON.stringify(value)});i.dispatchEvent(new Event('input',{bubbles:true}));return i.value})()`);
const reload = async () => { await send("Page.reload", { ignoreCache: true }); await delay(5000); };
const readTitle = () => evalv("document.querySelector('[data-testid=\"story-edit-title\"]')?.value");
const changed = `${original}-R22`;
await setTitle(changed); await delay(300);
await evalv("document.querySelector('[data-testid=\"story-save-title-locale\"]')?.click()"); await delay(3500);
await reload();
const afterChangedReload = await readTitle();
await setTitle(original); await delay(300);
await evalv("document.querySelector('[data-testid=\"story-save-title-locale\"]')?.click()"); await delay(3500);
await reload();
const afterRestoreReload = await readTitle();
const result = { storyId, original, changed, afterChangedReload, afterRestoreReload, url: await evalv("location.href"), ok: afterChangedReload === changed && afterRestoreReload === original };
fs.writeFileSync(outFile, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));
socket.close(); proc.kill();
try { fs.rmSync(userData, { recursive: true, force: true }); } catch {}
if (!result.ok) process.exitCode = 2;
