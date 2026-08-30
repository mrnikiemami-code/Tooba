import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const OUT = 'D:/Users/User/source/repos/SarvNewVer/docs/evidence/TB-P07-T035/screenshots';
const REPORT_NOTES = 'D:/Users/User/source/repos/SarvNewVer/docs/evidence/TB-P07-T035/audit-notes.json';
const ADMIN = 'http://127.0.0.1:3000';
const SHOPEIVA = 'http://127.0.0.1:3001';
const DEV_ACTOR = '01a036c2-970e-7000-8eb7-94bf5cc2d8db';

fs.mkdirSync(OUT, { recursive: true });

const notes = {
  capturedAt: new Date().toISOString(),
  pages: [],
  incompleteFindings: [],
  productsSample: [],
  materialMismatches: [],
};

function scanIncomplete(text, pageLabel) {
  const patterns = [
    { re: /Coming soon/gi, label: 'Coming soon' },
    { re: /به‌زودی/g, label: 'به‌زودی' },
    { re: /\bTODO\b/g, label: 'TODO' },
    { re: /Bad Request/gi, label: 'Bad Request' },
    { re: /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, label: 'raw GUID' },
  ];
  for (const p of patterns) {
    const matches = text.match(p.re);
    if (matches && matches.length) {
      notes.incompleteFindings.push({
        page: pageLabel,
        kind: p.label,
        count: matches.length,
        samples: [...new Set(matches)].slice(0, 5),
      });
    }
  }
}

async function shot(page, name, fullPage = true) {
  const file = path.join(OUT, name);
  await page.screenshot({ path: file, fullPage });
  return file;
}

async function waitSettled(page, ms = 1500) {
  await page.waitForLoadState('networkidle', { timeout: 20000 }).catch(() => {});
  await page.waitForTimeout(ms);
}

