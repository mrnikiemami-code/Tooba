/**
 * TB-270-R2 — the OWN dispatch plan of an existing resend/replacement parcel.
 *
 * Two stages, one workflow: Fulfillment already created the physical parcel; this surface only makes it
 * dispatch-authorized. The tests below prove the reviewed intent (occurrence key + chosen customer slot) is
 * retained BEFORE the first POST, that a refusal/remount reconciles the SAME key instead of minting a second
 * review, that a blocker is shown as a blocker (never a invented calendar), and that a stored plan is displayed
 * exactly as the server reports it.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-shipment-plan-270r2.mount.test.ts
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
import { AdminShipmentPlanModal } from "./admin-shipment-plan-modal.tsx";
import { AdminOrderOperationalDeliverySection } from "./admin-order-operational-delivery-section.tsx";
import {
  clearShipmentPlanAttempt,
  readShipmentPlanAttempt,
  writeShipmentPlanAttempt,
} from "./admin-shipment-plan-attempt.ts";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import type {
  AdminOrderOperationRequest,
  AdminShipmentDispatchPlanOption,
  AdminShipmentDispatchPlanPreview,
} from "./admin-order-operations.ts";

ensureDom();

const CHECKOUT = "checkout-270r2";
const SHIPMENT = "sh-plan-1";
const VIEWER = "admin:viewer-a";

const CONFIRM = "[data-testid='admin-shipment-plan-confirm']";
const ERROR = "[data-testid='admin-shipment-plan-error']";
const RECONCILE = "[data-testid='admin-shipment-plan-reconcile']";
const BLOCKER = "[data-testid='admin-shipment-plan-blocker']";
const CONFIRMED = "[data-testid='admin-shipment-plan-confirmed']";
const EMPTY = "[data-testid='admin-shipment-plan-empty']";

function option(date: string, opens: string, closes: string): AdminShipmentDispatchPlanOption {
  return {
    receivingDate: date,
    receivingOpensAt: opens,
    receivingClosesAt: closes,
    receivingTimeZone: "Asia/Tehran",
    handoffDate: date,
    handoffOpensAt: "08:00",
    handoffClosesAt: "20:00",
    handoffTimeZone: "Asia/Tehran",
    preparationStartUtc: `${date}T03:00:00Z`,
    transferDepartureUtc: `${date}T06:00:00Z`,
    handoffDeadlineUtc: `${date}T16:30:00Z`,
  };
}

function preview(overrides: Partial<AdminShipmentDispatchPlanPreview> = {}): AdminShipmentDispatchPlanPreview {
  return {
    checkoutId: CHECKOUT,
    sellerOrderId: "so1",
    fulfillmentId: "f1",
    shipmentId: SHIPMENT,
    shippingGroupId: "g1",
    attemptKind: "resend",
    sourceShipmentId: "sh-origin",
    damagedResolutionId: null,
    replacementReservationId: null,
    sourceFingerprint: "tb270r2-line=1",
    readinessSource: "return_receipt",
    readinessAtUtc: "2026-11-14T09:00:00Z",
    methodCode: "store_courier",
    methodLabel: "پیشتاز",
    snapshotId: "snap-1",
    draftId: "draft-1",
    sourceRevision: 7,
    formatVersion: 4,
    areaGroupId: "area-1",
    rateRowId: "rate-1",
    servedAtUtc: "2026-11-15T06:00:00Z",
    reviewFingerprint: "fingerprint-1",
    options: [option("2026-11-16", "09:00", "10:00"), option("2026-11-16", "11:00", "12:00"), option("2026-11-17", "09:00", "10:00")],
    confirmedPlan: null,
    blockerCode: null,
    blockerFa: null,
    missingCapturedInputs: null,
    schedulingReasonCode: "shipping.scheduling.ok",
    ...overrides,
  };
}

function ok<T>(data: T) {
  return { state: "ok" as const, data, status: 200 };
}

function resetScope(viewer: string = VIEWER, shipmentId: string = SHIPMENT) {
  clearShipmentPlanAttempt({ viewerScopeKey: viewer, checkoutId: CHECKOUT, shipmentId });
}

test("mounted: a feasible slot is deliberately selected, the COMPLETE reviewed envelope is written BEFORE the POST, and success clears it", async () => {
  resetScope();
  const posts: AdminOrderOperationRequest[] = [];
  const storedAtPost: Array<ReturnType<typeof readShipmentPlanAttempt>> = [];
  const { container, unmount } = await mount(
    createElement(AdminShipmentPlanModal, {
      open: true,
      onClose: () => undefined,
      checkoutId: CHECKOUT,
      shipmentId: SHIPMENT,
      viewerScopeKey: VIEWER,
      onCompleted: () => undefined,
      previewLoader: async () => ok(preview()),
      operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
        posts.push(body);
        // The durable envelope must already exist when the request leaves the browser.
        storedAtPost.push(
          readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT }),
        );
        return ok({});
      },
    }),
  );

  await waitFor(() => qq(container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  assert.equal((q(container, CONFIRM) as HTMLButtonElement).disabled, true, "no date may be preselected");

  await click(q(container, "[data-testid='admin-shipment-plan-slot-2026-11-16-11:00']"));
  await flush();
  assert.equal((q(container, CONFIRM) as HTMLButtonElement).disabled, false);
  assert.match(q(container, "[data-testid='admin-shipment-plan-deadlines']").textContent ?? "", /۲۰:۰۰/);

  await click(q(container, CONFIRM));
  await waitFor(() => posts.length === 1);

  assert.equal(posts[0]!.code, "plan_shipment_dispatch");
  assert.equal(posts[0]!.shipmentId, SHIPMENT);
  assert.equal(posts[0]!.dispatchPlanReceivingDate, "2026-11-16");
  assert.equal(posts[0]!.dispatchPlanReceivingOpensAt, "11:00");
  assert.equal(posts[0]!.dispatchPlanReceivingClosesAt, "12:00");
  assert.ok(posts[0]!.idempotencyKey, "the reviewed occurrence key is sent");
  const atPost = storedAtPost[0]!;
  assert.equal(atPost.occurrenceKey, posts[0]!.idempotencyKey, "the key was persisted before the POST");
  assert.deepEqual(
    JSON.parse(atPost.requestBody ?? "{}"),
    posts[0],
    "the EXACT reviewed request body was persisted before it was sent",
  );
  assert.equal(atPost.reviewFingerprint, "fingerprint-1", "the server-issued review context is retained");
  assert.equal(atPost.receivingTimeZone, "Asia/Tehran", "the reviewed receiving zone is retained");
  assert.equal(atPost.uncertain, false, "a request that has not failed is not uncertain");
  await waitFor(() => readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT }) === null);

  await unmount();
});

test("mounted: a DETERMINISTIC refusal ENDS that review — nothing is retained, and the next review uses a NEW key", async () => {
  resetScope();
  const keys: string[] = [];
  let status = 409;
  const props = {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ok(preview()),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      keys.push(body.idempotencyKey ?? "");
      return status === 200
        ? ok({})
        : { state: "error" as const, data: null, status, message: "order.shipment_plan.selection_expired" };
    },
  };

  const first = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(first.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(first.container, CONFIRM));
  await waitFor(() => keys.length === 1);
  await waitFor(() => Boolean(first.container.querySelector(ERROR)));
  assert.ok(
    first.container.querySelector("[data-testid='admin-shipment-plan-prior-outcome']"),
    "the exact prior outcome is shown to the operator",
  );
  assert.equal(
    first.container.querySelector("[data-testid='admin-shipment-plan-frozen']"),
    null,
    "a deterministic refusal is NOT presented as an unknown outcome",
  );
  assert.equal(
    readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT }),
    null,
    "the refused occurrence is ended, so its key/body cannot be reused",
  );
  await first.unmount();

  // A remount starts from the owning preview alone: no retained slot, no frozen retry.
  const second = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => qq(second.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  assert.equal(
    (q(second.container, CONFIRM) as HTMLButtonElement).disabled,
    true,
    "nothing is preselected after a refused review",
  );
  assert.equal(
    second.container.querySelector("[data-testid='admin-shipment-plan-retry-same']"),
    null,
    "there is no frozen retry for a deterministic refusal",
  );
  await click(q(second.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  status = 200;
  await click(q(second.container, CONFIRM));
  await waitFor(() => keys.length === 2);
  assert.notEqual(keys[1], keys[0], "a fresh review never reuses an already-refused key");
  await second.unmount();
});

test("mounted: an UNKNOWN outcome (transport loss) freezes the EXACT reviewed body and re-sends it byte-for-byte", async () => {
  resetScope();
  const bodies: string[] = [];
  let fail = true;
  const props = {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ok(preview()),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      bodies.push(JSON.stringify(body));
      return fail
        ? { state: "error" as const, data: null, status: 0, message: "host-unreachable" }
        : ok({});
    },
  };

  const first = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(first.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(first.container, CONFIRM));
  await waitFor(() => bodies.length === 1);
  await waitFor(() => Boolean(first.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  assert.ok(first.container.querySelector(RECONCILE), "the operator is told the exact request is preserved");
  assert.ok(
    first.container.querySelector("[data-testid='admin-shipment-plan-retry-same']"),
    "the only offered action is re-sending the same reviewed request",
  );
  const recorded = readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT });
  assert.equal(recorded?.uncertain, true, "the outcome is recorded as genuinely unknown");
  assert.deepEqual(JSON.parse(recorded?.requestBody ?? "{}"), JSON.parse(bodies[0]!), "the exact body is retained");
  await first.unmount();

  // A remount must NOT mint a second review: the retained body is the only thing that can be sent again.
  fail = false;
  const second = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => Boolean(second.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  assert.equal(
    second.container.querySelector("[data-testid='admin-shipment-plan-confirm']"),
    null,
    "no fresh confirmation is possible while the outcome is unknown",
  );
  assert.equal(
    qq(second.container, "[data-testid^='admin-shipment-plan-slot-']").every(
      (node) => (node as HTMLButtonElement).disabled,
    ),
    true,
    "no different slot can be chosen while frozen",
  );
  await click(q(second.container, "[data-testid='admin-shipment-plan-retry-same']"));
  await waitFor(() => bodies.length === 2);
  assert.equal(bodies[1], bodies[0], "the retry re-sends the identical reviewed body (same key, same slot)");
  await second.unmount();
});

test("mounted: a stock-pending blocker shows the blocker and NO calendar (no invented date)", async () => {
  resetScope();
  let posts = 0;
  const { container, unmount } = await mount(
    createElement(AdminShipmentPlanModal, {
      open: true,
      onClose: () => undefined,
      checkoutId: CHECKOUT,
      shipmentId: SHIPMENT,
      viewerScopeKey: VIEWER,
      previewLoader: async () => ok(preview({
        options: [],
        blockerCode: "order.shipment_plan.replacement_stock_pending",
        blockerFa: "موجودی سالم این جایگزینی هنوز تخصیص نیافته است؛ تا آماده‌شدن رزرو، تاریخی ارائه نمی‌شود.",
        schedulingReasonCode: null,
      })),
      operationExecutor: async () => {
        posts += 1;
        return ok({});
      },
    }),
  );

  await waitFor(() => Boolean(container.querySelector(BLOCKER)));
  assert.match(q(container, BLOCKER).textContent ?? "", /موجودی سالم/);
  assert.equal(qq(container, "[data-testid^='admin-shipment-plan-slot-']").length, 0);
  assert.equal((q(container, CONFIRM) as HTMLButtonElement).disabled, true);
  await click(q(container, CONFIRM)).catch(() => undefined);
  await flush();
  assert.equal(posts, 0, "a blocked parcel never posts a plan");
  await unmount();
});

test("mounted: a confirmed plan is displayed as stored history and cannot be re-confirmed", async () => {
  resetScope();
  const confirmed = {
    planId: "plan-1",
    occurrenceKey: "occurrence-1",
    receivingDate: "2026-11-20",
    receivingOpensAt: "09:00",
    receivingClosesAt: "10:00",
    receivingTimeZone: "Asia/Tehran",
    handoffDate: "2026-11-18",
    handoffOpensAt: "08:00",
    handoffClosesAt: "20:00",
    handoffTimeZone: "Asia/Tehran",
    preparationStartUtc: "2026-11-18T03:00:00Z",
    transferDepartureUtc: "2026-11-18T06:00:00Z",
    handoffDeadlineUtc: "2026-11-18T16:30:00Z",
    preparationMinutes: 120,
    handoffMinutes: 60,
    transportWorkingDays: 2,
    policyVersion: 3,
    confirmedByUserId: "user-1",
    confirmedAtUtc: "2026-11-15T06:00:00Z",
  };
  const { container, unmount } = await mount(
    createElement(AdminShipmentPlanModal, {
      open: true,
      onClose: () => undefined,
      checkoutId: CHECKOUT,
      shipmentId: SHIPMENT,
      viewerScopeKey: VIEWER,
      previewLoader: async () => ok(preview({ options: [], confirmedPlan: confirmed })),
      operationExecutor: async () => {
        throw new Error("a stored plan must never be posted again");
      },
    }),
  );

  await waitFor(() => Boolean(container.querySelector(CONFIRMED)));
  const text = q(container, CONFIRMED).textContent ?? "";
  assert.match(text, /برنامهٔ تأییدشدهٔ خودش/);
  assert.match(text, /به وقت تهران/);
  assert.match(text, /[۰-۹]/);
  assert.doesNotMatch(text, /2026-11-20/);
  assert.equal((q(container, CONFIRM) as HTMLButtonElement).disabled, true);
  assert.equal(qq(container, "[data-testid^='admin-shipment-plan-slot-']").length, 0);
  await unmount();
});

test("mounted: without a durable envelope the review refuses to POST instead of risking a second plan", async () => {
  resetScope();
  const original = (globalThis as { sessionStorage?: Storage }).sessionStorage;
  let posts = 0;
  const throwing = {
    getItem: () => {
      throw new Error("storage refused");
    },
    setItem: () => {
      throw new Error("storage refused");
    },
    removeItem: () => {
      throw new Error("storage refused");
    },
  } as unknown as Storage;

  try {
    (globalThis as { sessionStorage?: Storage }).sessionStorage = throwing;
    const { container, unmount } = await mount(
      createElement(AdminShipmentPlanModal, {
        open: true,
        onClose: () => undefined,
        checkoutId: CHECKOUT,
        shipmentId: SHIPMENT,
        viewerScopeKey: VIEWER,
        previewLoader: async () => ok(preview()),
        operationExecutor: async () => {
          posts += 1;
          return ok({});
        },
      }),
    );
    await waitFor(() => qq(container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
    await click(q(container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
    await flush();
    await click(q(container, CONFIRM));
    await waitFor(() => Boolean(container.querySelector(ERROR)));
    assert.match(q(container, ERROR).textContent ?? "", /ذخیرهٔ محلی/);
    assert.equal(posts, 0, "no plan is posted without a verified local envelope");
    await unmount();
  } finally {
    (globalThis as { sessionStorage?: Storage }).sessionStorage = original;
  }
});

test("store: a reviewed occurrence is scoped to viewer + checkout + exact parcel", () => {
  resetScope();
  const written = writeShipmentPlanAttempt({
    scope: { viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT },
    occurrenceKey: "occurrence-a",
    receivingDate: "2026-11-16",
    receivingOpensAt: "09:00",
    receivingClosesAt: "10:00",
  });
  assert.equal(written?.durable, true);
  assert.equal(
    readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT })?.occurrenceKey,
    "occurrence-a",
  );
  assert.equal(
    readShipmentPlanAttempt({ viewerScopeKey: "admin:viewer-b", checkoutId: CHECKOUT, shipmentId: SHIPMENT }),
    null,
    "another viewer never inherits the review",
  );
  assert.equal(
    readShipmentPlanAttempt({ viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: "sh-other" }),
    null,
    "another parcel never inherits the review",
  );
  resetScope();
});

/** Minimal detail: one marketplace group covering two sellers, each with one linked attempt parcel. */
function detailWithLinkedAttempts(): AdminOrderDetail {
  const mapped = mapAdminOrderDetail({
    checkoutId: "plan-c1",
    reference: "PLAN-R2",
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
        shipments: [{
          shipmentId: "sh1",
          status: "Created",
          carrierDisplayName: "پست",
          shippingGroupId: "g1",
          shippingMethodCode: "post",
          itemCount: 1,
          lines: [{ orderLineId: "ol1", quantity: 1 }],
          trackingReference: null,
          activePackageNumber: null,
          packageLockedReasonFa: null,
          resendOfShipmentId: "sh0",
        }],
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
        shipments: [{
          shipmentId: "sh2",
          status: "Created",
          carrierDisplayName: "پست",
          shippingGroupId: "g2",
          shippingMethodCode: "post",
          itemCount: 1,
          lines: [{ orderLineId: "ol2", quantity: 1 }],
          trackingReference: null,
          activePackageNumber: null,
          packageLockedReasonFa: null,
        }],
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
        operationalGroupKey: "plan-group",
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
            lines: [{ orderLineId: "ol1", title: "شال آرمان", remainingPackedQuantity: 2, packedQuantity: 2 }],
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
            lines: [{ orderLineId: "ol2", title: "پیراهن نیلوا", remainingPackedQuantity: 1, packedQuantity: 1 }],
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
  return mapped!;
}

test("static render: the SAME Created card offers planning only, and the stored plan is shown as the server reported it", () => {
  const planned: string[] = [];
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail: detailWithLinkedAttempts(),
      pendingCode: null,
      actorMayManage: true,
      parcelCapabilities: (_seller: unknown, shipment: { shipmentId: string }) => ({
        canCancel: true,
        canTrack: false,
        canDispatch: false,
        canDeliver: false,
        canResend: false,
        canResolveDamagedGoods: false,
        canCreateCourierReturn: false,
        canPlanDispatch: shipment.shipmentId === "sh1",
      }),
      shipmentPlansByShipmentId: {
        sh1: {
          shipmentId: "sh1",
          planId: "plan-1",
          attemptKind: "resend",
          occurrenceKey: "occurrence-1",
          receivingDate: "2026-11-20",
          receivingOpensAt: "09:00",
          receivingClosesAt: "10:00",
          receivingTimeZone: "Asia/Tehran",
          handoffDate: "2026-11-18",
          handoffOpensAt: "08:00",
          handoffClosesAt: "20:00",
          handoffTimeZone: "Asia/Tehran",
          preparationStartUtc: "2026-11-18T03:00:00Z",
          transferDepartureUtc: "2026-11-18T06:00:00Z",
          handoffDeadlineUtc: "2026-11-18T16:30:00Z",
          preparationMinutes: 120,
          handoffMinutes: 60,
          transportWorkingDays: 2,
          policyVersion: 3,
          confirmedAtUtc: "2026-11-15T06:00:00Z",
        },
      },
      onParcelPlan: (_seller: unknown, shipmentId: string) => planned.push(shipmentId),
      onCreateShipment: () => undefined,
      onPrepareOperationalGroup: () => undefined,
    } as never),
  );

  // The unplanned/unscheduled parcel: one planning step, no dispatch claim.
  assert.ok(html.includes("admin-order-shipment-plan-sh1"));
  assert.match(html, /زمان‌بندی ارسال/);
  // The stored plan is Persian/Jalali with an accepted-zone label and never a raw ISO date.
  const planBlock = html.slice(html.indexOf("admin-order-parcel-plan-sh1"));
  assert.ok(planBlock.length > 0);
  const block = planBlock.slice(0, planBlock.indexOf("</div>"));
  assert.match(block, /موعد تحویل مشتری/);
  assert.match(block, /به وقت تهران/);
  assert.match(block, /[۰-۹]/);
  assert.doesNotMatch(block, /2026-11-20T|2026-11-20/);
  assert.equal(planned.length, 0, "rendering never plans on its own");
});

