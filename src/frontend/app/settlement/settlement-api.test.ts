import assert from "node:assert/strict";
import test from "node:test";
import {
  formatEntryType,
  formatPayoutStatus,
  formatSettlementMoney,
  mapPayoutRequest,
  mapSettlementBalance,
  mapSettlementEntry,
} from "./settlement-api.ts";

test("mapSettlementBalance maps host PascalCase payload", () => {
  const balance = mapSettlementBalance({
    SettlementAccountId: "acc1",
    SellerPartyId: "seller1",
    Currency: "IRR",
    PostedCredits: 100000,
    PostedDebits: 20000,
    ReservedPayouts: 5000,
    AvailableBalance: 75000,
  });
  assert.ok(balance);
  assert.equal(balance?.availableBalance, 75000);
  assert.equal(balance?.sellerPartyId, "seller1");
});

test("mapSettlementEntry maps entry row", () => {
  const entry = mapSettlementEntry({
    EntryId: "e1",
    EntryType: "Credit",
    GrossAmount: 100000,
    CommissionAmount: 10000,
    NetAmount: 90000,
    Currency: "IRR",
    SourceType: "PaymentSucceeded",
    SellerOrderId: "so1",
    PostedAt: "2026-08-27T00:00:00Z",
  });
  assert.ok(entry);
  assert.equal(entry?.netAmount, 90000);
});

test("mapPayoutRequest and formatters localize statuses", () => {
  const payout = mapPayoutRequest({
    PayoutRequestId: "p1",
    SellerPartyId: "s1",
    Amount: 50000,
    Currency: "IRR",
    Status: "Requested",
    IdempotencyKey: "k1",
    CreatedAt: "2026-08-27T00:00:00Z",
    UpdatedAt: "2026-08-27T00:00:00Z",
  });
  assert.equal(payout?.payoutRequestId, "p1");
  assert.equal(formatPayoutStatus("Succeeded"), "موفق");
  assert.equal(formatEntryType("Credit"), "واریز تسویه");
  assert.match(formatSettlementMoney(1000, "IRR"), /ریال/);
});
