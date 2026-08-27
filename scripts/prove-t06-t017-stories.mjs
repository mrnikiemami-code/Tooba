/**
 * TB-P06-T017 — live Stories + Shopeiva UI acceptance proofs (HTTP + CDP).
 * CDP pattern mirrors scripts/prove-t06-t016-r1-acceptance.mjs (native WebSocket).
 */
import fs from "node:fs";
import path from "node:path";
import http from "node:http";
import { spawn } from "node:child_process";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T017");
const capDir = path.join(outDir, "captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const base = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
let port = Number(process.env.TOOBA_CDP_PORT || 9731);

fs.mkdirSync(capDir, { recursive: true });

async function httpJson(url) {
  const response = await fetch(url, { redirect: "manual" });
  const text = await response.text();
  let json = null;
  try {
    json = JSON.parse(text);
  } catch {
    json = null;
  }
  return { url, status: response.status, json, text: text.slice(0, 400) };
}

async function httpProbe(url) {
  const response = await fetch(url, { redirect: "manual" });
  const text = await response.text();
  return {
    url,
    status: response.status,
    hasHomeStoriesMarker: text.includes('data-testid="home-stories"'),
    hasFakeStoryImagesConst: text.includes("STORY_IMAGES"),
    title: (text.match(/<title[^>]*>([^<]*)<\/title>/i) || [])[1] || null,
  };
}

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

async function withSession(viewport, work) {
  const sessionPort = port++;
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t017-chrome-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${sessionPort}`,
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
      const pages = await getJson(`http://127.0.0.1:${sessionPort}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // wait
    }
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("CDP not ready");
  }
  const socket = new WebSocket(webSocketUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve);
    socket.addEventListener("error", reject);
  });
  let id = 0;
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
      const messageId = ++id;
      pending.set(messageId, { resolve, reject });
      socket.send(JSON.stringify({ id: messageId, method, params }));
    });
  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: viewport.width < 800,
    });
    return await work({ send });
  } finally {
    try {
      socket.close();
    } catch {}
    try {
      chromeProcess.kill();
    } catch {}
    await delay(300);
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {}
  }
}

async function evalValue(send, expression) {
  const result = await send("Runtime.evaluate", {
    expression,
    awaitPromise: true,
    returnByValue: true,
  });
  return result?.result?.value;
}

async function screenshot(send, fileName) {
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(capDir, fileName), Buffer.from(shot.data, "base64"));
  return fileName;
}

const proof = {
  taskId: "TB-P06-T017",
  recordedAtUtc: new Date().toISOString(),
  host,
  base,
  shopeiva,
  http: {},
  browser: {},
  captures: [],
  checks: {},
};

proof.http.hostHealth = await httpProbe(`${host}/health`);
proof.http.storiesFa = await httpJson(`${host}/v1/storefront/stories?locale=fa`);
proof.http.storiesEn = await httpJson(`${host}/v1/storefront/stories?locale=en`);
proof.http.storiesViaFe = await httpJson(`${base}/v1/storefront/stories?locale=fa`);
proof.http.homeFa = await httpProbe(`${base}/fa`);
proof.http.adminStories = await httpProbe(`${base}/admin/stories`);
proof.http.shopeivaHome = await httpProbe(`${shopeiva}/`);

const faStories = Array.isArray(proof.http.storiesFa.json) ? proof.http.storiesFa.json : [];
const enStories = Array.isArray(proof.http.storiesEn.json) ? proof.http.storiesEn.json : [];
proof.checks.publicFaHasAtLeastTwo = faStories.length >= 2;
proof.checks.publicFaHasMobile = faStories.some((s) => s.title === "موبایل" || s.Title === "موبایل");
proof.checks.publicFaHasGames = faStories.some((s) => s.title === "بازی" || s.Title === "بازی");
proof.checks.publicFaExcludesEnglishRail = !faStories.some((s) => (s.title || s.Title) === "English rail");
proof.checks.publicEnHasEnglishRail = enStories.some((s) => (s.title || s.Title) === "English rail");
proof.checks.publicFaHasVideo = faStories.some((s) => s.isVideo || s.IsVideo);
proof.checks.publicFaHasStoryCta = faStories.some((s) => (s.ctaType || s.CtaType) && (s.ctaType || s.CtaType) !== "none");
proof.checks.feProxyStoriesOk = proof.http.storiesViaFe.status === 200 && Array.isArray(proof.http.storiesViaFe.json);
proof.checks.noFakeStoryImagesInHomeSsr = !proof.http.homeFa.hasFakeStoryImagesConst;

await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  await send("Page.navigate", { url: `${base}/fa` });
  let hydrated = false;
  for (let i = 0; i < 24; i += 1) {
    hydrated = Boolean(await evalValue(send, `!!document.querySelector('[data-testid="home-stories"]')`));
    if (hydrated) break;
    await delay(500);
  }
  proof.browser.homeStoriesHydrated = hydrated;
  proof.browser.storyCircleCount = await evalValue(
    send,
    `document.querySelectorAll('[data-testid="home-stories"] button').length`,
  );
  proof.captures.push(await screenshot(send, "01-tooba-home-stories-rail.png"));

  const point = await evalValue(
    send,
    `(() => {
      const el = document.querySelector('[data-testid="home-stories"] button');
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
    })()`,
  );
  proof.browser.storyClickPoint = point;
  if (point) {
    await send("Input.dispatchMouseEvent", {
      type: "mousePressed",
      x: point.x,
      y: point.y,
      button: "left",
      clickCount: 1,
    });
    await send("Input.dispatchMouseEvent", {
      type: "mouseReleased",
      x: point.x,
      y: point.y,
      button: "left",
      clickCount: 1,
    });
    await delay(1200);
  }
  proof.browser.storyModalVisible = Boolean(await evalValue(send, `!!document.querySelector('[data-testid="story-modal"]')`));
  proof.captures.push(await screenshot(send, "02-tooba-story-modal.png"));

  await send("Page.navigate", { url: `${shopeiva}/` });
  await delay(2500);
  proof.browser.shopeivaHasStoriesHeading = Boolean(
    await evalValue(send, `document.body.innerText.includes('استوری')`),
  );
  proof.captures.push(await screenshot(send, "03-shopeiva-home-stories.png"));

  await send("Page.navigate", { url: `${base}/admin/stories` });
  await delay(2500);
  proof.browser.adminStoriesPage = Boolean(
    await evalValue(
      send,
      `!!document.querySelector('[data-testid="admin-stories"]') || document.body.innerText.includes('استوری')`,
    ),
  );
  proof.captures.push(await screenshot(send, "04-admin-stories.png"));
});

proof.checks.browserHomeStoriesVisible = proof.browser.homeStoriesHydrated === true;
proof.checks.browserStoryCirclesPresent = Number(proof.browser.storyCircleCount || 0) >= 2;
proof.checks.browserStoryModalVisible = proof.browser.storyModalVisible === true;
proof.checks.browserAdminStories = proof.browser.adminStoriesPage === true;
proof.pass = Object.values(proof.checks).every(Boolean);

fs.writeFileSync(path.join(outDir, "_acceptance-proof.json"), JSON.stringify(proof, null, 2));
console.log(JSON.stringify({ pass: proof.pass, checks: proof.checks, browser: proof.browser, captures: proof.captures }, null, 2));
process.exit(proof.pass ? 0 : 2);
