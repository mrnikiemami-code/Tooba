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
import {
  clearOperationalGroupAttempt,
  readOperationalGroupAttempt,
  resolveOperationalGroupAttempt,
} from "./operational-group-occurrence-store.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../lib/admin/admin-result.ts";

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
    const occurrence = posts[0].body.operationalGroupOccurrenceId;
    assert.equal(typeof occurrence, "string");
    assert.match(
      String(occurrence),
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
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

test("mounted: retry keeps the prebound occurrence; a completed run mints a new one", async () => {
  let attempts = 0;
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    attempts += 1;
    if (attempts === 1) {
      return postResponse(409, { error: { code: "order.operational_group.workflow_pending" } });
    }
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
    const prepare = () => q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']");
    await click(prepare());
    await waitFor(() => qq(mounted.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    await flush();
    await click(prepare());
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts[0].body.operationalGroupOccurrenceId, posts[1].body.operationalGroupOccurrenceId);

    // The successful second attempt finished the occurrence: the next deliberate run is a new one.
    await flush();
    await click(prepare());
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 3);
    const third = calls.filter((c) => c.method === "POST")[2];
    assert.notEqual(third.body.operationalGroupOccurrenceId, posts[0].body.operationalGroupOccurrenceId);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: remount after a lost response reuses the same prebound occurrence", async () => {
  let attempt = 0;
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    attempt += 1;
    if (attempt === 1) throw new TypeError("network down");
    return postResponse(200, { ok: true, code: "prepare_operational_group" });
  });
  try {
    const first = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(first.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await click(q(first.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => qq(first.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    await first.unmount();

    // Remount: the reviewed request and its occurrence survive the mount boundary.
    const second = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(second.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await click(q(second.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts[0].body.operationalGroupOccurrenceId, posts[1].body.operationalGroupOccurrenceId);
    assert.deepEqual(posts[0].body.selections, posts[1].body.selections);
    await second.unmount();
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

// ---------------------------------------------------------------- TB-253-R4

const R4_SCOPE = "admin:r4-viewer";
const R4_CHECKOUT = "r4-c1";

function resetR4Attempt(groupKey = "mp-post-combined") {
  clearOperationalGroupAttempt(R4_CHECKOUT, groupKey, R4_SCOPE);
  clearOperationalGroupAttempt(R4_CHECKOUT, groupKey, "admin:other-viewer");
  sessionStorage.removeItem(`tb.operational-group-attempt.v2:${R4_CHECKOUT}:__group__:${groupKey}`);
}

/** Pins the viewer used by the panel's occurrence scope for the duration of a test. */
function withR4Viewer(): () => void {
  const previous = window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY);
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "r4-viewer");
  return () => {
    if (previous === null) window.localStorage.removeItem(ADMIN_ACTOR_STORAGE_KEY);
    else window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, previous);
  };
}

test("R4 mounted: the COMPLETE prebound envelope is persisted before the first POST", async () => {
  resetR4Attempt();
  const restoreViewer = withR4Viewer();
  let envelopeAtPost: ReturnType<typeof readOperationalGroupAttempt> = null;
  let bodyAtPost: Record<string, unknown> | null = null;
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    envelopeAtPost = readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", R4_SCOPE);
    bodyAtPost = call.body;
    return postResponse(500, { error: { code: "order.operation.failed" } });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: R4_CHECKOUT },
        checkoutId: R4_CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    const prepare = q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']");
    await click(prepare);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 1);
    await flush();

    // The envelope (not just the id) existed BEFORE the request was sent.
    assert.ok(envelopeAtPost, "envelope must be recorded before the first mutation");
    assert.deepEqual([...envelopeAtPost!.members].sort(), ["g1", "g2"]);
    assert.deepEqual(envelopeAtPost!.selections, [
      { orderLineId: "ol1", quantity: 2 },
      { orderLineId: "ol2", quantity: 1 },
    ]);
    assert.deepEqual(envelopeAtPost!.reuseShipmentIds, []);
    assert.ok(envelopeAtPost!.fingerprint.length > 0);
    assert.equal(envelopeAtPost!.occurrenceId, bodyAtPost!.operationalGroupOccurrenceId);

    // 5xx keeps the SAME occurrence and offers an explicit reconcile of it.
    const retained = readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", R4_SCOPE);
    assert.ok(retained);
    assert.equal(retained!.occurrenceId, envelopeAtPost!.occurrenceId);
    assert.equal(retained!.attempts, 1);
    assert.ok(retained!.lastFailedAtUtc);
    assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 1);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});

test("R4/R5 mounted: retrying the SAME envelope reconciles it; a CHANGED envelope is blocked while unresolved", async () => {
  resetR4Attempt();
  const restoreViewer = withR4Viewer();
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(500, { error: { code: "order.operation.failed" } });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: R4_CHECKOUT },
        checkoutId: R4_CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    const prepare = q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']");
    await click(prepare);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 1);
    await flush();
    const first = calls.filter((c) => c.method === "POST")[0];

    // Unchanged retry: exactly the recorded occurrence.
    await click(prepare);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const second = calls.filter((c) => c.method === "POST")[1];
    assert.equal(second.body.operationalGroupOccurrenceId, first.body.operationalGroupOccurrenceId);
    assert.deepEqual(second.body.selections, first.body.selections);
    assert.deepEqual(second.body.reuseShipmentIds, first.body.reuseShipmentIds);

    // R5: a CHANGED review while the recorded operation is unresolved must NOT mint a second
    // operation — the normal Create path is blocked, not merely the extra reconcile button.
    const recordedBefore = readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", R4_SCOPE);
    assert.ok(recordedBefore);
    await setInputValue(q(mounted.container, "[data-testid='admin-order-operational-qty-ol1']"), "1");
    await flush();
    assert.equal(
      (prepare as HTMLButtonElement).disabled,
      true,
      "the normal create path must be disabled while an unresolved envelope exists",
    );
    assert.equal(
      qq(mounted.container, "[data-testid='admin-order-operational-retained-block-mp-post-combined']").length,
      1,
    );
    await click(prepare);
    await flush();
    assert.equal(calls.filter((c) => c.method === "POST").length, 2);

    // The retained request is untouched: no remint, no overwrite.
    const recordedAfter = readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", R4_SCOPE);
    assert.equal(recordedAfter!.occurrenceId, recordedBefore!.occurrenceId);
    assert.deepEqual(recordedAfter!.selections, recordedBefore!.selections);
    assert.equal(recordedAfter!.fingerprint, recordedBefore!.fingerprint);

    // The same guard holds at the resolver boundary, so no caller can bypass the JSX state.
    const blocked = resolveOperationalGroupAttempt({
      checkoutId: R4_CHECKOUT,
      operationalGroupKey: "mp-post-combined",
      scopeKey: R4_SCOPE,
      request: {
        members: ["g1", "g2"],
        selections: [
          { orderLineId: "ol1", quantity: 1 },
          { orderLineId: "ol2", quantity: 1 },
        ],
        reuseShipmentIds: [],
      },
    });
    assert.equal(blocked.blockedByRetainedAttempt, true);
    assert.equal(blocked.changedRequest, true);
    assert.equal(blocked.occurrenceId, recordedBefore!.occurrenceId);
    const stillRecorded = readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", R4_SCOPE);
    assert.deepEqual(stillRecorded!.selections, recordedBefore!.selections);
    assert.equal(calls.filter((c) => c.method === "POST").length, 2);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});

