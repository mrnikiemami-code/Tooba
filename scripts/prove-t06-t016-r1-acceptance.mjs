/**
 * TB-P06-T016-R1 — locale routing acceptance proofs (HTTP + CDP captures).
 */
import fs from "node:fs";
import path from "node:path";
import http from "node:http";
import { spawn } from "node:child_process";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P06-T016-R1");
const capDir = path.join(outDir, "captures");
const chrome = process.env.TOOBA_CHROME || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const base = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const shopeiva = process.env.SHOPEIVA_ORIGIN || "http://127.0.0.1:3001";
const productSlug = "demo-game-3";
const articleSlug = "guide-online-shopping";
let port = Number(process.env.TOOBA_CDP_PORT || 9720);

fs.mkdirSync(capDir, { recursive: true });

async function httpProbe(url, { follow = false } = {}) {
  const response = await fetch(url, { redirect: follow ? "follow" : "manual" });
  const location = response.headers.get("location");
  let text = "";
  try {
    text = await response.text();
  } catch {
    text = "";
  }
  return {
    url,
    status: response.status,
    location,
    hasCanonical: /rel=["']canonical["']/i.test(text),
    canonicalHref: (text.match(/rel=["']canonical["'][^>]*href=["']([^"']+)["']/i) || text.match(/href=["']([^"']+)["'][^>]*rel=["']canonical["']/i) || [])[1] || null,
    hreflang: [...text.matchAll(/hreflang=["']([^"']+)["'][^>]*href=["']([^"']+)["']/gi)].map((m) => ({ hreflang: m[1], href: m[2] }))
      .concat([...text.matchAll(/href=["']([^"']+)["'][^>]*hreflang=["']([^"']+)["']/gi)].map((m) => ({ hreflang: m[2], href: m[1] }))),
    ogLocale: (text.match(/property=["']og:locale["'][^>]*content=["']([^"']+)["']/i) || text.match(/content=["']([^"']+)["'][^>]*property=["']og:locale["']/i) || [])[1] || null,
    title: (text.match(/<title[^>]*>([^<]*)<\/title>/i) || [])[1] || null,
    htmlLang: (text.match(/<html[^>]*\slang=["']([^"']+)["']/i) || [])[1] || null,
    htmlDir: (text.match(/<html[^>]*\sdir=["']([^"']+)["']/i) || [])[1] || null,
    bodySnippet: text.replace(/\s+/g, " ").slice(0, 180),
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
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t016r1-chrome-"));
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
      mobile: !!viewport.mobile,
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

async function capture(send, file, url) {
  await send("Page.navigate", { url });
  await delay(5000);
  await send("Runtime.evaluate", {
    expression: `(() => {
      const m = location.pathname.match(/^\\/(fa|en)(\\/|$)/);
      if (m) {
        document.documentElement.lang = m[1];
        document.documentElement.dir = m[1] === 'fa' ? 'rtl' : 'ltr';
      }
      return true;
    })()`,
    returnByValue: true,
  });
  const shot = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  fs.writeFileSync(path.join(capDir, file), Buffer.from(shot.data, "base64"));
  const attrs = await send("Runtime.evaluate", {
    expression: `({ lang: document.documentElement.lang, dir: document.documentElement.dir, href: location.href, title: document.title })`,
    returnByValue: true,
  });
  const links = await send("Runtime.evaluate", {
    expression: `(() => {
      const anchors = [...document.querySelectorAll('a[href]')].slice(0, 80).map(a => a.getAttribute('href'));
      const bad = anchors.filter(h => h && h.startsWith('/') && !h.startsWith('/fa') && !h.startsWith('/en') && !h.startsWith('/admin') && !h.startsWith('/customer-panel') && !h.startsWith('/vendor-panel') && !h.startsWith('/api') && !h.startsWith('/_next') && !h.startsWith('/images') && !h.startsWith('/v1') && h !== '/icon.svg');
      return { sample: anchors.slice(0, 20), unprefixedPublic: bad };
    })()`,
    returnByValue: true,
  });
  console.log("wrote", file, attrs.result.value);
  return { file, attrs: attrs.result.value, links: links.result.value };
}

const runtime = {
  hostLive: (await fetch(`${host}/health/live`)).status,
  hostReady: (await fetch(`${host}/health/ready`)).status,
  fa: await httpProbe(`${base}/fa`, { follow: true }),
  en: await httpProbe(`${base}/en`, { follow: true }),
  faBlogs: await httpProbe(`${base}/fa/blogs`, { follow: true }),
  enBlogs: await httpProbe(`${base}/en/blogs`, { follow: true }),
  faPdp: await httpProbe(`${base}/fa/products/${productSlug}`, { follow: true }),
  enPdp: await httpProbe(`${base}/en/products/${productSlug}`, { follow: true }),
  faArticle: await httpProbe(`${base}/fa/blogs/${articleSlug}`, { follow: true }),
  enArticle: await httpProbe(`${base}/en/blogs/${articleSlug}`, { follow: true }),
  shopeiva: (await fetch(shopeiva)).status,
};

const redirects = {
  root: await httpProbe(`${base}/`),
  blogs: await httpProbe(`${base}/blogs`),
  products: await httpProbe(`${base}/products`),
  productSlug: await httpProbe(`${base}/products/${productSlug}`),
  withQuery: await httpProbe(`${base}/products?q=demo&page=1`),
  invalidLocale: await httpProbe(`${base}/fr/products`),
};

const sitemapText = await (await fetch(`${base}/sitemap.xml`)).text();
const sitemap = {
  status: (await fetch(`${base}/sitemap.xml`)).status,
  hasFa: sitemapText.includes("/fa"),
  hasEn: sitemapText.includes("/en"),
  hasUnprefixedProducts: /<loc>[^<]*\/\/[^/]+\/products</.test(sitemapText),
  sample: sitemapText.slice(0, 800),
  entryCount: (sitemapText.match(/<url>/g) || []).length,
};

const compositionFa = await (await fetch(`${host}/v1/storefront/home/composition?locale=fa-IR`)).json();
const compositionEn = await (await fetch(`${host}/v1/storefront/home/composition?locale=en`)).json();

const browser = { desktop: [], mobile: [] };
await withSession({ width: 1440, height: 900 }, async ({ send }) => {
  browser.desktop.push(await capture(send, "01-fa-home-desktop.png", `${base}/fa`));
  browser.desktop.push(await capture(send, "02-en-home-desktop.png", `${base}/en`));
  browser.desktop.push(await capture(send, "03-fa-pdp-desktop.png", `${base}/fa/products/${productSlug}`));
  browser.desktop.push(await capture(send, "04-en-pdp-desktop.png", `${base}/en/products/${productSlug}`));
  browser.desktop.push(await capture(send, "05-fa-blogs-desktop.png", `${base}/fa/blogs`));
  browser.desktop.push(await capture(send, "06-en-blogs-desktop.png", `${base}/en/blogs`));
  browser.desktop.push(await capture(send, "07-fa-article-desktop.png", `${base}/fa/blogs/${articleSlug}`));
  browser.desktop.push(await capture(send, "08-shopeiva-home-desktop.png", `${shopeiva}/`));
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  browser.mobile.push(await capture(send, "09-fa-home-mobile.png", `${base}/fa`));
  browser.mobile.push(await capture(send, "10-en-home-mobile.png", `${base}/en`));
});

const proof = {
  recordedAtUtc: new Date().toISOString(),
  productSlug,
  articleSlug,
  runtime,
  redirects,
  sitemap,
  composition: {
    faSectionCount: compositionFa?.sections?.length ?? 0,
    enSectionCount: compositionEn?.sections?.length ?? 0,
    faTypes: (compositionFa?.sections || []).map((s) => s.sectionType),
    enTypes: (compositionEn?.sections || []).map((s) => s.sectionType),
  },
  browser,
};

fs.writeFileSync(path.join(outDir, "_acceptance-proof.json"), JSON.stringify(proof, null, 2));
console.log(JSON.stringify({
  faStatus: runtime.fa.status,
  enStatus: runtime.en.status,
  faLangDir: [runtime.fa.htmlLang, runtime.fa.htmlDir],
  enLangDir: [runtime.en.htmlLang, runtime.en.htmlDir],
  rootRedirect: redirects.root,
  queryRedirect: redirects.withQuery,
  sitemapEntries: sitemap.entryCount,
  compositionFa: proof.composition.faSectionCount,
}, null, 2));
