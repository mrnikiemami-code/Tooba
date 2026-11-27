// TB-296 P1-C — executable Group D/E handlers (real browser + Host + PostgreSQL + guards/gates).
// Test-only module. Never mutates product, tests, guards, matrices/contracts or seeds.
// IDs: R10-09-P/N/B, R10-11-P/N/B, R10-12-P/B, R10-13-P/B, R10-14-P/N/B.
// Isolated P1C- owned fixtures; exact finally cleanup through p1c-support.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const crypto = require("node:crypto");
const { spawnSync } = require("node:child_process");
const { pathToFileURL } = require("node:url");
const { randomUUID } = require("node:crypto");
const S = require("./p1c-support.cjs");
const { uuid, deterministicId, runCase, psql, snapshot, http, SKU_PREFIX, NotRun, EMPTY } = S;

const repo = path.resolve(__dirname, "../../../..");
const FRONT = "http://127.0.0.1:3000";
const HOST = S.HOST;
const ADMIN = S.ADMIN_HEADER;
const MATRIX_PATH = path.join(repo, "docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/ACCEPTANCE-MATRIX.json");
const ACCEPTANCE_GATE = path.join(repo, "docs/opencode/test-infra/acceptance-gate.cjs");
const ARCHITECT_GATE = path.join(repo, "docs/opencode/test-infra/architect-review-gate.cjs");
const FALLBACK_PLAYWRIGHT = path.join(repo, "docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283/browser/node_modules/playwright");
const DEFAULT_SELLER = "01a030d1-40db-7000-b90c-a0705133f0eb";
const DEFAULT_VARIANT = "01a0967d-a2eb-7000-9a12-779c15789bac";
const R8_FORM_MS = 1598;
const R8_PICKER_MS = 103;

function sha256(value) {
  return crypto.createHash("sha256").update(value).digest("hex");
}
function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8").replace(/^\uFEFF/, ""));
}
function command(exe, args, options = {}) {
  const result = spawnSync(exe, args, {
    cwd: options.cwd || repo,
    encoding: "utf8",
    timeout: options.timeout || 900000,
    maxBuffer: 64 * 1024 * 1024,
    windowsHide: true,
  });
  return {
    command: [exe, ...args].join(" "),
    exit: result.status,
    stdout: result.stdout || "",
    stderr: result.stderr || "",
    error: result.error ? String(result.error) : null,
  };
}
function tail(value, size = 4000) {
  const text = String(value || "");
  return text.length > size ? text.slice(text.length - size) : text;
}
function requireExit(result, label) {
  assert.equal(result.exit, 0, label + " failed with exit " + result.exit + "\nstdout:\n" + tail(result.stdout) + "\nstderr:\n" + tail(result.stderr));
}
function git(args) {
  const result = command("git", args);
  assert.equal(result.exit, 0, "git " + args.join(" ") + " failed: " + tail(result.stderr));
  return result.stdout.trim();
}
function gitHead() {
  const head = git(["rev-parse", "HEAD"]);
  assert.match(head, /^[0-9a-f]{40}$/i, "full source commit required");
  return head;
}
function nextBuildId() {
  const file = path.join(repo, "src/frontend/.next/BUILD_ID");
  assert.ok(fs.existsSync(file), "Next BUILD_ID missing");
  const value = fs.readFileSync(file, "utf8").trim();
  assert.ok(value.length > 0, "empty Next BUILD_ID");
  return value;
}
function servedHostDll() {
  const candidates = [
    process.env.P1C_HOST_DLL,
    path.join(repo, ".tmp-p1c-final-host/Tooba.Host.dll"),
    path.join(repo, "src/backend/Host/Tooba.Host/bin/Debug/net8.0/Tooba.Host.dll"),
  ].filter(Boolean);
  const found = candidates.find((candidate) => fs.existsSync(candidate));
  assert.ok(found, "Served Host DLL not found");
  return path.resolve(found);
}
function hostDllSha() {
  return sha256(fs.readFileSync(servedHostDll()));
}
function databaseIdentity() {
  const value = psql("SELECT current_database()||'|'||coalesce(inet_server_addr()::text,'local')||'|'||coalesce(inet_server_port()::text,'local')");
  assert.ok(value.startsWith("tooba_alpha|"), "Not authorized DEV database: " + value);
  return value;
}
async function providerIdentity() {
  const response = await http("/__platform-commerce");
  assert.equal(response.status, 200, "provider provenance endpoint");
  return response.body;
}
function parseDotnetCounts(output) {
  const match = output.match(/Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)/);
  assert.ok(match, "dotnet test summary missing in output");
  return { failed: Number(match[1]), passed: Number(match[2]), skipped: Number(match[3]), total: Number(match[4]) };
}
function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}
function p95(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.min(sorted.length - 1, Math.ceil(sorted.length * 0.95) - 1);
  return sorted[Math.max(0, index)];
}

let playwrightCache = null;
function playwright() {
  if (playwrightCache) return playwrightCache;
  const candidates = [process.env.P1C_PLAYWRIGHT, "playwright", FALLBACK_PLAYWRIGHT].filter(Boolean);
  let lastError = null;
  for (const candidate of candidates) {
    try {
      playwrightCache = require(candidate.startsWith(".") || path.isAbsolute(candidate) ? path.resolve(candidate) : candidate);
      return playwrightCache;
    } catch (error) {
      lastError = error;
    }
  }
  throw new Error("playwright module unavailable: " + (lastError && lastError.message));
}

// Local exact cleanup (p1c-support keeps its implementation private). Same identity guards and
// scoped deletes; never a broad purge.
function cleanupExact(ids, seller) {
  assert.ok(Array.isArray(ids) && ids.length > 0, "cleanup requires owned ids");
  const before = snapshot(ids);
  assert.ok(before.offers.every((row) => ids.includes(row.offer_id) && row.seller_party_id === seller && String(row.seller_sku || "").startsWith(SKU_PREFIX)),
    "Unexpected fixture identity; refusing to delete any row");
  assert.ok(before.positions.every((row) => ids.includes(row.offer_id)), "Unexpected position identity; refusing cleanup");
  const keys = ids.map((value) => S.quote(uuid(value))).join(",");
  const outbox = ids.map((value) => "strpos(payload::text," + S.quote('"' + uuid(value) + '"') + ")>0").join(" OR ");
  psql("BEGIN;" +
    " DELETE FROM inventory.reservations WHERE stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id IN (" + keys + "));" +
    " DELETE FROM inventory.offer_inventory_changes WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM inventory.offer_allocations WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM inventory.stock_positions WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM pricing.offer_price_changes WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM pricing.prices WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.offer_history WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.offers WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.outbox_messages WHERE " + outbox + ";" +
    " DELETE FROM pricing.outbox_messages WHERE " + outbox + ";" +
    " DELETE FROM inventory.outbox_messages WHERE " + outbox + ";" +
    " COMMIT;");
  const after = snapshot(ids);
  assert.ok(EMPTY(after), "Residual fixture data after cleanup");
  return { beforeCleanup: before, afterCleanup: after };
}

