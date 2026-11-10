/**
 * TB-260-R1 — Case E: the mounted shared operational route focuses the EXACT physical target.
 *
 * The queue now sends a group, parcel or package locator (never "the seller's earliest group"). This mounts the
 * real route component with a mocked server detail and asserts:
 *   1. a Created parcel awaiting tracking is shown as real work, not «کار فیزیکی باقی نمانده»;
 *   2. a parcel locator resolves to ITS OWN group only;
 *   3. a package locator resolves to the package's member groups and names the exact package number;
 *   4. a locator that no longer exists is reported, and the view stays finance-free.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test \
 *     "app/admin/orders/[checkoutId]/operations/admin-order-operations-route.mount.test.ts"
 */
import "../../../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { createElement } from "react";
import { mount, qq, waitFor, flush } from "../../../../../features/order-carrier-execution/test-dom-harness.ts";
import { AdminOrderOperationsRouteScreen } from "./admin-order-operations-route-screen.tsx";

const CHECKOUT = "c-ops-route-260r1";
const FULFILLMENT = "f1";
const GROUP_ONE = "gk-260r1-a";
const GROUP_TWO = "gk-260r1-b";
const GROUP_ONE_ID = "g1";
const GROUP_TWO_ID = "g2";
const PARCEL_ONE = "p1-260r1";
const PARCEL_TWO = "p2-260r1";
const PACKAGE_ID = "pkg-260r1";
const PACKAGE_NUMBER = "MP-260R1PKG";

const FINANCE_MARKERS = ["بخش مالی", "سهم فروشندگان", "تسویه", "کیف پول", "بازگشت وجه"];

function detailPayload() {
  const sellerLine = {
    orderLineId: "ol1",
    offerId: "o1",
    productTitle: "شال آرمان",
    quantity: 2,
    quantityPacked: 2,
    quantityAllocated: 2,
    unitAmount: 1000,
    linePayable: 2000,
    currency: "IRR",
  };
  return {
    checkoutId: CHECKOUT,
    reference: "ORD-260R1",
    currency: "IRR",
    subtotal: 2000,
    taxAmount: 0,
    discountAmount: 0,
    payableAmount: 2000,
    paymentState: "Paid",
    status: "Submitted",
    recipientName: "سارا",
    contactMobile: "09120000000",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان ولیعصر، پلاک ۱",
    postalCode: "1234567890",
    shippingMethodLabel: "پیشتاز",
    sellerOrders: [{
      sellerOrderId: "so1",
      fulfillmentId: FULFILLMENT,
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      lines: [sellerLine],
      shipments: [
        {
          shipmentId: PARCEL_ONE,
          status: "Created",
          trackingReference: "TRK-260R1-1",
          shippingGroupId: GROUP_ONE_ID,
          shippingMethodCode: "post",
          lines: [{ orderLineId: "ol1", quantity: 1 }],
        },
        {
          shipmentId: PARCEL_TWO,
          status: "Created",
          trackingReference: null,
          shippingGroupId: GROUP_TWO_ID,
          shippingMethodCode: "post",
          lines: [{ orderLineId: "ol1", quantity: 1 }],
        },
      ],
    }],
    shippingGroups: [],
    operationalDeliveryGroups: [
      {
        operationalGroupKey: GROUP_ONE,
        methodLabel: "پیشتاز",
        methodCode: "post",
        deliveryDate: "2026-11-12",
        deliveryTimeWindow: "09:00–10:00",
        responsibilityLabelFa: "ارسال توسط مارکت‌پلیس",
        isCrossSellerMarketplace: true,
        consolidateEligible: false,
        combinedFeeTotal: 125000,
        currency: "IRR",
        members: [{
          shippingGroupId: GROUP_ONE_ID,
          sellerOrderId: "so1",
          sellerPartyId: "sp1",
          sellerDisplayName: "آرمان",
          orderLineIds: ["ol1"],
          createParcelEligible: false,
          createParcelBlockReasonFa: null,
          transportMode: "Manual",
          feeAmount: 125000,
          currency: "IRR",
          lines: [{ orderLineId: "ol1", title: "شال آرمان", remainingPackedQuantity: 0, packedQuantity: 1 }],
        }],
        feeBreakdown: [],
        actorMayManageOperations: true,
        plannedDispatch: null,
      },
      {
        operationalGroupKey: GROUP_TWO,
        methodLabel: "پیشتاز",
        methodCode: "post",
        deliveryDate: "2026-11-13",
        deliveryTimeWindow: "11:00–12:00",
        responsibilityLabelFa: "ارسال توسط مارکت‌پلیس",
        isCrossSellerMarketplace: true,
        consolidateEligible: false,
        combinedFeeTotal: 125000,
        currency: "IRR",
        members: [{
          shippingGroupId: GROUP_TWO_ID,
          sellerOrderId: "so1",
          sellerPartyId: "sp1",
          sellerDisplayName: "آرمان",
          orderLineIds: ["ol1"],
          createParcelEligible: false,
          createParcelBlockReasonFa: null,
          transportMode: "Manual",
          feeAmount: 125000,
          currency: "IRR",
          lines: [{ orderLineId: "ol1", title: "شال آرمان", remainingPackedQuantity: 0, packedQuantity: 1 }],
        }],
        feeBreakdown: [],
        actorMayManageOperations: true,
        plannedDispatch: null,
      },
    ],
    consolidatedPackages: [{
      consolidatedPackageId: PACKAGE_ID,
      packageNumber: PACKAGE_NUMBER,
      status: "Created",
      shippingMethodCode: "post",
      shippingMethodLabel: "پیشتاز",
      trackingReference: null,
      sellerCount: 1,
      memberShipmentCount: 2,
      members: [
        { shipmentId: PARCEL_ONE, sellerPartyId: "sp1", fulfillmentId: FULFILLMENT },
        { shipmentId: PARCEL_TWO, sellerPartyId: "sp1", fulfillmentId: FULFILLMENT },
      ],
    }],
  };
}