test("R4 mounted: an undefined outcome (cancellation) keeps the exact envelope", async () => {
  resetR4Attempt();
  const observed: string[] = [];
  const detail = baseDetail();
  const mounted = await mount(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
      occurrenceScopeKey: R4_SCOPE,
      onPrepareOperationalGroup: (_group, request) => {
        observed.push(request.operationalGroupOccurrenceId);
        return undefined;
      },
    }),
  );
  await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
  await flush();
  const afterFirst = readOperationalGroupAttempt("c1", "mp-post-combined", R4_SCOPE);
  assert.ok(afterFirst, "cancelled/undefined outcome must not clear the attempt");
  assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 1);
  await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
  await flush();
  assert.equal(observed.length, 2);
  assert.equal(observed[0], observed[1]);
  await mounted.unmount();
  clearOperationalGroupAttempt("c1", "mp-post-combined", R4_SCOPE);
});

test("R4 store: an occurrence recorded by one viewer scope is never inherited by another", () => {
  const request = {
    members: ["g1", "g2"],
    selections: [
      { orderLineId: "ol1", quantity: 2 },
      { orderLineId: "ol2", quantity: 1 },
    ],
    reuseShipmentIds: [],
  };
  const a = resolveOperationalGroupAttempt({
    checkoutId: R4_CHECKOUT,
    operationalGroupKey: "mp-post-combined",
    scopeKey: "admin:viewer-a",
    request,
  });
  const aAgain = resolveOperationalGroupAttempt({
    checkoutId: R4_CHECKOUT,
    operationalGroupKey: "mp-post-combined",
    scopeKey: "admin:viewer-a",
    request,
  });
  assert.equal(aAgain.occurrenceId, a.occurrenceId);
  assert.equal(aAgain.resumingRecordedAttempt, true);

  const b = resolveOperationalGroupAttempt({
    checkoutId: R4_CHECKOUT,
    operationalGroupKey: "mp-post-combined",
    scopeKey: "admin:viewer-b",
    request,
  });
  assert.notEqual(b.occurrenceId, a.occurrenceId);
  assert.equal(b.resumingRecordedAttempt, false);
  assert.equal(readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", "admin:viewer-b")!.occurrenceId, b.occurrenceId);
  assert.equal(readOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", "admin:viewer-a")!.occurrenceId, a.occurrenceId);

  clearOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", "admin:viewer-a");
  clearOperationalGroupAttempt(R4_CHECKOUT, "mp-post-combined", "admin:viewer-b");
});

test("R4 mounted: partial server progress + lost response keeps the envelope even when create is no longer ready", async () => {
  const checkout = "r4-c2";
  const groupKey = "mp-post-combined";
  clearOperationalGroupAttempt(checkout, groupKey, R4_SCOPE);
  const restoreViewer = withR4Viewer();
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    const index = calls.filter((c) => c.method === "POST").length;
    if (index === 1) throw new TypeError("connection reset");
    return postResponse(200, { ok: true, code: "prepare_operational_group" });
  });
  try {
    const first = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: checkout },
        checkoutId: checkout,
      }),
    );
    await waitFor(() => qq(first.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await click(q(first.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => qq(first.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    await first.unmount();
    const recorded = readOperationalGroupAttempt(checkout, groupKey, R4_SCOPE);
    assert.ok(recorded);
    const firstPost = calls.filter((c) => c.method === "POST")[0];

    // Partial server progress consumed the packed quantities AND create is no longer ready: the
    // reviewed envelope must still be reconcilable, not silently re-derived from remaining = 0.
    const progressed = baseDetail();
    progressed.checkoutId = checkout;
    progressed.operationalDeliveryGroups = progressed.operationalDeliveryGroups!.map((group) => ({
      ...group,
      consolidateEligible: false,
      consolidateBlockReasonFa: "مقدار بسته‌بندی‌شدهٔ باقی‌مانده صفر است.",
      members: group.members.map((member) => ({
        ...member,
        lines: (member.lines ?? []).map((line) => ({ ...line, remainingPackedQuantity: 0 })),
      })),
    }));
    const second = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: progressed,
        checkoutId: checkout,
      }),
    );
    await waitFor(() => qq(second.container, "[data-testid^='admin-order-operational-group-']").length === 1);
    assert.equal(qq(second.container, "[data-testid^='admin-order-operational-prepare-']").length, 0);
    assert.equal(qq(second.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 1);
    const reconcile = q(second.container, "[data-testid='admin-order-operational-reconcile-retry-mp-post-combined']");
    assert.ok(reconcile);

    await click(reconcile);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts.length, 2);
    assert.equal(posts[1].body.operationalGroupOccurrenceId, firstPost.body.operationalGroupOccurrenceId);
    assert.deepEqual(posts[1].body.selections, firstPost.body.selections);
    assert.deepEqual(posts[1].body.operationalGroupShippingGroupIds, firstPost.body.operationalGroupShippingGroupIds);

    // Completed reconciliation clears the retained envelope; no implicit extra parcel/occurrence.
    assert.equal(qq(second.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 0);
    assert.equal(readOperationalGroupAttempt(checkout, groupKey, R4_SCOPE), null);
    assert.equal(calls.filter((c) => c.method === "POST").length, 2);
    await second.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    clearOperationalGroupAttempt(checkout, groupKey, R4_SCOPE);
  }
});

