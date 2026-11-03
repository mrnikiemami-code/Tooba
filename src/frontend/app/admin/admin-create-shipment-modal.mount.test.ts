/**
 * TB-252-R1 — mount AdminCreateShipmentModal: same-tick single request, thrown error, Manual payload.
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
import type { AdminAcceptedShippingGroup, AdminOrderLine, AdminSellerOrder } from "./admin-api.ts";
import type { AdminOrderOperationRequest } from "./admin-order-operations.ts";

const CHECKOUT = "chk-252r1";
const SELLER = "so-252r1";
const FULFILLMENT = "ff-252r1";
const GROUP_A = "grp-252r1-a";
const GROUP_B = "grp-252r1-b";
const LINE_A = "line-252r1-a";
const LINE_B = "line-252r1-b";

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
    orderNumber: "TB-252R1",
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
    deliveryTimeWindow: "14:00–15:00",
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

test("mount modal Manual: same-tick burst emits one request with group+qty; no method catalogue", async () => {
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
      operationExecutor: executor as never,
    }),
  );

  const submit = q(tree.container, '[data-testid="admin-create-shipment-submit"]');
  assert.equal(tree.container.querySelector('[data-testid="admin-create-shipment-accepted-summary"]')?.textContent?.includes("پیشتاز"), true);
  assert.equal(tree.container.querySelector('select, [data-testid*="method-catalogue"]'), null);

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

test("mount modal: thrown executor shows error, releases pending, preserves qty edits", async () => {
  const executor = async () => {
    throw new Error("order.operation.failed");
  };
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
      operationExecutor: executor as never,
    }),
  );

  const qty = q(tree.container, `[data-testid="admin-create-shipment-qty-${LINE_A}"]`) as HTMLInputElement;
  assert.equal(Number(qty.value), 2);

  const submit = q(tree.container, '[data-testid="admin-create-shipment-submit"]');
  await click(submit);
  await waitFor(() => Boolean(tree.container.querySelector('[data-testid="admin-create-shipment-error"]')));
  assert.match(q(tree.container, '[data-testid="admin-create-shipment-error"]').textContent ?? "", /ناموفق|failed|سفارش/i);
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
        createElement("p", null, `${gA.deliveryDate} · owner ${gA.ownerTenantKey}`),
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
        createElement("p", null, `${gB.deliveryDate} · owner ${gB.ownerTenantKey}`),
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
            operationExecutor: (async () => ({ state: "ok" as const })) as never,
          })
        : null,
    );
  }

  const tree = await mount(createElement(DualFixture));
  const cardA = q(tree.container, `[data-testid="admin-order-shipping-group-${GROUP_A}"]`);
  const cardB = q(tree.container, `[data-testid="admin-order-shipping-group-${GROUP_B}"]`);
  assert.match(cardA.textContent ?? "", /2026-11-05/);
  assert.match(cardB.textContent ?? "", /2026-11-07/);
  assert.match(cardA.textContent ?? "", new RegExp(LINE_A));
  assert.match(cardB.textContent ?? "", new RegExp(LINE_B));
  assert.ok(tree.container.querySelector(`[data-testid="admin-order-group-create-shipment-${GROUP_A}"]`));
  assert.equal(tree.container.querySelector(`[data-testid="admin-order-group-create-shipment-${GROUP_B}"]`), null);
  assert.match(q(tree.container, `[data-testid="carrier-block-${GROUP_B}"]`).textContent ?? "", /حامل API/);
  assert.ok(tree.container.querySelector('[data-testid="admin-create-shipment-modal"]'));
  assert.equal(qq(tree.container, '[data-testid="admin-create-shipment-modal"]').length, 1);
  await tree.unmount();
});