async function prepareAdmin(ctx) {
  const seller = uuid(process.env.P1C_SELLER_ID || DEFAULT_SELLER);
  const variant = uuid(process.env.P1C_CREATE_VARIANT_ID || DEFAULT_VARIANT);
  ctx.state.seller = seller;
  const dev = await ctx.api("/v1/admin/dev-context");
  assert.equal(dev.status, 200, "admin dev-context");
  const actor = uuid(dev.body.actorUserId);
  const admin = { [ADMIN]: actor };
  const channels = await ctx.api("/v1/admin/offers/allowed-channels", "GET", undefined, admin);
  assert.equal(channels.status, 200, "allowed-channels");
  assert.ok(Array.isArray(channels.body) && channels.body.length > 0, "No authorized channel");
  const channel = channels.body[0];
  const contextsResponse = await ctx.api("/v1/seller/dev-contexts");
  const contexts = Array.isArray(contextsResponse.body?.actors) ? contextsResponse.body.actors : [];
  assert.ok(contexts.some((row) => row.sellerPartyId.toLowerCase() === seller), "Seller is not a current DEV synthetic identity");
  const active = await ctx.api("/v1/admin/offers/active-listing?sellerPartyId=" + seller + "&catalogVariantId=" + variant + "&channel=" + encodeURIComponent(channel), "GET", undefined, admin);
  assert.equal(active.status, 200, "active-listing read");
  assert.equal(active.body, null, "Candidate variant already has an active listing");
  const requestId = "P1C-" + randomUUID();
  const offerId = deterministicId(seller, variant, requestId);
  ctx.addOffer(offerId);
  assert.ok(EMPTY(ctx.observe("before-" + requestId.slice(-8))), "Deterministic candidate identity is not isolated");
  const sku = SKU_PREFIX + randomUUID();
  const created = await ctx.api("/v1/admin/offers", "POST", {
    sellerPartyId: seller,
    catalogVariantId: variant,
    channel,
    sellerSku: sku,
    status: "Draft",
    requestId,
  }, admin);
  assert.equal(created.status, 201, "isolated create " + JSON.stringify(created.body));
  assert.equal(created.body.offerId, offerId, "deterministic offer identity");
  assert.equal(created.body.sellerPartyId, seller, "created seller");
  assert.equal(created.body.status, "Draft", "created status");
  ctx.trace.fixture = { seller, variant, channel, actor, offerId, sku, requestId, contexts };
  return { seller, variant, channel, actor, admin, offerId, sku, requestId, contexts };
}

async function adminBrowser(browser, actor) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await context.addInitScript((value) => localStorage.setItem("tooba.adminActorUserId", value), actor);
  const page = await context.newPage();
  return { context, page };
}

async function waitWorkspace(page, expectedLang) {
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  if (expectedLang) {
    await page.waitForFunction(
      (lang) => document.querySelector("[data-testid=offer-workspace]")?.getAttribute("lang") === lang,
      expectedLang,
      { timeout: 20000 },
    );
  }
}

async function closeBrowser(browser) {
  if (browser) await browser.close().catch(() => undefined);
}

// ---------------------------------------------------------------- R10-09 locale

function writeResolverProbe() {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), "p1c-locale-"));
  const file = path.join(directory, "resolver.mjs");
  const moduleUrl = pathToFileURL(path.join(repo, "src/frontend/lib/i18n/registry-locale-model.ts")).href;
  const script = [
    "import { parseRegistryLanguages, resolveRegistryLanguage } from " + JSON.stringify(moduleUrl) + ";",
    "const live = await (await fetch(" + JSON.stringify(HOST + "/v1/languages") + ", { headers: { Accept: 'application/json' } })).json();",
    "const rows = parseRegistryLanguages(live);",
    "const fixture = parseRegistryLanguages([",
    "  { code: 'en-US', urlPrefix: 'en', culture: 'en-US', direction: 'ltr', calendarDisplay: 'Gregorian', isActive: true, isDefault: true, nativeName: 'English' },",
    "  { code: 'de-DE', urlPrefix: 'de', culture: 'de-DE', direction: 'ltr', calendarDisplay: 'Gregorian', isActive: true, isDefault: false, nativeName: 'Deutsch' },",
    "  { code: 'fa-IR', urlPrefix: 'fa', culture: 'fa-IR', direction: 'rtl', calendarDisplay: 'Jalali', isActive: true, isDefault: false, nativeName: 'فارسی' }",
    "]);",
    "console.log(JSON.stringify({",
    "  live: rows.map(r => ({ prefix: r.urlPrefix, code: r.code, direction: r.direction, calendar: r.calendar, isDefault: r.isDefault })),",
    "  liveResolved: { fa: resolveRegistryLanguage(rows, 'fa')?.code, en: resolveRegistryLanguage(rows, 'en')?.code, ru: resolveRegistryLanguage(rows, 'ru')?.code, unknown: resolveRegistryLanguage(rows, 'xx')?.code },",
    "  fixture: { de: resolveRegistryLanguage(fixture, 'de')?.code, unknown: resolveRegistryLanguage(fixture, 'xx')?.code, fa: resolveRegistryLanguage(fixture, 'fa')?.code }",
    "}));",
  ].join("\n");
  fs.writeFileSync(file, script, { flag: "wx" });
  return { directory, file };
}

// ---------------------------------------------------------------- R10-11 backend

function runOfferArchitecture(ctx) {
  const result = command("dotnet", [
    "test",
    "src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj",
    "--no-restore",
    "--nologo",
    "--filter",
    "FullyQualifiedName~Architecture|FullyQualifiedName~Endpoints|FullyQualifiedName~Application",
    "--logger",
    "console;verbosity=minimal",
  ]);
  ctx.trace.commands = ctx.trace.commands || [];
  ctx.trace.commands.push({ label: "offer-architecture-endpoints-application", ...result, stdout: tail(result.stdout), stderr: tail(result.stderr) });
  requireExit(result, "Offer architecture/endpoint/validation tests");
  const counts = parseDotnetCounts(result.stdout);
  assert.equal(counts.failed, 0, "Offer test failures: " + JSON.stringify(counts));
  assert.ok(counts.passed > 0, "No Offer tests executed");
  return counts;
}

function runHostArchitectureBoundary(ctx) {
  const result = command("dotnet", [
    "test",
    "src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj",
    "--no-restore",
    "--nologo",
    "--filter",
    "FullyQualifiedName~ArchitectureBoundary",
    "--logger",
    "console;verbosity=minimal",
  ]);
  ctx.trace.commands = ctx.trace.commands || [];
  ctx.trace.commands.push({ label: "host-architecture-boundary", ...result, stdout: tail(result.stdout), stderr: tail(result.stderr) });
  requireExit(result, "Host architecture boundary tests");
  const counts = parseDotnetCounts(result.stdout);
  assert.equal(counts.failed, 0, "Host boundary failures: " + JSON.stringify(counts));
  assert.ok(counts.passed > 0, "No Host boundary tests executed");
  return counts;
}

function buildHost(ctx) {
  const result = command("dotnet", ["build", "src/backend/Host/Tooba.Host/Tooba.Host.csproj", "--nologo", "-v", "m"]);
  ctx.trace.commands = ctx.trace.commands || [];
  ctx.trace.commands.push({ label: "host-build", ...result, stdout: tail(result.stdout, 6000), stderr: tail(result.stderr) });
  requireExit(result, "Host build");
  assert.match(result.stdout, /0 Error\(s\)/, "Host build reported errors");
  return result;
}

