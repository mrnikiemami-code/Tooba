/**
 * TB-296 — real mounted Offer workspace behavior against a fake adapter.
 * These tests exercise the shared admin/seller seven-tab workspace UI; no HTTP is used.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  ensureDom,
  mount,
  q,
  qq,
  setInputValue,
  waitFor,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { OfferWorkspace } from "../../../lib/offer-workspace/offer-workspace.tsx";
import { normalizeDigits } from "../../../lib/offer-workspace/model.ts";
import { offerWorkspaceMessages } from "../../../lib/offer-workspace/messages.ts";
import type {
  OfferWorkspaceAdapter,
  OfferWorkspaceDetail,
} from "../../../lib/offer-workspace/types.ts";

ensureDom();

function baseDetail(over: Partial<OfferWorkspaceDetail> = {}): OfferWorkspaceDetail {
  return {
    offerId: "offer-1",
    sellerPartyId: "seller-1",
    sellerDisplayName: "فروشگاه نیلوا",
    catalogVariantId: "variant-1",
    productId: "product-1",
    productTitle: "نایک شلوار مردانه",
    brandName: "Nike",
    sellerSku: "SKU-1",
    status: "Active",
    channel: "Marketplace",
    amount: 1000,
    currency: "IRR",
    onHand: 10,
    reserved: 2,
    availableUnits: 8,
    catalogReadOnly: true,
    returnPolicyChoice: "Default",
    customReturnWindowDays: null,
    defaultReturnWindowDays: 7,
    sellerCanOverrideReturnPolicy: true,
    minReturnWindowDays: 1,
    maxReturnWindowDays: 30,
    allowNonReturnableOffers: true,
    minimumOrderQuantity: 1,
    maximumOrderQuantity: 5,
    productUnitCode: "pcs",
    productUnitName: "عدد",
    productUnitShortName: "عدد",
    inventoryLocations: [
      { stockItemId: "stock-a", locationId: "loc-a", locationCode: "WH-A", onHand: 10, reserved: 2, available: 8 },
      { stockItemId: "stock-b", locationId: "loc-b", locationCode: "WH-B", onHand: 4, reserved: 0, available: 4 },
    ],
    ...over,
  };
}

interface Calls {
  commercial: number;
  price: number;
  inventory: number;
  fulfillment: number;
  created: number;
  createdId: string | null;
  cancelled: number;
}

function editAdapter(over: Partial<OfferWorkspaceAdapter> = {}, detail = baseDetail()): {
  adapter: OfferWorkspaceAdapter;
  calls: Calls;
  current: () => OfferWorkspaceDetail;
} {
  const calls: Calls = { commercial: 0, price: 0, inventory: 0, fulfillment: 0, created: 0, createdId: null, cancelled: 0 };
  let current = detail;
  const adapter: OfferWorkspaceAdapter = {
    role: "admin",
    mode: "edit",
    offerId: detail.offerId,
    loadDetail: async () => ({ ok: true, detail: current }),
    searchProducts: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
    loadProductVariants: async () => [],
    createDraft: async () => ({ ok: false, errorCode: "edit" }),
    saveCommercial: async (patch) => {
      calls.commercial += 1;
      current = { ...current, ...patchToDetail(patch) };
      return { ok: true, detail: current };
    },
    savePrice: async (input) => {
      calls.price += 1;
      current = { ...current, amount: input.amount, currency: input.currency };
      return { ok: true, detail: current };
    },
    saveInventory: async (input) => {
      calls.inventory += 1;
      current = {
        ...current,
        inventoryLocations: current.inventoryLocations.map((row) =>
          row.stockItemId === input.stockItemId
            ? { ...row, onHand: input.onHand, available: input.onHand - row.reserved }
            : row,
        ),
      };
      current = {
        ...current,
        onHand: current.inventoryLocations.reduce((sum, row) => sum + row.onHand, 0),
        availableUnits: current.inventoryLocations.reduce((sum, row) => sum + row.available, 0),
      };
      return { ok: true, detail: current };
    },
    saveFulfillment: async (input) => { calls.fulfillment += 1; current = { ...current, fulfillmentResponsibility: input.responsibility, fulfillmentShippingTypeId: input.shippingTypeId }; return { ok: true }; },
    loadShippingTypes: async () => [
      { shippingTypeId: "type-standard", code: "standard", label: "عادی", isActive: true },
      { shippingTypeId: "type-heavy", code: "heavy", label: "سنگین", isActive: false },
    ],
    onCreated: (offerId) => {
      calls.created += 1;
      calls.createdId = offerId;
    },
    onCancel: () => {
      calls.cancelled += 1;
    },
    ...over,
  };
  return { adapter, calls, current: () => current };
}

function patchToDetail(patch: {
  sellerSku: string | null;
  status?: string | null;
  returnPolicyChoice?: string | null;
  customReturnWindowDays?: number | null;
  minimumOrderQuantity?: number | null;
  maximumOrderQuantity?: number | null;
}): Partial<OfferWorkspaceDetail> {
  return {
    sellerSku: patch.sellerSku,
    ...(patch.status != null ? { status: patch.status } : {}),
    ...(patch.returnPolicyChoice != null ? { returnPolicyChoice: patch.returnPolicyChoice } : {}),
    ...(patch.customReturnWindowDays !== undefined
      ? { customReturnWindowDays: patch.customReturnWindowDays }
      : {}),
    ...(patch.minimumOrderQuantity !== undefined
      ? { minimumOrderQuantity: patch.minimumOrderQuantity }
      : {}),
    ...(patch.maximumOrderQuantity !== undefined
      ? { maximumOrderQuantity: patch.maximumOrderQuantity }
      : {}),
  };
}

async function mountEdit(over: Partial<OfferWorkspaceAdapter> = {}, detail = baseDetail()) {
  const harness = editAdapter(over, detail);
  const tree = await mount(createElement(OfferWorkspace, { adapter: harness.adapter }));
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-workspace]")));
  return { ...harness, ...tree };
}

test("edit workspace renders the seven tabs in order and only one panel at a time", async () => {
  const tree = await mountEdit();
  const labels = qq(tree.container, "[role=tab]").map((tab) => (tab.textContent ?? "").replace(/[۰-۹]/g, "").trim());
  assert.deepEqual(labels, ["مشخصات آفر", "قیمت‌گذاری", "تخفیفات", "موجودی", "ارسال", "شرایط فروش", "تاریخچه"]);
  assert.ok(tree.container.querySelector("[data-testid=offer-tab-panel-identity]"));
  assert.equal(tree.container.querySelector("[data-testid=offer-tab-panel-pricing]"), null);

  await import("../../../features/order-carrier-execution/test-dom-harness.ts").then(({ click }) =>
    click(q(tree.container, "[data-testid=offer-tab-button-pricing]")),
  );
  assert.ok(tree.container.querySelector("[data-testid=offer-tab-panel-pricing]"));
  assert.equal(tree.container.querySelector("[data-testid=offer-tab-panel-identity]"), null);
  await tree.unmount();
});

test("edit identity is locked but seller SKU stays editable and dirty", async () => {
  const tree = await mountEdit();
  assert.equal(tree.container.querySelector("[data-testid=offer-seller-picker]"), null);
  assert.equal(tree.container.querySelector("[data-testid=offer-variant-picker]"), null);
  assert.match(q(tree.container, "[data-testid=offer-workspace-seller]").textContent ?? "", /نیلوا/);
  await setInputValue(q(tree.container, "[data-testid=offer-seller-sku]"), "SKU-NEW");
  assert.ok(tree.container.querySelector("[data-testid=offer-workspace-dirty]"));
  await tree.unmount();
});

test("IRR price with decimals is invalid and never calls the price writer", async () => {
  const tree = await mountEdit();
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  await setInputValue(q(tree.container, "[data-testid=offer-price-input]"), "1.5");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.price, 0);
  assert.match(q(tree.container, "[data-testid=offer-save-error]").textContent ?? "", /اعشار/);
  await tree.unmount();
});

test("valid price save persists and clears the pricing dirty flag", async () => {
  const tree = await mountEdit();
  const { click, setInputValue: set } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  await set(q(tree.container, "[data-testid=offer-price-input]"), "1500");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.price, 1);
  assert.equal(tree.current().amount, 1500);
  await waitFor(() => !tree.container.querySelector("[data-testid=offer-workspace-dirty]"));
  await tree.unmount();
});

test("inventory below reserved blocks save; valid adjust requires a reason and then persists", async () => {
  const tree = await mountEdit();
  const { click, setInputValue: set } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-inventory]"));
  const input = qq(tree.container, "[data-testid=offer-inventory-input]")[0]!;
  await set(input, "1");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.inventory, 0);
  assert.match(q(tree.container, "[data-testid=offer-save-error]").textContent ?? "", /رزرو/);

  await set(input, "12");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.inventory, 0);
  assert.match(q(tree.container, "[data-testid=offer-save-error]").textContent ?? "", /دلیل/);

  await set(q(tree.container, "[data-testid=offer-inventory-reason]"), "شمارش انبار");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.inventory, 1);
  assert.equal(tree.current().inventoryLocations[0]?.onHand, 12);
  await tree.unmount();
});

test("return policy hides the custom window unless Custom is selected", async () => {
  const tree = await mountEdit();
  const { click, setSelectValue: select } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-conditions]"));
  assert.equal(tree.container.querySelector("[data-testid=offer-custom-return-days]"), null);
  await select(q(tree.container, "[data-testid=offer-return-policy]"), "Custom");
  assert.ok(tree.container.querySelector("[data-testid=offer-custom-return-days]"));
  await select(q(tree.container, "[data-testid=offer-return-policy]"), "NonReturnable");
  assert.equal(tree.container.querySelector("[data-testid=offer-custom-return-days]"), null);
  await tree.unmount();
});

test("archived lifecycle is locked and buyability is derived", async () => {
  const tree = await mountEdit({}, baseDetail({ status: "Archived" }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-conditions]"));
  assert.equal((q(tree.container, "[data-testid=offer-lifecycle]") as HTMLSelectElement).disabled, true);
  assert.match(q(tree.container, "[data-testid=offer-tab-conditions]").textContent ?? "", /بایگانی‌شده/);
  await tree.unmount();
});

test("admin shipping gates the type until responsibility is chosen and saves the real contract", async () => {
  const tree = await mountEdit();
  const { click, setSelectValue: select } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-shipping]"));
  const type = q(tree.container, "[data-testid=offer-shipping-type]") as HTMLSelectElement;
  assert.equal(type.disabled, true);
  assert.equal(tree.container.querySelector("[data-testid=offer-shipping-service]"), null);
  await select(q(tree.container, "[data-testid=offer-shipping-responsibility]"), "Seller");
  assert.equal(type.disabled, false);
  await select(type, "type-standard");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.fulfillment, 1); await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-shipping-notice]")));
  await tree.unmount();
});

test("discounts and history stay explicitly gated with no fabricated data", async () => {
  const tree = await mountEdit();
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-discounts]"));
  const discounts = q(tree.container, "[data-testid=offer-tab-discounts]").textContent ?? "";
  assert.match(discounts, /تخفیفی برای این آفر ثبت نشده/);
  assert.match(discounts, /هنوز فعال نیست/);
  await click(q(tree.container, "[data-testid=offer-tab-button-history]"));
  const history = q(tree.container, "[data-testid=offer-tab-history]").textContent ?? "";
  assert.match(history, /تاریخچه‌ای برای این آفر در دسترس نیست/);
  assert.equal(tree.container.querySelectorAll("[data-testid=offer-tab-history] li").length, 0);
  await tree.unmount();
});

test("denied detail renders the denied state instead of a fake workspace", async () => {
  const harness = editAdapter({
    loadDetail: async () => ({ ok: false, errorCode: "seller.offer.missing", denied: true }),
  });
  const tree = await mount(createElement(OfferWorkspace, { adapter: harness.adapter }));
  await waitFor(() => /دسترسی مجاز نیست/.test(tree.container.textContent ?? ""));
  assert.match(tree.container.textContent ?? "", /دسترسی مجاز نیست/);
  assert.equal(tree.container.querySelector("[data-testid=offer-workspace]"), null);
  await tree.unmount();
});

test("seller create mode disables dependent tabs, does not auto-select, and creates once", async () => {
  const calls: Calls = { commercial: 0, price: 0, inventory: 0, fulfillment: 0, created: 0, createdId: null, cancelled: 0 };
  const adapter: OfferWorkspaceAdapter = {
    role: "seller",
    mode: "create",
    fixedSeller: { sellerPartyId: "seller-1", displayName: "" },
    loadChannels: async () => ["Marketplace"],
    loadInventoryLocations: async () => [],
    searchProducts: async () => ({
      rows: [{ productId: "product-1", title: "کالای اول", brandName: "برند", primaryMediaAssetId: null, publishedVariantCount: 1 }],
      totalCount: 1,
      page: 1,
      pageSize: 10,
      hasMore: false,
    }),
    loadProductVariants: async () => ([
      { catalogVariantId: "variant-1", productId: "product-1", productTitle: "کالای اول", displayLabel: "رنگ: آبی · سایز: ۳۲", axisLabels: [{ name: "رنگ", value: "آبی" }, { name: "سایز", value: "۳۲" }], catalogCode: "C1", status: "Published" },
    ]),
    createDraft: async (input) => {
      calls.created += 1;
      calls.createdId = input.catalogVariantId;
      return { ok: true, offerId: "offer-new" };
    },
    saveCommercial: async () => ({ ok: false, errorCode: "create" }),
    savePrice: async () => ({ ok: false, errorCode: "create" }),
    saveInventory: async () => ({ ok: false, errorCode: "create" }),
    onCreated: (offerId) => {
      calls.createdId = offerId;
    },
    onCancel: () => {
      calls.cancelled += 1;
    },
  };
  window.sessionStorage.clear();
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  const pricingTab = q(tree.container, "[data-testid=offer-tab-button-pricing]") as HTMLButtonElement;
  assert.equal(pricingTab.disabled, true);
  assert.match(q(tree.container, "[data-testid=offer-variant-picker-toggle]").textContent ?? "", /محصول/);

  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 1);
  await click(q(tree.container, "[data-testid=offer-product-dialog-option]"));
  await waitFor(() => /رنگ: آبی/.test(tree.container.textContent ?? ""));
  await click(q(tree.container, "[data-testid=seller-create-submit]"));
  assert.equal(calls.created, 1);
  assert.equal(calls.createdId, "offer-new");
  await tree.unmount();
});

test("tab switches preserve typed draft values (UX-03)", async () => {
  const tree = await mountEdit();
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  await setInputValue(q(tree.container, "[data-testid=offer-price-input]"), "1500");
  await click(q(tree.container, "[data-testid=offer-tab-button-conditions]"));
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  const failedValue = (q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement).value;
  assert.equal(normalizeDigits(failedValue).replace(/[٬,]/g, ""), "1500");
  assert.ok(tree.container.querySelector("[data-testid=offer-workspace-dirty]"));
  await tree.unmount();
});

test("picker selection survives paging in both directions (UX-29)", async () => {
  const adapter: OfferWorkspaceAdapter = {
    role: "admin",
    mode: "create",
    loadChannels: async () => ["Marketplace"],
    loadInventoryLocations: async () => [],
    searchSellers: async (_search, page) => ({
      rows: page === 1
        ? [{ sellerPartyId: "seller-a", displayName: "فروشنده الف" }]
        : [{ sellerPartyId: "seller-b", displayName: "فروشنده ب" }],
      totalCount: 2,
      page,
      pageSize: 1,
      hasMore: page === 1,
    }),
    searchProducts: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
    loadProductVariants: async () => [],
    createDraft: async () => ({ ok: false, errorCode: "unused" }),
    saveCommercial: async () => ({ ok: false, errorCode: "create" }),
    savePrice: async () => ({ ok: false, errorCode: "create" }),
    saveInventory: async () => ({ ok: false, errorCode: "create" }),
    onCreated: () => undefined,
    onCancel: () => undefined,
  };
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-seller-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-seller-dialog-option]").length === 1);
  await click(qq(tree.container, "[data-testid=offer-seller-dialog-option]")[0]!); await click(q(tree.container, "[data-testid=offer-seller-picker-toggle]")); await waitFor(() => qq(tree.container, "[data-testid=offer-seller-dialog-option]").length === 1); await click(q(tree.container, "[data-testid=offer-seller-dialog-next]"));
  await waitFor(() => (qq(tree.container, "[data-testid=offer-seller-dialog-option]")[0]?.textContent ?? "").includes("ب"));
  await click(q(tree.container, "[data-testid=offer-seller-dialog-prev]"));
  await waitFor(() => (qq(tree.container, "[data-testid=offer-seller-dialog-option]")[0]?.textContent ?? "").includes("الف"));
  assert.equal(qq(tree.container, "[data-testid=offer-seller-dialog-option]")[0]?.getAttribute("aria-pressed"), "true");
  await tree.unmount();
});

test("a failed save keeps the typed data and surfaces the error (UX-22)", async () => {
  const tree = await mountEdit({
    savePrice: async () => ({ ok: false, errorCode: "platform.unexpected" }),
  });
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  await setInputValue(q(tree.container, "[data-testid=offer-price-input]"), "1500");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.ok(tree.container.querySelector("[data-testid=offer-message-error]"));
  const failedValue = (q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement).value;
  assert.equal(normalizeDigits(failedValue).replace(/[٬,]/g, ""), "1500");
  assert.ok(tree.container.querySelector("[data-testid=offer-workspace]"));
  await tree.unmount();
});

test("unsaved changes arm the beforeunload guard (UX-23)", async () => {
  const tree = await mountEdit();
  await setInputValue(q(tree.container, "[data-testid=offer-seller-sku]"), "SKU-UNSAVED");
  const event = new window.Event("beforeunload", { cancelable: true });
  window.dispatchEvent(event);
  assert.equal(event.defaultPrevented, true);
  await tree.unmount();
});
function createAdapter(over: Partial<OfferWorkspaceAdapter> = {}): OfferWorkspaceAdapter {
  return {
    role: "admin",
    mode: "create",
    loadChannels: async () => ["Marketplace"],
    loadInventoryLocations: async () => [],
    searchSellers: async () => ({ rows: [{ sellerPartyId: "seller-1", displayName: "فروشنده" }], totalCount: 1, page: 1, pageSize: 10, hasMore: false }),
    searchProducts: async () => ({
      rows: [
        { productId: "p-1", title: "محصول چندتنوع", brandName: "برند", primaryMediaAssetId: null, publishedVariantCount: 2 },
        { productId: "p-2", title: "محصول تک‌تنوع", brandName: null, primaryMediaAssetId: null, publishedVariantCount: 1 },
        { productId: "p-3", title: "محصول بدون تنوع", brandName: null, primaryMediaAssetId: null, publishedVariantCount: 0 },
      ],
      totalCount: 3,
      page: 1,
      pageSize: 10,
      hasMore: false,
    }),
    loadProductVariants: async (productId) => productId === "p-1"
      ? [
          { catalogVariantId: "v-1a", productId: "p-1", productTitle: "محصول چندتنوع", displayLabel: "رنگ: آبی · سایز: ۳۲", axisLabels: [{ name: "رنگ", value: "آبی" }, { name: "سایز", value: "۳۲" }], catalogCode: "C-1A", status: "Published" },
          { catalogVariantId: "v-1b", productId: "p-1", productTitle: "محصول چندتنوع", displayLabel: "رنگ: مشکی · سایز: ۳۴", axisLabels: [{ name: "رنگ", value: "مشکی" }, { name: "سایز", value: "۳۴" }], catalogCode: "C-1B", status: "Published" },
        ]
      : productId === "p-2"
        ? [{ catalogVariantId: "v-2a", productId: "p-2", productTitle: "محصول تک‌تنوع", displayLabel: "ظرفیت: ۱ لیتر", axisLabels: [{ name: "ظرفیت", value: "۱ لیتر" }], catalogCode: "C-2A", status: "Published" }]
        : [],
    createDraft: async () => ({ ok: false, errorCode: "unused" }),
    saveCommercial: async () => ({ ok: false, errorCode: "create" }),
    savePrice: async () => ({ ok: false, errorCode: "create" }),
    saveInventory: async () => ({ ok: false, errorCode: "create" }),
    onCreated: () => undefined,
    onCancel: () => undefined,
    ...over,
  };
}

test("product and variant are independent: product change clears the old variant", async () => {
  window.sessionStorage.clear();
  const tree = await mount(createElement(OfferWorkspace, { adapter: createAdapter() }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[0]!);
  await waitFor(() => /محصول چندتنوع/.test(tree.container.textContent ?? ""));
  await click(q(tree.container, "[data-testid=offer-variant-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-variant-option]").length === 2);
  assert.match(q(tree.container, "[data-testid=offer-variant-dialog]").textContent ?? "", /رنگ: آبی/);
  assert.match(q(tree.container, "[data-testid=offer-variant-dialog]").textContent ?? "", /سایز: ۳۲/);
  await click(qq(tree.container, "[data-testid=offer-variant-option]")[0]!);
  await waitFor(() => /رنگ: آبی/.test(tree.container.textContent ?? ""));

  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[1]!);
  await waitFor(() => /ظرفیت: ۱ لیتر/.test(tree.container.textContent ?? ""));
  assert.match(tree.container.textContent ?? "", /یک تنوع قابل عرضه دارد/);
  assert.doesNotMatch(q(tree.container, "[data-testid=offer-variant-picker-toggle]").textContent ?? "", /آبی/);
  await tree.unmount();
});

test("a product without sellable variants shows a clear message", async () => {
  window.sessionStorage.clear();
  const tree = await mount(createElement(OfferWorkspace, { adapter: createAdapter() }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[2]!);
  await waitFor(() => /این محصول تنوع قابل عرضه ندارد/.test(tree.container.textContent ?? ""));
  await click(q(tree.container, "[data-testid=offer-variant-picker-toggle]"));
  await waitFor(() => /این محصول تنوع قابل عرضه ندارد/.test(q(tree.container, "[data-testid=offer-variant-dialog]").textContent ?? ""));
  await tree.unmount();
});

test("existing active listing is explained and linked before create", async () => {
  window.sessionStorage.clear();
  const adapter = createAdapter({ findActiveOffer: async () => ({ offerId: "existing-offer" }) });
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-seller-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-seller-dialog-option]").length === 1);
  await click(q(tree.container, "[data-testid=offer-seller-dialog-option]"));
  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[1]!);
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-message-duplicate]")));
  const link = q(tree.container, "[data-testid=offer-message-duplicate] a") as HTMLAnchorElement;
  assert.equal(link.getAttribute("href"), "/admin/offers/existing-offer");
  assert.equal((q(tree.container, "[data-testid=admin-offer-submit]") as HTMLButtonElement).disabled, true);
  await tree.unmount();
});

test("formatted amount input normalizes Persian digits and never turns empty into zero", async () => {
  const tree = await mountEdit();
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  const input = q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement;
  await setInputValue(input, "۱۲۳۴۵۶۷");
  assert.equal(normalizeDigits(input.value).replace(/[٬,]/g, ""), "1234567");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.price, 1);
  assert.equal(tree.current().amount, 1234567);

  await setInputValue(input, "");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.equal(tree.calls.price, 1);
  assert.match(q(tree.container, "[data-testid=offer-save-error]").textContent ?? "", /مبلغ را وارد کنید/);
  await tree.unmount();
});

test("partial inventory save keeps the failed location draft and retries only the remainder", async () => {
  const harness = editAdapter();
  let failSecond = true;
  const calls: string[] = [];
  const original = harness.adapter.saveInventory;
  harness.adapter.saveInventory = async (input) => {
    calls.push(input.stockItemId);
    if (input.stockItemId === "stock-b" && failSecond) {
      failSecond = false;
      return { ok: false, errorCode: "platform.unexpected" };
    }
    const result = await original(input);
    return result;
  };
  window.sessionStorage.clear();
  const tree = await mount(createElement(OfferWorkspace, { adapter: harness.adapter }));
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-workspace]")));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-inventory]"));
  const rows = qq(tree.container, "[data-testid=offer-inventory-row]");
  const inputA = q(rows[0]!, "[data-testid=offer-inventory-input]");
  const inputB = q(rows[1]!, "[data-testid=offer-inventory-input]");
  await setInputValue(inputA, "12");
  await setInputValue(inputB, "9");
  await setInputValue(q(tree.container, "[data-testid=offer-inventory-reason]"), "شمارش");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.deepEqual(calls, ["stock-a", "stock-b"]);
  assert.match(tree.container.textContent ?? "", /بخشی از تغییرات ذخیره شد/);
  const freshRows = qq(tree.container, "[data-testid=offer-inventory-row]");
  assert.equal((q(freshRows[1]!, "[data-testid=offer-inventory-input]") as HTMLInputElement).value, "9");
  assert.equal((q(tree.container, "[data-testid=offer-inventory-reason]") as HTMLTextAreaElement).value, "شمارش");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.deepEqual(calls, ["stock-a", "stock-b", "stock-b"]);
  assert.equal(harness.current().inventoryLocations.find((row) => row.stockItemId === "stock-b")?.onHand, 9);
  await tree.unmount();
});

test("a network save is uncertain: draft kept, no retry button, reload action offered", async () => {
  const tree = await mountEdit({ savePrice: async () => ({ ok: false, errorCode: "host-unreachable" }) });
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-pricing]"));
  await setInputValue(q(tree.container, "[data-testid=offer-price-input]"), "1500");
  await click(q(tree.container, "[data-testid=admin-offer-save]"));
  assert.match(q(tree.container, "[data-testid=offer-message-reload]").textContent ?? "", /نتیجه ذخیره قطعی نیست/);
  assert.equal(tree.container.querySelector("[data-testid=offer-save-retry]"), null);
  assert.ok(tree.container.querySelector("[data-testid=offer-message-reload]"));
  assert.equal(normalizeDigits((q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement).value).replace(/[٬,]/g, ""), "1500");
  await tree.unmount();
});

test("language navigation restores unsaved selections and drafts", async () => {
  window.sessionStorage.clear();
  const first = await mountEdit();
  await setInputValue(q(first.container, "[data-testid=offer-seller-sku]"), "SKU-LANG");
  const firstDirty = first.container.querySelector("[data-testid=offer-workspace-dirty]");
  assert.ok(firstDirty);
  await first.unmount();

  const second = await mount(createElement(OfferWorkspace, { adapter: first.adapter }));
  await waitFor(() => /SKU-LANG/.test((q(second.container, "[data-testid=offer-seller-sku]") as HTMLInputElement).value));
  assert.equal((q(second.container, "[data-testid=offer-seller-sku]") as HTMLInputElement).value, "SKU-LANG");
  await second.unmount();
});

test("an isolated third-language definition drives direction and copy without touching the live registry", async () => {
  window.sessionStorage.clear();
  const harness = editAdapter();
  const tree = await mount(createElement(OfferWorkspace, {
    adapter: harness.adapter,
    localeDefinition: { code: "tr-TR", culture: "tr-TR", language: "tr", direction: "ltr", calendar: "gregorian" },
    messages: { titleEdit: "Teklif", tabs: { ...offerWorkspaceMessages.fa.tabs, identity: "Kimlik" } },
  }));
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-workspace]")));
  const root = q(tree.container, "[data-testid=offer-workspace]");
  assert.equal(root.getAttribute("dir"), "ltr");
  assert.equal(root.getAttribute("lang"), "tr-TR");
  assert.match(tree.container.textContent ?? "", /Kimlik/);
  await tree.unmount();
});

test("rapid double submit creates exactly one draft", async () => {
  window.sessionStorage.clear();
  let created = 0;
  const adapter = createAdapter({
    createDraft: async () => {
      created += 1;
      await new Promise((resolve) => setTimeout(resolve, 80));
      return { ok: true, offerId: "offer-once" };
    },
  });
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-seller-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-seller-dialog-option]").length === 1);
  await click(q(tree.container, "[data-testid=offer-seller-dialog-option]"));
  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[1]!);
  await waitFor(() => /ظرفیت:/.test(tree.container.textContent ?? ""));
  const submit = q(tree.container, "[data-testid=admin-offer-submit]");
  await click(submit);
  await click(submit);
  await waitFor(() => created > 0);
  assert.equal(created, 1);
  await tree.unmount();
});

test("shipping selection survives a language/reload remount with scoped recovery", async () => {
  window.sessionStorage.clear();
  const first = await mountEdit();
  const { click, setSelectValue: select } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(first.container, "[data-testid=offer-tab-button-shipping]"));
  await select(q(first.container, "[data-testid=offer-shipping-responsibility]"), "Seller");
  await waitFor(() => !(q(first.container, "[data-testid=offer-shipping-type]") as HTMLSelectElement).disabled);
  await select(q(first.container, "[data-testid=offer-shipping-type]"), "type-standard");
  await first.unmount();

  const second = await mount(createElement(OfferWorkspace, { adapter: first.adapter }));
  await waitFor(() => Boolean(second.container.querySelector("[data-testid=offer-workspace]")));
  await click(q(second.container, "[data-testid=offer-tab-button-shipping]"));
  await waitFor(() => Boolean(second.container.querySelector("[data-testid=offer-shipping-responsibility]")));
  assert.equal((q(second.container, "[data-testid=offer-shipping-responsibility]") as HTMLSelectElement).value, "Seller");
  assert.equal((q(second.container, "[data-testid=offer-shipping-type]") as HTMLSelectElement).value, "type-standard");
  await second.unmount();
});

test("partial create keeps the created Draft recoverable through MessageBox", async () => {
  window.sessionStorage.clear();
  let navigated = "";
  const adapter = createAdapter({
    createAndSaveDraft: async () => ({ ok: false, errorCode: "host-unreachable", createdOfferId: "offer-partial", partial: true }),
    onCreated: (offerId) => { navigated = offerId; },
  });
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-seller-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-seller-dialog-option]").length === 1);
  await click(q(tree.container, "[data-testid=offer-seller-dialog-option]"));
  await click(q(tree.container, "[data-testid=offer-product-picker-toggle]"));
  await waitFor(() => qq(tree.container, "[data-testid=offer-product-dialog-option]").length === 3);
  await click(qq(tree.container, "[data-testid=offer-product-dialog-option]")[1]!);
  await waitFor(() => /ظرفیت:/.test(tree.container.textContent ?? ""));
  await click(q(tree.container, "[data-testid=admin-offer-submit]"));
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-message-error]")));
  const link = q(tree.container, "[data-testid=offer-message-error] a") as HTMLAnchorElement;
  assert.equal(link.getAttribute("href"), "/admin/offers/offer-partial");
  assert.equal(navigated, "");
  await tree.unmount();
});
