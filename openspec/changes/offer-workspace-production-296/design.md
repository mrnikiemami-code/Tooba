# Design — offer-workspace-production-296
Workspace: header summary (product/variant/seller/status/price+currency/market) + 7 RTL tabs (مشخصات آفر، قیمت‌گذاری،
تخفیفات، موجودی، ارسال، شرایط فروش، تاریخچه). Only current tab rendered. Single primary action bar; unsaved guard;
per-tab errors. Admin vs seller via role/permission gates.
Reuse: public AppDataGrid for the tables (already); design-system components for the form.
Contracts: create/get/patch Offer; PUT fulfillment (responsibility); return policy + qty limits + status via patch.
Blocked domains (no contract): discounts-definition, inventory-adjust, offer-history, per-offer shipping-service.
