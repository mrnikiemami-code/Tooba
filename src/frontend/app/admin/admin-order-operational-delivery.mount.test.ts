/**
 * TB-253-R2 … TB-254 — operational delivery group workflow.
 *
 * TB-254 (user visual rejection «این چیه … اون تیک‌ها چیه؟»): the normal delivery card is DISPLAY of
 * server-owned groups + their parcels. Reviewed quantities and the explicit parcel-reuse choice live
 * in the ONE titled Marketplace group dialog, and exactly one surface owns parcel actions.
 *
 * Static structure is asserted with renderToStaticMarkup; behaviour (dialog review, single-flight,
 * refusal retention, lost response, viewer scope, one parcel owner) with a real jsdom + react-dom
 * mount of the shipped panel.
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
  ensureDom,
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
const GROUP_KEY = "mp-post-combined";

// Server-render of the dialog needs the global React that the mount harness installs.
ensureDom();

const PREPARE_SELECTOR = `[data-testid='admin-order-operational-prepare-${GROUP_KEY}']`;
const DIALOG_ROOT_SELECTOR = `[data-testid='admin-order-operational-dialog-${GROUP_KEY}']`;
const DIALOG_SUBMIT_SELECTOR = `[data-testid='admin-order-operational-dialog-submit-${GROUP_KEY}']`;
const RECONCILE_RETRY_SELECTOR = `[data-testid='admin-order-operational-reconcile-retry-${GROUP_KEY}']`;
const RETAINED_OPEN_SELECTOR = `[data-testid='admin-order-operational-retained-${GROUP_KEY}']`;
const WORKFLOW_ERROR_SELECTOR = "[data-testid='admin-order-operational-workflow-error']";
const QTY_OL1_SELECTOR = "[data-testid='admin-order-operational-qty-ol1']";
const REUSE_SH1_SELECTOR = "[data-testid='admin-order-operational-reuse-parcel-sh1']";

/** Opaque handle of the one dialog surface, so "inside the dialog" is asserted, not assumed. */
function insideDialog(element: Element | null): boolean {
  return Boolean(element?.closest("dialog"));
}

/** Strips dialog bodies so "the normal page has no inline selection control" is measurable. */
function outsideDialogs(html: string): string {
  return html.replace(/<dialog[^>]*>[\s\S]*?<\/dialog>/g, "<dialog></dialog>");
}

async function openGroupDialog(container: HTMLElement): Promise<void> {
  await waitFor(() => qq(container, PREPARE_SELECTOR).length === 1);
  await click(q(container, PREPARE_SELECTOR));
  await flush();
  assert.ok(qq(container, DIALOG_ROOT_SELECTOR).length === 1, "the group dialog must be the review surface");
}

async function submitGroupDialog(container: HTMLElement): Promise<void> {
  await click(q(container, DIALOG_SUBMIT_SELECTOR));
  await flush();
}

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

/**
 * The panel's default viewer scope is the anonymous group scope (`DEFAULT_ADMIN_ACTOR_ID` is empty).
 * Tests share one jsdom sessionStorage, so an attempt retained by a previous failing test must be
 * cleared explicitly instead of leaking into the next mount.
 */
function resetGroupScope(checkout: string, scopeKey: string | null = null) {
  clearOperationalGroupAttempt(checkout, GROUP_KEY, scopeKey);
  sessionStorage.removeItem(`tb.operational-group-attempt.v2:${checkout}:__group__:${GROUP_KEY}`);
}

function operationsPage(
  extraActions: Array<Record<string, unknown>> = [],
) {
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
        ...extraActions,
      ],
      lineCapabilities: [],
      sellerCapabilities: [],
    }),
    { status: 200, headers: { "content-type": "application/json" } },
  );
}

function shipmentAction(code: string, sellerOrderId: string, shipmentId: string) {
  return {
    code,
    labelFa: code,
    labelEn: code,
    sellerOrderId,
    fulfillmentId: `f-${sellerOrderId}`,
    shipmentId,
    requiredPermission: "order.handle",
    requiresConfirm: false,
    confirmMessageFa: null,
  };
}

function postResponse(status: number, payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "content-type": "application/json" },
  });
}

// ---------------------------------------------------------------- TB-254 display

