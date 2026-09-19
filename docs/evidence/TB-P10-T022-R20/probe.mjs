/**
 * TB-P10-T022-R20 — Admin campaign create/members/prices/publish + Builder/Storefront/Cart regression.
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R20");
const report = { ok: true, steps: [], errors: [], campaignId: null, tip: null };
const stamp = Date.now();

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function hostJson(path, { method = "GET", body, actor, headers = {} } = {}) {
  const h = { Accept: "application/json", ...headers };
  if (actor) h["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (body !== undefined) h["Content-Type"] = "application/json";
  const r = await fetch(`${HOST}${path}`, {
    method,
    headers: h,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { ok: r.ok, status: r.status, data, text };
}

function idOf(obj, ...keys) {
  for (const k of keys) {
    if (obj?.[k] != null) return obj[k];
  }
  return null;
}

async function main() {
  let ready = false;
  for (let i = 0; i < 90; i++) {
    try {
      const r = await fetch(`${HOST}/v1/admin/dev-context`);
      if (r.ok) {
        ready = true;
        break;
      }
    } catch {
      /* wait */
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
  rec("A-host-ready", ready);
  if (!ready) {
    writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
    process.exit(1);
  }

  const dev = await hostJson("/v1/admin/dev-context");
  const actor = idOf(dev.data, "actorUserId", "ActorUserId");
  rec("dev-actor", Boolean(actor), { actor });

  const types = await hostJson("/v1/admin/merchandising-campaigns/types?locale=fa-IR", { actor });
  const typeItems = types.data?.items || types.data?.Items || [];
  const amazing = typeItems.find(
    (t) =>
      String(t.displayName || t.DisplayName || "").includes("شگفت") ||
      String(t.displayName || t.DisplayName || "").toLowerCase().includes("amazing"),
  );
  rec("B-types", types.ok && Boolean(amazing), {
    status: types.status,
    count: typeItems.length,
    name: amazing?.displayName || amazing?.DisplayName,
  });
  const promotionTypeId = idOf(amazing, "promotionTypeId", "PromotionTypeId");

  const now = new Date();
  const startAt = new Date(now.getTime() - 60 * 60 * 1000).toISOString();
  const endAt = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000).toISOString();
  const created = await hostJson("/v1/admin/merchandising-campaigns", {
    method: "POST",
    actor,
    body: {
      promotionTypeId,
      startAt,
      endAt,
      priority: 200,
      translations: [
        { locale: "fa-IR", title: `کمپین ادمین R20 ${stamp}`, subtitle: "زیرعنوان", badgeText: "شگفت" },
        { locale: "en-US", title: `Admin R20 ${stamp}`, subtitle: "Sub", badgeText: "Deal" },
      ],
    },
  });
  const campaignId = idOf(created.data, "campaignId", "CampaignId");
  report.campaignId = campaignId;
  rec("C-create-draft", created.ok && Boolean(campaignId), { status: created.status, campaignId });

  const listed = await hostJson("/v1/admin/merchandising-campaigns?search=R20&take=50", { actor });
  const listItems = listed.data?.items || listed.data?.Items || [];
  rec(
    "D-list-contains",
    listed.ok && listItems.some((r) => idOf(r, "campaignId", "CampaignId") === campaignId),
    { total: listed.data?.total ?? listed.data?.Total },
  );

  const candidates = await hostJson(
    `/v1/admin/merchandising-campaigns/offer-candidates?take=50&search=`,
    { actor },
  );
  const candItems = candidates.data?.items || candidates.data?.Items || [];
  const inStock = candItems.filter((c) => c.inStock ?? c.InStock);
  rec("E-offer-candidates", candidates.ok && inStock.length >= 5, {
    status: candidates.status,
    count: candItems.length,
    inStock: inStock.length,
  });

  const offerIds = inStock
    .slice(0, 5)
    .map((c) => idOf(c, "sellerOfferId", "SellerOfferId"))
    .filter(Boolean);
  for (const oid of offerIds) {
    const add = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}/members`, {
      method: "POST",
      actor,
      body: { sellerOfferId: oid },
    });
    rec(`F-add-member-${oid.slice(0, 8)}`, add.ok, { status: add.status });
  }

  const dup = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}/members`, {
    method: "POST",
    actor,
    body: { sellerOfferId: offerIds[0] },
  });
  rec("G-duplicate-rejected", !dup.ok || dup.status >= 400, { status: dup.status });

  const reversed = [...offerIds].reverse();
  const reorder = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}/members/order`, {
    method: "PUT",
    actor,
    body: { orderedSellerOfferIds: reversed },
  });
  rec("H-reorder", reorder.ok, { status: reorder.status });

  const detailAfterReorder = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}`, { actor });
  const members = detailAfterReorder.data?.members || detailAfterReorder.data?.Members || [];
  const orderOk =
    members.length >= 5 &&
    idOf(members[0], "sellerOfferId", "SellerOfferId") === reversed[0];
  rec("I-reorder-persisted", orderOk, {
    first: idOf(members[0], "sellerOfferId", "SellerOfferId"),
    expected: reversed[0],
  });

  const pricedOffers = reversed.slice(0, 3);
  for (const oid of pricedOffers) {
    const member = members.find((m) => idOf(m, "sellerOfferId", "SellerOfferId") === oid);
    const base = Number(member?.baseAmount ?? member?.BaseAmount ?? 100000);
    const promo = Math.max(1000, Math.round(base * 0.7));
    const price = await hostJson(
      `/v1/admin/merchandising-campaigns/${campaignId}/members/${oid}/price`,
      {
        method: "PUT",
        actor,
        body: { amount: promo, currency: member?.currency || member?.Currency || "IRR" },
      },
    );
    rec(`J-price-${oid.slice(0, 8)}`, price.ok, { status: price.status, promo });
  }

  const reload = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}`, { actor });
  const reloadMembers = reload.data?.members || reload.data?.Members || [];
  const pricedCount = reloadMembers.filter(
    (m) => (m.campaignAmount ?? m.CampaignAmount) != null,
  ).length;
  rec("K-prices-persisted", pricedCount >= 3, { pricedCount });

  const translations = reload.data?.translations || reload.data?.Translations || [];
  const hasFa = translations.some(
    (t) => String(t.locale || t.Locale).startsWith("fa") && String(t.title || t.Title).includes("R20"),
  );
  const hasEn = translations.some(
    (t) => String(t.locale || t.Locale).startsWith("en") && String(t.title || t.Title).includes("R20"),
  );
  rec("L-translations-fa-en", hasFa && hasEn, { locales: translations.map((t) => t.locale || t.Locale) });

  const publish = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}/publish`, {
    method: "POST",
    actor,
  });
  rec("M-publish", publish.ok, { status: publish.status });

  const afterPub = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}`, { actor });
  const runtime = afterPub.data?.runtimeLabel || afterPub.data?.RuntimeLabel;
  const life = afterPub.data?.lifecycleStatus || afterPub.data?.LifecycleStatus;
  rec("N-runtime-active", runtime === "active" && String(life).toLowerCase() === "published", {
    runtime,
    life,
  });

  const listActive = await hostJson("/v1/admin/merchandising-campaigns?runtimeWindow=active&take=50", {
    actor,
  });
  const activeItems = listActive.data?.items || listActive.data?.Items || [];
  rec(
    "O-list-active",
    activeItems.some((r) => idOf(r, "campaignId", "CampaignId") === campaignId),
    { count: activeItems.length },
  );

  // Builder page with PromotionCampaign AMAZING CampaignId=null
  const pageSlug = `r20-admin-camp-${stamp}`;
  const page = await hostJson("/v1/admin/pages", {
    method: "POST",
    actor,
    body: {
      title: `R20 Admin Camp ${stamp}`,
      slug: pageSlug,
      locale: "fa",
      pageType: "Landing",
      seoTitle: "R20",
      seoDescription: "Admin campaign resolve",
    },
  });
  const pageId = idOf(page.data, "pageId", "PageId");
  rec("P-create-page", page.ok && Boolean(pageId), { status: page.status, pageId });

  const showcaseConfig = {
    title: "پیشنهاد شگفت‌انگیز",
    source: "PromotionCampaign",
    promotionTypeCode: "AMAZING",
    campaignId: null,
    take: 8,
    variantKey: "product.card-carousel",
  };
  const section = await hostJson(`/v1/admin/pages/${pageId}/sections`, {
    method: "POST",
    actor,
    body: {
      sectionType: "ProductCollection",
      config: JSON.stringify(showcaseConfig),
      isEnabled: true,
    },
  });
  rec("Q-add-showcase", section.ok, { status: section.status, body: section.data });

  await hostJson(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    actor,
    body: { status: "Published" },
  });

  const preview = await hostJson(`/v1/admin/pages/${pageId}/preview`, { actor });
  const previewText = JSON.stringify(preview.data || {});
  const previewHasCampaign = previewText.includes(campaignId);
  rec("R-builder-preview-resolves", preview.ok && previewHasCampaign, {
    status: preview.status,
    hasId: previewHasCampaign,
  });

  const publicFa = await hostJson(`/v1/storefront/pages/${pageSlug}?locale=fa`);
  const products = publicFa.data?.products || publicFa.data?.Products || [];
  const promoCard = products.find((c) => {
    const cid = c.merchandisingCampaignId ?? c.MerchandisingCampaignId;
    return cid === campaignId;
  });
  rec("S-storefront-resolves", publicFa.ok && Boolean(promoCard), {
    status: publicFa.status,
    productCount: products.length,
    campaignMatch: Boolean(promoCard),
  });

  try {
    const sf = await fetch(`${FE}/fa/${pageSlug}`);
    const html = await sf.text();
    rec("S2-storefront-html", sf.ok && html.length > 500, { status: sf.status, len: html.length });
  } catch (e) {
    rec("S2-storefront-html", false, { error: String(e) });
  }

  // Cart ATC with campaign context BEFORE archive
  const tipOffer = pricedOffers[0];
  const tipMember = reloadMembers.find((m) => idOf(m, "sellerOfferId", "SellerOfferId") === tipOffer);
  const tipAmount = tipMember?.campaignAmount ?? tipMember?.CampaignAmount;
  const guest = await hostJson("/v1/storefront/cart", { method: "POST" });
  const guestSecret = guest.data?.guestSecret ?? guest.data?.GuestSecret;
  const cart = guest.data?.cart ?? guest.data?.Cart ?? guest.data;
  const cartId = cart?.cartId ?? cart?.CartId;
  let version = cart?.version ?? cart?.Version;
  rec("T-cart-create", guest.ok && Boolean(cartId) && Boolean(guestSecret), {
    status: guest.status,
    cartId,
  });

  const addLine = await hostJson(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    method: "POST",
    headers: { "X-Tooba-Guest-Secret": guestSecret },
    body: {
      offerId: tipOffer,
      quantity: 1,
      merchandisingCampaignId: campaignId,
    },
  });
  const lines = addLine.data?.lines || addLine.data?.Lines || [];
  const line = Array.isArray(lines) ? lines[0] : null;
  const unit =
    line?.unitAmountExclusiveOfTax ??
    line?.UnitAmountExclusiveOfTax ??
    line?.unitAmount ??
    line?.UnitAmount;
  const lineCamp = line?.merchandisingCampaignId ?? line?.MerchandisingCampaignId;
  rec("U-cart-campaign-price", addLine.ok && Number(unit) === Number(tipAmount) && lineCamp === campaignId, {
    status: addLine.status,
    unit,
    tipAmount,
    lineCamp,
    err: addLine.ok ? undefined : addLine.data,
  });

  const archive = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}/archive`, {
    method: "POST",
    actor,
  });
  rec("V-archive", archive.ok, { status: archive.status });

  const afterArch = await hostJson(`/v1/admin/merchandising-campaigns/${campaignId}`, { actor });
  const archLife = afterArch.data?.lifecycleStatus || afterArch.data?.LifecycleStatus;
  const archRuntime = afterArch.data?.runtimeLabel || afterArch.data?.RuntimeLabel;
  rec("W-archived-state", String(archLife).toLowerCase() === "archived" || archRuntime === "archived", {
    archLife,
    archRuntime,
  });

  // Bust public page cache (2min) so archive eligibility is observable without waiting.
  await hostJson(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    actor,
    body: { status: "Published" },
  });

  let stillPrimary = true;
  let productCount2 = 0;
  let publicOk = false;
  for (let i = 0; i < 8; i++) {
    const publicFa2 = await hostJson(`/v1/storefront/pages/${pageSlug}?locale=fa`);
    publicOk = publicFa2.ok;
    const products2 = publicFa2.data?.products || publicFa2.data?.Products || [];
    productCount2 = products2.length;
    stillPrimary = products2.some(
      (c) => (c.merchandisingCampaignId ?? c.MerchandisingCampaignId) === campaignId,
    );
    if (!stillPrimary) break;
    await new Promise((r) => setTimeout(r, 400));
  }
  const activeAfter = await hostJson("/v1/admin/merchandising-campaigns?runtimeWindow=active&take=50", {
    actor,
  });
  const activeIds = (activeAfter.data?.items || activeAfter.data?.Items || []).map((r) =>
    idOf(r, "campaignId", "CampaignId"),
  );
  const notInActive = !activeIds.includes(campaignId);
  const previewArch = await hostJson(`/v1/admin/pages/${pageId}/preview`, { actor });
  const previewArchText = JSON.stringify(previewArch.data || {});
  const previewStill = previewArchText.includes(campaignId);
  rec("X-archived-not-primary", notInActive && (!stillPrimary || !previewStill), {
    stillPrimary,
    previewStill,
    productCount: productCount2,
    notInActive,
    publicOk,
  });

  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((e) => {
  report.ok = false;
  report.errors.push(String(e));
  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
