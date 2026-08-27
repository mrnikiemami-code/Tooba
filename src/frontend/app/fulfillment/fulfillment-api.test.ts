import assert from "node:assert/strict";
import test from "node:test";
import {
  formatFulfillmentDate,
  formatFulfillmentStatus,
  formatShipmentStatus,
  mapFulfillmentList,
  mapFulfillmentSnapshot,
  normalizeFulfillmentStatus,
} from "./fulfillment-api.ts";

test("normalizeFulfillmentStatus maps numeric Host enum to canonical name", () => {
  assert.equal(normalizeFulfillmentStatus(5), "Delivered");
  assert.equal(normalizeFulfillmentStatus("Packed"), "Packed");
});

test("mapFulfillmentSnapshot maps numeric fulfillment status from Host", () => {
  const snapshot = mapFulfillmentSnapshot({
    fulfillmentId: "f1",
    sellerOrderId: "so1",
    checkoutId: "c1",
    sellerPartyId: "s1",
    status: 5,
    recipientName: "علی",
    contactMobile: "09120000000",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان ۱",
    postalCode: "1234567890",
    shippingMethodCode: "standard",
    shippingMethodLabel: "پست",
    items: [],
    shipments: [{ shipmentId: "sh1", status: 3, carrierDisplayName: "پost", trackingReference: "TRK", items: [] }],
  });
  assert.equal(snapshot?.status, "Delivered");
  assert.equal(snapshot?.shipments[0]?.status, "Delivered");
});

test("mapFulfillmentSnapshot maps host PascalCase payload", () => {
  const snapshot = mapFulfillmentSnapshot({
    FulfillmentId: "f1",
    SellerOrderId: "so1",
    CheckoutId: "c1",
    SellerPartyId: "s1",
    Status: "Packed",
    RecipientName: "علی",
    ContactMobile: "09120000000",
    ProvinceName: "تهران",
    CityName: "تهران",
    PostalAddress: "خیابان ۱",
    PostalCode: "1234567890",
    ShippingMethodCode: "standard",
    ShippingMethodLabel: "پست",
    Items: [{ FulfillmentItemId: "fi1", OrderLineId: "ol1", QuantityOrdered: 2, QuantityShipped: 1, ReservationId: null }],
    Shipments: [{
      ShipmentId: "sh1",
      Status: "Created",
      CarrierDisplayName: "پست",
      TrackingReference: "TRK-1",
      DispatchedAt: null,
      DeliveredAt: null,
      Items: [{ OrderLineId: "ol1", Quantity: 1 }],
    }],
  });
  assert.ok(snapshot);
  assert.equal(snapshot?.fulfillmentId, "f1");
  assert.equal(snapshot?.items[0]?.quantityOrdered, 2);
  assert.equal(snapshot?.shipments[0]?.trackingReference, "TRK-1");
});

test("mapFulfillmentList derives grid rows with tracking references", () => {
  const rows = mapFulfillmentList([
    {
      fulfillmentId: "f1",
      sellerOrderId: "so1",
      checkoutId: "c1",
      status: "Dispatched",
      recipientName: "علی",
      cityName: "تهران",
      shipments: [
        { shipmentId: "sh1", status: "Dispatched", carrierDisplayName: "پست", trackingReference: "A", dispatchedAt: null, deliveredAt: null, items: [] },
        { shipmentId: "sh2", status: "Created", carrierDisplayName: "پست", trackingReference: null, dispatchedAt: null, deliveredAt: null, items: [] },
      ],
      items: [],
    },
  ]);
  assert.equal(rows.length, 1);
  assert.equal(rows[0]?.shipmentCount, 2);
  assert.deepEqual(rows[0]?.trackingReferences, ["A"]);
});

test("formatters localize known fulfillment and shipment statuses", () => {
  assert.equal(formatFulfillmentStatus("ReadyToFulfill"), "آماده ارسال");
  assert.equal(formatShipmentStatus("InTransit"), "در مسیر");
  assert.notEqual(formatFulfillmentDate("2026-08-27T10:00:00Z"), "—");
  assert.equal(formatFulfillmentDate(null), "—");
});