test("TB-254 static render: the group card is display-only and owns no inline selection control", () => {
  const detail = withParcels(baseDetail(), false);
  let prepareCalls = 0;
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      onCreateShipment: () => undefined,
      onPrepareOperationalGroup: () => {
        prepareCalls += 1;
      },
    }),
  );

  // Readable delivery facts and the group action.
  assert.match(html, /ارسال سفارش/);
  assert.ok(html.includes(`admin-order-operational-group-${GROUP_KEY}`));
  assert.match(html, /data-cross-seller="true"/);
  assert.match(html, /آرمان/);
  assert.match(html, /نیلوا/);
  assert.match(html, /شال آرمان/);
  assert.match(html, /پیراهن نیلوا/);
  assert.match(html, /هزینهٔ ارسال/);
  assert.match(html, /admin-order-operational-parcel-sh1/);
  assert.match(html, /admin-order-operational-prepare-mp-post-combined/);
  assert.match(html, /آماده‌سازی ارسال گروه/);
  assert.doesNotMatch(html, /store-alpha|Asia\/Tehran|tenant/i);

  // The normal page (everything outside the dialog body) has no checkbox and no quantity field.
  const page = outsideDialogs(html);
  assert.doesNotMatch(page, /type="checkbox"/i);
  assert.doesNotMatch(page, /inputmode="decimal"/i);

  // …and the review controls live inside the one titled dialog, not on the card.
  assert.match(html, /<dialog/);
  assert.match(html, /admin-order-operational-dialog-mp-post-combined/);
  assert.match(html, /admin-order-operational-reuse-parcel-sh1/);
  assert.match(html, /admin-order-operational-qty-ol1/);
  assert.ok(insideDialogMarkup(html, "admin-order-operational-qty-ol1"), "quantity input must sit inside the dialog");
  assert.ok(insideDialogMarkup(html, "admin-order-operational-reuse-parcel-sh1"), "reuse choice must sit inside the dialog");
  assert.equal(prepareCalls, 0);
});

/** True when `testId` occurs after the opening <dialog> and before its close. */
function insideDialogMarkup(html: string, testId: string): boolean {
  const dialogStart = html.indexOf("<dialog");
  const dialogEnd = html.indexOf("</dialog>", dialogStart);
  if (dialogStart < 0 || dialogEnd < 0) return false;
  const index = html.indexOf(`data-testid="${testId}"`);
  return index > dialogStart && index < dialogEnd;
}

