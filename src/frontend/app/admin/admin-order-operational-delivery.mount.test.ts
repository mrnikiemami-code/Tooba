/**
 * TB-253-R2 — operational delivery group workflow.
 *
 * Static server-render structure is asserted with renderToStaticMarkup.
 * Behaviour (review quantities, single-flight request, inline refusal, lost response,
 * authority) is asserted with a real jsdom + react-dom mount of the shipped panel.
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import test from "node:test";
import {
  mapAdminOrderDetail,
  type AdminOrderDetail,
} from "./admin-api.ts";
import { AdminOrderOperationalDeliverySection } from "./admin-order-operational-delivery-section.tsx";
import { AdminOrderItemsShippingPanel } from "./admin-order-items-shipping-panel.tsx";
import {
  click,
  clickBurst,
  flush,
  mount,
  q,
  qq,
  setInputValue,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";

const CHECKOUT = "c1";

function baseDetail(overrides: Partial<AdminOrderDetail> = {}): AdminOrderDetail {
  const mapped = mapAdminOrderDetail({
    checkoutId: CHECKOUT,
    reference: "R1",
    currency: "IRR",
    sellerOrders: [
      {
        sellerOrderId: "so1",
        orderNumber: "SO-1",
        sellerDisplayName: "آرمان",
        lines: [{
          orderLineId: "ol1",
          offerId: "o1",
          productTitle: "شال آرمان",
          quantity: 2,
          quantityPacked: 2,
          quantityAllocated: 0,
          unitAmount: 1000,
          linePayable: 2000,
          currency: "IRR",
        }],
        shipments: [],
      },
      {
        sellerOrderId: "so2",
        orderNumber: "SO-2",
        sellerDisplayName: "نیلوا",
        lines: [{
          orderLineId: "ol2",
          offerId: "o2",
          productTitle: "پیراهن نیلوا",
          quantity: 1,
          quantityPacked: 1,
          quantityAllocated: 0,
          unitAmount: 3000,
          linePayable: 3000,
          currency: "IRR",
        }],
        shipments: [],
      },
    ],
    shippingGroups: [
      {
        shippingGroupId: "g1",
        sellerOrderId: "so1",
        sellerPartyId: "sp1",
        methodCode: "post",
        methodLabel: "پیشتاز",
        serviceCode: null,
        optionCode: null,
        shippingChargeSnapshot: 125000,
        currency: "IRR",
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        deliveryTimeZone: "Asia/Tehran",
        ownerKind: "Marketplace",
        ownerTenantKey: null,
        ownerSellerPartyId: null,
        orderLineIds: ["ol1"],
        createParcelEligible: true,
        createParcelBlockReasonFa: null,
        payableAtCheckoutAmount: 125000,
        carrierCollectAmount: 0,
        transportMode: "Manual",
      },
      {
        shippingGroupId: "g2",
        sellerOrderId: "so2",
        sellerPartyId: "sp2",
        methodCode: "post",
        methodLabel: "پیشتاز",
        serviceCode: null,
        optionCode: null,
        shippingChargeSnapshot: 125000,
        currency: "IRR",
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        deliveryTimeZone: "Asia/Tehran",
        ownerKind: "Marketplace",
        ownerTenantKey: null,
        ownerSellerPartyId: null,
        orderLineIds: ["ol2"],
        createParcelEligible: true,
        createParcelBlockReasonFa: null,
        payableAtCheckoutAmount: 125000,
        carrierCollectAmount: 0,
        transportMode: "Manual",
      },
    ],
    operationalDeliveryGroups: [
      {
        operationalGroupKey: "mp-post-combined",
        methodLabel: "پیشتاز",
        methodCode: "post",
        serviceCode: null,
        optionCode: null,
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        responsibilityLabelFa: "ارسال توسط مارکت‌پلیس",
        isCrossSellerMarketplace: true,
        separateReasonFa: null,
        consolidateEligible: true,
        consolidateBlockReasonFa: null,
        combinedFeeTotal: 250000,
        currency: "IRR",
        members: [
          {
            shippingGroupId: "g1",
            sellerOrderId: "so1",
            sellerPartyId: "sp1",
            sellerDisplayName: "آرمان",
            orderLineIds: ["ol1"],
            createParcelEligible: true,
            createParcelBlockReasonFa: null,
            transportMode: "Manual",
            feeAmount: 125000,
            currency: "IRR",
            lines: [{
              orderLineId: "ol1",
              title: "شال آرمان",
              remainingPackedQuantity: 2,
              packedQuantity: 2,
            }],
          },
          {
            shippingGroupId: "g2",
            sellerOrderId: "so2",
            sellerPartyId: "sp2",
            sellerDisplayName: "نیلوا",
            orderLineIds: ["ol2"],
            createParcelEligible: true,
            createParcelBlockReasonFa: null,
            transportMode: "Manual",
            feeAmount: 125000,
            currency: "IRR",
            lines: [{
              orderLineId: "ol2",
              title: "پیراهن نیلوا",
              remainingPackedQuantity: 1,
              packedQuantity: 1,
            }],
          },
        ],
        feeBreakdown: [
          { shippingGroupId: "g1", sellerDisplayName: "آرمان", feeAmount: 125000, currency: "IRR" },
          { shippingGroupId: "g2", sellerDisplayName: "نیلوا", feeAmount: 125000, currency: "IRR" },
        ],
        actorMayManageOperations: true,
      },
    ],
  });
  assert.ok(mapped);
  return { ...mapped!, ...overrides };
}

function withParcels(detail: AdminOrderDetail, packaged: boolean): AdminOrderDetail {
  return {
    ...detail,
    sellerOrders: detail.sellerOrders.map((seller, idx) => ({
      ...seller,
      shipments: [{
        shipmentId: `sh${idx + 1}`,
        status: "Created",
        carrierDisplayName: "پست",
        shippingGroupId: idx === 0 ? "g1" : "g2",
        shippingMethodCode: "post",
        itemCount: 1,
        lines: [{ orderLineId: idx === 0 ? "ol1" : "ol2", quantity: 1 }],
        trackingReference: null,
        activePackageNumber: packaged ? `PKG-${idx + 1}` : null,
        packageLockedReasonFa: null,
      }],
    })),
  };
}

type FetchCall = { method: string; body: Record<string, unknown> };

function installFetch(responder: (call: FetchCall, index: number) => Response | Promise<Response>): FetchCall[] {
  const calls: FetchCall[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const method = (init?.method ?? "GET").toUpperCase();
    let body: Record<string, unknown> = {};
    if (typeof init?.body === "string") {
      body = JSON.parse(init.body) as Record<string, unknown>;
    }
    const call = { method, body };
    calls.push(call);
    try {
      return await responder(call, calls.length);
    } catch (error) {
      if (error instanceof Error) throw error;
      throw new Error(String(error));
    }
  }) as typeof fetch;
  (globalThis as typeof globalThis & { __restoreFetch?: () => void }).__restoreFetch = () => {
    globalThis.fetch = original;
  };
  return calls;
}

function restoreFetch() {
  const g = globalThis as typeof globalThis & { __restoreFetch?: () => void };
  g.__restoreFetch?.();
  delete g.__restoreFetch;
}

function operationsPage() {
  return new Response(
    JSON.stringify({
      checkoutId: CHECKOUT,
      actions: [
        {
          code: "prepare_operational_group",
          labelFa: "ایجاد مرسوله‌ها و بستهٔ مشترک",
          labelEn: "Prepare group parcels and common package",
          requiredPermission: "order.handle",
          requiresConfirm: true,
          confirmMessageFa: "تأیید؟",
        },
      ],
      lineCapabilities: [],
      sellerCapabilities: [],
    }),
    { status: 200, headers: { "content-type": "application/json" } },
  );
}

function postResponse(status: number, payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "content-type": "application/json" },
  });
}

test("static render: Marketplace group shows both sellers, items, parcel and group action", () => {
  const detail = withParcels(baseDetail(), false);
  let prepareCalls = 0;
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
      onPrepareOperationalGroup: () => {
        prepareCalls += 1;
      },
    }),
  );
  assert.match(html, /ارسال سفارش/);
  assert.match(html, /admin-order-operational-group-mp-post-combined/);
  assert.match(html, /data-cross-seller="true"/);
  assert.match(html, /آرمان/);
  assert.match(html, /نیلوا/);
  assert.match(html, /شال آرمان/);
  assert.match(html, /پیراهن نیلوا/);
  assert.match(html, /جمع هزینه ارسال/);
  assert.match(html, /admin-order-operational-reuse-parcel-sh1/);
  assert.match(html, /admin-order-operational-prepare-mp-post-combined/);
  assert.match(html, /ایجاد مرسوله‌ها و بستهٔ مشترک این گروه/);
  assert.doesNotMatch(html, /store-alpha|Asia\/Tehran|tenant/i);
  assert.equal(prepareCalls, 0);
});

test("mounted: reviewed quantities produce exactly one group request across two sellers", async () => {
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(200, {
      ok: true,
      code: "prepare_operational_group",
      packageNumber: "PKG-1",
    });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    assert.equal(
      (q(mounted.container, "[data-testid='admin-order-operational-qty-ol1']") as HTMLInputElement).value,
      "2",
    );
    await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 1);
    await flush();
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts.length, 1);
    assert.equal(posts[0].body.code, "prepare_operational_group");
    assert.deepEqual(posts[0].body.operationalGroupShippingGroupIds, ["g1", "g2"]);
    assert.deepEqual(posts[0].body.selections, [
      { orderLineId: "ol1", quantity: 2 },
      { orderLineId: "ol2", quantity: 1 },
    ]);
    assert.deepEqual(posts[0].body.reuseShipmentIds, []);
    assert.equal(posts[0].body.shippingMethodCode, null);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: same-tick double click performs exactly one mutation", async () => {
  let release: (() => void) | null = null;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  const calls = installFetch(async (call) => {
    if (call.method === "GET") return operationsPage();
    await gate;
    return postResponse(200, { ok: true, code: "prepare_operational_group" });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await clickBurst(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"), 2);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    release?.();
    await flush();
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: known refusal is inline and reviewed quantities are retained", async () => {
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(409, { error: { code: "order.operational_group.workflow_selection_required" } });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await setInputValue(q(mounted.container, "[data-testid='admin-order-operational-qty-ol1']"), "2");
    await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => qq(mounted.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    assert.equal(
      (q(mounted.container, "[data-testid='admin-order-operational-qty-ol1']") as HTMLInputElement).value,
      "2",
    );
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: lost response surfaces inline error, remount reconciles recorded parcels", async () => {
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    throw new TypeError("network down");
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => qq(mounted.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    await mounted.unmount();

    // Remount with the server's recorded parcels/packages: no second allocation is offered.
    const reconciledDetail = withParcels(baseDetail(), true);
    reconciledDetail.operationalDeliveryGroups = reconciledDetail.operationalDeliveryGroups!.map((group) => ({
      ...group,
      consolidateEligible: false,
      consolidateBlockReasonFa: "بستهٔ تجمیعی این گروه قبلاً ایجاد شده است.",
    }));
    const reconciled = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: reconciledDetail,
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(reconciled.container, "[data-testid^='admin-order-operational-group-']").length === 1);
    assert.equal(qq(reconciled.container, "[data-testid^='admin-order-operational-prepare-']").length, 0);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    await reconciled.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: actor without manage permission sees no actionable create", async () => {
  installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(200, { ok: true });
  });
  try {
    const detail = baseDetail();
    const gated: AdminOrderDetail = {
      ...detail,
      operationalDeliveryGroups: detail.operationalDeliveryGroups!.map((group) => ({
        ...group,
        actorMayManageOperations: false,
      })),
    };
    const mounted = await mount(
      createElement(AdminOrderOperationalDeliverySection, {
        detail: gated,
        pendingCode: null,
        selectedBySeller: {},
        qtyByLine: {},
        shippableQty: () => 1,
        onCreateShipment: () => undefined,
        onPrepareOperationalGroup: () => undefined,
      }),
    );
    assert.equal(qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length, 0);
    assert.equal(qq(mounted.container, "[data-testid^='admin-order-operational-create-']").length, 0);
    assert.equal(qq(mounted.container, "[data-testid^='admin-order-operational-no-authority-']").length, 1);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("static render: seller-owned groups stay independent and offer no group workflow", () => {
  const detail = baseDetail();
  const sellerOwned: AdminOrderDetail = {
    ...detail,
    operationalDeliveryGroups: [{
      ...detail.operationalDeliveryGroups![0],
      operationalGroupKey: "seller-a",
      isCrossSellerMarketplace: false,
      responsibilityLabelFa: "ارسال توسط فروشنده",
      separateReasonFa: "ارسال بر عهده فروشنده است و با فروشندگان دیگر تجمیع نمی‌شود.",
      consolidateEligible: false,
      consolidateBlockReasonFa: null,
      members: [detail.operationalDeliveryGroups![0].members[0]],
      feeBreakdown: [detail.operationalDeliveryGroups![0].feeBreakdown[0]],
    }],
  };
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail: sellerOwned,
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
      onPrepareOperationalGroup: () => undefined,
    }),
  );
  assert.match(html, /admin-order-operational-group-seller-a/);
  assert.equal((html.match(/data-cross-seller="false"/g) ?? []).length, 1);
  assert.doesNotMatch(html, /admin-order-operational-prepare-seller-a/);
});

test("panel source uses synchronous single-flight, try/finally and inline error", async () => {
  const { readFileSync } = await import("node:fs");
  const { dirname, join } = await import("node:path");
  const { fileURLToPath } = await import("node:url");
  const panel = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), "admin-order-items-shipping-panel.tsx"),
    "utf8",
  );
  assert.match(panel, /groupWorkflowInFlight/);
  assert.match(panel, /useRef\(false\)/);
  assert.match(panel, /prepare_operational_group/);
  assert.match(panel, /try\s*\{/);
  assert.match(panel, /finally\s*\{/);
  assert.match(panel, /setGroupWorkflowErrorFa/);
  assert.doesNotMatch(panel, /onConsolidateGroup/);
});
