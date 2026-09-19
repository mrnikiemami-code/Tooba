import { writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R18");
const report = { ok: true, steps: [], errors: [], preview: {}, storefront: {} };

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function hostJson(path, { method = "GET", body, actor } = {}) {
  const headers = { Accept: "application/json" };
  if (actor) headers["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const r = await fetch(`${HOST}${path}`, {
    method,
    headers,
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

function promoCount(products) {
  return products.filter((p) => {
    const promo = p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax;
    const offer = p.offerAmountExclusiveOfTax ?? p.OfferAmountExclusiveOfTax;
    return promo != null && offer != null && Number(promo) < Number(offer);
  }).length;
}

function baseOnlyCount(products) {
  return products.filter((p) => {
    const promo = p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax;
    return promo == null;
  }).length;
}

const stamp = Date.now();
const slug = `r18-promo-${stamp}`;

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
  rec("host-ready", ready);
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
      title: `R18 Promo ${stamp}`,
      slug,
      locale: "fa",
      pageType: "Landing",
      seoTitle: "R18 Promo",
      seoDescription: "Campaign promo pricing proof",
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

  const preview = await hostJson(`/v1/admin/pages/${pageId}/preview`, { actor });
  const previewProducts = pickProducts(preview.data);
  const previewPromos = promoCount(previewProducts);
  const previewBaseOnly = baseOnlyCount(previewProducts);
  report.preview = {
    productCount: previewProducts.length,
    promoCount: previewPromos,
    baseOnlyCount: previewBaseOnly,
    sample: previewProducts.slice(0, 4).map((p) => ({
      offer: p.offerAmountExclusiveOfTax ?? p.OfferAmountExclusiveOfTax,
      promo: p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax,
      label: p.promotionLabel ?? p.PromotionLabel,
    })),
  };
  rec("preview-ok", preview.ok);
  rec("preview-has-products", previewProducts.length > 0);
  rec("preview-at-least-3-promos", previewPromos >= 3, { previewPromos });
  rec("preview-has-base-only-fallback", previewBaseOnly >= 1, { previewBaseOnly });

  const published = await hostJson(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    actor,
    body: { status: "Published" },
  });
  rec("publish", published.ok || published.status === 200, { status: published.status });

  const publicFa = await hostJson(`/v1/storefront/pages/${slug}?locale=fa`);
  const faProducts = pickProducts(publicFa.data);
  const faPromos = promoCount(faProducts);
  const faBaseOnly = baseOnlyCount(faProducts);
  report.storefront = {
    productCount: faProducts.length,
    promoCount: faPromos,
    baseOnlyCount: faBaseOnly,
    sample: faProducts.slice(0, 4).map((p) => ({
      offer: p.offerAmountExclusiveOfTax ?? p.OfferAmountExclusiveOfTax,
      promo: p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax,
    })),
  };
  rec("storefront-ok", publicFa.ok);
  rec("storefront-at-least-3-promos", faPromos >= 3, { faPromos });
  rec("storefront-base-only-fallback", faBaseOnly >= 1, { faBaseOnly });

  // Discount math check on first promo card
  const promoCard = faProducts.find((p) => (p.promotionalAmountExclusiveOfTax ?? p.PromotionalAmountExclusiveOfTax) != null);
  if (promoCard) {
    const offer = Number(promoCard.offerAmountExclusiveOfTax ?? promoCard.OfferAmountExclusiveOfTax);
    const promo = Number(promoCard.promotionalAmountExclusiveOfTax ?? promoCard.PromotionalAmountExclusiveOfTax);
    const pct = Math.round((1 - promo / offer) * 100);
    rec("discount-math-valid", promo < offer && pct > 0 && pct < 100, { offer, promo, pct });
  } else {
    rec("discount-math-valid", false);
  }

  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, preview: report.preview, storefront: report.storefront }, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(OUT, "probe-report.json"), JSON.stringify(report, null, 2));
  console.error(err);
  process.exit(1);
});