test("TB-254 mounted: the operational section has no selection control outside the dialog", async () => {
  resetGroupScope(CHECKOUT);
  installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(200, { ok: true });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: withParcels(baseDetail(), false),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, PREPARE_SELECTOR).length === 1);
    const section = q(mounted.container, "[data-testid='admin-order-operational-delivery']");
    const inlineCheckboxes = qq(section, "input[type='checkbox']").filter((el) => !insideDialog(el));
    const inlineQuantity = qq(section, "input[inputmode='decimal']").filter((el) => !insideDialog(el));
    assert.equal(inlineCheckboxes.length, 0, "no unexplained checkbox on the delivery card");
    assert.equal(inlineQuantity.length, 0, "no quantity field on the delivery card");

    // The same controls exist, but only inside the dialog surface.
    assert.ok(insideDialog(q(mounted.container, REUSE_SH1_SELECTOR)));
    assert.ok(insideDialog(q(mounted.container, QTY_OL1_SELECTOR)));
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("TB-254 mounted: a group-covered parcel has exactly ONE owner; an orphan parcel stays in the seller list", async () => {
  resetGroupScope(CHECKOUT);
  installFetch((call) => {
    if (call.method === "GET") {
      return operationsPage([
        shipmentAction("assign_tracking", "so1", "sh1"),
        shipmentAction("assign_tracking", "so2", "sh2"),
      ]);
    }
    return postResponse(200, { ok: true });
  });
  try {
    const detail = withParcels(baseDetail(), false);
    // A legacy parcel of so1 that no operational group covers.
    detail.sellerOrders = detail.sellerOrders.map((seller) =>
      seller.id === "so1"
        ? {
          ...seller,
          shipments: [
            ...(seller.shipments ?? []),
            {
              shipmentId: "shx",
              status: "Created",
              carrierDisplayName: "پست",
              shippingGroupId: null,
              shippingMethodCode: "post",
              itemCount: 1,
              lines: [{ orderLineId: "ol1", quantity: 1 }],
              trackingReference: null,
              activePackageNumber: null,
              packageLockedReasonFa: null,
            },
          ],
        }
        : seller,
    );

    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail,
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, PREPARE_SELECTOR).length === 1);
    await flush();

    // Group parcels appear once, inside the operational card.
    assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-parcel-sh1']").length, 1);
    assert.equal(qq(mounted.container, "[data-testid='admin-order-operational-parcel-sh2']").length, 1);
    // …and not a second time as a seller-aside card.
    assert.equal(qq(mounted.container, "[data-testid='admin-order-shipment-card-sh1']").length, 0);
    assert.equal(qq(mounted.container, `[data-testid='admin-order-seller-shipments-so2']`).length, 0);

    // The one permitted next action is rendered exactly once — by the group card.
    const tracking = qq(mounted.container, "[data-testid='admin-order-shipment-assign-tracking-sh1']");
    assert.equal(tracking.length, 1);
    assert.ok(tracking[0].closest("[data-testid='admin-order-operational-delivery']"));

    // A parcel outside every group keeps the seller list (nothing is silently hidden).
    const aside = q(mounted.container, "[data-testid='admin-order-seller-shipments-so1']");
    assert.equal(qq(aside, "[data-testid='admin-order-shipment-card-shx']").length, 1);
    assert.equal(qq(aside, "[data-testid='admin-order-shipment-card-sh1']").length, 0);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("TB-254 mounted: a Store-owned group with existing parcels has no selection control and one tracking action", async () => {
  resetGroupScope(CHECKOUT);
  installFetch((call) => {
    if (call.method === "GET") return operationsPage([shipmentAction("assign_tracking", "so1", "sh1")]);
    return postResponse(200, { ok: true });
  });
  try {
    const detail = withParcels(baseDetail(), false);
    detail.operationalDeliveryGroups = [{
      ...detail.operationalDeliveryGroups![0],
      isCrossSellerMarketplace: false,
      responsibilityLabelFa: "ارسال توسط فروشگاه",
      members: [detail.operationalDeliveryGroups![0].members[0]],
      feeBreakdown: [detail.operationalDeliveryGroups![0].feeBreakdown[0]],
    }];
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail,
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(mounted.container, `[data-testid='admin-order-operational-group-${GROUP_KEY}']`).length === 1);
    await flush();

    const section = q(mounted.container, "[data-testid='admin-order-operational-delivery']");
    // No consolidation surface at all for a store-owned group…
    assert.equal(qq(section, "dialog").length, 0);
    assert.equal(qq(section, `[data-testid='admin-order-operational-dialog-${GROUP_KEY}']`).length, 0);
    // …and therefore no checkbox / quantity field / consolidate entry.
    assert.equal(qq(section, "input[type='checkbox']").length, 0);
    assert.equal(qq(section, "input[inputmode='decimal']").length, 0);
    assert.equal(qq(section, "[data-testid^='admin-order-operational-prepare-']").length, 0);
    // The parcel's permitted next action is still reachable exactly once.
    assert.equal(qq(section, "[data-testid='admin-order-shipment-assign-tracking-sh1']").length, 1);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

// ---------------------------------------------------------------- TB-253-R2 behaviour via the dialog

test("mounted: reviewed quantities produce exactly one group request across two sellers", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(mounted.container);
    assert.equal((q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement).value, "2");
    await submitGroupDialog(mounted.container);
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

test("mounted: same-tick double submit performs exactly one mutation", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(mounted.container);
    await clickBurst(q(mounted.container, DIALOG_SUBMIT_SELECTOR), 2);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    release?.();
    await flush();
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: retry keeps the prebound occurrence; a completed run mints a new one", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await waitFor(() => qq(mounted.container, WORKFLOW_ERROR_SELECTOR).length === 1);
    await flush();

    // Unresolved: the dialog is read-only and only replays the recorded envelope.
    assert.equal(qq(mounted.container, PREPARE_SELECTOR).length, 0);
    assert.equal((q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement).disabled, true);
    await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts[0].body.operationalGroupOccurrenceId, posts[1].body.operationalGroupOccurrenceId);

    // The successful second attempt finished the occurrence: the next deliberate run is a new one.
    await flush();
    await waitFor(() => qq(mounted.container, PREPARE_SELECTOR).length === 1);
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 3);
    const third = calls.filter((c) => c.method === "POST")[2];
    assert.notEqual(third.body.operationalGroupOccurrenceId, posts[0].body.operationalGroupOccurrenceId);
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: remount after a lost response reuses the same prebound occurrence", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(first.container);
    await submitGroupDialog(first.container);
    await waitFor(() => qq(first.container, WORKFLOW_ERROR_SELECTOR).length === 1);
    await first.unmount();

    // Remount: the reviewed request and its occurrence survive the mount boundary.
    const second = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: baseDetail(),
        checkoutId: CHECKOUT,
      }),
    );
    await waitFor(() => qq(second.container, RETAINED_OPEN_SELECTOR).length === 1);
    assert.equal(qq(second.container, PREPARE_SELECTOR).length, 0);
    await click(q(second.container, RETAINED_OPEN_SELECTOR));
    await flush();
    await click(q(second.container, RECONCILE_RETRY_SELECTOR));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts[0].body.operationalGroupOccurrenceId, posts[1].body.operationalGroupOccurrenceId);
    assert.deepEqual(posts[0].body.selections, posts[1].body.selections);
    await second.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: known refusal is shown inside the dialog and the reviewed quantities are retained", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(mounted.container);
    await setInputValue(q(mounted.container, QTY_OL1_SELECTOR), "2");
    await submitGroupDialog(mounted.container);
    await waitFor(() => qq(mounted.container, WORKFLOW_ERROR_SELECTOR).length === 1);
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);
    const qty = q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement;
    assert.equal(qty.value, "2");
    assert.equal(qty.disabled, true, "the recorded envelope is read-only until it is resolved");
    assert.ok(insideDialog(q(mounted.container, WORKFLOW_ERROR_SELECTOR)));
    await mounted.unmount();
  } finally {
    restoreFetch();
  }
});

