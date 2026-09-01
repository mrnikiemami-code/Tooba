import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { enrichAdminOrderDetail, mapAdminOrderDetail } from "./admin-api.ts";

const screenPath = join(import.meta.dirname, "admin-order-detail-screen.tsx");
const screenSource = readFileSync(screenPath, "utf8");

test("order detail screen keeps compact premium layout hooks", () => {
  assert.match(screenSource, /data-testid="admin-order-detail"/);
  assert.match(screenSource, /min-h-\[104px\]/);
  assert.match(screenSource, /بخش مالی سفارش/);
  assert.match(screenSource, /formatAdminPaymentProvider/);
  assert.doesNotMatch(screenSource, /AgGridReact/);
});

test("T042-R1 finance fields still map after visual polish", () => {
  const detail = mapAdminOrderDetail({
    checkoutId: "c1",
    reference: "TOOBA-101",
    status: "Paid",
    paymentState: "Paid",
    subtotal: 350000,
    payableAmount: 381500,
    currency: "IRR",
    sellerOrders: [{
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      paymentState: "Paid",
      payableAmount: 381500,
      currency: "IRR",
      lines: [{ offerId: "o1", productTitle: "کالا", quantity: 1, unitAmount: 350000, linePayable: 381500, currency: "IRR" }],
    }],
    payment: {
      paymentId: "p1",
      checkoutId: "c1",
      status: "Succeeded",
      amount: 381500,
      currency: "IRR",
      providerCode: "wallet",
      providerTransactionReference: "wallet:abc",
      createdAt: "2026-08-27T00:00:00Z",
      updatedAt: "2026-08-27T00:00:00Z",
      completedAt: "2026-08-27T00:00:00Z",
      reconcileEligible: false,
    },
  });
  assert.equal(detail?.lineCount, 1);
  assert.equal(detail?.sellerCount, 1);
  assert.equal(detail?.financialSummary.totalReceivedFromCustomer, 381500);
  assert.equal(detail?.financialEvents[0]?.paymentMethod, "کیف پول");

  const enriched = enrichAdminOrderDetail({
    ...detail!,
    lineCount: 0,
    sellerFinancials: [],
    financialEvents: [],
    financialSummary: {
      totalSellerShare: 0,
      totalCommission: 0,
      grossOrderProfit: 0,
      payableToSellers: 0,
      customerGrossAmount: 0,
      shippingCost: 0,
      customerDiscounts: 0,
      totalReceivedFromCustomer: 0,
      currency: "IRR",
    },
  });
  assert.equal(enriched.lineCount, 1);
  assert.equal(enriched.sellerFinancials.length, 1);
});
