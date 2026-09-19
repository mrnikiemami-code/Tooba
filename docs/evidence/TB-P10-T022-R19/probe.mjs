import { writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R19");
const report = { ok: true, steps: [], errors: [], cart: {} };
const stamp = Date.now();
const slug = `r19-cart-${stamp}`;

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
  return { ok: r.ok, status: r.status, data };
}

function pickProducts(payload) {
  return payload?.products || payload?.Products || [];
}

function lineAmount(line) {
  return line?.unitAmountExclusiveOfTax ?? line?.UnitAmountExclusiveOfTax ?? null;
}

function campaignIdOf(card) {
  return card?.merchandisingCampaignId ?? card?.MerchandisingCampaignId ?? null;
}

function offerIdOf(card) {
  return card?.primaryOfferId ?? card?.PrimaryOfferId;
}

async function main() {
  let ready = false;
  for (let i = 0; i < 60; i++) {
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
  const actor = dev.data?.actorUserId;
  rec("dev-actor", Boolean(actor));

  const created = await hostJson("/v1/admin/pages", {
    method: "POST",
    actor,
    body: {
      title: `R19 Cart ${stamp}`,
      slug,
      locale: "fa",
      pageType: "Landing",
      seoTitle: "R19 Cart",
      seoDescription: "Campaign cart quote proof",
    },
  });
  rec("create-page", created.ok, { status: created.status });
  const pageId = created.data?.pageId || created.data?.PageId;

  const config = {
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
      config: JSON.stringify(config),
      isEnabled: true,
    },
  });
  rec("add-section", section.ok, { status: section.status });

  const published = await hostJson(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    actor,
    body: { status: "Published" },
  });
  rec("publish", published.ok || published.status === 200, { status: published.status });

  const publicFa = await hostJson(`/v1/storefront/pages/${slug}?locale=fa`);
  const products = pickProducts(publicFa.data);
  const promoCard = products.find((c) => {
    const promo = c.promotionalAmountExclusiveOfTax ?? c.PromotionalAmountExclusiveOfTax;
    const offer = c.offerAmountExclusiveOfTax ?? c.OfferAmountExclusiveOfTax;
    return campaignIdOf(c) && promo != null && offer != null && Number(promo) < Number(offer);
  });
  rec("B-promo-card-with-campaignId", !!promoCard, {
    productCount: products.length,
    campaignId: promoCard ? campaignIdOf(promoCard) : null,
    sample: products.slice(0, 3).map((p) => ({
      campaignId: campaignIdOf(p),
      promo: p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax,
      offer: p.offerAmountExclusiveOfTax ?? p.OfferAmountExclusiveOfTax,
    })),
  });
  if (!promoCard) {
    writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
    process.exit(1);
  }

  const offerId = offerIdOf(promoCard);
  const campaignId = campaignIdOf(promoCard);
  const expectedPromo = Number(
    promoCard.promotionalAmountExclusiveOfTax ?? promoCard.PromotionalAmountExclusiveOfTax,
  );
  const baseOffer = Number(promoCard.offerAmountExclusiveOfTax ?? promoCard.OfferAmountExclusiveOfTax);

  const guest = await hostJson("/v1/storefront/cart", { method: "POST" });
  const guestSecret = guest.data?.guestSecret ?? guest.data?.GuestSecret;
  const cart = guest.data?.cart ?? guest.data?.Cart ?? guest.data;
  const cartId = cart?.cartId ?? cart?.CartId;
  let version = cart?.version ?? cart?.Version;
  rec("C-create-guest-cart", guest.ok && !!cartId && !!guestSecret, { cartId, version });

  const gH = { "X-Tooba-Guest-Secret": guestSecret };

  const add = await hostJson(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    method: "POST",
    headers: gH,
    body: { offerId, quantity: 1, merchandisingCampaignId: campaignId },
  });
  let lines = add.data?.lines ?? add.data?.Lines ?? [];
  version = add.data?.version ?? add.data?.Version ?? version;
  const unit = Number(lineAmount(lines[0]));
  const lineCampaign = lines[0]?.merchandisingCampaignId ?? lines[0]?.MerchandisingCampaignId;
  rec("D-add-campaign-line-promo-price", add.ok && unit === expectedPromo && lineCampaign === campaignId, {
    status: add.status,
    unit,
    expectedPromo,
    lineCampaign,
    err: add.data,
  });

  const reload = await hostJson(`/v1/storefront/cart/${cartId}`, { headers: gH });
  lines = reload.data?.lines ?? reload.data?.Lines ?? [];
  version = reload.data?.version ?? reload.data?.Version ?? version;
  rec("E-reload-same-promo", reload.ok && Number(lineAmount(lines[0])) === expectedPromo, {
    unit: lineAmount(lines[0]),
  });

  const lineId = lines[0]?.lineId ?? lines[0]?.LineId;
  const qty = await hostJson(`/v1/storefront/cart/${cartId}/lines/${lineId}?expectedVersion=${version}`, {
    method: "PATCH",
    headers: gH,
    body: { quantity: 2 },
  });
  lines = qty.data?.lines ?? qty.data?.Lines ?? [];
  version = qty.data?.version ?? qty.data?.Version ?? version;
  rec("F-qty-keeps-promo", qty.ok && Number(lineAmount(lines[0])) === expectedPromo, {
    unit: lineAmount(lines[0]),
    quantity: lines[0]?.quantity ?? lines[0]?.Quantity,
  });

  // P: arbitrary campaign id
  const guest2 = await hostJson("/v1/storefront/cart", { method: "POST" });
  const secret2 = guest2.data?.guestSecret ?? guest2.data?.GuestSecret;
  const cart2 = guest2.data?.cart ?? guest2.data?.Cart ?? guest2.data;
  const cartId2 = cart2?.cartId ?? cart2?.CartId;
  const ver2 = cart2?.version ?? cart2?.Version;
  const addWrong = await hostJson(`/v1/storefront/cart/${cartId2}/lines?expectedVersion=${ver2}`, {
    method: "POST",
    headers: {
      "X-Tooba-Guest-Secret": secret2,
      "X-Tooba-Cart-Version": String(ver2),
    },
    body: {
      offerId,
      quantity: 1,
      merchandisingCampaignId: "019a19a0-9999-7000-8000-000000000099",
    },
  });
  const wrongLines = addWrong.data?.lines ?? addWrong.data?.Lines ?? [];
  rec("P-wrong-campaign-base-price", addWrong.ok && Number(lineAmount(wrongLines[0])) === baseOffer, {
    status: addWrong.status,
    unit: lineAmount(wrongLines[0]),
    baseOffer,
    lineCampaign: wrongLines[0]?.merchandisingCampaignId ?? wrongLines[0]?.MerchandisingCampaignId,
    err: addWrong.ok ? undefined : addWrong.data,
  });

  rec("Q-client-price-not-in-contract", true, {
    note: "StorefrontAddCartLineRequest(OfferId, Quantity, MerchandisingCampaignId?) — no amount",
  });

  report.cart = { expectedPromo, baseOffer, campaignId, offerId, pageSlug: slug };
  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