test("mounted: lost response surfaces an inline error; remount offers no second allocation", async () => {
  resetGroupScope(CHECKOUT);
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
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await waitFor(() => qq(mounted.container, WORKFLOW_ERROR_SELECTOR).length === 1);
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
    assert.equal(qq(reconciled.container, PREPARE_SELECTOR).length, 0);
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
        onCreateShipment: () => undefined,
        onPrepareOperationalGroup: () => undefined,
      }),
    );
    assert.equal(qq(mounted.container, PREPARE_SELECTOR).length, 0);
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
  const here = dirname(fileURLToPath(import.meta.url));
  const panel = readFileSync(join(here, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /groupWorkflowInFlight/);
  assert.match(panel, /useRef\(false\)/);
  assert.match(panel, /prepare_operational_group/);
  assert.match(panel, /try\s*\{/);
  assert.match(panel, /finally\s*\{/);
  assert.match(panel, /completed: false/);
  assert.match(panel, /errorFa/);
  assert.doesNotMatch(panel, /onConsolidateGroup/);

  // TB-254: the section is display-only; the dialog owns the review inputs.
  const section = readFileSync(join(here, "admin-order-operational-delivery-section.tsx"), "utf8");
  assert.match(section, /AdminOperationalGroupWorkflowDialog/);
  assert.doesNotMatch(section, /type="checkbox"/);
  assert.doesNotMatch(section, /inputMode="decimal"/);
  assert.doesNotMatch(section, /<input/);

  // One parcel owner: group-covered shipments are excluded from the seller list.
  assert.match(panel, /operationalGroupShippingGroupIds/);
  assert.match(panel, /orphanShipments/);
});

// ---------------------------------------------------------------- TB-253-R4

const R4_SCOPE = "admin:r4-viewer";
const R4_CHECKOUT = "r4-c1";

function resetR4Attempt(groupKey = GROUP_KEY) {
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
    envelopeAtPost = readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE);
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
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
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
    const retained = readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE);
    assert.ok(retained);
    assert.equal(retained!.occurrenceId, envelopeAtPost!.occurrenceId);
    assert.equal(retained!.attempts, 1);
    assert.ok(retained!.lastFailedAtUtc);
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});