(async () => {
  const browser = await chromium.launch({
    headless: true,
    executablePath:
      'C:/Users/User/AppData/Local/ms-playwright/chromium-1148/chrome-win/chrome.exe',
  });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: 'fa-IR',
    extraHTTPHeaders: {
      'X-Tooba-Dev-Actor-User-Id': DEV_ACTOR,
    },
  });
  const page = await context.newPage();

  // ---- 1. Products grid ----
  await page.goto(`${ADMIN}/fa/admin/products`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 2500);
  const productsBody = await page.locator('body').innerText();
  scanIncomplete(productsBody, 'products-list');

  const hasPrice = /قیمت|Price|Stock|موجودی/i.test(productsBody);
  const pagingText = await page.locator('body').innerText();
  const totalMatch = pagingText.match(/283|از\s*283|of\s*283/i);

  // Try to read AG Grid header cells
  const headers = await page.locator('.ag-header-cell-text, [role="columnheader"]').allInnerTexts().catch(() => []);
  const rowCount = await page.locator('.ag-center-cols-container .ag-row, [role="row"]').count().catch(() => 0);

  notes.pages.push({
    route: '/fa/admin/products',
    headers,
    rowCountVisible: rowCount,
    mentions283: !!totalMatch,
    mentionsPriceOrStock: hasPrice,
    bodySnippet: productsBody.slice(0, 1200),
  });

  await shot(page, '01-products-grid.png', true);
  // viewport-only commercial look
  await page.screenshot({ path: path.join(OUT, '01b-products-grid-viewport.png'), fullPage: false });

  // Collect draft product links from grid + query API
  const productLinks = await page.evaluate(() => {
    const anchors = [...document.querySelectorAll('a[href*="/admin/products/"]')];
    const uniq = [];
    const seen = new Set();
    for (const a of anchors) {
      const href = a.getAttribute('href') || '';
      if (!/\/admin\/products\/[^/]+/.test(href)) continue;
      if (/\/new|\/create/.test(href)) continue;
      const id = href.match(/\/admin\/products\/([^/?#]+)/)?.[1];
      if (!id || id === 'new' || seen.has(id)) continue;
      seen.add(id);
      uniq.push({ href, text: (a.textContent || '').trim().slice(0, 80), id });
      if (uniq.length >= 12) break;
    }
    return uniq;
  });

  notes.productsSample = productLinks;

  const opened = [];

  let productIds = productLinks.map((p) => p.id).filter(Boolean);

  // Query admin products API through FE proxy
  const apiProbe = await page.evaluate(async () => {
    const headers = {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-Tooba-Dev-Actor-User-Id': '01a036c2-970e-7000-8eb7-94bf5cc2d8db',
    };
    const tries = [
      { url: '/v1/admin/products/query', method: 'POST', body: JSON.stringify({ page: 1, pageSize: 10 }) },
      { url: '/v1/admin/products?page=1&pageSize=10', method: 'GET' },
    ];
    for (const t of tries) {
      try {
        const res = await fetch(t.url, {
          method: t.method,
          headers,
          body: t.method === 'POST' ? t.body : undefined,
        });
        const text = await res.text();
        return { url: t.url, status: res.status, body: text.slice(0, 4000) };
      } catch (e) {
        /* continue */
      }
    }
    return null;
  });
  notes.productsApiProbe = apiProbe;
  if (apiProbe?.body) {
    try {
      const parsed = JSON.parse(apiProbe.body);
      const items = parsed.items || parsed.data || parsed.products || parsed.results || [];
      if (Array.isArray(items)) {
        for (const it of items) {
          const id = it.id || it.productId;
          if (id && !productIds.includes(id)) productIds.push(id);
        }
      }
    } catch {
      /* ignore */
    }
  }

  if (productIds.length < 4) {
    await page.goto(`${ADMIN}/fa/admin/products`, { waitUntil: 'domcontentloaded' });
    await waitSettled(page, 2500);
    const fromGrid = await page.evaluate(() => {
      const rows = [...document.querySelectorAll('.ag-row')];
      const ids = [];
      for (const r of rows) {
        const a = r.querySelector('a[href*="/products/"]');
        if (!a) continue;
        const m = (a.getAttribute('href') || '').match(/\/products\/([^/?#]+)/);
        if (m && m[1] !== 'new') ids.push(m[1]);
      }
      return [...new Set(ids)];
    });
    productIds = [...new Set([...productIds, ...fromGrid])];
  }

  notes.resolvedProductIds = productIds.slice(0, 8);

  // Canonical routes: /fa/admin/products/{id}?scope=view|edit
  let viewIdx = 2;
  let viewsDone = 0;
  for (const id of productIds) {
    if (viewsDone >= 3) break;
    const url = `${ADMIN}/fa/admin/products/${id}?scope=view`;
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => null);
    await waitSettled(page, 2500);
    const text = await page.locator('body').innerText();
    const bad = /404|Not Found|صفحه پیدا نشد|Cannot GET/i.test(text.slice(0, 500));
    if (!bad) {
      scanIncomplete(text, `product-view-${viewsDone + 1}`);
      await shot(page, `${String(viewIdx).padStart(2, '0')}-product-view-${viewsDone + 1}.png`, true);
      await page.screenshot({
        path: path.join(OUT, `${String(viewIdx).padStart(2, '0')}b-product-view-${viewsDone + 1}-viewport.png`),
        fullPage: false,
      });
      opened.push({ label: `product-view-${viewsDone + 1}`, url: page.url(), id, snippet: text.slice(0, 1800) });
      viewsDone++;
      viewIdx++;
    } else {
      notes.pages.push({ note: `failed to open view for ${id}`, snippet: text.slice(0, 400) });
    }
  }

  // EDIT one product
  if (productIds[0]) {
    const url = `${ADMIN}/fa/admin/products/${productIds[0]}?scope=edit`;
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => null);
    await waitSettled(page, 3000);
    const text = await page.locator('body').innerText();
    if (!/404|Not Found|Cannot GET/i.test(text.slice(0, 300))) {
      scanIncomplete(text, 'product-edit-1');
      await shot(page, '05-product-edit-1.png', true);
      await page.screenshot({ path: path.join(OUT, '05b-product-edit-1-viewport.png'), fullPage: false });
      opened.push({ label: 'product-edit-1', url: page.url(), id: productIds[0], snippet: text.slice(0, 2500) });
      notes.editStructure = {
        hasSummaryCards: /رسانه|تنوع|SEO|ترجم|آمادگی انتشار|وضعیت محصول/.test(text),
        hasTabs: /عمومی|ترجمه‌ها|ویژگی|تنوع|رسانه|انتشار|تاریخچه/.test(text),
        hasSave: /ذخیره/.test(text),
        hasCancel: /انصراف|پایان ویرایش/.test(text),
        hasMediaSidebar: /رسانه|گالری|تصویر/.test(text),
        hasChecklist: /چک.?لیست|تکمیل/.test(text),
        hasLanguagesPanel: /فارسی|English|العربية|ترجم/.test(text),
        hasDraftBadge: /پیش‌نویس|Draft/i.test(text),
        hasEditBadge: /ویرایش|Edit/i.test(text),
        snippet: text.slice(0, 3000),
      };
    }
  }

  notes.openedProducts = opened;

  // ---- Categories ----
  await page.goto(`${ADMIN}/fa/admin/categories`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 2500);
  let catText = await page.locator('body').innerText();
  scanIncomplete(catText, 'categories-tree');
  await shot(page, '06-categories-tree.png', true);
  await page.screenshot({ path: path.join(OUT, '06b-categories-tree-viewport.png'), fullPage: false });
  notes.pages.push({ route: '/fa/admin/categories', snippet: catText.slice(0, 1500) });

  // Click first expandable / category item for workspace
  const catClicked = await page.evaluate(() => {
    const candidates = [
      ...document.querySelectorAll('[role="treeitem"], .tree-item, [data-testid*="categor"], a[href*="/categories/"]'),
    ];
    for (const el of candidates) {
      const t = (el.textContent || '').trim();
      if (t.length > 1 && t.length < 80) {
        el.click();
        return t;
      }
    }
    // fallback: any button-like in sidebar
    const buttons = [...document.querySelectorAll('button, [role="button"]')];
    for (const b of buttons) {
      const t = (b.textContent || '').trim();
      if (t && t.length < 40 && !/افزودن|Add|Save|ذخیره/.test(t)) {
        b.click();
        return t;
      }
    }
    return null;
  });
  await waitSettled(page, 2000);
  catText = await page.locator('body').innerText();
  scanIncomplete(catText, 'category-workspace');
  await shot(page, '07-category-workspace.png', true);
  notes.pages.push({ route: 'category-workspace', clicked: catClicked, snippet: catText.slice(0, 1500) });

  // Try tabs if present
  const tabClicked = await page.evaluate(() => {
    const tabs = [...document.querySelectorAll('[role="tab"], button')].filter((el) =>
      /محصول|ترجم|ویژگی|فیلتر|رسانه|عمومی|Products|Attributes/i.test(el.textContent || ''),
    );
    if (tabs[1]) {
      tabs[1].click();
      return (tabs[1].textContent || '').trim();
    }
    if (tabs[0]) {
      tabs[0].click();
      return (tabs[0].textContent || '').trim();
    }
    return null;
  });
  await waitSettled(page, 1500);
  await shot(page, '07b-category-workspace-tab.png', true);
  notes.pages.push({ categoryTab: tabClicked });

  // ---- Product create (do not publish) ----
  await page.goto(`${ADMIN}/fa/admin/products/new`, { waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => null);
  await waitSettled(page, 2500);
  {
    const text = await page.locator('body').innerText();
    scanIncomplete(text, 'product-create');
    await shot(page, '08-product-create.png', true);
    await page.screenshot({ path: path.join(OUT, '08b-product-create-viewport.png'), fullPage: false });
    notes.pages.push({ route: '/fa/admin/products/new', snippet: text.slice(0, 1500), url: page.url() });
  }

  // Also try clicking Add Product from list
  await page.goto(`${ADMIN}/fa/admin/products`, { waitUntil: 'domcontentloaded' });
  await waitSettled(page, 1500);
  const addClicked = await page.evaluate(() => {
    const el = [...document.querySelectorAll('a,button')].find((e) =>
      /افزودن محصول|محصول جدید|Add Product|New Product/i.test(e.textContent || ''),
    );
    if (el) {
      el.click();
      return (el.textContent || '').trim();
    }
    return null;
  });
  await waitSettled(page, 2000);
  if (addClicked) {
    const text = await page.locator('body').innerText();
    scanIncomplete(text, 'product-create-via-button');
    await shot(page, '08c-product-create-via-button.png', true);
    notes.pages.push({ createViaButton: addClicked, url: page.url(), snippet: text.slice(0, 1200) });
  }

  // ---- Shopeiva home Popular Brands ----
  await page.goto(`${SHOPEIVA}/`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 3000);
  const shopText = await page.locator('body').innerText();
  scanIncomplete(shopText, 'shopeiva-home');

  const popularSection = await page.evaluate(() => {
    const all = document.body.innerText;
    const hasPopular = /Popular Brands|برندهای محبوب|برندهای پرطرفدار|برندها/i.test(all);
    const hasNoBrand = /No Brand|بدون برند|بدون‌برند/i.test(all);
    const blankish = [...document.querySelectorAll('img')].filter((img) => !img.getAttribute('src') || img.getAttribute('src') === '').length;
    // collect brand-like items near popular
    let popularBlock = '';
    const headings = [...document.querySelectorAll('h1,h2,h3,h4,section,[class*="brand"]')];
    for (const h of headings) {
      if (/Popular|برند/i.test(h.textContent || '')) {
        popularBlock = (h.closest('section') || h.parentElement)?.innerText?.slice(0, 1500) || h.textContent;
        break;
      }
    }
    return { hasPopular, hasNoBrand, blankish, popularBlock, bodySlice: all.slice(0, 2500) };
  });
  notes.shopeiva = popularSection;
  await shot(page, '09-shopeiva-home.png', true);
  await page.screenshot({ path: path.join(OUT, '09b-shopeiva-home-viewport.png'), fullPage: false });

  // Scroll to popular brands if needed
  await page.evaluate(() => {
    const el = [...document.querySelectorAll('*')].find((e) =>
      /Popular Brands|برندهای محبوب|برندهای پرطرفدار/i.test(e.textContent || '') && (e.textContent || '').length < 80,
    );
    el?.scrollIntoView({ behavior: 'instant', block: 'center' });
  });
  await waitSettled(page, 1000);
  await page.screenshot({ path: path.join(OUT, '09c-shopeiva-popular-brands.png'), fullPage: false });

  // Giant blank check heuristics from screenshots metadata
  notes.screenshotFiles = fs.readdirSync(OUT).filter((f) => f.endsWith('.png'));

  fs.writeFileSync(REPORT_NOTES, JSON.stringify(notes, null, 2), 'utf8');
  console.log(JSON.stringify({ ok: true, screenshots: notes.screenshotFiles.length, incomplete: notes.incompleteFindings.length, out: OUT }, null, 2));
  await browser.close();
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
