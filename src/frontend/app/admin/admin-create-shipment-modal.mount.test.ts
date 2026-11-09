/**
 * TB-252-R2 — mount AdminCreateShipmentModal: compact copy, Jalali date, one inline error, same-tick single request.
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-create-shipment-modal.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement, useState } from "react";
import test from "node:test";
import {
  click,
  clickBurst,
  flush,
  mount,
  q,
  qq,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import { AdminCreateShipmentModal } from "./admin-create-shipment-modal.tsx";
import {
  AdminAssignShipmentTrackingModal,
  suggestShipmentTracking,
} from "./admin-assign-shipment-tracking-modal.tsx";
import type {
  AdminAcceptedShippingGroup,
  AdminOrderDetail,
  AdminOrderLine,
  AdminSellerOrder,
} from "./admin-api.ts";
import type { AdminOrderOperationRequest } from "./admin-order-operations.ts";

const CHECKOUT = "chk-252r2";
const SELLER = "so-252r2";
const FULFILLMENT = "ff-252r2";
const GROUP_A = "grp-252r2-a";
const GROUP_B = "grp-252r2-b";
const LINE_A = "line-252r2-a";
const LINE_B = "line-252r2-b";

function line(id: string, title: string, packed: number): AdminOrderLine {
  return {
    id,
    orderLineId: id,
    title,
    sellerDisplayName: "فروشگاه آرمان",
    quantity: packed,
    unitAmount: 1000,
    linePayable: packed * 1000,
    currency: "IRR",
    quantityShipped: 0,
    quantityPacked: packed,
    quantityProcessing: packed,
    quantityAllocated: 0,
    imageUrl: null,
    operationalStatus: "Packed",
    isReturnable: null,
    returnWindowDays: null,
    returnPolicyLabel: null,
    returnDeadlineDisplay: null,
    returnRemainingDisplay: null,
    returnStatusCode: null,
  };
}

function seller(lines: AdminOrderLine[]): AdminSellerOrder {
  return {
    id: SELLER,
    orderNumber: "TB-252R2",
    sellerDisplayName: "فروشگاه آرمان",
    status: "Paid",
    paymentState: "Paid",
    payableAmount: 1000,
    currency: "IRR",
    lines,
    fulfillmentId: FULFILLMENT,
    fulfillmentStatus: "Packed",
    shipments: [],
  };
}

function group(
  id: string,
  lineIds: string[],
  opts: { transportMode?: string; eligible?: boolean; blockFa?: string | null; date?: string } = {},
): AdminAcceptedShippingGroup {
  return {
    shippingGroupId: id,
    sellerOrderId: SELLER,
    sellerPartyId: "seller-party-1",
    methodCode: "post",
    methodLabel: "پیشتاز",
    serviceCode: null,
    optionCode: "standard",
    shippingChargeSnapshot: 125000,
    currency: "IRR",
    deliveryDate: opts.date ?? "2026-11-05",
    deliveryTimeWindow: "09:00–10:00",
    deliveryTimeZone: "Asia/Tehran",
    ownerKind: "store",
    ownerTenantKey: "store-alpha",
    ownerSellerPartyId: null,
    orderLineIds: lineIds,
    createParcelEligible: opts.eligible ?? true,
    createParcelBlockReasonFa: opts.blockFa ?? null,
    payableAtCheckoutAmount: 125000,
    carrierCollectAmount: 0,
    transportMode: opts.transportMode ?? "Manual",
  };
}

function detail(): AdminOrderDetail {
  return {
    checkoutId: CHECKOUT,
    reference: "ref-252r2",
    createdAt: "2026-11-04T10:00:00Z",
    status: "Paid",
    paymentState: "Paid",
    lineCount: 1,
    sellerCount: 1,
    subtotal: 1000,
    taxAmount: 0,
    discountAmount: 0,
    payableAmount: 1000,
    currency: "IRR",
    recipientName: "محمد امامی",
    contactMobile: "09121234567",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان آزمایش ۱",
    postalCode: "1234567890",
    shippingMethodLabel: "پیشتاز",
    sellerOrders: [],
    consolidatedPackages: [],
    sellerFinancials: [],
    financialEvents: [],
    financialSummary: {} as AdminOrderDetail["financialSummary"],
    shippingGroups: [],
  };
}

test("mount modal Manual: compact localized summary; same-tick one request; no technical prose", async () => {
  const calls: AdminOrderOperationRequest[] = [];
  let resolveFirst: ((v: { state: "ok" }) => void) | null = null;
  const executor = async (_checkoutId: string, body: AdminOrderOperationRequest) => {
    calls.push(body);
    if (calls.length === 1) {
      await new Promise<{ state: "ok" }>((resolve) => {
        resolveFirst = resolve;
      });
      return { state: "ok" as const };
    }
    return { state: "ok" as const };
  };

  const so = seller([line(LINE_A, "کالا الف", 3)]);
  const g = group(GROUP_A, [LINE_A]);
  const tree = await mount(
    createElement(AdminCreateShipmentModal, {
      open: true,
      onClose: () => undefined,
      checkoutId: CHECKOUT,
      sellerOrder: so,
      fulfillmentId: FULFILLMENT,
      shippingGroup: g,
      selectedLines: [],
      orderDetail: detail(),
      operationExecutor: executor as never,
    }),
  );

  const root = q(tree.container, '[data-testid="admin-create-shipment-modal"]');
  const summary = q(tree.container, '[data-testid="admin-create-shipment-accepted-summary"]');
  const text = root.textContent ?? "";
  assert.match(summary.textContent ?? "", /پیشتاز/);
  assert.match(summary.textContent ?? "", /فروشگاه آرمان/);
  assert.match(summary.textContent ?? "", /09:00–10:00/);
  assert.doesNotMatch(text, /گروه پذیرفته‌شده|تعهد ارسال|فقط خواندنی|اقلام همین گروه|store-alpha|Asia\/Tehran|\bStore\b/i);
  assert.doesNotMatch(text, /2026-11-05/);
  assert.match(q(tree.container, '[data-testid="admin-create-shipment-recipient"]').textContent ?? "", /محمد امامی/);
  assert.equal(tree.container.querySelector('select, [data-testid*="method-catalogue"]'), null);

  const submit = q(tree.container, '[data-testid="admin-create-shipment-submit"]');
  await clickBurst(submit, 3);
  assert.equal(calls.length, 1);
  assert.equal(calls[0]?.shippingGroupId, GROUP_A);
  assert.equal(calls[0]?.code, "create_shipment");
  assert.equal(calls[0]?.shippingMethodCode, "post");
  assert.ok((calls[0]?.selections?.length ?? 0) >= 1);
  assert.equal(calls[0]?.selections?.[0]?.orderLineId, LINE_A);
  assert.equal((calls[0] as { providerMetadataJson?: unknown }).providerMetadataJson, undefined);

  resolveFirst?.({ state: "ok" });
  await flush();
  await tree.unmount();
});

test("mount modal: typed refusal shows one inline error, no toast path; preserves qty", async () => {
  const executor = async () => ({
    state: "error" as const,
    message: "fulfillment.shipping.post.recipient_required",
  });
  const so = seller([line(LINE_A, "کالا الف", 4)]);
  const g = group(GROUP_A, [LINE_A]);
  const tree = await mount(
    createElement(AdminCreateShipmentModal, {
      open: true,
      onClose: () => undefined,
      checkoutId: CHECKOUT,
      sellerOrder: so,
      fulfillmentId: FULFILLMENT,
      shippingGroup: g,
      selectedLines: [{ line: so.lines[0]!, quantity: 2 }],
      orderDetail: detail(),
      operationExecutor: executor as never,
    }),
  );

  const qty = q(tree.container, `[data-testid="admin-create-shipment-qty-${LINE_A}"]`) as HTMLInputElement;
  assert.equal(Number(qty.value), 2);

  const submit = q(tree.container, '[data-testid="admin-create-shipment-submit"]');
  await click(submit);
  await waitFor(() => Boolean(tree.container.querySelector('[data-testid="admin-create-shipment-error"]')));
  assert.equal(qq(tree.container, '[data-testid="admin-create-shipment-error"]').length, 1);
  assert.match(
    q(tree.container, '[data-testid="admin-create-shipment-error"]').textContent ?? "",
    /گیرنده|پست|ناموفق|failed|سفارش|خطایی رخ داد/i,
  );
  assert.equal(submit.hasAttribute("disabled") || (submit as HTMLButtonElement).disabled, false);
  assert.equal(Number(qty.value), 2);
  await tree.unmount();
});

test("mount dual Manual groups: distinct summaries; CarrierApi group has no generic submit", async () => {
  function DualFixture() {
    const [openA, setOpenA] = useState(true);
    const [openB, setOpenB] = useState(false);
    const so = seller([line(LINE_A, "کالا الف", 2), line(LINE_B, "کالا ب", 2)]);
    const gA = group(GROUP_A, [LINE_A], { date: "2026-11-05" });
    const gB = group(GROUP_B, [LINE_B], {
      transportMode: "CarrierApi",
      eligible: false,
      blockFa: "این گروه حامل API است؛ ایجاد مرسوله فقط از پنل اجرای حامل همین گروه انجام می‌شود.",
      date: "2026-11-07",
    });
    return createElement(
      "div",
      { "data-testid": "dual-group-fixture" },
      createElement(
        "div",
        { "data-testid": `admin-order-shipping-group-${GROUP_A}` },
        createElement("p", null, gA.methodLabel),
        createElement("p", null, `lines:${gA.orderLineIds.join(",")}`),
        gA.transportMode !== "CarrierApi" && gA.createParcelEligible
          ? createElement(
              "button",
              {
                type: "button",
                "data-testid": `admin-order-group-create-shipment-${GROUP_A}`,
                onClick: () => setOpenA(true),
              },
              "ایجاد",
            )
          : createElement("p", { role: "status" }, gA.createParcelBlockReasonFa),
      ),
      createElement(
        "div",
        { "data-testid": `admin-order-shipping-group-${GROUP_B}` },
        createElement("p", null, gB.methodLabel),
        createElement("p", null, `lines:${gB.orderLineIds.join(",")}`),
        gB.transportMode !== "CarrierApi" && gB.createParcelEligible
          ? createElement(
              "button",
              {
                type: "button",
                "data-testid": `admin-order-group-create-shipment-${GROUP_B}`,
                onClick: () => setOpenB(true),
              },
              "ایجاد",
            )
          : createElement("p", { role: "status", "data-testid": `carrier-block-${GROUP_B}` }, gB.createParcelBlockReasonFa),
      ),
      openA
        ? createElement(AdminCreateShipmentModal, {
            open: true,
            onClose: () => setOpenA(false),
            checkoutId: CHECKOUT,
            sellerOrder: so,
            fulfillmentId: FULFILLMENT,
            shippingGroup: gA,
            selectedLines: [],
            orderDetail: detail(),
            operationExecutor: (async () => ({ state: "ok" as const })) as never,
          })
        : null,
      openB
        ? createElement(AdminCreateShipmentModal, {
            open: true,
            onClose: () => setOpenB(false),
            checkoutId: CHECKOUT,
            sellerOrder: so,
            fulfillmentId: FULFILLMENT,
            shippingGroup: gB,
            selectedLines: [],
            orderDetail: detail(),
            operationExecutor: (async () => ({ state: "ok" as const })) as never,
          })
        : null,
    );
  }

  const tree = await mount(createElement(DualFixture));
  const cardA = q(tree.container, `[data-testid="admin-order-shipping-group-${GROUP_A}"]`);
  const cardB = q(tree.container, `[data-testid="admin-order-shipping-group-${GROUP_B}"]`);
  assert.match(cardA.textContent ?? "", new RegExp(LINE_A));
  assert.match(cardB.textContent ?? "", new RegExp(LINE_B));
  assert.doesNotMatch(cardA.textContent ?? "", /store-alpha|Asia\/Tehran|\bStore\b/);
  assert.ok(tree.container.querySelector(`[data-testid="admin-order-group-create-shipment-${GROUP_A}"]`));
  assert.equal(tree.container.querySelector(`[data-testid="admin-order-group-create-shipment-${GROUP_B}"]`), null);
  assert.match(q(tree.container, `[data-testid="carrier-block-${GROUP_B}"]`).textContent ?? "", /حامل API/);
  assert.ok(tree.container.querySelector('[data-testid="admin-create-shipment-modal"]'));
  assert.equal(qq(tree.container, '[data-testid="admin-create-shipment-modal"]').length, 1);
  const modalText = q(tree.container, '[data-testid="admin-create-shipment-modal"]').textContent ?? "";
  assert.doesNotMatch(modalText, /store-alpha|Asia\/Tehran|گروه پذیرفته‌شده|تعهد ارسال/);
  await tree.unmount();
});

// ---------------------------------------------------------------------------------------------
// TB-257 A — the operator tracking modal: whole-parcel suggestion, same-tick single flight, and
// inline refusal of a code already present in this order (no request at all).
// ---------------------------------------------------------------------------------------------

const TB257_SHIPMENT: AdminShipment = {
  shipmentId: "019a5c2e-4f10-7000-8000-000000000001",
  status: "Created",
  carrierDisplayName: "پست",
  trackingReference: null,
  itemCount: 1,
  lines: [{ orderLineId: "line-257-a", quantity: 1 }],
};

const TB257_OTHER_SHIPMENT: AdminShipment = {
  ...TB257_SHIPMENT,
  shipmentId: "019a5c2e-4f11-7000-8000-000000000002",
};

const TB257_SELLER: AdminSellerOrder = {
  id: "so-257",
  orderNumber: "SO-257",
  sellerDisplayName: "فروشگاه آرمان",
  status: "Paid",
  paymentState: "Paid",
  payableAmount: 1000,
  currency: "IRR",
  lines: [],
  fulfillmentId: "ff-257",
  fulfillmentStatus: "ReadyToFulfill",
  shipments: [TB257_SHIPMENT],
};

test("TB-257 static: the suggestion uses the WHOLE parcel id, so parcels in one minute differ", () => {
  const first = suggestShipmentTracking(TB257_SHIPMENT.shipmentId);
  const second = suggestShipmentTracking(TB257_OTHER_SHIPMENT.shipmentId);

  // The old rule kept only the first eight characters — one ~65 second bucket shared by both parcels.
  assert.equal(
    TB257_SHIPMENT.shipmentId.replace(/[^A-Za-z0-9]/g, "").slice(0, 8),
    TB257_OTHER_SHIPMENT.shipmentId.replace(/[^A-Za-z0-9]/g, "").slice(0, 8),
  );
  assert.match(first, /^TRK-[0-9A-F]{32}$/);
  assert.notEqual(first, second);
  assert.equal(first, suggestShipmentTracking(TB257_SHIPMENT.shipmentId));
});

test("TB-257 mounted: canonical suggestion, exactly one request per burst, input retained on refusal", async () => {
  let calls = 0;
  let release: (() => void) | null = null;
  const submitted: string[] = [];
  const tree = await mount(
    createElement(AdminAssignShipmentTrackingModal, {
      open: true,
      seller: TB257_SELLER,
      shipment: TB257_SHIPMENT,
      pending: false,
      existingTrackingCodes: [],
      onClose: () => undefined,
      onSubmit: async (trackingReference: string) => {
        calls += 1;
        submitted.push(trackingReference);
        if (calls === 1) {
          await new Promise<void>((resolve) => {
            release = resolve;
          });
          return false;
        }
        return true;
      },
    }),
  );

  const input = q(tree.container, '[data-testid="admin-shipment-tracking-input"]') as HTMLInputElement;
  assert.equal(input.value, suggestShipmentTracking(TB257_SHIPMENT.shipmentId));

  await clickBurst(q(tree.container, '[data-testid="admin-shipment-tracking-submit"]'), 3);
  assert.equal(calls, 1);
  assert.equal(submitted[0], suggestShipmentTracking(TB257_SHIPMENT.shipmentId));

  release?.();
  await flush();
  assert.equal(
    (q(tree.container, '[data-testid="admin-shipment-tracking-input"]') as HTMLInputElement).value,
    suggestShipmentTracking(TB257_SHIPMENT.shipmentId),
  );
  assert.match(q(tree.container, '[data-testid="admin-shipment-tracking-error"]').textContent ?? "", /ناموفق/);
  await tree.unmount();
});

test("TB-257 mounted: a code already used in this order is refused inline and sends nothing", async () => {
  let calls = 0;
  const tree = await mount(
    createElement(AdminAssignShipmentTrackingModal, {
      open: true,
      seller: TB257_SELLER,
      shipment: TB257_SHIPMENT,
      pending: false,
      existingTrackingCodes: ["TRK-ABC"],
      onClose: () => undefined,
      onSubmit: async () => {
        calls += 1;
        return true;
      },
    }),
  );

  const input = q(tree.container, '[data-testid="admin-shipment-tracking-input"]') as HTMLInputElement;
  const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")?.set;
  setter?.call(input, " trk-abc ");
  input.dispatchEvent(new Event("input", { bubbles: true }) as never);
  await flush();

  await click(q(tree.container, '[data-testid="admin-shipment-tracking-submit"]'));
  await flush();

  assert.equal(calls, 0);
  assert.match(q(tree.container, '[data-testid="admin-shipment-tracking-error"]').textContent ?? "", /قبلاً ثبت/);
  await tree.unmount();
});

test("TB-257 mounted: the automatic button restores this parcel's canonical candidate", async () => {
  const tree = await mount(
    createElement(AdminAssignShipmentTrackingModal, {
      open: true,
      seller: TB257_SELLER,
      shipment: TB257_OTHER_SHIPMENT,
      pending: false,
      existingTrackingCodes: [],
      onClose: () => undefined,
      onSubmit: async () => true,
    }),
  );

  const input = q(tree.container, '[data-testid="admin-shipment-tracking-input"]') as HTMLInputElement;
  const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")?.set;
  setter?.call(input, "OPERATOR-CODE");
  input.dispatchEvent(new Event("input", { bubbles: true }) as never);
  await flush();
  assert.equal(input.value, "OPERATOR-CODE");

  await click(q(tree.container, '[data-testid="admin-shipment-tracking-autofill"]'));
  await flush();
  assert.equal(
    (q(tree.container, '[data-testid="admin-shipment-tracking-input"]') as HTMLInputElement).value,
    suggestShipmentTracking(TB257_OTHER_SHIPMENT.shipmentId),
  );
  await tree.unmount();
});
