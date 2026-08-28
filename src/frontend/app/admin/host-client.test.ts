import assert from "node:assert/strict";
import test from "node:test";
import { mapAdminProductList, mapProductWorkspaceView } from "./host-client.ts";

test("list mapper keeps offer counts off the Product price field", () => {
  const rows = mapAdminProductList([
    { productId: "p1", title: "Shirt", status: "Published", variantCount: 1, offerCount: 2 },
  ]);
  assert.equal(rows[0]?.id, "p1");
  assert.equal(rows[0]?.offerCount, 2);
  assert.equal(rows[0]?.sellableUnits, 0);
  assert.equal(rows[0]?.categorySummary, "بدون دسته");
  assert.equal("price" in (rows[0] ?? {}), false);
});

test("workspace mapper keeps prices on offers not product", () => {
  const view = mapProductWorkspaceView({
    productId: "p1",
    title: "Shirt",
    status: "Published",
    kind: "Physical",
    brandName: null,
    categoryNames: ["wear"],
    primaryCategoryId: "cat-1",
    categoryPath: "wear › shirts",
    slug: "shirt",
    shortDescription: "Cotton shirt",
    translations: [
      {
        locale: "fa-IR",
        name: "پیراهن",
        slug: "shirt",
        shortDescription: "پیراهن نخی",
        description: null,
        seoTitle: null,
        seoDescription: null,
      },
    ],
    variants: [{ variantId: "v1", fingerprint: "size=m", status: "Published", catalogCodeSeam: "SHIRT-M", offerCount: 1 }],
    media: [{ mediaAssetId: "m1", primary: true, displayOrder: 0, altText: "front" }],
    offers: [
      {
        offerId: "o1",
        catalogVariantId: "v1",
        sellerPartyId: "s1",
        sellerDisplayName: "بازار اطلس",
        status: "Active",
        channel: "Web",
        sellerSku: "SKU",
      },
    ],
    prices: [{ priceId: "pr1", offerId: "o1", market: "IR", currency: "IRR", amountExclusiveOfTax: 10, status: "Active" }],
    taxClassifications: [{ offerId: "o1", categoryCode: "standard", displayName: "std" }],
      stock: [{ offerId: "o1", locationId: "l1", locationCode: "THR", locationName: "انبار تهران", onHand: 2, reserved: 0, available: 2 }],
    seo: { slugSeam: "shirt", seoTitleSeam: "Shirt", semanticNote: "Semantic Content != Page Composition" },
    publication: { catalogStatus: "Published", purchasableHint: true, checks: [] },
    activity: [],
    audit: [],
    permissions: { canView: true, canEditCatalog: true, canEditCommercial: true, canEditInventory: true, canPublish: false },
    catalogUpdatedAt: "2026-08-24T00:00:00Z",
    readinessWarnings: [],
    unsupportedMutations: ["media-binary-upload"],
  });
  assert.ok(view);
  assert.equal(view?.prices[0]?.offerId, "o1");
  assert.equal(view?.stock[0]?.offerId, "o1");
  assert.equal(view?.permissions.canPublish, false);
  assert.equal(view?.media[0]?.mediaAssetId, "m1");
  assert.equal(view?.media[0]?.displayOrder, 0);
  assert.equal(view?.media[0]?.altText, "front");
  assert.equal(view?.variants[0]?.catalogCodeSeam, "SHIRT-M");
  assert.equal(view?.primaryCategoryId, "cat-1");
  assert.equal(view?.categoryPath, "wear › shirts");
  assert.equal(view?.slug, "shirt");
  assert.equal(view?.shortDescription, "Cotton shirt");
  assert.equal(view?.translations?.[0]?.locale, "fa-IR");
  assert.equal(view?.translations?.[0]?.name, "پیراهن");
});
