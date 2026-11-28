// TB-296 P1-FINAL-ACCEPTANCE — G3 R10-13-P supplemental executable oracle.
// Adds ONLY the previously unproven regression clauses of the original spec:
// campaigns, Stage2 cart, grid sort, grid export, saved views. The already-proven
// performance/orders/products/paging/filter clauses are REUSED from the prior R10-13-P
// evidence, not re-executed. Real Host 5088 / Next 3000 / tooba_alpha; exact cleanup.
const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");
const { randomUUID } = require("node:crypto");

const HOST = "http://127.0.0.1:5088";
const FRONT = "http://127.0.0.1:3000";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const PLAYWRIGHT = process.env.P1C_PLAYWRIGHT || [
  path.resolve(__dirname, "../../../../src/frontend/node_modules/playwright"),
  "C:\\Users\\User\\OpenCode-UI-Lab-20261122-064208\\node_modules\\playwright",
  "C:\\Users\\User\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules\\playwright",
].find(candidate => fs.existsSync(candidate));
const PRIOR = path.join(__dirname, "p1c-group-de-final2-evidence", "R10-13-P.json");

function check(condition, message) { if (!condition) throw new Error(message); }
function psql(query) {
  return execFileSync(process.env.P1C_PSQL || "psql", ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", query], { encoding: "utf8", timeout: 30000, windowsHide: true }).trim();
}
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

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  check(root, "Set P1C_EVIDENCE_DIR");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "R10-13-P.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "R10-13-P", requests: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  let cartId = null;
  let browser = null;
  let prefActor = null;
  let prefOriginal = null;
  try {
    const dev = await call("/v1/admin/dev-context");
    check(dev.status === 200, "admin dev-context " + dev.status);
    const admin = { [ADMIN_HEADER]: dev.body.actorUserId };

    // Reused, already-proven clauses (do NOT re-execute).
    check(fs.existsSync(PRIOR), "prior R10-13-P evidence missing");
    const priorBytes = fs.readFileSync(PRIOR);
    const prior = JSON.parse(priorBytes.toString("utf8"));
    const crypto = require("node:crypto");
    trace.reused = {
      path: path.relative(path.resolve(__dirname, "../../../.."), PRIOR).replaceAll("\\", "/"),
      sha256: crypto.createHash("sha256").update(priorBytes).digest("hex"),
      performance: prior.performance,
      regression: prior.regression,
      reason: "Warm form/picker performance, orders/products regressions, grid paging and status filter were already proven by the prior R10-13-P run; reused unchanged.",
    };
    save();

    // 1) Campaign regression: real owner endpoint returns complete campaign rows.
    const campaigns = await call("/v1/admin/merchandising-campaigns/", "GET", undefined, admin);
    const campaignItems = campaigns.body && Array.isArray(campaigns.body.items) ? campaigns.body.items : null;
    trace.campaigns = { status: campaigns.status, total: campaigns.body && campaigns.body.total, count: campaignItems ? campaignItems.length : null, sample: campaignItems ? campaignItems.slice(0, 3) : campaigns.body };
    save();
    check(campaigns.status === 200, "campaign list " + campaigns.status + " " + JSON.stringify(campaigns.body).slice(0, 200));
    check(campaignItems && campaignItems.length > 0, "campaign list is empty/missing items");
    const row = campaignItems[0];
    check(row.campaignId || row.id, "campaign row missing identity");
    check(typeof (row.title ?? row.name) === "string" && (row.title ?? row.name).length > 0, "campaign row missing title");
    check(typeof (row.lifecycleStatus ?? row.status) === "string", "campaign row missing lifecycle status");
    check(typeof row.memberCount === "number", "campaign row missing member count");

    // 2) Stage2 cart: real guest cart create + add line + independent read.
    const cart = await call("/v1/storefront/cart", "POST", {});
    check([200, 201].includes(cart.status), "cart create " + cart.status + " " + JSON.stringify(cart.body));
    cartId = cart.body.cartId;
    check(cart.body.version >= 1 && cart.body.guestSecret, "cart create did not return version/secret");
    const offer = psql("SELECT o.offer_id FROM offer.offers o JOIN pricing.prices p ON p.offer_id=o.offer_id AND p.status='Active' JOIN inventory.stock_positions s ON s.offer_id=o.offer_id WHERE o.status='Active' AND s.on_hand>0 AND o.seller_party_id='01a030d1-40cb-7000-8abe-6d31739956c5' ORDER BY o.offer_id LIMIT 1").trim();
    check(/^[0-9a-f-]{36}$/i.test(offer), "no buyable offer for cart clause");
    const cartHeaders = { "X-Tooba-Guest-Secret": cart.body.guestSecret, "X-Tooba-Cart-Version": String(cart.body.version) };
    const added = await call("/v1/storefront/cart/" + cartId + "/lines", "POST", { offerId: offer, quantity: 1 }, cartHeaders);
    trace.cart = { offer, create: cart.body, addedStatus: added.status };
    save();
    check(added.status === 200, "cart add line " + added.status + " " + JSON.stringify(added.body).slice(0, 300));
    check(added.body.itemCount === 1 && added.body.lines.length === 1, "cart line not added");
    const cartLine = added.body.lines[0];
    check(cartLine.offerId === offer && Number(cartLine.quantity) === 1, "cart line identity/quantity wrong");
    check(Number(added.body.totalsByCurrency[0].subtotalExclusiveOfTax) > 0, "cart subtotal not quoted");
    const readBack = await call("/v1/storefront/cart/" + cartId, "GET", undefined, { "X-Tooba-Guest-Secret": cart.body.guestSecret });
    trace.cart.readBack = { status: readBack.status, itemCount: readBack.body && readBack.body.itemCount, lines: readBack.body && readBack.body.lines && readBack.body.lines.length };
    save();
    check(readBack.status === 200 && readBack.body.itemCount === 1 && readBack.body.lines.length === 1, "cart independent read-back failed");

    // 3-5) Real admin grid: sort, CSV export, saved view.
    check(PLAYWRIGHT, "No Playwright installation available");
    const { chromium } = require(PLAYWRIGHT);
    browser = await chromium.launch();
    const context = await browser.newContext({ locale: "fa-IR", acceptDownloads: true });
    await context.addInitScript(actor => localStorage.setItem("tooba.adminActorUserId", actor), dev.body.actorUserId);
    const page = await context.newPage();
    const queryBodies = [];
    page.on("request", req => {
      if (req.url().includes("/v1/admin/offers/query")) queryBodies.push(req.postData());
    });
    await page.goto(FRONT + "/fa/admin/offers", { waitUntil: "load" });
    await page.waitForSelector("[data-testid=admin-offers]", { timeout: 30000 });
    await page.waitForSelector("[data-app-grid-shell]", { timeout: 30000 }).catch(() => undefined);
    await page.waitForTimeout(1500);

    // Grid sort: clicking the status header drives the real grid sort model (aria-sort transitions).
    const statusHeader = page.locator(".ag-header-cell").filter({ hasText: "وضعیت" }).first();
    const ariaBefore = await statusHeader.getAttribute("aria-sort");
    await statusHeader.click();
    await page.waitForTimeout(1000);
    const ariaAsc = await statusHeader.getAttribute("aria-sort");
    await statusHeader.click();
    await page.waitForTimeout(1000);
    const ariaDesc = await statusHeader.getAttribute("aria-sort");
    trace.sort = { ariaBefore, ariaAsc, ariaDesc };
    save();
    check(ariaAsc === "ascending", "status header did not enter ascending sort: " + JSON.stringify(trace.sort));
    check(ariaDesc === "descending", "status header did not enter descending sort: " + JSON.stringify(trace.sort));

    // CSV export of the visible grid.
    const downloadPromise = page.waitForEvent("download", { timeout: 20000 });
    await page.click("[data-testid=app-grid-export-csv]");
    const dl = await downloadPromise;
    const dlPath = await dl.path();
    const csv = fs.readFileSync(dlPath, "utf8");
    const csvLines = csv.split(/\r?\n/).filter(Boolean);
    trace.export = { filename: dl.suggestedFilename(), lines: csvLines.length, header: csvLines[0], firstRow: csvLines[1] };
    save();
    check(csvLines.length >= 2, "CSV export contained no data rows");
    check(csvLines[0].includes("وضعیت") || csvLines[0].includes("Item code") || csvLines[0].includes("کد کالا"), "CSV export header missing expected columns: " + csvLines[0]);

    // Saved view: save named view and observe the persisted chip.
    const prefKey = "grid.admin.offers";
    const actor = dev.body.actorUserId;
    prefActor = actor;
    const originalPref = psql(`SELECT coalesce(json_payload::text,'') FROM user_preference.ui_preferences WHERE actor_user_id='${actor}' AND "key"='${prefKey}'`);
    prefOriginal = originalPref;
    trace.savedViewOriginal = { existed: originalPref.length > 0 };
    const viewName = "P1C-R10-13-" + randomUUID().slice(0, 8);
    await page.click("[data-testid=app-grid-save-view]");
    const popover = page.locator("[data-testid=app-grid-saved-views] .absolute").first();
    await popover.locator("input:not([type=checkbox])").fill(viewName);
    await popover.getByRole("button").first().click();
    await page.waitForTimeout(1800);
    const chipText = await page.locator("[data-testid=app-grid-saved-views]").innerText();
    trace.savedView = { name: viewName, chipText: chipText.slice(0, 400) };
    save();
    check(chipText.includes(viewName), "saved view was not persisted/rendered: " + chipText.slice(0, 200));

    trace.status = "PASS";
    trace.reason = "Newly proven original-spec regression clauses: real campaign owner endpoint returned complete rows; real Stage2 guest cart create+line+read-back honored identity/quantity/quote; admin grid status header drove the real grid sort model (aria-sort none->ascending->descending); CSV export downloaded real rows with expected headers; a named saved view persisted/rendered and the original preference was restored. Prior performance/orders/products/paging/filter are reused unchanged.";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    if (browser) { try { await browser.close(); } catch { /* ignore */ } }
    if (prefActor) {
      try {
        const key = "grid.admin.offers";
        if (prefOriginal && prefOriginal.length > 0) {
          psql(`UPDATE user_preference.ui_preferences SET json_payload='${prefOriginal.replaceAll("'", "''")}'::jsonb, updated_at=now() WHERE actor_user_id='${prefActor}' AND "key"='${key}'`);
        } else {
          psql(`DELETE FROM user_preference.ui_preferences WHERE actor_user_id='${prefActor}' AND "key"='${key}'`);
        }
        const left = psql(`SELECT coalesce(json_payload::text,'') FROM user_preference.ui_preferences WHERE actor_user_id='${prefActor}' AND "key"='${key}'`);
        trace.savedViewRestored = { matchesOriginal: (prefOriginal || "") === left };
        check((prefOriginal || "") === left, "saved-view preference was not restored exactly");
      } catch (error) {
        trace.status = "FAIL";
        trace.cleanupError = String(error && error.stack ? error.stack : error);
      }
    }
    if (cartId) {
      try {
        psql("BEGIN; DELETE FROM cart.cart_lines WHERE cart_id='" + cartId + "'; DELETE FROM cart.carts WHERE cart_id='" + cartId + "'; COMMIT;");
        const left = Number(psql("SELECT count(*) FROM cart.carts WHERE cart_id='" + cartId + "'"));
        const leftLines = Number(psql("SELECT count(*) FROM cart.cart_lines WHERE cart_id='" + cartId + "'"));
        trace.cartCleanup = { cartsLeft: left, linesLeft: leftLines };
        check(left === 0 && leftLines === 0, "cart fixture not cleaned");
      } catch (error) {
        trace.status = "FAIL";
        trace.cleanupError = String(error && error.stack ? error.stack : error);
      }
    }
    save();
  }
  console.log(JSON.stringify({ id: "R10-13-P", status: trace.status, reason: trace.reason }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