function runModuleTests(ctx, label, project) {
  const result = command("dotnet", ["test", project, "--no-restore", "--nologo", "--logger", "console;verbosity=minimal"]);
  ctx.trace.commands = ctx.trace.commands || [];
  ctx.trace.commands.push({ label, ...result, stdout: tail(result.stdout), stderr: tail(result.stderr) });
  requireExit(result, label);
  const counts = parseDotnetCounts(result.stdout);
  assert.equal(counts.failed, 0, label + " failures: " + JSON.stringify(counts));
  assert.ok(counts.passed > 0, label + " executed no tests");
  return counts;
}

// ---------------------------------------------------------------- R10-14 gate

function buildGateFixture({ id, sourceCommit, hostSha256, nextBuildIdValue, database, assertions, root, suffix = "" }) {
  const matrix = readJson(MATRIX_PATH);
  const testObject = matrix.tests.find((row) => row.testId === id);
  assert.ok(testObject, "Immutable matrix test missing " + id);
  const scenarioSha256 = sha256(JSON.stringify(testObject));
  const base = id + suffix;
  const files = {
    matrix: path.join(root, base + ".matrix.json"),
    plan: path.join(root, base + ".plan.json"),
    run: path.join(root, base + ".run.json"),
    evidence: path.join(root, base + ".evidence.json"),
  };
  for (const file of Object.values(files)) assert.ok(!fs.existsSync(file), "Do not overwrite gate evidence " + file);
  fs.writeFileSync(files.evidence, JSON.stringify({ testId: id, scenarioSha256, sourceCommit, assertions }, null, 2), { flag: "wx" });
  const evidenceSha256 = sha256(fs.readFileSync(files.evidence));
  fs.writeFileSync(files.matrix, JSON.stringify({ taskId: matrix.taskId, tests: [testObject] }, null, 2), { flag: "wx" });
  fs.writeFileSync(files.plan, JSON.stringify({
    taskId: matrix.taskId,
    sourceCommit,
    tests: [{
      testId: id,
      scenarioSha256,
      assertionIds: assertions.map((row) => row.assertionId),
      expectedFacts: Object.fromEntries(assertions.map((row) => [row.assertionId, row.expected])),
    }],
  }, null, 2), { flag: "wx" });
  const run = {
    taskId: matrix.taskId,
    sourceCommit,
    hostSha256,
    nextBuildId: nextBuildIdValue,
    database,
    testBuildCommit: sourceCommit,
    testBuildSucceeded: true,
    tests: [{
      testId: id,
      scenarioSha256,
      sourceCommit,
      hostSha256,
      nextBuildId: nextBuildIdValue,
      status: "PASS",
      executed: true,
      assertions,
      evidencePath: path.basename(files.evidence),
      evidenceSha256,
    }],
  };
  fs.writeFileSync(files.run, JSON.stringify(run, null, 2), { flag: "wx" });
  return { files, run };
}
function invokeAcceptanceGate(files, root) {
  return command(process.execPath, [ACCEPTANCE_GATE, files.matrix, files.plan, files.run, root]);
}
function parseGateOutput(result) {
  try {
    return JSON.parse(result.stdout);
  } catch {
    return { ok: false, errors: ["unparseable gate output"], raw: tail(result.stdout) };
  }
}

// ---------------------------------------------------------------- handlers

