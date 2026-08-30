import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const OUT = 'D:/Users/User/source/repos/SarvNewVer/docs/evidence/TB-P07-T035/screenshots';
const NOTES = 'D:/Users/User/source/repos/SarvNewVer/docs/evidence/TB-P07-T035/audit-notes-supplement.json';
const ADMIN = 'http://127.0.0.1:3000';
const SHOPEIVA = 'http://127.0.0.1:3001';
const DEV_ACTOR = '01a036c2-970e-7000-8eb7-94bf5cc2d8db';

const notes = { capturedAt: new Date().toISOString() };

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
    extraHTTPHeaders: { 'X-Tooba-Dev-Actor-User-Id': DEV_ACTOR },
  });
  const page = await context.newPage();

  // Products paging / total
  await page.goto(`${ADMIN}/fa/admin/products`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 2500);
  const paging = await page.evaluate(() => {
    const body = document.body.innerText;
    const pager = [...document.querySelectorAll('.ag-paging-panel, [class*="pag"], footer, nav')]
      .map((e) => (e.textContent || '').trim())
      .filter((t) => t && /صفحه|Page|of|از|283|\d+/.test(t))
      .slice(0, 10);
    return {
      bodyHas283: /283/.test(body),
      bodyHasPrice: /قیمت|Price|موجودی|Stock\b/i.test(body),
      pagerTexts: pager,
      last200: body.slice(-800),
    };
  });
  notes.paging = paging;
  // Scroll grid bottom to reveal pager
  await page.evaluate(() => {
    const panel = document.querySelector('.ag-paging-panel') || document.querySelector('.ag-root-wrapper');
    panel?.scrollIntoView({ block: 'end' });
    window.scrollTo(0, document.body.scrollHeight);
  });
  await waitSettled(page, 800);
  await page.screenshot({ path: path.join(OUT, '01c-products-grid-paging.png'), fullPage: true });

  // Categories correct route
  await page.goto(`${ADMIN}/fa/admin/catalog/categories`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 3000);
  let catText = await page.locator('body').innerText();
  notes.categories = { url: page.url(), snippet: catText.slice(0, 2000), incomplete: [] };
  for (const [re, label] of [
    [/Coming soon/gi, 'Coming soon'],
    [/به‌زودی/g, 'به‌زودی'],
    [/\bTODO\b/g, 'TODO'],
    [/Bad Request/gi, 'Bad Request'],
    [/Not found|This route does not exist/gi, 'Not found'],
  ]) {
    const m = catText.match(re);
    if (m) notes.categories.incomplete.push({ label, count: m.length, samples: m.slice(0, 3) });
  }
  await page.screenshot({ path: path.join(OUT, '06-categories-tree.png'), fullPage: true });
  await page.screenshot({ path: path.join(OUT, '06b-categories-tree-viewport.png'), fullPage: false });

  // Click a tree item / L1
  const clicked = await page.evaluate(() => {
    const items = [
      ...document.querySelectorAll('[role="treeitem"], [data-testid*="tree"], .category-tree button, .category-tree a, li button, [class*="tree"] button'),
    ];
    for (const el of items) {
      const t = (el.textContent || '').trim().replace(/\s+/g, ' ');
      if (t.length >= 2 && t.length < 60) {
        el.click();
        return t.slice(0, 80);
      }
    }
    // any clickable with category-like text
    const all = [...document.querySelectorAll('button, a, [role="button"]')];
    for (const el of all) {
      const t = (el.textContent || '').trim();
      if (/دیجیتال|پوشاک|خانه|موبایل|کالای|ورزش|کتاب|غذا|خودرو/.test(t) && t.length < 80) {
        el.click();
        return t.slice(0, 80);
      }
    }
    return null;
  });
  await waitSettled(page, 2500);
  catText = await page.locator('body').innerText();
  notes.categoryWorkspace = { clicked, snippet: catText.slice(0, 2000), tabs: [] };
  await page.screenshot({ path: path.join(OUT, '07-category-workspace.png'), fullPage: true });

  // Click a workspace tab if present
  const tab = await page.evaluate(() => {
    const tabs = [...document.querySelectorAll('[role="tab"], button')].filter((el) =>
      /محصول|ترجم|ویژگی|فیلتر|رسانه|عمومی|منو|برچسب|Products|Attributes|Facets|Mega/i.test(el.textContent || ''),
    );
    const pick = tabs.find((t) => /محصول|Products/i.test(t.textContent || '')) || tabs[1] || tabs[0];
    if (pick) {
      pick.click();
      return (pick.textContent || '').trim().slice(0, 60);
    }
    return null;
  });
  await waitSettled(page, 2000);
  notes.categoryWorkspace.tab = tab;
  notes.categoryWorkspace.afterTabSnippet = (await page.locator('body').innerText()).slice(0, 1500);
  await page.screenshot({ path: path.join(OUT, '07b-category-workspace-tab.png'), fullPage: true });

  // Product VIEW - scroll to description for raw HTML evidence
  const id = '01a05229-4211-7000-9048-43d8fd5998ff';
  await page.goto(`${ADMIN}/fa/admin/products/${id}?scope=view`, { waitUntil: 'domcontentloaded' });
  await waitSettled(page, 2500);
  const viewScan = await page.evaluate(() => {
    const body = document.body.innerText;
    return {
      hasRawHtmlTags: /<\/?[a-z][\s\S]*>/i.test(body) || /<p>|<ul>|<li>|<strong>/.test(body),
      hasIsoTimestamp: /2026-\d{2}-\d{2}T/.test(body),
      hasComingSoon: /Coming soon|به‌زودی|TODO|Bad Request/i.test(body),
      hasGuidClassic: /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i.test(body),
      issuesBadge: (body.match(/\d+\s*Issues|هشدار|Issues/g) || []).slice(0, 5),
      mediaBroken: [...document.querySelectorAll('img')].filter((i) => !i.naturalWidth && i.src).length,
      imgCount: document.querySelectorAll('img').length,
      placeholderCount: [...document.querySelectorAll('*')].filter((e) => /تصویر اصلی|placeholder/i.test(e.textContent || '') && (e.textContent || '').length < 40).length,
    };
  });
  notes.viewScan = viewScan;
  // Click media tab
  await page.evaluate(() => {
    const t = [...document.querySelectorAll('[role="tab"], button, a')].find((e) =>
      /^(رسانه|Media)$/i.test((e.textContent || '').trim()),
    );
    t?.click();
  });
  await waitSettled(page, 1500);
  await page.screenshot({ path: path.join(OUT, '02c-product-view-media-tab.png'), fullPage: false });
  notes.mediaTabSnippet = (await page.locator('body').innerText()).slice(0, 1200);

  // EDIT media tab
  await page.goto(`${ADMIN}/fa/admin/products/${id}?scope=edit`, { waitUntil: 'domcontentloaded' });
  await waitSettled(page, 2500);
  await page.evaluate(() => {
    const t = [...document.querySelectorAll('[role="tab"], button, a')].find((e) =>
      /^(رسانه|Media)$/i.test((e.textContent || '').trim()),
    );
    t?.click();
  });
  await waitSettled(page, 1500);
  await page.screenshot({ path: path.join(OUT, '05c-product-edit-media-tab.png'), fullPage: false });

  // Scroll view general for HTML leak
  await page.goto(`${ADMIN}/fa/admin/products/${id}?scope=view`, { waitUntil: 'domcontentloaded' });
  await waitSettled(page, 2000);
  await page.evaluate(() => {
    const el = [...document.querySelectorAll('*')].find((e) => /<p>|توضیح کامل|خلاصه کوتاه/.test(e.textContent || ''));
    el?.scrollIntoView({ block: 'center' });
  });
  await waitSettled(page, 500);
  await page.screenshot({ path: path.join(OUT, '02d-product-view-description.png'), fullPage: false });

  // Shopeiva Popular Brands deep scan
  await page.goto(`${SHOPEIVA}/`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitSettled(page, 3000);
  await page.evaluate(() => {
    const el = [...document.querySelectorAll('h1,h2,h3,h4,section,div')].find(
      (e) => (e.textContent || '').trim() === 'برندهای محبوب' || /برندهای محبوب/.test(e.textContent || '') && (e.textContent || '').length < 40,
    );
    el?.scrollIntoView({ block: 'center' });
  });
  await waitSettled(page, 1000);
  const brands = await page.evaluate(() => {
    const section =
      [...document.querySelectorAll('section,div')].find((e) => {
        const h = e.querySelector('h1,h2,h3,h4');
        return h && /برندهای محبوب|Popular Brands/i.test(h.textContent || '');
      }) || null;
    const text = section ? section.innerText : '';
    const labels = section
      ? [...section.querySelectorAll('a,button,figcaption,span,p')]
          .map((e) => (e.textContent || '').trim())
          .filter((t) => t && t.length < 40)
          .slice(0, 40)
      : [];
    return {
      found: !!section,
      textSlice: text.slice(0, 1500),
      hasNoBrand: /No Brand|بدون برند/i.test(text),
      hasBlankish: /N\/A|undefined|null/i.test(text),
      draftLeakHints: (text.match(/پیش‌نویس|Draft|نسخه \d|مدل \d|سری \d|مجموعه \d/gi) || []).slice(0, 20),
      labels: [...new Set(labels)].slice(0, 30),
    };
  });
  notes.shopeivaBrands = brands;
  await page.screenshot({ path: path.join(OUT, '09c-shopeiva-popular-brands.png'), fullPage: false });
  await page.screenshot({ path: path.join(OUT, '09d-shopeiva-popular-brands-full.png'), fullPage: true });

  // Query total products count
  const total = await page.evaluate(async () => {
    // from admin origin via navigation
    return null;
  });
  await page.goto(`${ADMIN}/fa/admin/products`, { waitUntil: 'domcontentloaded' });
  await waitSettled(page, 1500);
  const apiTotal = await page.evaluate(async () => {
    const res = await fetch('/v1/admin/products/query', {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'X-Tooba-Dev-Actor-User-Id': '01a036c2-970e-7000-8eb7-94bf5cc2d8db',
      },
      body: JSON.stringify({ page: 1, pageSize: 25 }),
    });
    const j = await res.json();
    return {
      status: res.status,
      total: j.total ?? j.totalCount ?? j.count ?? j.meta?.total,
      pageSize: j.pageSize ?? j.items?.length,
      keys: Object.keys(j),
    };
  });
  notes.apiTotal = apiTotal;

  fs.writeFileSync(NOTES, JSON.stringify(notes, null, 2), 'utf8');
  console.log(JSON.stringify({ ok: true, notesPath: NOTES, apiTotal, brandsFound: brands.found, catIncomplete: notes.categories.incomplete }, null, 2));
  await browser.close();
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
