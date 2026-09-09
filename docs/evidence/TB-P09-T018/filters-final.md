# Filters Final

Quick tabs (server `queueFilter`): همه / نیازمند اقدام / آماده پردازش / آماده بسته‌بندی / آماده ارسال / بدون کد رهگیری / در مسیر / تحویل‌شده / مشکل‌دار.

Mapping unchanged from T008 (real `FulfillmentStatus` / missing-tracking). No dead Processing-style values.

Column filters: seller name, shipping method, status, recipient/city, **order number** (SellerOrder.OrderNumber). Runtime all tabs HTTP 200.