// ---------------------------------------------------------------- TB-253-R5

test("R5 mounted: a viewer switch cannot act on the previous viewer's retained refs", async () => {
  const checkout = "r5-c1";
  const groupKey = "mp-post-combined";
  clearOperationalGroupAttempt(checkout, groupKey, "admin:viewer-a");
  clearOperationalGroupAttempt(checkout, groupKey, "admin:viewer-b");
  const restoreViewer = withR4Viewer();
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    throw new TypeError("connection reset");
  });
  try {
    window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "viewer-a");
    const first = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: checkout },
        checkoutId: checkout,
      }),
    );
    await waitFor(() => qq(first.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    await click(q(first.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => qq(first.container, "[data-testid='admin-order-operational-workflow-error']").length === 1);
    await flush();
    const viewerARecord = readOperationalGroupAttempt(checkout, groupKey, "admin:viewer-a");
    assert.ok(viewerARecord);
    const firstPost = calls.filter((c) => c.method === "POST")[0];
    await first.unmount();

    // Another admin session sees NO retained envelope and no reconcile action for viewer A.
    window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "viewer-b");
    const second = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: checkout },
        checkoutId: checkout,
      }),
    );
    await waitFor(() => qq(second.container, "[data-testid^='admin-order-operational-prepare-']").length === 1);
    assert.equal(qq(second.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 0);
    assert.equal(qq(second.container, "[data-testid='admin-order-operational-retained-block-mp-post-combined']").length, 0);
    await click(q(second.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const secondPost = calls.filter((c) => c.method === "POST")[1];
    assert.notEqual(secondPost.body.operationalGroupOccurrenceId, firstPost.body.operationalGroupOccurrenceId);
    assert.equal(readOperationalGroupAttempt(checkout, groupKey, "admin:viewer-a")!.occurrenceId, viewerARecord!.occurrenceId);
    await second.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    clearOperationalGroupAttempt(checkout, groupKey, "admin:viewer-a");
    clearOperationalGroupAttempt(checkout, groupKey, "admin:viewer-b");
  }
});

