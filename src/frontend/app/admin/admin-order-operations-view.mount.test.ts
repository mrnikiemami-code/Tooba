/**
 * TB-260 — Case C: the shared, finance-free operational order view.
 *
 * The work queue opens ONE operational surface, and the full order page renders the SAME component. These
 * tests mount that surface with the real panel and assert:
 *   1. the exact queue locator is shown explicitly, and a locator that no longer resolves is reported;
 *   2. no financial / payment / settlement surface is present, while the SAME operational section is;
 *   3. authority decides whether a mutation entry exists (no button without authority);
 *   4. a retained unresolved attempt keeps its reconcile entry reachable inside this view.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operations-view.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement, type ReactNode } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import test from "node:test";
import { mapAdminOrderDetail, type AdminOrderDetail } from "./admin-api.ts";
import { AdminOrderItemsShippingPanel } from "./admin-order-items-shipping-panel.tsx";
import { ensureDom, flush, mount, qq, waitFor } from "../../features/order-carrier-execution/test-dom-harness.ts";
import { OrderOperationsView, type OrderOperationsTargetFact } from "../../features/order-operational-groups/index.ts";
import {
  clearOperationalGroupAttempt,
  writeOperationalGroupAttempt,
} from "./operational-group-occurrence-store.ts";

const CHECKOUT = "c-ops-260";
const GROUP_KEY = "mp-post-combined";
const OTHER_KEY = "mp-post-second";

ensureDom();

function baseDetail(overrides: Partial<AdminOrderDetail> = {}): AdminOrderDetail {
  const mapped = mapAdminOrderDetail({
    checkoutId: CHECKOUT,
    reference: "ORD-260",
    currency: "IRR",
    subtotal: 5000,
    taxAmount: 0,
    discountAmount: 0,
    payableAmount: 5000,
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
    }],
    shippingGroups: [],
    operationalDeliveryGroups: [
      {
        operationalGroupKey: GROUP_KEY,
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
        combinedFeeTotal: 125000,
        currency: "IRR",
        members: [{
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
          lines: [{ orderLineId: "ol1", title: "شال آرمان", remainingPackedQuantity: 2, packedQuantity: 2 }],
        }],
        feeBreakdown: [{ shippingGroupId: "g1", sellerDisplayName: "آرمان", feeAmount: 125000, currency: "IRR" }],
        actorMayManageOperations: true,
        plannedDispatch: {
          handoffDate: "2026-11-11",
          handoffTimeWindow: "10:00–13:00",
          handoffTimeZone: "Asia/Tehran",
          transportWorkingDays: 1,
          policyVersion: 1,
          distinctVariants: 1,
        },
      },
    ],
  });
  assert.ok(mapped);
  return { ...mapped!, ...overrides };
}

function targetFact(overrides: Partial<OrderOperationsTargetFact> = {}): OrderOperationsTargetFact {
  return {
    groupKey: GROUP_KEY,
    methodLabel: "پیشتاز",
    customerDeliverySummary: "۱۰ آبان ۱۴۰۵ · 14:00–15:00",
    plannedDispatchSummary: "۱۱ آبان ۱۴۰۵ · 10:00–13:00 · (Asia/Tehran)",
    parcelSummary: "بدون مرسوله",
    readinessLabel: "آمادهٔ اقدام",
    readinessTone: "ready",
    ...overrides,
  };
}

type ViewProps = {
  state: "ready" | "loading" | "denied" | "error";
  targetRequested: boolean;
  targets: OrderOperationsTargetFact[];
  children?: ReactNode;
  backHref?: string;
};

function view(props: ViewProps) {
  const elementProps = {
    checkoutId: CHECKOUT,
    reference: "ORD-260",
    state: props.state,
    errorMessage: null,
    onRetry: () => {},
    backHref: props.backHref ?? "/admin/fulfillments",
    fullOrderHref: `/admin/orders/${CHECKOUT}`,
    recipientName: "سارا",
    recipientMobile: "09120000000",
    destinationLine: "تهران، تهران، خیابان ولیعصر، پلاک ۱",
    methodLabel: "پیشتاز",
    targetRequested: props.targetRequested,
    targets: props.targets,
  };
  // `children` must travel as the createElement argument, never as a prop.
  return props.children === undefined
    ? createElement(OrderOperationsView, elementProps)
    : createElement(OrderOperationsView, elementProps, props.children);
}

function panel(detail: AdminOrderDetail) {
  return createElement(AdminOrderItemsShippingPanel, {
    detail,
    checkoutId: CHECKOUT,
    onCompleted: () => {},
  });
}

const FINANCE_MARKERS = [
  "بخش مالی",
  "سهم فروشندگان",
  "تسویه",
  "کیف پول",
  "پرداخت‌ها / واریزها",
  "رسید مشتری",
  "بازگشت وجه",
];

test("queue locator is explicit, and the frame contains no financial surface", () => {
  const detail = baseDetail();
  const html = renderToStaticMarkup(
    view({
      state: "ready",
      targetRequested: true,
      targets: [targetFact()],
      backHref: "/admin/fulfillments?filter=dispatch_today",
      children: panel(detail),
    }),
  );

  // Exact target, with the accepted plan and the customer commitment kept apart.
  assert.ok(html.includes(`data-testid="admin-order-operations-target-${GROUP_KEY}"`));
  assert.ok(html.includes("۱۱ آبان ۱۴۰۵ · 10:00–13:00"));
  assert.ok(html.includes("۱۰ آبان ۱۴۰۵ · 14:00–15:00"));
  // The SAME operational section the full order page renders is what this view shows.
  assert.ok(html.includes('data-testid="admin-order-items-shipping"'));
  assert.ok(html.includes('data-testid="admin-order-operational-delivery"'));
  assert.ok(html.includes(`data-testid="admin-order-operational-group-${GROUP_KEY}"`));
  // Back to the queue keeps the exact filter, and the full order stays reachable as a separate link.
  assert.ok(html.includes("/admin/fulfillments?filter=dispatch_today"));
  assert.ok(html.includes(`/admin/orders/${CHECKOUT}`));
  // Finance-free by construction.
  for (const marker of FINANCE_MARKERS) {
    assert.ok(!html.includes(marker), `unexpected financial surface: ${marker}`);
  }
});

test("a locator that no longer resolves is reported instead of silently showing another group", () => {
  const html = renderToStaticMarkup(
    view({ state: "ready", targetRequested: true, targets: [] }),
  );
  assert.ok(html.includes('data-testid="admin-order-operations-target-missing"'));
  assert.ok(html.includes('data-testid="admin-order-operations-target"'));
});

test("a group with no accepted plan never invents a dispatch date", () => {
  const html = renderToStaticMarkup(
    view({
      state: "ready",
      targetRequested: true,
      targets: [targetFact({ plannedDispatchSummary: null, readinessLabel: "در انتظار بسته‌بندی", readinessTone: "blocked" })],
    }),
  );
  assert.ok(html.includes("موعد ارسال مشخص نشده"));
  assert.ok(html.includes("در انتظار بسته‌بندی"));
});

test("TB-260-R1: a package work row names the exact package it targets", () => {
  const html = renderToStaticMarkup(
    createElement(OrderOperationsView, {
      checkoutId: CHECKOUT,
      reference: "ORD-260",
      state: "ready",
      errorMessage: null,
      onRetry: () => {},
      backHref: "/admin/fulfillments",
      fullOrderHref: `/admin/orders/${CHECKOUT}`,
      recipientName: "سارا",
      recipientMobile: "09120000000",
      destinationLine: "تهران، تهران، خیابان ولیعصر، پلاک ۱",
      methodLabel: "پیشتاز",
      targetRequested: true,
      targets: [targetFact({ readinessLabel: "در انتظار ثبت کد رهگیری بسته", readinessTone: "ready" })],
      targetPackageNumber: "MP-260R1PACK",
    }),
  );
  // The package is the single physical action owner, so its exact number is shown — not a seller summary.
  assert.ok(html.includes('data-testid="admin-order-operations-target-package"'));
  assert.ok(html.includes("MP-260R1PACK"));
  // A Created package awaiting tracking is real work: the readiness line must never say "nothing left".
  assert.ok(html.includes("در انتظار ثبت کد رهگیری بسته"));
  assert.ok(!html.includes("کار فیزیکی باقی نمانده"));
});

test("mounted: authority decides the mutation entry; retained attempt stays reconcilable", async () => {
  const detail = baseDetail();
  const calls: string[] = [];
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    calls.push(String(input));
    return new Response(JSON.stringify({ actions: [], lineCapabilities: [], sellerCapabilities: [] }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  clearOperationalGroupAttempt(CHECKOUT, GROUP_KEY, null);
  try {
    const mounted = await mount(
      view({ state: "ready", targetRequested: true, targets: [targetFact()], children: panel(detail) }),
    );
    await waitFor(() => qq(mounted.container, `[data-testid='admin-order-operational-group-${GROUP_KEY}']`).length === 1);
    await flush();

    // Authorised + consolidate-eligible: exactly one prepare entry (the ONE action owner).
    assert.equal(
      qq(mounted.container, `[data-testid='admin-order-operational-prepare-${GROUP_KEY}']`).length,
      1,
      "an authorised operator must reach the group workflow from this view",
    );
    // The other group of the same order is not claimed as the target.
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operations-target-${OTHER_KEY}']`).length, 0);
    // No inline quantity/checkbox selection on the normal page (253/254 contract preserved): the reviewed
    // quantities exist only inside the group dialogue, which is closed until the operator opens it.
    const qtyInputs = qq(mounted.container, "[data-testid^='admin-order-operational-qty-']");
    for (const input of qtyInputs) {
      const dialog = input.closest("dialog");
      assert.ok(dialog, "a review quantity must live inside the group dialogue");
      assert.equal((dialog as HTMLDialogElement).open, false, "the dialogue must stay closed until asked");
    }
    assert.equal(qq(mounted.container, "input[type='checkbox']").length, 0, "no inline selection control");
    await mounted.unmount();

    // Unauthorised viewer: the operational card stays readable, but offers no mutation entry.
    const denied = baseDetail({
      operationalDeliveryGroups: (detail.operationalDeliveryGroups ?? []).map((group) => ({
        ...group,
        actorMayManageOperations: false,
      })),
    });
    const second = await mount(
      view({ state: "ready", targetRequested: true, targets: [targetFact()], children: panel(denied) }),
    );
    await waitFor(() => qq(second.container, `[data-testid='admin-order-operational-group-${GROUP_KEY}']`).length === 1);
    await flush();
    assert.equal(qq(second.container, `[data-testid='admin-order-operational-prepare-${GROUP_KEY}']`).length, 0);
    assert.equal(qq(second.container, `[data-testid='admin-order-operational-no-authority-${GROUP_KEY}']`).length, 1);
    await second.unmount();

    // A retained unresolved attempt keeps its reconcile entry reachable inside the SAME view.
    const written = writeOperationalGroupAttempt({
      occurrenceId: "occ-retained-260",
      checkoutId: CHECKOUT,
      operationalGroupKey: GROUP_KEY,
      scopeKey: "",
      fingerprint: "fp-260",
      attempts: 1,
      lastFailedAtUtc: null,
      lastErrorCode: "order.operation.unavailable",
      members: ["g1"],
      selections: [{ orderLineId: "ol1", quantity: 2 }],
      reuseShipmentIds: [],
      packingMode: "common",
    });
    assert.equal(written, true, "the retained envelope must be durable for this proof");
    const third = await mount(
      view({ state: "ready", targetRequested: true, targets: [targetFact()], children: panel(detail) }),
    );
    await waitFor(() => qq(third.container, `[data-testid='admin-order-operational-group-${GROUP_KEY}']`).length === 1);
    await flush();
    assert.equal(
      qq(third.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length,
      1,
      "the recorded attempt must stay reconcilable from the shared view",
    );
    assert.equal(qq(third.container, `[data-testid='admin-order-operational-prepare-${GROUP_KEY}']`).length, 0);
    await third.unmount();
    assert.ok(calls.length >= 1, "the panel reads its own capabilities from the server");
  } finally {
    globalThis.fetch = originalFetch;
    clearOperationalGroupAttempt(CHECKOUT, GROUP_KEY, null);
  }
});
