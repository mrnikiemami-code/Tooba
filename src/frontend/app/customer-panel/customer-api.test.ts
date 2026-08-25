import assert from "node:assert/strict";
import test from "node:test";
import {
  CUSTOMER_DEV_ACTOR_HEADER,
  customerStatusClasses,
  formatCustomerOrderStatus,
  mapCustomerDashboard,
  mapCustomerOrder,
  mapCustomerOrderDetail,
  mapCustomerProfile,
} from "./customer-api.ts";

test("customer mapper keeps checkout identity and snapshot amount", () => {
  const order = mapCustomerOrder({
    checkoutId: "checkout-1",
    reference: "ORD-1",
    payableAmount: 1_850_000,
    currency: "IRR",
    paymentState: "Paid",
  });
  assert.equal(order?.checkoutId, "checkout-1");
  assert.equal(order?.payableAmount, 1_850_000);
  assert.equal("price" in (order ?? {}), false);
});

test("customer payment presentation preserves backend pending paid and failed states", () => {
  assert.equal(formatCustomerOrderStatus("PendingPayment"), "در انتظار پرداخت");
  assert.equal(formatCustomerOrderStatus("Paid"), "پرداخت‌شده");
  assert.equal(formatCustomerOrderStatus("Failed"), "پرداخت ناموفق");
  assert.match(customerStatusClasses("Failed"), /red/);
});

test("customer dashboard exposes capability availability without fake counts", () => {
  const page = mapCustomerDashboard({
    actorUserId: "actor-1",
    displayName: "مشتری",
    totalOrders: 2,
    pendingOrders: 1,
    paidOrders: 1,
    wishlistAvailable: false,
    addressBookAvailable: false,
    recentOrders: [],
  });
  assert.equal(page?.wishlistAvailable, false);
  assert.equal(page?.addressBookAvailable, false);
  assert.equal(page?.totalOrders, 2);
});

test("customer detail maps seller and shipping snapshots", () => {
  const page = mapCustomerOrderDetail({
    checkoutId: "checkout-1",
    reference: "ORD-1",
    paymentState: "Failed",
    recipientName: "مشتری",
    postalAddress: "تهران",
    sellerOrders: [{
      sellerOrderId: "seller-order-1",
      sellerDisplayName: "فروشگاه آرمان",
      lines: [{ offerId: "offer-1", title: "کالا", quantity: 2, linePayable: 200 }],
    }],
  });
  assert.equal(page?.sellerOrders[0]?.sellerDisplayName, "فروشگاه آرمان");
  assert.equal(page?.sellerOrders[0]?.lines[0]?.quantity, 2);
  assert.equal(page?.postalAddress, "تهران");
  assert.equal(page?.paymentState, "Failed");
});

test("customer profile remains read-only when backend has no write capability", () => {
  const page = mapCustomerProfile({ actorUserId: "actor-1", displayName: "مشتری", editable: false });
  assert.equal(page?.editable, false);
  assert.equal(CUSTOMER_DEV_ACTOR_HEADER, "X-Tooba-Dev-Actor-User-Id");
});
