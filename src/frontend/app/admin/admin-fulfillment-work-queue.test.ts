import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  FULFILLMENT_QUEUE_FILTERS,
  FULFILLMENT_SAFE_BULK_ACTION_CODES,
  areFulfillmentBulkCompatible,
  formatFulfillmentStatus,
  formatFulfillmentQueueQuantity,
  formatFulfillmentShipmentSummary,
  mapFulfillmentList,
  type FulfillmentListRow,
} from "../fulfillment/fulfillment-api.ts";
import { filterOperationsForScope } from "./admin-order-operations-scope.ts";

const dir = dirname(fileURLToPath(import.meta.url));

function row(partial: Partial<FulfillmentListRow> & { sellerPartyId: string; availableActionCodes: string[] }): FulfillmentListRow {
  return {
    id: partial.id ?? "f1",
    fulfillmentId: partial.fulfillmentId ?? "f1",
    sellerOrderId: partial.sellerOrderId ?? "so1",
    checkoutId: partial.checkoutId ?? "c1",
    sellerPartyId: partial.sellerPartyId,
    sellerDisplayName: partial.sellerDisplayName ?? "فروشنده",
    orderReference: partial.orderReference ?? "ORD",
    status: partial.status ?? "ReadyToFulfill",
    recipientName: partial.recipientName ?? "علی",
    cityName: partial.cityName ?? "تهران",
    shippingMethodCode: partial.shippingMethodCode ?? "post",
    shippingMethodLabel: partial.shippingMethodLabel ?? "پست",
    itemCount: partial.itemCount ?? 1,
    quantityOrdered: partial.quantityOrdered ?? 1,
    quantityShipped: partial.quantityShipped ?? 0,
    shipmentCount: partial.shipmentCount ?? 0,
    primaryShipmentId: partial.primaryShipmentId ?? null,
    trackingSummary: partial.trackingSummary ?? "",
    trackingReferences: partial.trackingReferences ?? [],
    createdAt: partial.createdAt ?? "",
    updatedAt: partial.updatedAt ?? "",
    availableActionCodes: partial.availableActionCodes,
  };
}

test("work queue screen uses AppDataGrid not AgGridReact", () => {
  const source = readFileSync(join(dir, "admin-fulfillment-work-queue-screen.tsx"), "utf8");
  assert.match(source, /AppDataGrid/);
  assert.doesNotMatch(source, /AgGridReact/);
  assert.match(source, /bulkActions/);
  assert.match(source, /fulfillment-queue/);
  assert.match(source, /admin\/orders\/\$\{row\.checkoutId\}/);
  assert.match(source, /admin-fulfillment-queue-filters/);
  assert.match(source, /PartialDispatched/);
});

test("AdminFulfillmentsScreen delegates to work queue screen", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /AdminFulfillmentWorkQueueScreen/);
  assert.match(screens, /export function AdminFulfillmentsScreen/);
});

test("quick filters cover required FA tabs mapped to backend keys", () => {
  const ids = FULFILLMENT_QUEUE_FILTERS.map((x) => x.id);
  for (const id of [
    "all",
    "needs_action",
    "ready_to_process",
    "ready_to_pack",
    "ready_to_ship",
    "missing_tracking",
    "in_transit",
    "delivered",
    "problem",
  ]) {
    assert.ok(ids.includes(id as (typeof ids)[number]), id);
  }
  assert.equal(FULFILLMENT_QUEUE_FILTERS.find((x) => x.id === "ready_to_process")?.labelFa, "آماده پردازش");
});

test("mapFulfillmentList maps work-queue DTO with human method label", () => {
  const rows = mapFulfillmentList([
    {
      fulfillmentId: "f1",
      sellerOrderId: "so1",
      checkoutId: "c1",
      sellerPartyId: "s1",
      sellerDisplayName: "فروشگاه الف",
      orderReference: "ORD-9",
      status: "Packed",
      recipientName: "علی",
      cityName: "تهران",
      shippingMethodCode: "tipax",
      shippingMethodLabel: "تیپاکس",
      itemCount: 2,
      quantityOrdered: 3,
      quantityShipped: 0,
      shipmentCount: 0,
      primaryShipmentId: null,
      trackingSummary: "",
      createdAt: "2026-09-01T00:00:00Z",
      updatedAt: "2026-09-02T00:00:00Z",
      availableActionCodes: ["create_shipment", "mark_packed"],
    },
  ]);
  assert.equal(rows.length, 1);
  assert.equal(rows[0]?.shippingMethodLabel, "تیپاکس");
  assert.equal(rows[0]?.orderReference, "ORD-9");
  assert.deepEqual(rows[0]?.availableActionCodes, ["create_shipment", "mark_packed"]);
});

test("bulk compatible rejects cross-seller and mixed actions", () => {
  const a = row({ sellerPartyId: "s1", availableActionCodes: ["mark_processing", "mark_packed"] });
  const b = row({ id: "f2", fulfillmentId: "f2", sellerPartyId: "s1", availableActionCodes: ["mark_processing"] });
  const c = row({ id: "f3", fulfillmentId: "f3", sellerPartyId: "s2", availableActionCodes: ["mark_processing"] });
  assert.equal(areFulfillmentBulkCompatible([a, b], "mark_processing"), true);
  assert.equal(areFulfillmentBulkCompatible([a, b], "mark_packed"), false);
  assert.equal(areFulfillmentBulkCompatible([a, c], "mark_processing"), false);
  assert.ok(FULFILLMENT_SAFE_BULK_ACTION_CODES.includes("dispatch_shipment"));
});

test("fulfillment-queue scope keeps only fulfillment ops for matching id", () => {
  const actions = [
    { code: "mark_processing", fulfillmentId: "f1" },
    { code: "cancel", fulfillmentId: null },
    { code: "dispatch_shipment", fulfillmentId: "f2" },
    { code: "request_return", fulfillmentId: "f1" },
  ];
  const filtered = filterOperationsForScope(actions, "fulfillment-queue", "f1");
  assert.deepEqual(filtered.map((x) => x.code), ["mark_processing"]);
});

test("queue quantity and shipment summary are decimal-aware and GUID-free", () => {
  const source = readFileSync(join(dir, "admin-fulfillment-work-queue-screen.tsx"), "utf8");
  assert.match(source, /formatFulfillmentQueueQuantity/);
  assert.match(source, /formatFulfillmentShipmentSummary/);
  assert.match(source, /availableActionCodes\.length > 0/);
  assert.doesNotMatch(source, /checkoutId\.slice/);
  assert.doesNotMatch(source, /primaryShipmentId\.slice/);
  assert.doesNotMatch(source, /toLocaleString\("fa-IR"\)\}`/);
});

test("formatFulfillmentQueueQuantity strips zeros and keeps 1.25", () => {
  const qty = formatFulfillmentQueueQuantity({ itemCount: 1, quantityOrdered: 1.25, quantityShipped: 0.5 });
  assert.match(qty, /1\.25/);
  assert.match(qty, /0\.75/);
  assert.doesNotMatch(qty, /000000/);
  assert.equal(formatFulfillmentShipmentSummary({ shipmentCount: 0 }), "—");
  assert.match(formatFulfillmentShipmentSummary({ shipmentCount: 2 }), /مرسوله/);
});

test("work queue query client posts to work-queue endpoint", () => {
  const api = readFileSync(join(dir, "../fulfillment/fulfillment-api.ts"), "utf8");
  assert.match(api, /\/v1\/admin\/fulfillments\/work-queue\/query/);
  assert.match(api, /\/v1\/admin\/fulfillments\/work-queue\/bulk/);
});
