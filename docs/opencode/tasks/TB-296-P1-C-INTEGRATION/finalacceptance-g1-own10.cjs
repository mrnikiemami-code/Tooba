// TB-296 P1-FINAL-ACCEPTANCE — G1 OWN-10 executable oracle.
// Live Marketplace edition Host (5088) bound to the SAME approved DEV database (tooba_alpha),
// real seller browser + real HTTP + independent PostgreSQL. The platform-fulfilled offer is the
// EXISTING demo fixture (no invented grant, no alternative DB). Verification only.
const fs = require("node:fs");
const path = require("node:path");

const HOST = "http://127.0.0.1:5088";
const FRONT = "http://127.0.0.1:3000";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";
const SELLER = "01a030d1-40cb-7000-8abe-6d31739956c5";
const ACTOR = "01a03628-3f68-7000-844d-99f1cadb54b0";
const OFFER = process.env.FA_PLATFORM_OFFER || "964ac1b4-b39e-4f29-9d41-e4402fdf21f1";
const PLAYWRIGHT = process.env.P1C_PLAYWRIGHT || [
  path.resolve(__dirname, "../../../../src/frontend/node_modules/playwright"),
  "C:\\Users\\User\\OpenCode-UI-Lab-20261122-064208\\node_modules\\playwright",
  "C:\\Users\\User\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules\\playwright",
].find(candidate => fs.existsSync(candidate));

function check(condition, message) { if (!condition) throw new Error(message); }

