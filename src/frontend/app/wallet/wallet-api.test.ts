import assert from "node:assert/strict";
import test from "node:test";
import {
  formatGiftCardStatus,
  formatLedgerEntryLabel,
  formatWalletMoney,
  isCreditDirection,
  mapGiftCardDetail,
  mapGiftCardIssueResult,
  mapGiftCardListPage,
  mapGiftCardRedeemResult,
  mapWalletDemoPreview,
  mapWalletLedgerPage,
  mapWalletSummary,
  toPersianDigits,
} from "./wallet-api.ts";

test("mapWalletSummary maps camel and Pascal ledger-derived balance", () => {
  const summary = mapWalletSummary({
    AccountId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    CustomerActorUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    Currency: "IRR",
    Status: "Active",
    Balance: 1250000,
    TotalCredits: 1500000,
    TotalDebits: 250000,
    EntryCount: 4,
    CreatedAt: "2026-08-27T10:00:00Z",
  });

  assert.ok(summary);
  assert.equal(summary!.accountId, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  assert.equal(summary!.balance, 1250000);
  assert.equal(summary!.totalCredits, 1500000);
  assert.equal(summary!.entryCount, 4);
});

test("mapWalletLedgerPage accepts paged envelope and labels gift credit", () => {
  const page = mapWalletLedgerPage({
    items: [
      {
        entryId: "11111111-1111-1111-1111-111111111111",
        accountId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        type: "GiftCardCredit",
        amount: 500000,
        currency: "IRR",
        direction: "Credit",
        sourceType: "GiftCard",
        sourceId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        createdAt: "2026-08-27T11:00:00Z",
        metadata: null,
      },
    ],
    total: 1,
    page: 1,
    pageSize: 20,
    balance: 500000,
  });

  assert.equal(page.items.length, 1);
  assert.equal(page.balance, 500000);
  assert.equal(formatLedgerEntryLabel(page.items[0]!), "اعتبار کارت هدیه");
  assert.equal(isCreditDirection(page.items[0]!.direction), true);
});

test("mapGiftCardRedeemResult and issue/list/detail adapters", () => {
  const redeem = mapGiftCardRedeemResult({
    redemptionId: "r1",
    cardId: "c1",
    accountId: "a1",
    amount: 100000,
    walletBalance: 200000,
    cardStatus: "Redeemed",
    cardRemainingAmount: 0,
    idempotentReplay: false,
  });
  assert.ok(redeem);
  assert.equal(redeem!.walletBalance, 200000);

  const list = mapGiftCardListPage({
    Items: [
      {
        CardId: "c1",
        Currency: "IRR",
        InitialAmount: 100000,
        RemainingAmount: 0,
        Status: "Redeemed",
        IssuedAt: "2026-08-01T00:00:00Z",
        ExpiresAt: null,
        RecipientActorUserId: null,
        CreatedByActorUserId: "admin",
        RedemptionCount: 1,
      },
    ],
    Total: 1,
    Page: 1,
    PageSize: 50,
  });
  assert.equal(list.items.length, 1);
  assert.equal(formatGiftCardStatus(list.items[0]!.status), "مصرف‌شده");

  const detail = mapGiftCardDetail({
    cardId: "c1",
    currency: "IRR",
    initialAmount: 100000,
    remainingAmount: 0,
    status: "Redeemed",
    issuedAt: "2026-08-01T00:00:00Z",
    expiresAt: null,
    recipientActorUserId: null,
    createdByActorUserId: "admin",
    redemptions: [
      {
        redemptionId: "r1",
        cardId: "c1",
        accountId: "a1",
        amount: 100000,
        createdAt: "2026-08-02T00:00:00Z",
      },
    ],
  });
  assert.ok(detail);
  assert.equal(detail!.redemptions.length, 1);

  const issued = mapGiftCardIssueResult({
    card: list.items[0],
    displayCode: "TOOBA-GIFT-DEMO-001",
    idempotentReplay: false,
  });
  assert.ok(issued);
  assert.equal(issued!.displayCode, "TOOBA-GIFT-DEMO-001");
});

test("mapWalletDemoPreview and FA money helpers", () => {
  const demo = mapWalletDemoPreview({
    customerActorUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    accountId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    balance: 750000,
    unusedGiftCardId: "u1",
    unusedGiftCardDemoCode: "DEMO-UNUSED",
    partiallyRedeemedGiftCardId: "p1",
    expiredGiftCardId: "e1",
    revokedGiftCardId: "r1",
    note: "dev only",
  });
  assert.ok(demo);
  assert.equal(demo!.unusedGiftCardDemoCode, "DEMO-UNUSED");
  assert.equal(toPersianDigits(12), "۱۲");
  assert.equal(formatWalletMoney(1250000), toPersianDigits((1250000).toLocaleString("en-US")));
});
