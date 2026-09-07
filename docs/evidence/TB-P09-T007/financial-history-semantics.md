# Financial History Semantics

READ-ONLY Order money movements only:

- CustomerReceipt → دریافت از مشتری
- CustomerRefund → بازگشت وجه به مشتری / جزئی
- SellerPayout (SettlementEntry Credit for this SellerOrder) → واریز سهم فروشنده
- SellerRefundAdjustment (Debit/refund source) → کسر از حساب فروشنده بابت بازگشت وجه

Not shown: pending payable, theoretical commission without posting, full multi-order payout batch amount.