async function call(route, method = "GET", payload, headers = {}) {
  const res = await fetch(HOST + route, {
    method,
    headers: { "Accept-Language": "fa-IR", ...(payload !== undefined ? { "Content-Type": "application/json" } : {}), ...headers },
    body: payload !== undefined ? JSON.stringify(payload) : undefined,
    signal: AbortSignal.timeout(20000),
  });
  const raw = await res.text();
  let body; try { body = raw ? JSON.parse(raw) : null; } catch { body = raw; }
  return { status: res.status, body };
}
function psqlJson(q) {
  const { execFileSync } = require("node:child_process");
  const out = execFileSync(process.env.P1C_PSQL || "psql", ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", q], { encoding: "utf8", timeout: 30000, windowsHide: true }).trim();
  return JSON.parse(out);
}

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  check(root, "Set P1C_EVIDENCE_DIR");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "OWN-10.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "OWN-10", requests: [], sql: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  try {
    const headers = { [ADMIN_HEADER]: ACTOR, [SELLER_HEADER]: SELLER };

    const channels = await call("/v1/seller/offers/allowed-channels", "GET", undefined, headers);
    trace.channels = channels; save();
    check(channels.status === 200 && Array.isArray(channels.body) && channels.body.includes("Marketplace"), "Marketplace seller context not served: " + JSON.stringify(channels));

    const detail = await call("/v1/seller/offers/" + OFFER, "GET", undefined, headers);
    trace.detail = detail; save();
    check(detail.status === 200, "seller detail " + detail.status);
    check(detail.body.fulfillmentResponsibility === "Platform", "fixture offer is not platform-fulfilled");
    const cap = detail.body.capabilities || {};
    check(cap.canEditPhysicalInventory === false, "platform physical must be read-only: " + JSON.stringify(cap));
    check(cap.canEditOfferedQuantity === true, "platform allocation must be editable: " + JSON.stringify(cap));
    trace.capabilities = cap;

    const languages = await call("/v1/languages");
    const dbLanguages = psqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('code',code,'prefix',url_prefix,'direction',direction,'calendar',calendar_display,'active',is_active,'default',is_default) ORDER BY sort_order),'[]'::jsonb) FROM localization.languages");
    trace.languages = { endpoint: languages.body, db: dbLanguages }; save();
    const third = dbLanguages.find(x => x.active && x.code === "ru-RU");
    check(third, "no configured distinct third locale");
    check(new Set(dbLanguages.map(x => x.prefix)).size === dbLanguages.length, "locale prefixes are not distinct");

    check(PLAYWRIGHT, "No Playwright installation available");
    const { chromium } = require(PLAYWRIGHT);
    const browser = await chromium.launch();
    const apiRequests = [];
    try {
      const context = await browser.newContext({ locale: "fa-IR" });
      await context.addInitScript(ids => {
        localStorage.setItem("tooba.sellerPartyId", ids.seller);
        localStorage.setItem("tooba.sellerActorUserId", ids.actor);
      }, { seller: SELLER, actor: ACTOR });
      const page = await context.newPage();
      page.on("request", req => {
        if (req.url().includes("/v1/seller/offers/" + OFFER)) apiRequests.push({ url: req.url(), headers: req.headers() });
      });

      const geometry = [];
      for (const width of [360, 768, 1440]) {
        await page.setViewportSize({ width, height: 900 });
        await page.goto(FRONT + "/fa/vendor-panel/products/" + OFFER, { waitUntil: "load" });
        await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
        await page.waitForTimeout(700);
        const facts = await page.evaluate(() => {
          const root = document.querySelector("[data-testid=offer-workspace]");
          const tabs = Array.from(document.querySelectorAll("[data-testid^=offer-tab-button-]")).map(x => x.getAttribute("data-testid"));
          return {
            overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
            dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang"), tabs,
            title: document.querySelector("[data-testid=offer-workspace-title]")?.textContent?.trim() || "",
          };
        });
        geometry.push({ width, ...facts });
        check(facts.overflow <= 0, "seller workspace overflows at " + width + ": " + facts.overflow);
        check(facts.dir === "rtl" && String(facts.lang || "").startsWith("fa"), "seller fa shell dir/lang wrong at " + width + ": " + JSON.stringify(facts));
        check(facts.tabs.length === 7, "seller workspace does not expose seven tabs at " + width + ": " + facts.tabs.join(","));
      }
      trace.geometry = geometry;

      // Real capability controls on the inventory tab at desktop width.
      await page.setViewportSize({ width: 1440, height: 900 });
      await page.goto(FRONT + "/fa/vendor-panel/products/" + OFFER, { waitUntil: "load" });
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.click("[data-testid=offer-tab-button-inventory]");
      await page.waitForSelector("[data-testid=offer-tab-inventory]", { timeout: 15000 });
      const controls = await page.evaluate(() => {
        const offered = document.querySelector("[data-testid=offer-offered-quantity-input]");
        const reason = document.querySelector("[data-testid=offer-inventory-reason]");
        const physical = Array.from(document.querySelectorAll("[data-testid=offer-inventory-input]"));
        return {
          offeredPresent: !!offered,
          offeredDisabled: offered ? offered.disabled === true : null,
          reasonPresent: !!reason,
          physicalCount: physical.length,
          physicalAllDisabled: physical.every(x => x.disabled === true),
        };
      });
      trace.controls = controls;
      check(controls.offeredPresent && controls.offeredDisabled === false, "allocation is not editable in the real seller UI: " + JSON.stringify(controls));
      check(controls.reasonPresent === false, "seller can edit platform physical reason (physical not read-only): " + JSON.stringify(controls));
      if (controls.physicalCount > 0) check(controls.physicalAllDisabled, "seller physical input is not disabled");

      // Draft preservation across the real in-app language switch (unsaved offered-quantity value).
      await page.fill("[data-testid=offer-offered-quantity-input]", "7");
      await page.waitForTimeout(300);
      await Promise.all([
        page.waitForURL(/\/en\/vendor-panel\/products\//, { timeout: 20000 }),
        page.selectOption("[data-testid=panel-language-select]", "en"),
      ]);
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.waitForTimeout(800);
      const preserved = await page.evaluate(() => {
        const root = document.querySelector("[data-testid=offer-workspace]");
        const offered = document.querySelector("[data-testid=offer-offered-quantity-input]");
        return {
          url: location.pathname,
          dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang"),
          overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
          offeredValue: offered ? offered.value : null,
        };
      });
      trace.localeSwitch = preserved;
      check(preserved.dir === "ltr" && String(preserved.lang || "").startsWith("en"), "english shell dir/lang wrong: " + JSON.stringify(preserved));
      check(preserved.overflow <= 0, "english shell overflows: " + preserved.overflow);
      check(preserved.offeredValue === "7", "unsaved draft was not preserved across the in-app locale switch: " + JSON.stringify(preserved));

      // Third registry locale (LTR).
      await page.goto(FRONT + "/" + third.prefix + "/vendor-panel/products/" + OFFER, { waitUntil: "load" });
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.waitForTimeout(500);
      const thirdFacts = await page.evaluate(() => {
        const root = document.querySelector("[data-testid=offer-workspace]");
        return { dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang"), overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth };
      });
      trace.thirdLocale = thirdFacts;
      check(thirdFacts.overflow <= 0, "third-locale workspace overflows: " + thirdFacts.overflow);
      check(thirdFacts.dir === String(third.direction).toLowerCase() && String(thirdFacts.lang || "").startsWith(third.prefix), "third-locale shell mismatch: " + JSON.stringify(thirdFacts));

      // Invalid/disabled locale clause is already proven by R10-09-N (invalid public prefix 404,
      // unknown panel prefix canonicalized to the active default with zero writes) and R10-09-B
      // (changed default), so it is REUSED, not re-executed. Record the vendor-panel observation only.
      const invalid = await page.goto(FRONT + "/zz/vendor-panel/products/" + OFFER, { waitUntil: "load" });
      const invalidFacts = await page.evaluate(() => ({ url: location.pathname, lang: document.documentElement.getAttribute("lang"), dir: document.documentElement.getAttribute("dir"), hasWorkspace: !!document.querySelector("[data-testid=offer-workspace]") }));
      trace.invalidPrefix = { status: invalid ? invalid.status() : null, ...invalidFacts, reused: "R10-09-N/R10-09-B" };
      check(String(invalidFacts.lang || "").startsWith("fa") && invalidFacts.dir === "rtl", "invalid panel prefix did not canonicalize to the active default shell: " + JSON.stringify(trace.invalidPrefix));

      trace.apiHeaders = apiRequests.map(r => ({ url: r.url, sellerPartyId: r.headers["x-tooba-seller-party-id"], actor: r.headers["x-tooba-dev-actor-user-id"], acceptLanguage: r.headers["accept-language"] }));
      check(apiRequests.length > 0, "no seller API request was captured from the real browser");
      check(apiRequests.every(r => r.headers["x-tooba-seller-party-id"] === SELLER && r.headers["x-tooba-dev-actor-user-id"] === ACTOR),
        "seller API headers were not the authorized identity: " + JSON.stringify(trace.apiHeaders));
      check(apiRequests.some(r => r.headers["accept-language"]), "seller API request carried no Accept-Language header");

      // Independent DB confirmation that the fixture was not mutated by the read-only browser run.
      const dbState = psqlJson("SELECT jsonb_build_object('responsibility',(SELECT fulfillment_responsibility FROM offer.offers WHERE offer_id='" + OFFER + "'),'onHand',(SELECT coalesce(sum(on_hand),0) FROM inventory.stock_positions WHERE offer_id='" + OFFER + "'),'offered',(SELECT coalesce(sum(quantity),0) FROM inventory.offer_allocations WHERE offer_id='" + OFFER + "'),'allocations',(SELECT count(*) FROM inventory.offer_allocations WHERE offer_id='" + OFFER + "'))");
      trace.dbState = dbState;
      check(dbState.responsibility === "Platform", "DB responsibility changed");
      check(Number(dbState.offered) === 0 && Number(dbState.allocations) === 0, "read-only browser run wrote allocation: " + JSON.stringify(dbState));

      trace.status = "PASS";
      trace.reason = "Live Marketplace edition on the same tooba_alpha DB: real seller browser at 360/768/1440 shows platform physical read-only (no edit control/reason), allocation editable, fa/rtl and en/ltr and ru/ltr shells without overflow, in-app locale switch preserved the unsaved offered-quantity draft, authorized seller/actor API headers were present, and independent SQL confirms no write.";
    } finally {
      await browser.close();
    }
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    save();
  }
  console.log(JSON.stringify({ id: "OWN-10", status: trace.status, reason: trace.reason }));
}

main().catch(error => { console.error(error); process.exit(1); });
