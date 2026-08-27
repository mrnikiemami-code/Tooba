import assert from "node:assert/strict";
import test from "node:test";
import {
  formatRefundDestination,
  formatReturnDate,
  formatReturnStatus,
  formatRefundAttemptStatus,
  mapReturnList,
  mapReturnSnapshot,
  normalizeRefundDestination,
} from "./return-api.ts";

test("mapReturnSnapshot maps host PascalCase payload", () => {
  const snapshot = mapReturnSnapshot({
    ReturnRequestId: "r1",
    SellerOrderId: "so1",
    CheckoutId: "c1",
    SellerPartyId: "s1",
    RequestedByUserId: "u1",
    Status: "Requested",
    Reason: "معیوب",
    Currency: "IRR",
    RefundAmount: 0,
    PaymentId: "p1",
    Destination: "Wallet",
    CreatedAt: "2026-08-27T00:00:00Z",
    UpdatedAt: "2026-08-27T00:00:00Z",
    Items: [{
      ReturnItemId: "ri1",
      OrderLineId: "ol1",
      Quantity: 1,
      UnitPriceSnapshot: 100000,
      Currency: "IRR",
      ReservationId: null,
    }],
    RefundAttempts: [],
  });
  assert.ok(snapshot);
  assert.equal(snapshot?.returnRequestId, "r1");
  assert.equal(snapshot?.items[0]?.quantity, 1);
  assert.equal(snapshot?.destination, "Wallet");
});

test("mapReturnSnapshot defaults destination to OriginalPayment", () => {
  const snapshot = mapReturnSnapshot({
    returnRequestId: "r2",
    sellerOrderId: "so2",
    checkoutId: "c2",
    sellerPartyId: "s2",
    requestedByUserId: "u2",
    status: "Requested",
    reason: null,
    currency: "IRR",
    refundAmount: 0,
    paymentId: null,
    createdAt: "2026-08-27T00:00:00Z",
    updatedAt: "2026-08-27T00:00:00Z",
    items: [],
    refundAttempts: [],
  });
  assert.equal(snapshot?.destination, "OriginalPayment");
});

test("mapReturnList derives grid rows", () => {
  const rows = mapReturnList([{
    returnRequestId: "r1",
    sellerOrderId: "so1",
    checkoutId: "c1",
    status: "Completed",
    refundAmount: 50000,
    currency: "IRR",
    items: [{ returnItemId: "ri1", orderLineId: "ol1", quantity: 1, unitPriceSnapshot: 50000, currency: "IRR", reservationId: null }],
    refundAttempts: [],
    sellerPartyId: "s1",
    requestedByUserId: "u1",
    reason: null,
    paymentId: null,
    destination: "OriginalPayment",
    createdAt: "2026-08-27T00:00:00Z",
    updatedAt: "2026-08-27T00:00:00Z",
  }]);
  assert.equal(rows.length, 1);
  assert.equal(rows[0]?.itemCount, 1);
});

test("formatters localize return statuses and destinations", () => {
  assert.equal(formatReturnStatus("Requested"), "در انتظار بررسی");
  assert.equal(formatRefundAttemptStatus("Succeeded"), "موفق");
  assert.equal(formatReturnDate(null), "—");
  assert.equal(formatRefundDestination("Wallet"), "کیف پول");
  assert.equal(formatRefundDestination("OriginalPayment"), "پرداخت اصلی");
  assert.equal(normalizeRefundDestination("wallet"), "Wallet");
  assert.equal(normalizeRefundDestination("unknown"), "OriginalPayment");
});
