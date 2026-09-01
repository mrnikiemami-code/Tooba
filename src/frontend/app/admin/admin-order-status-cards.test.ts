import assert from "node:assert/strict";
import test from "node:test";
import {
  orderLifecycleStatusSource,
  paymentLifecycleStatusSource,
  resolveOrderStatusCard,
  resolvePaymentStatusCard,
} from "./admin-order-status-cards.ts";

test("unpaid order can show different order vs payment status labels", () => {
  const detail = {
    status: "Submitted",
    paymentState: "PendingPayment",
    payment: { status: "Pending" },
  };
  const order = resolveOrderStatusCard(detail);
  const payment = resolvePaymentStatusCard(detail);
  assert.equal(order.text, "ثبت‌شده");
  assert.equal(payment.text, "در انتظار");
  assert.notEqual(order.text, payment.text);
});

test("paid order can show different order vs payment status labels", () => {
  const detail = {
    status: "Submitted",
    paymentState: "Paid",
    payment: { status: "Succeeded" },
  };
  const order = resolveOrderStatusCard(detail);
  const payment = resolvePaymentStatusCard(detail);
  assert.equal(order.text, "ثبت‌شده");
  assert.equal(payment.text, "موفق");
  assert.notEqual(order.text, payment.text);
});

test("order lifecycle never falls back to payment state", () => {
  assert.equal(orderLifecycleStatusSource({ status: "" }), "Submitted");
  assert.equal(paymentLifecycleStatusSource({ paymentState: "", payment: null }), "PendingPayment");
});

test("payment lifecycle prefers gateway status over aggregate paymentState", () => {
  assert.equal(
    paymentLifecycleStatusSource({ paymentState: "PendingPayment", payment: { status: "Succeeded" } }),
    "Succeeded",
  );
});