test("R5 mounted: only a proven no-claim refusal releases the retained envelope", async () => {
  const detail = baseDetail();
  const scope = R4_SCOPE;
  const outcomes: Array<{ completed: false; noClaimRefusalCode?: string | null } | undefined> = [];
  const seen: string[] = [];
  clearOperationalGroupAttempt("c1", "mp-post-combined", scope);
  const mounted = await mount(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
      occurrenceScopeKey: scope,
      onPrepareOperationalGroup: (_group, request) => {
        seen.push(request.operationalGroupOccurrenceId);
        const next = outcomes.shift();
        return next;
      },
    }),
  );
  try {
    // Transport/unknown outcome (undefined) keeps the envelope.
    outcomes.push(undefined);
    await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await flush();
    assert.ok(readOperationalGroupAttempt("c1", "mp-post-combined", scope));
    assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 1);

    // A tcp/5xx-style failure carries no code, so it can never release the envelope.
    outcomes.push({ completed: false, noClaimRefusalCode: null });
    await click(q(mounted.container, "[data-testid='admin-order-operational-reconcile-retry-mp-post-combined']"));
    await flush();
    assert.ok(readOperationalGroupAttempt("c1", "mp-post-combined", scope));
    assert.equal(seen.length, 2);
    assert.equal(seen[0], seen[1]);

    // A typed pre-claim refusal proves there is no claim: explicit safe transition back to a review.
    outcomes.push({ completed: false, noClaimRefusalCode: "order.operational_group.workflow_group_foreign" });
    await click(q(mounted.container, "[data-testid='admin-order-operational-reconcile-retry-mp-post-combined']"));
    await flush();
    assert.equal(readOperationalGroupAttempt("c1", "mp-post-combined", scope), null);
    assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-reconcile-mp-post-combined']").length, 0);

    // The next deliberate run is a genuinely new occurrence.
    outcomes.push({ completed: true });
    await click(q(mounted.container, "[data-testid='admin-order-operational-prepare-mp-post-combined']"));
    await flush();
    assert.equal(seen.length, 4);
    assert.notEqual(seen[3], seen[0]);
    assert.equal(readOperationalGroupAttempt("c1", "mp-post-combined", scope), null);
  } finally {
    await mounted.unmount();
    clearOperationalGroupAttempt("c1", "mp-post-combined", scope);
  }
});