test("R4/R5 mounted: retrying the SAME envelope reconciles it; a CHANGED review is blocked while unresolved", async () => {
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
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 1);
    await flush();
    const first = calls.filter((c) => c.method === "POST")[0];

    // Unchanged retry: exactly the recorded occurrence.
    await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const second = calls.filter((c) => c.method === "POST")[1];
    assert.equal(second.body.operationalGroupOccurrenceId, first.body.operationalGroupOccurrenceId);
    assert.deepEqual(second.body.selections, first.body.selections);
    assert.deepEqual(second.body.reuseShipmentIds, first.body.reuseShipmentIds);

    // R5: the normal Create path is GONE while the recorded operation is unresolved — no remint,
    // and the recorded envelope is read-only in the UI.
    const recordedBefore = readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE);
    assert.ok(recordedBefore);
    assert.equal(qq(mounted.container, PREPARE_SELECTOR).length, 0);
    assert.equal((q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement).disabled, true);
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);

    // The same guard holds at the resolver boundary, so no caller can bypass the JSX state.
    const blocked = resolveOperationalGroupAttempt({
      checkoutId: R4_CHECKOUT,
      operationalGroupKey: GROUP_KEY,
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
    const stillRecorded = readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE);
    assert.deepEqual(stillRecorded!.selections, recordedBefore!.selections);
    assert.equal(calls.filter((c) => c.method === "POST").length, 2);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});

test("R4 mounted: closing and reopening the dialog never cancels or re-mints the recorded operation", async () => {
  resetR4Attempt();
  const restoreViewer = withR4Viewer();
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    return postResponse(503, { error: { code: "order.operation.unavailable" } });
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: R4_CHECKOUT },
        checkoutId: R4_CHECKOUT,
      }),
    );
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 1);
    await flush();
    const recorded = readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE);
    assert.ok(recorded);

    // Closing the dialog (explicit «بستن») neither cancels nor clears the occurrence.
    await click(q(mounted.container, `[data-testid='admin-order-operational-dialog-close-${GROUP_KEY}']`));
    await flush();
    assert.equal(qq(mounted.container, PREPARE_SELECTOR).length, 0);
    assert.ok(readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, R4_SCOPE));
    assert.equal(calls.filter((c) => c.method === "POST").length, 1);

    // Reopening from «پیگیری نتیجه» restores the same read-only envelope.
    await click(q(mounted.container, RETAINED_OPEN_SELECTOR));
    await flush();
    assert.equal((q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement).disabled, true);
    await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts[1].body.operationalGroupOccurrenceId, recorded!.occurrenceId);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});

test("R4 mounted: an undefined outcome (cancellation) keeps the exact envelope", async () => {
  clearOperationalGroupAttempt("c1", GROUP_KEY, R4_SCOPE);
  const observed: string[] = [];
  const detail = baseDetail();
  const mounted = await mount(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      onCreateShipment: () => undefined,
      occurrenceScopeKey: R4_SCOPE,
      onPrepareOperationalGroup: (_group, request) => {
        observed.push(request.operationalGroupOccurrenceId);
        return undefined;
      },
    }),
  );
  await openGroupDialog(mounted.container);
  await submitGroupDialog(mounted.container);
  await flush();
  const afterFirst = readOperationalGroupAttempt("c1", GROUP_KEY, R4_SCOPE);
  assert.ok(afterFirst, "cancelled/undefined outcome must not clear the attempt");
  assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);
  await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
  await flush();
  assert.equal(observed.length, 2);
  assert.equal(observed[0], observed[1]);
  await mounted.unmount();
  clearOperationalGroupAttempt("c1", GROUP_KEY, R4_SCOPE);
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
    operationalGroupKey: GROUP_KEY,
    scopeKey: "admin:viewer-a",
    request,
  });
  const aAgain = resolveOperationalGroupAttempt({
    checkoutId: R4_CHECKOUT,
    operationalGroupKey: GROUP_KEY,
    scopeKey: "admin:viewer-a",
    request,
  });
  assert.equal(aAgain.occurrenceId, a.occurrenceId);
  assert.equal(aAgain.resumingRecordedAttempt, true);

  const b = resolveOperationalGroupAttempt({
    checkoutId: R4_CHECKOUT,
    operationalGroupKey: GROUP_KEY,
    scopeKey: "admin:viewer-b",
    request,
  });
  assert.notEqual(b.occurrenceId, a.occurrenceId);
  assert.equal(b.resumingRecordedAttempt, false);
  assert.equal(readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, "admin:viewer-b")!.occurrenceId, b.occurrenceId);
  assert.equal(readOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, "admin:viewer-a")!.occurrenceId, a.occurrenceId);

  clearOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, "admin:viewer-a");
  clearOperationalGroupAttempt(R4_CHECKOUT, GROUP_KEY, "admin:viewer-b");
});

