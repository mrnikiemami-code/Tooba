/**
 * TB-260 — the work-queue client contract.
 *
 * The queue never derives a deadline, a "today" or a bucket in the browser: the server classifies every row
 * and the client only maps it. These tests pin the mapping (camelCase and PascalCase payloads), the honest
 * "no plan" rendering, and the fact that the quick filters the UI sends are exactly the filter ids the
 * Fulfillment module declares.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  FULFILLMENT_DISPATCH_FILTERS,
  formatCustomerReceiving,
  formatDispatchBucketLabel,
  formatHandoffWindowLabel,
  formatPlannedHandoff,
  mapFulfillmentQueueCounts,
  normalizeDispatchToken,
} from "./fulfillment-work-queue-dispatch.ts";
import { mapFulfillmentList, formatFulfillmentQueueQuantity } from "./fulfillment-api.ts";

const ROW = {
  fulfillmentId: "11111111-1111-1111-1111-111111111111",
  sellerOrderId: "22222222-2222-2222-2222-222222222222",
  checkoutId: "33333333-3333-3333-3333-333333333333",
  sellerPartyId: "44444444-4444-4444-4444-444444444444",
  sellerDisplayName: "آرمان",
  orderReference: "ORD-1",
  status: "Packed",
  recipientName: "سارا",
  cityName: "تهران",
  shippingMethodCode: "post",
  shippingMethodLabel: "پیشتاز",
  itemCount: 1,
  quantityOrdered: 2,
  quantityShipped: 0,
  shipmentCount: 0,
  primaryShipmentId: null,
  trackingSummary: "",
  createdAt: "2026-11-10T06:00:00+00:00",
  updatedAt: "2026-11-10T06:00:00+00:00",
  availableActionCodes: ["mark_packed"],
  dispatchBucket: "today",
  handoffWindowState: "open",
  plannedHandoffDate: "2026-11-10",
  plannedHandoffWindow: "00:00–23:59",
  plannedHandoffTimeZone: "Asia/Tehran",
  handoffPlanVariants: 1,
  dispatchDeadlineUtc: "2026-11-10T20:29:59+00:00",
  customerReceivingDate: "2026-11-11",
  customerReceivingWindow: "09:00-17:00",
  targetShippingGroupId: "55555555-5555-5555-5555-555555555555",
  nextStepCode: "mark_processing",
  nextStepLabelFa: "شروع پردازش",
  nextStepBlockerFa: null,
  pendingDispatch: true,
};

test("dispatch timing fields are mapped from the server row, never derived locally", () => {
  const [row] = mapFulfillmentList([ROW]);
  assert.ok(row);
  assert.equal(row.dispatchBucket, "today");
  assert.equal(row.handoffWindowState, "open");
  assert.equal(row.plannedHandoffDate, "2026-11-10");
  assert.equal(row.plannedHandoffWindow, "00:00–23:59");
  assert.equal(row.plannedHandoffTimeZone, "Asia/Tehran");
  assert.equal(row.dispatchDeadlineUtc, "2026-11-10T20:29:59+00:00");
  assert.equal(row.customerReceivingDate, "2026-11-11");
  assert.equal(row.customerReceivingWindow, "09:00-17:00");
  assert.equal(row.targetShippingGroupId, "55555555-5555-5555-5555-555555555555");
  assert.equal(row.nextStepLabelFa, "شروع پردازش");
  assert.equal(row.pendingDispatch, true);
});

test("a row without timing stays truthful instead of inventing a bucket or a date", () => {
  const withoutTiming = { ...ROW } as Record<string, unknown>;
  for (const key of [
    "dispatchBucket", "handoffWindowState", "plannedHandoffDate", "plannedHandoffWindow",
    "plannedHandoffTimeZone", "handoffPlanVariants", "dispatchDeadlineUtc", "customerReceivingDate",
    "customerReceivingWindow", "targetShippingGroupId", "nextStepCode", "nextStepLabelFa",
    "nextStepBlockerFa", "pendingDispatch",
  ]) {
    delete withoutTiming[key];
  }
  const [row] = mapFulfillmentList([withoutTiming]);
  assert.ok(row);
  assert.equal(row.dispatchBucket, "unspecified");
  assert.equal(row.plannedHandoffDate, null);
  assert.equal(formatPlannedHandoff(row), "مشخص نشده");
  assert.equal(formatCustomerReceiving(row), "—");
  assert.equal(row.pendingDispatch, false);
});

test("counts are read from the server's dispatchCounts and default to zero, never to a page tally", () => {
  assert.deepEqual(mapFulfillmentQueueCounts({ totalActive: 3, due: 1, today: 1, overdue: 0, upcoming: 1, unspecified: 1, inTransit: 2 }), {
    totalActive: 3, due: 1, today: 1, overdue: 0, upcoming: 1, unspecified: 1, inTransit: 2,
  });
  assert.deepEqual(mapFulfillmentQueueCounts({ TotalActive: 1, Due: 1, Today: 1, Overdue: 0, Upcoming: 0, Unspecified: 0, InTransit: 0 }), {
    totalActive: 1, due: 1, today: 1, overdue: 0, upcoming: 0, unspecified: 0, inTransit: 0,
  });
  assert.deepEqual(mapFulfillmentQueueCounts(null), {
    totalActive: 0, due: 0, today: 0, overdue: 0, upcoming: 0, unspecified: 0, inTransit: 0,
  });
});

test("quick filters use the exact ids the Fulfillment module declares", () => {
  assert.deepEqual(FULFILLMENT_DISPATCH_FILTERS.map((tab) => tab.id), [
    "dispatch_overdue",
    "dispatch_today",
    "dispatch_due",
    "dispatch_upcoming",
    "dispatch_unspecified",
  ]);
  assert.equal(formatDispatchBucketLabel("overdue"), "سررسیدشده");
  assert.equal(formatDispatchBucketLabel("today"), "ارسال امروز");
  assert.equal(formatDispatchBucketLabel("backlog"), "عقب‌افتاده");
  assert.equal(formatDispatchBucketLabel("upcoming"), "آینده");
  assert.equal(formatDispatchBucketLabel("unspecified"), "موعد ارسال مشخص نشده");
  assert.equal(formatHandoffWindowLabel("open"), "بازهٔ تحویل باز است");
  // The server ships wire-stable PascalCase names; they must never leak into the operator UI as-is.
  assert.equal(formatDispatchBucketLabel("Today"), "ارسال امروز");
  assert.equal(formatDispatchBucketLabel("InTransit"), "در مسیر");
  assert.equal(formatDispatchBucketLabel("NotQueued"), "در صف نیست");
  assert.equal(formatHandoffWindowLabel("TodayNotOpen"), "هنوز به بازه نرسیده");
  assert.equal(formatHandoffWindowLabel("None"), "بدون بازه");
  assert.equal(normalizeDispatchToken("TodayNotOpen"), "today_not_open");
  assert.equal(normalizeDispatchToken("in_transit"), "in_transit");
  assert.equal(formatPlannedHandoff({
    plannedHandoffDate: "2026-11-10",
    plannedHandoffWindow: "10:00–13:00",
    plannedHandoffTimeZone: "Asia/Tehran",
  }), "2026-11-10 · 10:00–13:00 (Asia/Tehran)");
});

test("TB-260-R1: two physical work units of ONE seller order keep distinct row ids and exact locators", () => {
  const parcel = {
    ...ROW,
    workUnitKey: "parcel:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    workUnitKind: "Parcel",
    workUnitState: "Created",
    targetShipmentId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    targetShippingGroupId: "55555555-5555-5555-5555-555555555555",
    targetPackageId: null,
    targetPackageNumber: null,
    scopeLineCount: 1,
    scopeQuantity: 1,
    scopeRemainingQuantity: 0,
  };
  const packageRow = {
    ...ROW,
    workUnitKey: "package:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    workUnitKind: "Package",
    workUnitState: "Created",
    targetShipmentId: null,
    targetShippingGroupId: null,
    targetPackageId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    targetPackageNumber: "MP-BBBBBBBBBBBB",
    scopeLineCount: 2,
    scopeQuantity: 2,
    scopeRemainingQuantity: 0,
  };

  const rows = mapFulfillmentList([parcel, packageRow]);
  assert.equal(rows.length, 2);
  assert.notEqual(rows[0]!.id, rows[1]!.id);
  assert.equal(rows[0]!.id, "parcel:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  assert.equal(rows[1]!.id, "package:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  // The server token is PascalCase; the client keeps ONE canonical lowercase kind for its checks.
  assert.equal(rows[0]!.workUnitKind, "parcel");
  assert.equal(rows[1]!.workUnitKind, "package");
  assert.equal(rows[0]!.fulfillmentId, rows[1]!.fulfillmentId);
  assert.equal(rows[0]!.targetShipmentId, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  assert.equal(rows[1]!.targetPackageId, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  assert.equal(rows[1]!.targetPackageNumber, "MP-BBBBBBBBBBBB");
});

test("TB-260-R1: the quantity cell reports THIS work unit's scope, never the whole seller order", () => {
  const [scoped] = mapFulfillmentList([{
    ...ROW,
    workUnitKey: "group:cccccccc-cccc-cccc-cccc-cccccccccccc",
    workUnitKind: "Group",
    scopeLineCount: 1,
    scopeQuantity: 4,
    scopeRemainingQuantity: 2,
  }]);
  assert.ok(scoped);
  assert.equal(formatFulfillmentQueueQuantity(scoped), "۱ قلم · مقدار 4 · باقی 2");
  // A payload without scope keeps the pre-existing whole-row calculation (no invented numbers).
  assert.equal(
    formatFulfillmentQueueQuantity({ itemCount: 2, quantityOrdered: 5, quantityShipped: 1 }),
    "۲ قلم · مقدار 5 · باقی 4",
  );
});