async function withServerDetail<T>(run: () => Promise<T>): Promise<T> {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(`/v1/admin/orders/${CHECKOUT}`)) {
      return new Response(JSON.stringify(detailPayload()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response(JSON.stringify({ actions: [], lineCapabilities: [], sellerCapabilities: [] }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  try {
    return await run();
  } finally {
    globalThis.fetch = originalFetch;
  }
}

function route(props: { shipmentId?: string | null; packageId?: string | null; shippingGroupId?: string | null }) {
  return createElement(AdminOrderOperationsRouteScreen, {
    checkoutId: CHECKOUT,
    fulfillmentId: FULFILLMENT,
    shippingGroupId: props.shippingGroupId ?? null,
    shipmentId: props.shipmentId ?? null,
    packageId: props.packageId ?? null,
    fromQueue: true,
    backFilter: "dispatch_today",
  });
}

test("mounted: a Created parcel awaiting tracking is real work and resolves to ITS OWN group", async () => {
  await withServerDetail(async () => {
    const mounted = await mount(route({ shipmentId: PARCEL_TWO }));
    await waitFor(() => qq(mounted.container, `[data-testid='admin-order-operations-target-${GROUP_TWO}']`).length === 1);
    await flush();
    const html = mounted.container.innerHTML;

    // The exact parcel locator resolves to its own accepted group only — never the sibling's group.
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operations-target-${GROUP_ONE}']`).length, 0);
    assert.ok(html.includes("در انتظار ثبت کد رهگیری"), "a Created parcel awaiting tracking is real work");
    assert.ok(!html.includes("کار فیزیکی باقی نمانده"), "a Created parcel must not be labelled as no work");
    // The shared operational panel is the SAME component and stays finance-free.
    assert.equal(qq(mounted.container, "[data-testid='admin-order-items-shipping']").length, 1);
    for (const marker of FINANCE_MARKERS) {
      assert.ok(!html.includes(marker), `unexpected financial surface: ${marker}`);
    }
    await mounted.unmount();
  });
});

test("mounted: a tracked Created parcel reads as ready, and a package locator names it exactly", async () => {
  await withServerDetail(async () => {
    const tracked = await mount(route({ shipmentId: PARCEL_ONE }));
    await waitFor(() => qq(tracked.container, `[data-testid='admin-order-operations-target-${GROUP_ONE}']`).length === 1);
    await flush();
    assert.ok(tracked.container.innerHTML.includes("آمادهٔ ارسال به حامل"));
    await tracked.unmount();

    const packaged = await mount(route({ packageId: PACKAGE_ID }));
    await waitFor(() => qq(packaged.container, "[data-testid='admin-order-operations-target-package']").length === 1);
    await flush();
    const html = packaged.container.innerHTML;
    assert.ok(html.includes(PACKAGE_NUMBER), "the exact package number must be shown");
    // Both member groups are in scope for the package, so the target frame shows the package's real members.
    assert.ok(html.includes(`data-testid="admin-order-operations-target-${GROUP_ONE}"`));
    assert.ok(html.includes(`data-testid="admin-order-operations-target-${GROUP_TWO}"`));
    assert.ok(html.includes("در انتظار ثبت کد رهگیری بسته"));
    await packaged.unmount();
  });
});

test("mounted: a stale parcel/package locator is reported instead of showing another group", async () => {
  await withServerDetail(async () => {
    const parcel = await mount(route({ shipmentId: "p-missing-260r1" }));
    await waitFor(() => qq(parcel.container, "[data-testid='admin-order-operations-target-missing']").length === 1);
    await flush();
    assert.equal(qq(parcel.container, `[data-testid='admin-order-operations-target-${GROUP_ONE}']`).length, 0);
    await parcel.unmount();

    const packaged = await mount(route({ packageId: "pkg-missing-260r1" }));
    await waitFor(() => qq(packaged.container, "[data-testid='admin-order-operations-target-missing']").length === 1);
    await flush();
    assert.equal(qq(packaged.container, "[data-testid='admin-order-operations-target-package']").length, 0);
    await packaged.unmount();
  });
});