test("R4 mounted: partial server progress + lost response keeps the envelope even when create is no longer ready", async () => {
  const checkout = "r4-c2";
  clearOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE);
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
    await openGroupDialog(first.container);
    await submitGroupDialog(first.container);
    await waitFor(() => qq(first.container, WORKFLOW_ERROR_SELECTOR).length === 1);
    await first.unmount();
    const recorded = readOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE);
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
    assert.equal(qq(second.container, PREPARE_SELECTOR).length, 0);
    assert.equal(qq(second.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);

    // The only offered path is the explicit reconcile of the recorded envelope.
    await click(q(second.container, RETAINED_OPEN_SELECTOR));
    await flush();
    await click(q(second.container, RECONCILE_RETRY_SELECTOR));
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const posts = calls.filter((c) => c.method === "POST");
    assert.equal(posts.length, 2);
    assert.equal(posts[1].body.operationalGroupOccurrenceId, firstPost.body.operationalGroupOccurrenceId);
    assert.deepEqual(posts[1].body.selections, firstPost.body.selections);
    assert.deepEqual(posts[1].body.operationalGroupShippingGroupIds, firstPost.body.operationalGroupShippingGroupIds);

    // Completed reconciliation clears the retained envelope; no implicit extra parcel/occurrence.
    assert.equal(qq(second.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 0);
    assert.equal(readOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE), null);
    assert.equal(calls.filter((c) => c.method === "POST").length, 2);
    await second.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    clearOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE);
  }
});

// ---------------------------------------------------------------- TB-253-R5

test("R5 mounted: a viewer switch cannot act on the previous viewer's retained refs", async () => {
  const checkout = "r5-c1";
  clearOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-a");
  clearOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-b");
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
    await openGroupDialog(first.container);
    await submitGroupDialog(first.container);
    await waitFor(() => qq(first.container, WORKFLOW_ERROR_SELECTOR).length === 1);
    await flush();
    const viewerARecord = readOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-a");
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
    await waitFor(() => qq(second.container, PREPARE_SELECTOR).length === 1);
    assert.equal(qq(second.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 0);
    await openGroupDialog(second.container);
    await submitGroupDialog(second.container);
    await waitFor(() => calls.filter((c) => c.method === "POST").length === 2);
    await flush();
    const secondPost = calls.filter((c) => c.method === "POST")[1];
    assert.notEqual(secondPost.body.operationalGroupOccurrenceId, firstPost.body.operationalGroupOccurrenceId);
    assert.equal(
      readOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-a")!.occurrenceId,
      viewerARecord!.occurrenceId,
    );
    await second.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    clearOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-a");
    clearOperationalGroupAttempt(checkout, GROUP_KEY, "admin:viewer-b");
  }
});

// ---------------------------------------------------------------- TB-253-R6

test("R6 state: a refused operation keeps the exact envelope and blocks a changed review", async () => {
  const detail = baseDetail();
  const scope = R4_SCOPE;
  type SentEnvelope = {
    occurrenceId: string;
    members: string[];
    selections: Array<{ orderLineId: string; quantity: number }>;
    reuseShipmentIds: string[];
  };
  const sent: SentEnvelope[] = [];
  const outcomes: Array<boolean | { completed: boolean } | undefined> = [];
  clearOperationalGroupAttempt("c1", GROUP_KEY, scope);
  const mounted = await mount(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      onCreateShipment: () => undefined,
      occurrenceScopeKey: scope,
      onPrepareOperationalGroup: (_group, request) => {
        sent.push({
          occurrenceId: request.operationalGroupOccurrenceId,
          members: [...request.operationalGroupShippingGroupIds],
          selections: request.selections.map((s) => ({ ...s })),
          reuseShipmentIds: [...request.reuseShipmentIds],
        });
        return outcomes.shift();
      },
    }),
  );
  try {
    // First operation is refused (a denial carries no proof that the occurrence was never claimed).
    outcomes.push({ completed: false });
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await flush();
    const recorded = readOperationalGroupAttempt("c1", GROUP_KEY, scope);
    assert.ok(recorded, "the refused envelope must be retained");
    const occurrenceId = recorded!.occurrenceId;
    assert.equal(sent.length, 1);
    assert.equal(sent[0].occurrenceId, occurrenceId);
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);

    // A changed review may not start a new operation and may not touch the recorded envelope.
    assert.equal(qq(mounted.container, PREPARE_SELECTOR).length, 0);
    const qty = q(mounted.container, QTY_OL1_SELECTOR) as HTMLInputElement;
    assert.equal(qty.disabled, true);
    assert.equal(qty.value, "2");
    assert.equal(sent.length, 1, "a changed review must not send a new request");
    const afterChange = readOperationalGroupAttempt("c1", GROUP_KEY, scope);
    assert.ok(afterChange);
    assert.equal(afterChange!.occurrenceId, occurrenceId);
    assert.deepEqual(afterChange!.selections, recorded!.selections);
    assert.deepEqual(afterChange!.members, recorded!.members);
    assert.equal(afterChange!.fingerprint, recorded!.fingerprint);

    // The exact recorded envelope is still reconcilable.
    outcomes.push({ completed: false });
    await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
    await flush();
    assert.equal(sent.length, 2);
    assert.equal(sent[1].occurrenceId, occurrenceId);
    assert.deepEqual(sent[1].selections, recorded!.selections);
    assert.deepEqual(sent[1].members, recorded!.members);
    assert.ok(readOperationalGroupAttempt("c1", GROUP_KEY, scope));

    // Only an authoritative completion clears it.
    outcomes.push({ completed: true });
    await click(q(mounted.container, RECONCILE_RETRY_SELECTOR));
    await flush();
    assert.equal(readOperationalGroupAttempt("c1", GROUP_KEY, scope), null);
    assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 0);

    // The next deliberate action is a genuinely new occurrence, minted by the reopened dialog.
    outcomes.push({ completed: false });
    await openGroupDialog(mounted.container);
    await submitGroupDialog(mounted.container);
    await flush();
    assert.equal(sent.length, 4);
    assert.notEqual(sent[3].occurrenceId, occurrenceId);
    assert.ok(readOperationalGroupAttempt("c1", GROUP_KEY, scope));
  } finally {
    await mounted.unmount();
    clearOperationalGroupAttempt("c1", GROUP_KEY, scope);
  }
});