module.exports = {
  "R10-09-P": () => runCase("R10-09-P", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const languages = await ctx.api("/v1/languages");
    assert.equal(languages.status, 200, "public language registry");
    const rows = languages.body;
    assert.ok(Array.isArray(rows) && rows.length >= 3, "Need FA/EN and configured third locale");
    const byPrefix = Object.fromEntries(rows.map((row) => [row.urlPrefix, row]));
    for (const prefix of ["fa", "en", "ru"]) {
      assert.ok(byPrefix[prefix] && byPrefix[prefix].isActive === true, "Active registry locale missing: " + prefix);
    }
    assert.equal(byPrefix.fa.isDefault, true, "FA must be the configured default");
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, fx.actor);
      const observed = [];
      const assertLocale = async (prefix, expected) => {
        const row = byPrefix[prefix];
        const root = page.locator("[data-testid=offer-workspace]");
        assert.equal(await root.getAttribute("lang"), row.code, prefix + " lang must come from registry code");
        assert.equal(await root.getAttribute("dir"), row.direction, prefix + " direction must come from registry");
        observed.push({ prefix, lang: await root.getAttribute("lang"), dir: await root.getAttribute("dir"), tab: (await page.locator("[data-testid=offer-tab-button-identity]").innerText()).replace(/\s+/g, " ").trim() });
      };
      await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      await assertLocale("fa");
      const draftSku = fx.sku + "-DRAFT";
      await page.locator("[data-testid=offer-seller-sku]").fill(draftSku);
      await page.waitForTimeout(300);
      await page.goto(FRONT + "/en/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "en-US");
      await assertLocale("en");
      assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), draftSku, "Edit draft must survive FA->EN switch");
      await page.goto(FRONT + "/ru/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "ru-RU");
      await assertLocale("ru");
      assert.match(await page.locator("[data-testid=offer-tab-button-identity]").innerText(), /Идентификация/, "Third-locale registry copy missing");
      assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), draftSku, "Edit draft must survive EN->RU switch");

      const createSku = SKU_PREFIX + randomUUID();
      await page.goto(FRONT + "/fa/admin/offers/new", { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      await page.locator("[data-testid=offer-seller-sku]").fill(createSku);
      await page.waitForTimeout(300);
      await page.goto(FRONT + "/en/admin/offers/new", { waitUntil: "load" });
      await waitWorkspace(page, "en-US");
      assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), createSku, "Create draft must survive locale switch");

      await page.goto(FRONT + "/ru/admin/offers", { waitUntil: "load" });
      await page.waitForSelector("[data-testid=admin-offers]", { timeout: 30000 });
      await page.waitForFunction(() => document.querySelector("[data-testid=admin-offers]")?.getAttribute("dir") === "ltr");
      assert.match(await page.locator("[data-testid=admin-offers]").innerText(), /Предложения продавцов/, "Actual list route must use third-locale registry copy");

      ctx.trace.registry = rows.map((row) => ({ code: row.code, urlPrefix: row.urlPrefix, direction: row.direction, calendarDisplay: row.calendarDisplay, isActive: row.isActive, isDefault: row.isDefault }));
      ctx.trace.localeObservations = observed;
      return { status: "PASS", reason: "Actual create/edit/list routes at FA/EN/RU obeyed live registry code/direction and preserved local drafts across real locale switches." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  "R10-09-N": () => runCase("R10-09-N", async (ctx) => {
    const beforeCount = Number(psql("SELECT count(*) FROM offer.offers"));
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, "00000000-0000-4000-8000-000000000000");
      const invalidPublic = await page.goto(FRONT + "/fr/offers", { waitUntil: "load" });
      assert.equal(invalidPublic.status(), 404, "Invalid public locale prefix must be a real 404");
      const bodyText = (await page.locator("body").innerText()).replace(/\s+/g, " ");
      assert.match(bodyText, /Not found|یافت نشد|404/i, "404 route body missing");
      const unknownPanel = await page.goto(FRONT + "/zz/admin/offers", { waitUntil: "load" });
      assert.equal(unknownPanel.status(), 200, "Unknown panel prefix must be served through the shared shell");
      await page.waitForFunction(() => window.location.pathname === "/fa/admin/offers", null, { timeout: 20000 });
      assert.equal(await page.locator("[data-testid=admin-offers]").getAttribute("dir"), "rtl", "Canonical default direction missing after fallback");
      const afterCount = Number(psql("SELECT count(*) FROM offer.offers"));
      assert.equal(afterCount, beforeCount, "Invalid/unknown locale routing must not write offers");
      ctx.trace.negativeRoute = { invalidPublicStatus: invalidPublic.status(), unknownPanelStatus: unknownPanel.status(), finalPath: new URL(page.url()).pathname, beforeCount, afterCount, bodyText: bodyText.slice(0, 200) };
      return { status: "PASS", reason: "Invalid public two-letter prefix returned a real 404; unknown panel prefix canonicalized to the active default /fa/admin/offers with zero offer writes." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  "R10-09-B": () => runCase("R10-09-B", async (ctx) => {
    const languages = await ctx.api("/v1/languages");
    assert.equal(languages.status, 200);
    const apiRows = languages.body;
    const dbRows = JSON.parse(psql("SELECT coalesce(jsonb_agg(jsonb_build_object('code',code,'prefix',url_prefix,'direction',lower(direction),'calendar',lower(calendar_display),'active',is_active,'default',is_default) ORDER BY sort_order),'[]'::jsonb) FROM localization.languages"));
    const dbActive = dbRows.filter((row) => row.active);
    assert.equal(apiRows.filter((row) => row.isActive).length, dbActive.length, "API active registry must match DB active rows");
    for (const dbRow of dbActive) {
      const apiRow = apiRows.find((row) => row.urlPrefix === dbRow.prefix);
      assert.ok(apiRow, "DB prefix missing in API: " + dbRow.prefix);
      assert.equal(apiRow.code, dbRow.code, "code mismatch for " + dbRow.prefix);
      assert.equal(apiRow.direction.toLowerCase(), dbRow.direction, "direction mismatch for " + dbRow.prefix);
      assert.equal(String(apiRow.calendarDisplay).toLowerCase(), dbRow.calendar, "calendar mismatch for " + dbRow.prefix);
      assert.equal(apiRow.isDefault === true, dbRow.default === true, "default mismatch for " + dbRow.prefix);
    }
    const moduleFile = path.join(repo, "src/frontend/lib/i18n/registry-locale-model.ts");
    const moduleSha256 = sha256(fs.readFileSync(moduleFile));
    const probe = writeResolverProbe();
    try {
      const result = command(process.execPath, ["--experimental-strip-types", probe.file], { cwd: repo, timeout: 120000 });
      ctx.trace.resolverProbe = { ...result, stdout: tail(result.stdout), stderr: tail(result.stderr), moduleSha256 };
      requireExit(result, "Actual shared registry resolver probe");
      const observed = JSON.parse(result.stdout);
      assert.equal(observed.liveResolved.fa, "fa-IR");
      assert.equal(observed.liveResolved.en, "en-US");
      assert.equal(observed.liveResolved.ru, "ru-RU");
      assert.equal(observed.liveResolved.unknown, "fa-IR", "Unknown prefix must resolve to live default");
      assert.equal(observed.fixture.de, "de-DE", "Isolated fixture prefix must resolve by metadata");
      assert.equal(observed.fixture.unknown, "en-US", "Changed default must be metadata-driven, not hardcoded FA");
      assert.equal(observed.fixture.fa, "fa-IR", "Explicit prefix must still win over changed default");
      ctx.trace.resolverObserved = observed;
      return { status: "PASS", reason: "Live DB registry matched the public API, and the actual shared resolver honored an isolated changed default (en-US) plus code-differs-from-prefix (de-DE) fixture; module SHA captured." };
    } finally {
      fs.rmSync(probe.directory, { recursive: true, force: true });
    }
  }),

  "R10-11-P": () => runCase("R10-11-P", async (ctx) => {
    const status = git(["status", "--porcelain", "--", "src/backend"]);
    assert.equal(status, "", "Product backend source is dirty; guard comparison requires immutable source");
    const offer = runOfferArchitecture(ctx);
    const host = runHostArchitectureBoundary(ctx);
    ctx.trace.backendGuard = { sourceClean: true, offer, host };
    return { status: "PASS", reason: "Actual Offer architecture/endpoint/validation guards (" + offer.passed + ") and Host architecture-boundary guards (" + host.passed + ") executed against immutable backend source with zero failures." };
  }),

  "R10-11-N": () => runCase("R10-11-N", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const foreign = fx.contexts.find((row) => row.sellerPartyId.toLowerCase() !== fx.seller);
    assert.ok(foreign, "Need a second real DEV seller context");
    const foreignHeaders = { [ADMIN]: uuid(foreign.actorUserId), [S.SELLER_HEADER]: uuid(foreign.sellerPartyId) };
    const before = ctx.observe("before-denials");
    const readForeign = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET", undefined, foreignHeaders);
    assert.ok([401, 403].includes(readForeign.status), "Foreign admin detail read must fail closed, got " + readForeign.status);
    const readAnonymous = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET");
    assert.equal(readAnonymous.status, 401, "Anonymous admin detail read must be 401");
    const queryForeign = await ctx.api("/v1/admin/offers/query", "POST", { page: 1, pageSize: 5, filters: [], sorts: [] }, foreignHeaders);
    assert.ok([401, 403].includes(queryForeign.status), "Foreign admin query must fail closed, got " + queryForeign.status);
    const patchForeign = await ctx.api("/v1/admin/offers/" + fx.offerId, "PATCH", { sellerSku: fx.sku + "-HACK", status: "Draft" }, foreignHeaders);
    assert.ok([401, 403].includes(patchForeign.status), "Foreign admin write must fail closed, got " + patchForeign.status);
    const priceForeign = await ctx.api("/v1/admin/offers/" + fx.offerId + "/price", "PUT", { amount: 123, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() }, foreignHeaders);
    assert.ok([401, 403].includes(priceForeign.status), "Foreign admin price write must fail closed, got " + priceForeign.status);
    const sellerForeign = await ctx.api("/v1/seller/offers/" + fx.offerId, "GET", undefined, foreignHeaders);
    assert.ok([401, 403, 404].includes(sellerForeign.status), "Foreign seller query must fail closed, got " + sellerForeign.status);
    const after = ctx.observe("after-denials");
    assert.deepEqual(after, before, "Denied command/query must leave owned state unchanged");
    ctx.trace.denials = [readForeign, readAnonymous, queryForeign, patchForeign, priceForeign, sellerForeign].map((row) => ({ route: row.route, method: row.method, status: row.status, errorCode: row.body?.errorCode ?? null }));
    return { status: "PASS", reason: "Actual anonymous/foreign admin query and write commands plus foreign seller query all failed closed; independent before/after PostgreSQL snapshot was identical." };
  }),

  "R10-11-B": () => runCase("R10-11-B", async (ctx) => {
    const build = buildHost(ctx);
    const pricing = runModuleTests(ctx, "pricing-adjacent-consumer", "src/backend/Modules/Pricing/Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj");
    const inventory = runModuleTests(ctx, "inventory-adjacent-consumer", "src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj");
    ctx.trace.compatibility = { buildExit: build.exit, pricing, inventory };
    return { status: "PASS", reason: "Host compiled from current immutable source and both adjacent Pricing (" + pricing.passed + ") and Inventory (" + inventory.passed + ") consumers passed with zero failures." };
  }),

  // ------------------------------------------------------------ R10-12 feedback

  "R10-12-P": () => runCase("R10-12-P", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const providerSource = fs.readFileSync(path.join(repo, "src/frontend/app/providers.tsx"), "utf8");
    assert.match(providerSource, /autoClose=\{3000\}/, "Shared Toast auto-close policy changed");
    const beforeSql = ctx.observe("before-save");
    assert.equal(beforeSql.offers.length, 1);
    assert.equal(beforeSql.history.length, 1);
    const savedSku = fx.sku + "-SAVED";
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, fx.actor);
      await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      await page.locator("[data-testid=offer-seller-sku]").fill(savedSku);
      await page.waitForTimeout(200);
      const toast = page.locator(".Toastify__toast");
      await page.locator("[data-testid=admin-offer-save]").click();
      await toast.first().waitFor({ state: "visible", timeout: 15000 });
      const visibleCount = await toast.count();
      const toastText = (await toast.first().innerText()).replace(/\s+/g, " ").trim();
      assert.equal(visibleCount, 1, "Expected exactly one shared success toast, got " + visibleCount);
      assert.match(toastText, /ذخیره شد/, "Unexpected success toast copy: " + toastText);
      assert.equal(await page.locator("[data-testid=offer-save-notice]").count(), 0, "No permanent success banner is allowed");
      const started = Date.now();
      await page.waitForFunction(() => document.querySelectorAll(".Toastify__toast").length === 0, null, { timeout: 8000 });
      const closeMs = Date.now() - started;
      const detail = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET", undefined, fx.admin);
      assert.equal(detail.status, 200);
      assert.equal(detail.body.sellerSku, savedSku, "Independent GET must observe the saved SKU");
      const state = ctx.observe("saved");
      assert.equal(state.offers[0].seller_sku, savedSku, "Independent SQL must observe the saved SKU");
      assert.equal(state.history.length, 2, "Create plus one successful save event");
      ctx.trace.feedback = { visibleCount, toastText, closeMs, noPermanentBanner: true, sharedAutoClose3000: true };
      return { status: "PASS", reason: "Actual successful admin save produced exactly one shared Toast (" + toastText + ") under the shared autoClose=3000 policy; it closed (observed " + closeMs + "ms after our post-visible check) and no permanent banner remained; independent GET/SQL observed the persisted SKU and one new history event." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  "R10-12-B": () => runCase("R10-12-B", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const original = ctx.observe("before-dirty-exit");
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, fx.actor);
      await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      const dirty = fx.sku + "-DIRTY";
      await page.locator("[data-testid=offer-seller-sku]").fill(dirty);
      await page.waitForTimeout(200);
      await page.locator("[data-testid=offer-cancel]").click();
      await page.waitForSelector("[data-testid=offer-message-confirm]", { timeout: 10000 });
      const dialogButtons = (await page.locator("dialog[open] button").allInnerTexts()).map((text) => text.replace(/\s+/g, " ").trim());
      assert.ok(dialogButtons.includes("بازگشت به فرم"), "Dirty-exit cancel action missing");
      assert.ok(dialogButtons.includes("خروج بدون ذخیره"), "Dirty-exit confirm action missing");
      await page.getByRole("button", { name: "بازگشت به فرم" }).click();
      await page.waitForTimeout(300);
      assert.equal(await page.locator("dialog[open]").count(), 0, "Cancel must close the dialog");
      assert.equal(await page.locator("[data-testid=offer-workspace]").count(), 1, "Cancel must keep the form");
      assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), dirty, "Cancel must keep the draft value");
      await page.locator("[data-testid=offer-cancel]").click();
      await page.waitForSelector("[data-testid=offer-message-confirm]", { timeout: 10000 });
      await page.getByRole("button", { name: "خروج بدون ذخیره" }).click();
      await page.waitForURL(/\/fa\/admin\/offers$/, { timeout: 20000 });
      const unchanged = ctx.observe("after-confirm");
      assert.deepEqual(unchanged, original, "Dirty-exit confirm must not write the draft");
      const reread = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET", undefined, fx.admin);
      assert.equal(reread.body.sellerSku, fx.sku, "Draft must not have been persisted");
      await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), fx.sku, "Confirmed exit must clear the discardable draft");

      // Duplicate link on the actual seller create route.
      const contexts = fx.contexts;
      const candidates = await ctx.api("/v1/admin/offers/query", "POST", { page: 1, pageSize: 100, filters: [], sorts: [] }, fx.admin);
      assert.equal(candidates.status, 200);
      let existing = null;
      for (const item of candidates.body.items || []) {
        if (item.status !== "Active") continue;
        const detail = await ctx.api("/v1/admin/offers/" + item.offerId, "GET", undefined, fx.admin);
        if (detail.status !== 200) continue;
        const sellerContext = contexts.find((row) => row.sellerPartyId.toLowerCase() === String(detail.body.sellerPartyId).toLowerCase());
        if (sellerContext) { existing = { offer: detail.body, seller: sellerContext }; break; }
      }
      if (!existing) throw new NotRun("No active listing is owned by a current DEV seller context for the duplicate-link flow.");
      const variants = await ctx.api("/v1/admin/catalog/products/" + existing.offer.productId + "/variants?locale=fa-IR", "GET", undefined, fx.admin);
      const variant = (Array.isArray(variants.body) ? variants.body : []).find((row) => row.catalogVariantId === existing.offer.catalogVariantId);
      assert.ok(variant && variant.displayLabel, "Existing active variant label missing");
      const countSql = "SELECT count(*) FROM offer.offers WHERE seller_party_id='" + uuid(existing.seller.sellerPartyId) + "' AND catalog_variant_id='" + uuid(existing.offer.catalogVariantId) + "'";
      const countBefore = Number(psql(countSql));
      const sellerContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
      await sellerContext.addInitScript(({ actor, seller }) => {
        localStorage.setItem("tooba.sellerActorUserId", actor);
        localStorage.setItem("tooba.sellerPartyId", seller);
      }, { actor: existing.seller.actorUserId, seller: existing.seller.sellerPartyId });
      const sellerPage = await sellerContext.newPage();
      await sellerPage.goto(FRONT + "/vendor-panel/products/new", { waitUntil: "load" });
      await sellerPage.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await sellerPage.click("[data-testid=offer-product-picker-toggle]");
      await sellerPage.fill("[data-testid=offer-product-dialog-search]", existing.offer.productTitle);
      await sellerPage.keyboard.press("Enter");
      await sellerPage.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
      await sellerPage.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: existing.offer.productTitle }).first().click();
      await sellerPage.waitForTimeout(900);
      await sellerPage.click("[data-testid=offer-variant-picker-toggle]");
      await sellerPage.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
      const target = sellerPage.locator("[data-testid=offer-variant-option]").filter({ hasText: variant.displayLabel }).first();
      assert.ok(await target.count(), "Exact active variant option missing");
      await target.click();
      await sellerPage.waitForSelector("[data-testid=offer-message-duplicate]", { timeout: 20000 });
      const href = await sellerPage.locator("[data-testid=offer-message-open-existing]").getAttribute("href");
      assert.equal(href, "/vendor-panel/products/" + existing.offer.offerId, "Duplicate link must open the existing owned offer");
      const countAfter = Number(psql(countSql));
      assert.equal(countAfter, countBefore, "Duplicate detection must not create a second offer");
      await sellerContext.close();
      ctx.trace.dirtyExit = { dialogButtons, dirtyValueKeptOnCancel: true, confirmClearedDraft: true };
      ctx.trace.duplicate = { existingOfferId: existing.offer.offerId, sellerPartyId: existing.seller.sellerPartyId, variantId: existing.offer.catalogVariantId, href, countBefore, countAfter };
      return { status: "PASS", reason: "Actual dirty exit kept the form/draft on cancel and discarded only on confirm with no write; the real seller duplicate route showed the shared MessageBox link to the existing offer and created no duplicate row." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  // ------------------------------------------------------- R10-13 performance

  "R10-13-P": () => runCase("R10-13-P", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, fx.actor);
      await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
      await waitWorkspace(page, "fa-IR");
      const formSamples = [];
      for (let index = 0; index < 5; index += 1) {
        const started = Date.now();
        await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
        await waitWorkspace(page, "fa-IR");
        formSamples.push(Date.now() - started);
      }
      const pickerSamples = [];
      for (let index = 0; index < 5; index += 1) {
        await page.goto(FRONT + "/fa/admin/offers/new", { waitUntil: "load" });
        await waitWorkspace(page, "fa-IR");
        const started = Date.now();
        await page.locator("[data-testid=offer-product-picker-toggle]").click();
        await page.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
        pickerSamples.push(Date.now() - started);
      }
      const metrics = {
        formMedianMs: median(formSamples), formP95Ms: p95(formSamples), formSamples,
        pickerMedianMs: median(pickerSamples), pickerP95Ms: p95(pickerSamples), pickerSamples,
        r8FormMs: R8_FORM_MS, r8PickerMs: R8_PICKER_MS,
      };
      assert.ok(metrics.formMedianMs <= R8_FORM_MS, "Form median regression: " + metrics.formMedianMs + " > " + R8_FORM_MS);
      assert.ok(metrics.formP95Ms <= R8_FORM_MS * 1.25, "Form p95 material regression: " + metrics.formP95Ms);
      assert.ok(metrics.pickerMedianMs <= R8_PICKER_MS, "Picker median regression: " + metrics.pickerMedianMs + " > " + R8_PICKER_MS);
      assert.ok(metrics.pickerP95Ms <= R8_PICKER_MS * 1.5, "Picker p95 material regression: " + metrics.pickerP95Ms);

      const orders = await ctx.api("/v1/admin/orders/query", "POST", { page: 1, pageSize: 10, filters: [], sorts: [] }, fx.admin);
      assert.equal(orders.status, 200);
      assert.ok(Array.isArray(orders.body.items) && orders.body.items.length > 0, "Orders regression: no rows");
      assert.ok(orders.body.items.every((row) => row.checkoutId && row.status && row.currency), "Orders regression: incomplete rows");
      const products = await ctx.api("/v1/admin/catalog/products?page=1&pageSize=10&locale=fa-IR", "GET", undefined, fx.admin);
      assert.equal(products.status, 200);
      assert.ok(Array.isArray(products.body.items) && products.body.items.length > 0, "Products regression: no rows");
      assert.ok(products.body.items.every((row) => row.productId && row.title), "Products regression: incomplete rows");
      const pageOne = await ctx.api("/v1/admin/offers/query", "POST", { page: 1, pageSize: 5, filters: [], sorts: [] }, fx.admin);
      const pageTwo = await ctx.api("/v1/admin/offers/query", "POST", { page: 2, pageSize: 5, filters: [], sorts: [] }, fx.admin);
      assert.equal(pageOne.status, 200);
      assert.equal(pageTwo.status, 200);
      const idsOne = (pageOne.body.items || []).map((row) => row.offerId);
      const idsTwo = (pageTwo.body.items || []).map((row) => row.offerId);
      assert.ok(idsOne.length > 0 && idsTwo.length > 0, "Grid paging returned empty pages");
      assert.equal(idsOne.filter((id) => idsTwo.includes(id)).length, 0, "Grid pages overlap");
      const filtered = await ctx.api("/v1/admin/offers/query", "POST", { page: 1, pageSize: 10, filters: [{ field: "status", operator: "eq", value: "Draft" }], sorts: [] }, fx.admin);
      assert.equal(filtered.status, 200);
      assert.ok((filtered.body.items || []).length > 0 && filtered.body.items.every((row) => row.status === "Draft"), "Grid status filter not honored");
      ctx.trace.performance = metrics;
      ctx.trace.regression = { orders: orders.body.items.length, products: products.body.items.length, paging: { page1: idsOne.length, page2: idsTwo.length, overlap: 0 }, filteredDrafts: (filtered.body.items || []).length };
      return { status: "PASS", reason: "5-sample warm form median/p95 " + metrics.formMedianMs + "/" + metrics.formP95Ms + "ms and picker " + metrics.pickerMedianMs + "/" + metrics.pickerP95Ms + "ms; both medians are at or below R8 1598/103ms and p95 stays within the declared 1.25x/1.5x material-regression bounds; actual orders, products and grid paging/filter regressions returned complete rows." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  "R10-13-B": () => runCase("R10-13-B", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const detail = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET", undefined, fx.admin);
    assert.equal(detail.status, 200);
    // seller_sku is varchar(64); keep the fixture valid while still exercising a long label.
    const longSku = fx.sku + "-LONG-" + "X".repeat(18);
    assert.ok(longSku.length <= 64, "Long-label fixture exceeds seller_sku column");
    const patched = await ctx.api("/v1/admin/offers/" + fx.offerId, "PATCH", { sellerSku: longSku, status: "Draft", expectedVersion: detail.body.revisions.offer }, fx.admin);
    assert.equal(patched.status, 200, "Long-label fixture patch " + JSON.stringify(patched.body));
    assert.equal(patched.body.sellerSku, longSku);
    ctx.trace.longLabel = { sellerSku: longSku, length: longSku.length };
    const { chromium } = playwright();
    const browser = await chromium.launch();
    try {
      const { page } = await adminBrowser(browser, fx.actor);
      const widths = [];
      for (const width of [360, 768, 1440]) {
        await page.setViewportSize({ width, height: 900 });
        await page.goto(FRONT + "/fa/admin/offers/" + fx.offerId, { waitUntil: "load" });
        await waitWorkspace(page, "fa-IR");
        await page.waitForTimeout(400);
        const formOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
        assert.ok(formOverflow <= 1, "Form horizontal overflow at " + width + ": " + formOverflow);
        assert.equal(await page.locator("[data-testid=offer-seller-sku]").inputValue(), longSku, "Long label value lost at " + width);
        assert.ok(await page.locator("[data-testid=offer-seller-sku]").isVisible(), "Identity input not visible at " + width);
        const saveVisible = await page.locator("[data-offer-save]").evaluateAll((nodes) => nodes.some((node) => Boolean(node.offsetParent || node.getClientRects().length)));
        assert.ok(saveVisible, "No usable save control visible at " + width);
        assert.ok(await page.locator("[data-testid=offer-tab-button-pricing]").isVisible(), "Pricing tab not usable at " + width);
        await page.goto(FRONT + "/ru/admin/offers", { waitUntil: "load" });
        await page.waitForSelector("[data-app-grid-shell]", { timeout: 30000 });
        await page.waitForTimeout(400);
        const listOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
        assert.ok(listOverflow <= 1, "List horizontal overflow at " + width + ": " + listOverflow);
        assert.equal(await page.locator("[data-testid=admin-offers]").getAttribute("dir"), "ltr", "Third-locale list direction at " + width);
        assert.match(await page.locator("[data-testid=admin-offers]").innerText(), /Предложения продавцов/, "Third-locale long label missing at " + width);
        widths.push({ width, formOverflow, listOverflow, saveVisible });
      }
      ctx.trace.responsive = widths;
      return { status: "PASS", reason: "Actual long-label offer form and third-locale grid stayed within viewport at 360/768/1440 with visible usable save/tab controls and zero unintended horizontal overflow." };
    } finally {
      await closeBrowser(browser);
    }
  }),

  // --------------------------------------------------------- R10-14 evidence

  "R10-14-P": () => runCase("R10-14-P", async (ctx) => {
    const fx = await prepareAdmin(ctx);
    const root = process.env.P1C_EVIDENCE_DIR;
    assert.ok(root, "P1C_EVIDENCE_DIR required");
    const sourceCommit = gitHead();
    const buildId = nextBuildId();
    const hostPath = servedHostDll();
    const hostSha256 = hostDllSha();
    const database = databaseIdentity();
    const provider = await providerIdentity();
    const focusedTests = command("dotnet", [
      "test",
      "src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj",
      "--no-restore",
      "--nologo",
      "--filter",
      "FullyQualifiedName~OfferArchitectureGuardTests|FullyQualifiedName~OfferEndpointValidatorCoverageGuardTests",
      "--logger",
      "console;verbosity=minimal",
    ]);
    ctx.trace.commands = ctx.trace.commands || [];
    ctx.trace.commands.push({ label: "focused-offer-guards", ...focusedTests, stdout: tail(focusedTests.stdout), stderr: tail(focusedTests.stderr) });
    requireExit(focusedTests, "Focused Offer guards");
    const focusedCounts = parseDotnetCounts(focusedTests.stdout);
    assert.equal(focusedCounts.failed, 0, "Focused Offer guard failures");
    const detail = await ctx.api("/v1/admin/offers/" + fx.offerId, "GET", undefined, fx.admin);
    assert.equal(detail.status, 200);
    const state = ctx.observe("gate-fixture");
    assert.equal(state.offers.length, 1);
    assert.equal(state.history.length, 1);
    const assertions = [
      {
        assertionId: "provider_identity",
        pass: true,
        expected: { edition: "SingleStore", tenantId: "store-alpha", database: "tooba_alpha" },
        observed: { edition: provider.edition, tenantId: provider.tenantId, database: database.split("|")[0] },
      },
      {
        assertionId: "durable_offer",
        pass: true,
        expected: { offerId: fx.offerId, sellerPartyId: fx.seller, status: "Draft", sellerSku: fx.sku },
        observed: { offerId: detail.body.offerId, sellerPartyId: detail.body.sellerPartyId, status: detail.body.status, sellerSku: detail.body.sellerSku },
      },
      {
        assertionId: "independent_sql",
        pass: true,
        expected: { offerId: fx.offerId, status: "Draft", seller_sku: fx.sku, history: 1 },
        observed: { offerId: state.offers[0].offer_id, status: state.offers[0].status, seller_sku: state.offers[0].seller_sku, history: state.history.length },
      },
      {
        assertionId: "focused_guards",
        pass: true,
        expected: { exit: 0, failed: 0 },
        observed: { exit: focusedTests.exit, failed: focusedCounts.failed },
      },
    ];
    for (const fact of assertions) assert.deepEqual(fact.observed, fact.expected, "Gate fact mismatch " + fact.assertionId + ": " + JSON.stringify(fact));
    const fixture = buildGateFixture({ id: "R10-14-P", sourceCommit, hostSha256, nextBuildIdValue: buildId, database: database.split("|")[0], assertions, root });
    const gate = invokeAcceptanceGate(fixture.files, root);
    ctx.trace.acceptanceGate = { ...gate, stdout: tail(gate.stdout), stderr: tail(gate.stderr) };
    requireExit(gate, "Acceptance gate real fixture");
    const parsed = parseGateOutput(gate);
    assert.equal(parsed.ok, true, "Acceptance gate rejected real fixture: " + JSON.stringify(parsed));
    assert.deepEqual(parsed.errors, [], "Acceptance gate errors: " + JSON.stringify(parsed.errors));
    ctx.trace.identity = { sourceCommit, buildId, hostPath, hostSha256, database, provider };
    ctx.trace.gateFiles = fixture.files;
    return { status: "PASS", reason: "Real isolated fixture, provider/build/DB identities, evidence hash and focused Offer guards were accepted by the actual acceptance gate bound to the served Next build and Host DLL." };
  }),

  "R10-14-N": () => runCase("R10-14-N", async (ctx) => {
    const root = process.env.P1C_EVIDENCE_DIR;
    assert.ok(root, "P1C_EVIDENCE_DIR required");
    const acceptanceSelf = command(process.execPath, [ACCEPTANCE_GATE, "--self-test"]);
    requireExit(acceptanceSelf, "Acceptance gate self-test");
    assert.match(acceptanceSelf.stdout, /PASS: positive fixture \+ 11 negative controls/, "Acceptance gate negative-control count changed: " + acceptanceSelf.stdout.trim());
    const architectSelf = command(process.execPath, [ARCHITECT_GATE, "--self-test"]);
    requireExit(architectSelf, "Architect review gate self-test");
    assert.match(architectSelf.stdout, /PASS: positive fixture \+ 14 negative controls/, "Architect gate negative-control count changed: " + architectSelf.stdout.trim());
    const sourceCommit = gitHead();
    const hostSha256 = hostDllSha();
    const buildId = nextBuildId();
    const database = databaseIdentity().split("|")[0];
    const assertions = [
      { assertionId: "acceptance_self_test", pass: true, expected: true, observed: acceptanceSelf.exit === 0 && /11 negative controls/.test(acceptanceSelf.stdout) },
      { assertionId: "architect_self_test", pass: true, expected: true, observed: architectSelf.exit === 0 && /14 negative controls/.test(architectSelf.stdout) },
    ];
    for (const fact of assertions) assert.deepEqual(fact.observed, fact.expected, "Self-test fact mismatch " + fact.assertionId);
    const fixture = buildGateFixture({ id: "R10-14-N", sourceCommit, hostSha256, nextBuildIdValue: buildId, database, assertions, root });
    const valid = invokeAcceptanceGate(fixture.files, root);
    requireExit(valid, "Acceptance gate valid negative-control fixture");
    assert.equal(parseGateOutput(valid).ok, true, "Valid negative-control fixture must be accepted before tampering");

    const tamper = (label, mutate) => {
      const run = JSON.parse(JSON.stringify(fixture.run));
      mutate(run);
      const file = path.join(root, "R10-14-N." + label + ".run.json");
      assert.ok(!fs.existsSync(file), "Do not overwrite tamper evidence");
      fs.writeFileSync(file, JSON.stringify(run, null, 2), { flag: "wx" });
      const result = invokeAcceptanceGate({ ...fixture.files, run: file }, root);
      const parsed = parseGateOutput(result);
      return { label, exit: result.exit, ok: parsed.ok, errors: parsed.errors, stdout: tail(result.stdout) };
    };
    const missing = tamper("missing-test", (run) => { run.tests = []; });
    assert.equal(missing.exit, 1, "Missing test must be rejected");
    assert.ok((missing.errors || []).some((error) => /missing plan\/result R10-14-N/.test(error)), "Missing-test rejection reason absent: " + JSON.stringify(missing.errors));
    const changed = tamper("changed-fact", (run) => { run.tests[0].assertions[0].observed = false; });
    assert.equal(changed.exit, 1, "Changed fact must be rejected");
    assert.ok((changed.errors || []).some((error) => /oracle equality mismatch R10-14-N/.test(error)), "Changed-fact rejection reason absent: " + JSON.stringify(changed.errors));
    const tampered = tamper("tampered-evidence", (run) => { run.tests[0].evidenceSha256 = "0".repeat(64); });
    assert.equal(tampered.exit, 1, "Tampered evidence hash must be rejected");
    assert.ok((tampered.errors || []).some((error) => /hash mismatch R10-14-N/.test(error)), "Tampered-evidence rejection reason absent: " + JSON.stringify(tampered.errors));
    ctx.trace.negativeControls = {
      acceptanceSelf: { exit: acceptanceSelf.exit, stdout: tail(acceptanceSelf.stdout), stderr: tail(acceptanceSelf.stderr) },
      architectSelf: { exit: architectSelf.exit, stdout: tail(architectSelf.stdout), stderr: tail(architectSelf.stderr) },
      valid: { exit: valid.exit, ok: parseGateOutput(valid).ok },
      missing, changed, tampered,
    };
    return { status: "PASS", reason: "Actual acceptance and architect gates rejected all mandated negative controls: missing test, changed fact and tampered evidence hash, plus their own 11 and 14 self-test controls." };
  }),

  "R10-14-B": async () => {
    const root = process.env.P1C_EVIDENCE_DIR;
    assert.ok(root, "P1C_EVIDENCE_DIR required");
    fs.mkdirSync(root, { recursive: true });
    const evidence = path.join(root, "R10-14-B.json");
    assert.ok(!fs.existsSync(evidence), "Do not overwrite historical evidence");
    const trace = { id: "R10-14-B", status: "NOT_RUN", requests: [], sql: [] };
    const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
    let seller = null;
    let offerId = null;
    let cleanupNeeded = false;
    try {
      trace.preflight = S.preflight();
      const dev = await http("/v1/admin/dev-context");
      assert.equal(dev.status, 200);
      const actor = uuid(dev.body.actorUserId);
      const admin = { [ADMIN]: actor };
      seller = uuid(process.env.P1C_SELLER_ID || DEFAULT_SELLER);
      const variant = uuid(process.env.P1C_CREATE_VARIANT_ID || DEFAULT_VARIANT);
      const channels = await http("/v1/admin/offers/allowed-channels", "GET", undefined, admin);
      assert.equal(channels.status, 200);
      const channel = channels.body[0];
      const contexts = await http("/v1/seller/dev-contexts");
      assert.ok(contexts.body.actors.some((row) => row.sellerPartyId.toLowerCase() === seller), "Seller is not a current DEV synthetic identity");
      const requestId = "P1C-" + randomUUID();
      offerId = deterministicId(seller, variant, requestId);
      const sku = SKU_PREFIX + randomUUID();
      const before = snapshot([offerId]);
      trace.before = before;
      assert.ok(EMPTY(before), "Candidate identity is not isolated");
      const created = await http("/v1/admin/offers", "POST", { sellerPartyId: seller, catalogVariantId: variant, channel, sellerSku: sku, status: "Draft", requestId }, admin);
      trace.requests.push({ label: "create", status: created.status, body: created.body });
      assert.equal(created.status, 201, "isolated create failed");
      cleanupNeeded = true;
      const priced = await http("/v1/admin/offers/" + offerId + "/price", "PUT", { amount: 123000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() }, admin);
      trace.requests.push({ label: "price", status: priced.status, body: priced.body });
      assert.equal(priced.status, 200, "fixture price write failed");
      const mid = snapshot([offerId]);
      trace.midTestState = mid;
      trace.sql.push({ label: "mid-test", state: mid });
      assert.equal(mid.offers.length, 1);
      assert.equal(mid.prices.length, 1);
      assert.equal(mid.priceFacts.length, 1);
      const unrelatedId = psql("SELECT offer_id FROM offer.offers WHERE offer_id <> '" + uuid(offerId) + "' ORDER BY offer_id LIMIT 1");
      assert.match(unrelatedId, /^[0-9a-f-]{36}$/i, "No unrelated offer available for baseline comparison");
      const unrelatedBefore = psql("SELECT md5(to_jsonb(t)::text) FROM offer.offers t WHERE offer_id='" + uuid(unrelatedId) + "'");
      trace.unrelated = { offerId: unrelatedId, before: unrelatedBefore };
      try {
        throw new Error("P1C induced mid-test failure before cleanup certification");
      } catch (error) {
        trace.midTestError = String(error && error.stack ? error.stack : error);
      } finally {
        trace.cleanup = cleanupExact([offerId], seller);
        trace.afterCleanup = snapshot([offerId]);
        trace.unrelated.after = psql("SELECT md5(to_jsonb(t)::text) FROM offer.offers t WHERE offer_id='" + uuid(unrelatedId) + "'");
        trace.reread = await http("/v1/admin/offers/" + offerId, "GET", undefined, admin);
        trace.sql.push({ label: "after-cleanup", state: trace.afterCleanup });
        assert.ok(EMPTY(trace.afterCleanup), "Residual fixture rows after finally cleanup");
        assert.equal(trace.unrelated.after, trace.unrelated.before, "Unrelated offer row changed");
        assert.ok([400, 404].includes(trace.reread.status), "Independent reread must not find the cleaned offer, got " + trace.reread.status);
        cleanupNeeded = false;
      }
      trace.status = "PASS";
      trace.reason = "Induced mid-test failure was followed by exact finally cleanup; independent snapshot is empty, the unrelated offer hash is unchanged and the cleaned offer reread is not found.";
    } catch (error) {
      trace.status = cleanupNeeded ? "FAIL" : "NOT_RUN";
      trace.reason = String(error && error.stack ? error.stack : error);
    } finally {
      if (cleanupNeeded && seller && offerId) {
        try {
          trace.emergencyCleanup = cleanupExact([offerId], seller);
        } catch (error) {
          trace.status = "FAIL";
          trace.cleanupError = String(error && error.stack ? error.stack : error);
          trace.reason = (trace.reason || "") + "; emergency cleanup not certified";
        }
      }
      save();
    }
    return { status: trace.status, reason: trace.reason, evidence };
  },
};
