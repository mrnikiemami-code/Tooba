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
    searchVariants: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
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
    saveFulfillment: async () => {
      calls.fulfillment += 1;
      return { ok: true };
    },
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
  const labels = qq(tree.container, "[role=tab]").map((tab) => tab.textContent?.trim());
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
  const service = q(tree.container, "[data-testid=offer-shipping-service]") as HTMLInputElement;
  assert.equal(type.disabled, true);
  assert.equal(service.disabled, true);
  await select(q(tree.container, "[data-testid=offer-shipping-responsibility]"), "Seller");
  assert.equal(type.disabled, false);
  assert.equal(service.disabled, false);
  await select(type, "type-standard");
  await click(q(tree.container, "[data-testid=offer-shipping-save]"));
  assert.equal(tree.calls.fulfillment, 1);
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-shipping-notice]")));
  await tree.unmount();
});

test("discounts and history stay explicitly gated with no fabricated data", async () => {
  const tree = await mountEdit();
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-tab-button-discounts]"));
  const discounts = q(tree.container, "[data-testid=offer-tab-discounts]").textContent ?? "";
  assert.match(discounts, /Promotion/);
  assert.match(discounts, /ساختگی/);
  await click(q(tree.container, "[data-testid=offer-tab-button-history]"));
  const history = q(tree.container, "[data-testid=offer-tab-history]").textContent ?? "";
  assert.match(history, /قرارداد/);
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
    searchVariants: async () => ({
      rows: [
        { catalogVariantId: "variant-1", productId: "product-1", productTitle: "کالای اول", catalogCode: "C1", productStatus: "Published" },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
      hasMore: false,
    }),
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
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  await waitFor(() => Boolean(tree.container.querySelector("[data-testid=offer-variant-picker]")));
  await waitFor(() => qq(tree.container, "[data-testid=offer-variant-option]").length === 1);

  const pricingTab = q(tree.container, "[data-testid=offer-tab-button-pricing]") as HTMLButtonElement;
  assert.equal(pricingTab.disabled, true);
  assert.equal(qq(tree.container, "[data-testid=offer-variant-option]").some((el) => el.getAttribute("aria-pressed") === "true"), false);

  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(q(tree.container, "[data-testid=offer-variant-option]"));
  assert.equal(qq(tree.container, "[data-testid=offer-variant-option]")[0]?.getAttribute("aria-pressed"), "true");
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
  assert.equal((q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement).value, "1500");
  assert.ok(tree.container.querySelector("[data-testid=offer-workspace-dirty]"));
  await tree.unmount();
});

test("picker selection survives paging in both directions (UX-29)", async () => {
  const adapter: OfferWorkspaceAdapter = {
    role: "admin",
    mode: "create",
    searchSellers: async (_search, page) => ({
      rows: page === 1
        ? [{ sellerPartyId: "seller-a", displayName: "فروشنده الف" }]
        : [{ sellerPartyId: "seller-b", displayName: "فروشنده ب" }],
      totalCount: 2,
      page,
      pageSize: 1,
      hasMore: page === 1,
    }),
    searchVariants: async () => ({ rows: [], totalCount: 0, page: 1, pageSize: 10, hasMore: false }),
    createDraft: async () => ({ ok: false, errorCode: "unused" }),
    saveCommercial: async () => ({ ok: false, errorCode: "create" }),
    savePrice: async () => ({ ok: false, errorCode: "create" }),
    saveInventory: async () => ({ ok: false, errorCode: "create" }),
    onCreated: () => undefined,
    onCancel: () => undefined,
  };
  const tree = await mount(createElement(OfferWorkspace, { adapter }));
  await waitFor(() => qq(tree.container, "[data-testid=offer-seller-option]").length === 1);
  const { click } = await import("../../../features/order-carrier-execution/test-dom-harness.ts");
  await click(qq(tree.container, "[data-testid=offer-seller-option]")[0]!);
  await click(q(tree.container, "[data-testid=offer-picker-next]"));
  await waitFor(() => (qq(tree.container, "[data-testid=offer-seller-option]")[0]?.textContent ?? "").includes("ب"));
  await click(q(tree.container, "[data-testid=offer-picker-prev]"));
  await waitFor(() => (qq(tree.container, "[data-testid=offer-seller-option]")[0]?.textContent ?? "").includes("الف"));
  assert.equal(qq(tree.container, "[data-testid=offer-seller-option]")[0]?.getAttribute("aria-pressed"), "true");
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
  assert.ok(tree.container.querySelector("[data-testid=offer-save-error]"));
  assert.equal((q(tree.container, "[data-testid=offer-price-input]") as HTMLInputElement).value, "1500");
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