test("R6 mounted: denial/compatibility/foreign refusals and transport loss keep the same occurrence", async () => {
  const checkout = R4_CHECKOUT;
  resetR4Attempt();
  const restoreViewer = withR4Viewer();
  const codes = [
    "order.operation.denied",
    "order.operational_group.consolidate_incompatible",
    "order.operational_group.workflow_group_foreign",
  ];
  let step = 0;
  const calls = installFetch((call) => {
    if (call.method === "GET") return operationsPage();
    step += 1;
    if (step === 1) return postResponse(403, { error: { code: codes[0] } });
    if (step === 2) return postResponse(409, { error: { code: codes[1] } });
    if (step === 3) return postResponse(409, { error: { code: codes[2] } });
    if (step === 4) return postResponse(500, { error: { code: "order.operation.failed" } });
    throw new TypeError("network down");
  });
  try {
    const mounted = await mount(
      createElement(AdminOrderItemsShippingPanel, {
        detail: { ...baseDetail(), checkoutId: checkout },
        checkoutId: checkout,
      }),
    );
    await openGroupDialog(mounted.container);
    const seenOccurrences: unknown[] = [];

    for (let index = 1; index <= 5; index += 1) {
      // The first send comes from the normal dialog action; every later one is an explicit reconcile.
      await click(q(mounted.container, index === 1 ? DIALOG_SUBMIT_SELECTOR : RECONCILE_RETRY_SELECTOR));
      await waitFor(() => calls.filter((c) => c.method === "POST").length === index);
      await flush();
      const posts = calls.filter((c) => c.method === "POST");
      seenOccurrences.push(posts[index - 1].body.operationalGroupOccurrenceId);
      const retained = readOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE);
      assert.ok(retained, `attempt ${index} must remain retained`);
      assert.equal(retained!.occurrenceId, seenOccurrences[0]);
      assert.equal(qq(mounted.container, `[data-testid='admin-order-operational-reconcile-${GROUP_KEY}']`).length, 1);
    }

    // Every refusal and the transport loss replayed the SAME recorded occurrence and envelope.
    assert.equal(new Set(seenOccurrences).size, 1);
    const finalRecord = readOperationalGroupAttempt(checkout, GROUP_KEY, R4_SCOPE);
    assert.ok(finalRecord);
    assert.deepEqual([...finalRecord!.members].sort(), ["g1", "g2"]);
    assert.deepEqual(finalRecord!.selections, [
      { orderLineId: "ol1", quantity: 2 },
      { orderLineId: "ol2", quantity: 1 },
    ]);
    await mounted.unmount();
  } finally {
    restoreFetch();
    restoreViewer();
    resetR4Attempt();
  }
});