test("TB-270-R3: an EXPIRED old slot still reconciles the exact retained intent and offers no new choice", async () => {
  resetScope();
  const bodies: string[] = [];
  let fail = true;
  let loaderCalls = 0;
  const props = {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => {
      loaderCalls += 1;
      // The review offered 09:00; every later read of the OWNING calendar no longer offers that slot.
      return ok(preview({
        options: loaderCalls === 1
          ? [option("2026-11-16", "09:00", "10:00"), option("2026-11-17", "09:00", "10:00")]
          : [option("2026-11-18", "11:00", "12:00")],
      }));
    },
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      bodies.push(JSON.stringify(body));
      return fail
        ? { state: "error" as const, data: null, status: 0, message: "host-unreachable" }
        : ok({});
    },
  };

  const first = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(first.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(first.container, CONFIRM));
  await waitFor(() => bodies.length === 1);
  await waitFor(() => Boolean(first.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  await first.unmount();

  fail = false;
  const second = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => Boolean(second.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  assert.equal(
    qq(second.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']").length,
    0,
    "the reviewed slot is genuinely gone from the freshly generated calendar",
  );
  assert.equal(
    second.container.querySelector(CONFIRM),
    null,
    "a fresh confirmation is impossible while the old intent is unresolved",
  );
  await click(q(second.container, "[data-testid='admin-shipment-plan-retry-same']"));
  await waitFor(() => bodies.length === 2);
  assert.equal(
    bodies[1],
    bodies[0],
    "the expired slot reconciles the SAME reviewed request instead of silently selecting another",
  );
  await second.unmount();
});

test("TB-270-R3: a late answer for the PREVIOUS target never writes the newer surface", async () => {
  resetScope();
  let releaseStale: ((value: null) => void) | null = null;
  const staleGate = new Promise<null>((resolve) => {
    releaseStale = resolve;
  });
  const staleLoader = async () => {
    await staleGate;
    return ok(preview({
      shipmentId: "sh-stale",
      options: [option("2026-12-01", "09:00", "10:00")],
    }));
  };
  const freshLoader = async () => ok(preview({
    shipmentId: "sh-fresh",
    options: [option("2026-11-20", "11:00", "12:00")],
  }));
  const base = {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    viewerScopeKey: VIEWER,
    operationExecutor: async () => ok({}),
  };

  const tree = await mount(createElement(AdminShipmentPlanModal, {
    ...base,
    shipmentId: "sh-stale",
    previewLoader: staleLoader,
  }));
  // The operator retargets the SAME surface to another parcel before the first read answers.
  await tree.rerender(createElement(AdminShipmentPlanModal, {
    ...base,
    shipmentId: "sh-fresh",
    previewLoader: freshLoader,
  }));
  await waitFor(() => qq(tree.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);

  releaseStale!(null);
  await flush();
  assert.equal(
    qq(tree.container, "[data-testid='admin-shipment-plan-slot-2026-12-01-09:00']").length,
    0,
    "the previous parcel's calendar never reaches the new target",
  );
  assert.ok(
    q(tree.container, "[data-testid='admin-shipment-plan-slot-2026-11-20-11:00']"),
    "the surface keeps showing its own parcel's calendar",
  );
  await tree.unmount();
});

// ---------------------------------------------------------------- TB-270-R4: first-send durability + lifecycle fence

const SCOPE = { viewerScopeKey: VIEWER, checkoutId: CHECKOUT, shipmentId: SHIPMENT };

function planBody(key: string, date = "2026-11-16", opens = "09:00", closes = "10:00"): string {
  return JSON.stringify({
    code: "plan_shipment_dispatch",
    shipmentId: SHIPMENT,
    idempotencyKey: key,
    dispatchPlanReceivingDate: date,
    dispatchPlanReceivingOpensAt: opens,
    dispatchPlanReceivingClosesAt: closes,
    dispatchPlanReviewFingerprint: "fingerprint-1",
  });
}

test("TB-270-R4: the FIRST post is unresolved the instant it may be sent — an unmount before any catch recovers it exactly", async () => {
  resetScope();
  const posts: string[] = [];
  // The first POST never answers: no catch, no finally, no epoch guard ever runs for it.
  const props = {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ok(preview()),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      posts.push(JSON.stringify(body));
      return new Promise<never>(() => undefined);
    },
  };

  const first = await mount(createElement(AdminShipmentPlanModal, props));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(first.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(first.container, CONFIRM));
  await waitFor(() => posts.length === 1);

  const pending = readShipmentPlanAttempt(SCOPE);
  assert.ok(pending, "the exact intent is durable BEFORE the executor answers");
  assert.equal(pending.occurrenceKey, JSON.parse(posts[0]!).idempotencyKey);
  assert.equal(pending.requestBody, posts[0], "the exact serialized body is what was stored");
  assert.equal(pending.uncertain, false, "nothing caught an exception: this is not the uncertain branch");
  assert.equal(pending.unresolved, true, "it is unresolved from the instant the request may have been sent");

  // The surface simply disappears while the POST is still in flight.
  await first.unmount();
  assert.equal(
    readShipmentPlanAttempt(SCOPE)?.occurrenceKey,
    pending.occurrenceKey,
    "an unmount can neither discard nor replace the unresolved intent",
  );

  // Remount with a DIFFERENT offered slot: the retained intent wins and a fresh selection cannot overwrite it.
  let closed = 0;
  const retried: string[] = [];
  const second = await mount(createElement(AdminShipmentPlanModal, {
    ...props,
    previewLoader: async () => ok(preview({ options: [option("2026-11-17", "11:00", "12:00")] })),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      retried.push(JSON.stringify(body));
      return ok({});
    },
    onClose: () => { closed += 1; },
  }));
  await waitFor(() => Boolean(second.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  assert.equal(
    second.container.querySelector(CONFIRM),
    null,
    "a fresh confirmation cannot be minted while the first intent is unresolved",
  );
  assert.equal(
    qq(second.container, "[data-testid='admin-shipment-plan-slot-2026-11-17-11:00']").length,
    1,
    "the newer calendar is visible but not selectable as a new intent",
  );
  await click(q(second.container, "[data-testid='admin-shipment-plan-retry-same']"));
  await waitFor(() => retried.length === 1);
  assert.equal(retried[0], posts[0], "the retry is the EXACT serialized intent, not a rebuilt selection");
  await waitFor(() => closed === 1);
  await waitFor(() => readShipmentPlanAttempt(SCOPE) === null);
  await second.unmount();
});

test("TB-270-R4: an unresolved intent survives a FAILED preview, and a TB-270-R3 record is never assumed never-sent", async () => {
  resetScope();
  // A record written by the previous version: exact body retained, uncertain:false, no `unresolved` field at all.
  const legacy = planBody("legacy-key");
  sessionStorage.setItem(
    `tb.shipment-plan-attempt.v1:${CHECKOUT}:${VIEWER}:${SHIPMENT}`,
    JSON.stringify({
      viewerScopeKey: VIEWER,
      checkoutId: CHECKOUT,
      shipmentId: SHIPMENT,
      occurrenceKey: "legacy-key",
      receivingDate: "2026-11-16",
      receivingOpensAt: "09:00",
      receivingClosesAt: "10:00",
      receivingTimeZone: "Asia/Tehran",
      attempts: 1,
      lastFailedAtUtc: null,
      lastMessageFa: null,
      requestBody: legacy,
      reviewFingerprint: "fingerprint-1",
      uncertain: false,
    }),
  );

  const bodies: string[] = [];
  const mounted = await mount(createElement(AdminShipmentPlanModal, {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ({ state: "error" as const, data: null, status: 0, message: "host-unreachable" }),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      bodies.push(JSON.stringify(body));
      return ok({});
    },
  }));

  // Even with no calendar at all, the unresolved intent is the only action on this surface.
  await waitFor(() => Boolean(mounted.container.querySelector("[data-testid='admin-shipment-plan-retry-same']")));
  assert.equal(mounted.container.querySelector(CONFIRM), null);
  await click(q(mounted.container, "[data-testid='admin-shipment-plan-retry-same']"));
  await waitFor(() => bodies.length === 1);
  assert.equal(bodies[0], legacy, "the previous version's body is retried byte-for-byte, never rebuilt");
  await mounted.unmount();
});

test("TB-270-R4: a late answer after unmount/retarget neither clears a newer intent nor closes a newer surface", async () => {
  resetScope();
  let release: ((value: unknown) => void) | null = null;
  const gate = new Promise<unknown>((resolve) => { release = resolve; });
  const started: string[] = [];
  let closed = 0;
  const props = {
    open: true,
    onClose: () => { closed += 1; },
    checkoutId: CHECKOUT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ok(preview()),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      started.push(JSON.stringify(body));
      return gate as never;
    },
  };

  const first = await mount(createElement(AdminShipmentPlanModal, { ...props, shipmentId: SHIPMENT }));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(first.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(first.container, CONFIRM));
  await waitFor(() => started.length === 1);

  // The surface is retargeted to another parcel while the first POST is still open.
  await first.rerender(createElement(AdminShipmentPlanModal, { ...props, shipmentId: "sh-plan-2" }));
  await waitFor(() => qq(first.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);

  // A NEWER reviewed intent is recorded for the ORIGINAL scope (as the next surface would).
  const newer = planBody("newer-key", "2026-11-17", "11:00", "12:00");
  writeShipmentPlanAttempt({ scope: SCOPE, occurrenceKey: "newer-key", receivingDate: "2026-11-17", requestBody: newer, unresolved: true });

  release!(ok({}));
  await flush();
  assert.equal(closed, 0, "the old answer can never close the surface that replaced it");
  assert.ok(
    q(first.container, "[data-testid^='admin-shipment-plan-slot-']"),
    "the newer target keeps its own calendar",
  );
  assert.equal(
    readShipmentPlanAttempt(SCOPE)?.occurrenceKey,
    "newer-key",
    "a late success for an older occurrence cannot clear a newer stored intent",
  );
  await first.unmount();
  resetScope();
});

test("TB-270-R4: a conflict/authorization answer keeps the exact intent recoverable instead of assuming no write", async () => {
  resetScope();
  const bodies: string[] = [];
  let answer: { state: "ok"; data: unknown; status: number } | { state: "error"; data: null; status: number; message: string } =
    { state: "error", data: null, status: 409, message: "order.shipment_plan.conflict" };
  const mounted = await mount(createElement(AdminShipmentPlanModal, {
    open: true,
    onClose: () => undefined,
    checkoutId: CHECKOUT,
    shipmentId: SHIPMENT,
    viewerScopeKey: VIEWER,
    previewLoader: async () => ok(preview()),
    operationExecutor: async (_checkoutId: string, body: AdminOrderOperationRequest) => {
      bodies.push(JSON.stringify(body));
      return answer;
    },
  }));

  await waitFor(() => qq(mounted.container, "[data-testid^='admin-shipment-plan-slot-']").length >= 1);
  await click(q(mounted.container, "[data-testid='admin-shipment-plan-slot-2026-11-16-09:00']"));
  await flush();
  await click(q(mounted.container, CONFIRM));
  await waitFor(() => bodies.length === 1);

  // A conflict is NOT a proven no-write refusal: the exact intent stays frozen and retryable.
  await waitFor(() => Boolean(mounted.container.querySelector("[data-testid='admin-shipment-plan-frozen']")));
  const kept = readShipmentPlanAttempt(SCOPE);
  assert.ok(kept, "the reviewed intent is preserved");
  assert.equal(kept!.unresolved, true);
  assert.equal(kept!.requestBody, bodies[0]);
  assert.equal(mounted.container.querySelector("[data-testid='admin-shipment-plan-prior-outcome']"), null);

  // The explicit retry repeats the identical bytes; an authoritative replay answer then clears THAT occurrence.
  answer = ok({ replayed: true });
  await click(q(mounted.container, "[data-testid='admin-shipment-plan-retry-same']"));
  await waitFor(() => bodies.length === 2);
  assert.equal(bodies[1], bodies[0], "the conflict never caused a new intent or a new key");
  await waitFor(() => readShipmentPlanAttempt(SCOPE) === null);
  await mounted.unmount();
});
