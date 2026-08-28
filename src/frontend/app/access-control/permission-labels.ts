/**
 * برچسب انسانی مجوزها — هرگز `product.view` / `perm.*.desc` را به‌عنوان متن اصلی UI نشان نده.
 * هم‌تراز با PermissionCatalog در AccessControl.Application.
 */

export type PermissionLocale = "fa" | "en";

export type PermissionLabel = {
  title: string;
  description: string;
};

const FA: Record<string, PermissionLabel> = {
  "admin.dashboard.view": { title: "مشاهده داشبورد مدیریت", description: "دسترسی به داشبورد پنل ادمین" },
  "product.view": { title: "مشاهده محصول", description: "مشاهده فهرست و جزئیات محصولات" },
  "product.create": { title: "ایجاد محصول", description: "ایجاد محصول جدید در کاتالوگ" },
  "product.edit": { title: "ویرایش محصول", description: "ویرایش اطلاعات محصول" },
  "product.publish": { title: "انتشار محصول", description: "انتشار یا لغو انتشار محصول" },
  "catalog.attribute.view": { title: "مشاهده ویژگی کاتالوگ", description: "مشاهده تعاریف ویژگی" },
  "catalog.attribute.manage": { title: "مدیریت ویژگی کاتالوگ", description: "ایجاد و ویرایش تعاریف ویژگی" },
  "order.view": { title: "مشاهده سفارش", description: "مشاهده فهرست سفارش‌ها" },
  "order.detail": { title: "جزئیات سفارش", description: "مشاهده جزئیات یک سفارش" },
  "order.handle": { title: "رسیدگی به سفارش", description: "پردازش و رسیدگی به سفارش" },
  "order.fulfill": { title: "تحویل سفارش", description: "ثبت و مدیریت تحویل سفارش" },
  "order.cancel": { title: "لغو سفارش", description: "لغو سفارش" },
  "order.refund": { title: "بازپرداخت سفارش", description: "ثبت بازپرداخت مرتبط با سفارش" },
  "order.export": { title: "خروجی سفارش", description: "خروجی گرفتن از سفارش‌ها" },
  "seller.view": { title: "مشاهده فروشنده", description: "مشاهده فهرست و جزئیات فروشندگان" },
  "seller.approve": { title: "تأیید فروشنده", description: "تأیید یا رد فروشنده" },
  "payment.view": { title: "مشاهده پرداخت", description: "مشاهده تراکنش‌های پرداخت" },
  "payment.reconcile": { title: "مغایرت‌گیری پرداخت", description: "مغایرت‌گیری و تطبیق پرداخت‌ها" },
  "promotion.view": { title: "مشاهده پروموشن", description: "مشاهده کمپین‌ها و تخفیف‌ها" },
  "promotion.manage": { title: "مدیریت پروموشن", description: "ایجاد و ویرایش پروموشن" },
  "review.view": { title: "مشاهده نظرات", description: "مشاهده نظرات کاربران" },
  "review.moderate": { title: "مدیریت نظرات", description: "تأیید، رد یا حذف نظرات" },
  "story.view": { title: "مشاهده استوری", description: "مشاهده استوری‌ها" },
  "story.create": { title: "ایجاد استوری", description: "ایجاد استوری جدید" },
  "story.edit": { title: "ویرایش استوری", description: "ویرایش استوری" },
  "story.submit": { title: "ارسال استوری", description: "ارسال استوری برای بررسی" },
  "story.approve": { title: "تأیید استوری", description: "تأیید استوری" },
  "story.reject": { title: "رد استوری", description: "رد استوری" },
  "story.publish": { title: "انتشار استوری", description: "انتشار استوری" },
  "content.view": { title: "مشاهده محتوا", description: "مشاهده محتوای بلاگ و صفحات" },
  "content.create": { title: "ایجاد محتوا", description: "ایجاد محتوای جدید" },
  "content.edit": { title: "ویرایش محتوا", description: "ویرایش محتوا" },
  "content.publish": { title: "انتشار محتوا", description: "انتشار محتوا" },
  "pagecomposition.view": { title: "مشاهده ترکیب صفحه", description: "مشاهده ترکیب صفحهٔ خانه" },
  "pagecomposition.manage": { title: "مدیریت ترکیب صفحه", description: "ویرایش ترکیب صفحهٔ خانه" },
  "fulfillment.view": { title: "مشاهده ارسال و تحویل", description: "مشاهده وضعیت ارسال و تحویل" },
  "fulfillment.manage": { title: "مدیریت ارسال و تحویل", description: "مدیریت فرآیند ارسال و تحویل" },
  "return.view": { title: "مشاهده مرجوعی", description: "مشاهده درخواست‌های مرجوعی" },
  "return.manage": { title: "مدیریت مرجوعی", description: "رسیدگی به مرجوعی‌ها" },
  "refund.view": { title: "مشاهده بازپرداخت", description: "مشاهده بازپرداخت‌ها" },
  "refund.manage": { title: "مدیریت بازپرداخت", description: "ثبت و مدیریت بازپرداخت" },
  "settlement.view": { title: "مشاهده تسویه", description: "مشاهده تسویه فروشندگان" },
  "settlement.manage": { title: "مدیریت تسویه", description: "مدیریت تسویه و پرداخت به فروشنده" },
  "accesscontrol.view": { title: "مشاهده کنترل دسترسی", description: "مشاهده نقش‌ها و مجوزها" },
  "accesscontrol.manage": { title: "مدیریت کنترل دسترسی", description: "ویرایش نقش‌ها، مجوزها و تخصیص‌ها" },
  "support.view": { title: "مشاهده پشتیبانی", description: "مشاهده تیکت‌های پشتیبانی" },
  "support.create": { title: "ایجاد تیکت", description: "ایجاد تیکت پشتیبانی" },
  "support.reply": { title: "پاسخ پشتیبانی", description: "پاسخ به تیکت پشتیبانی" },
  "support.manage": { title: "مدیریت پشتیبانی", description: "مدیریت کامل تیکت‌های پشتیبانی" },
  "seller.settings.view": { title: "مشاهده تنظیمات فروشنده", description: "مشاهده تنظیمات فروشگاه" },
  "seller.settings.manage": { title: "مدیریت تنظیمات فروشنده", description: "ویرایش تنظیمات فروشگاه" },
  "wallet.view": { title: "مشاهده کیف پول", description: "مشاهده موجودی و تراکنش کیف پول" },
  "wallet.adjust": { title: "تعدیل کیف پول", description: "تعدیل موجودی کیف پول" },
  "giftcard.view": { title: "مشاهده کارت هدیه", description: "مشاهده کارت‌های هدیه" },
  "giftcard.manage": { title: "مدیریت کارت هدیه", description: "ایجاد و مدیریت کارت هدیه" },
};

const EN: Record<string, PermissionLabel> = {
  "admin.dashboard.view": { title: "View admin dashboard", description: "Access the admin panel dashboard" },
  "product.view": { title: "View products", description: "View product list and details" },
  "product.create": { title: "Create product", description: "Create a new catalog product" },
  "product.edit": { title: "Edit product", description: "Edit product information" },
  "product.publish": { title: "Publish product", description: "Publish or unpublish a product" },
  "catalog.attribute.view": { title: "View catalog attributes", description: "View attribute definitions" },
  "catalog.attribute.manage": { title: "Manage catalog attributes", description: "Create and edit attribute definitions" },
  "order.view": { title: "View orders", description: "View the order list" },
  "order.detail": { title: "Order details", description: "View a single order’s details" },
  "order.handle": { title: "Handle order", description: "Process and handle orders" },
  "order.fulfill": { title: "Fulfill order", description: "Manage order fulfillment" },
  "order.cancel": { title: "Cancel order", description: "Cancel an order" },
  "order.refund": { title: "Refund order", description: "Record an order-related refund" },
  "order.export": { title: "Export orders", description: "Export order data" },
  "seller.view": { title: "View sellers", description: "View seller list and details" },
  "seller.approve": { title: "Approve seller", description: "Approve or reject a seller" },
  "payment.view": { title: "View payments", description: "View payment transactions" },
  "payment.reconcile": { title: "Reconcile payments", description: "Reconcile payment records" },
  "promotion.view": { title: "View promotions", description: "View campaigns and discounts" },
  "promotion.manage": { title: "Manage promotions", description: "Create and edit promotions" },
  "review.view": { title: "View reviews", description: "View customer reviews" },
  "review.moderate": { title: "Moderate reviews", description: "Approve, reject, or remove reviews" },
  "story.view": { title: "View stories", description: "View stories" },
  "story.create": { title: "Create story", description: "Create a new story" },
  "story.edit": { title: "Edit story", description: "Edit a story" },
  "story.submit": { title: "Submit story", description: "Submit a story for review" },
  "story.approve": { title: "Approve story", description: "Approve a story" },
  "story.reject": { title: "Reject story", description: "Reject a story" },
  "story.publish": { title: "Publish story", description: "Publish a story" },
  "content.view": { title: "View content", description: "View blog and page content" },
  "content.create": { title: "Create content", description: "Create new content" },
  "content.edit": { title: "Edit content", description: "Edit content" },
  "content.publish": { title: "Publish content", description: "Publish content" },
  "pagecomposition.view": { title: "View page composition", description: "View home page composition" },
  "pagecomposition.manage": { title: "Manage page composition", description: "Edit home page composition" },
  "fulfillment.view": { title: "View fulfillment", description: "View fulfillment status" },
  "fulfillment.manage": { title: "Manage fulfillment", description: "Manage fulfillment workflow" },
  "return.view": { title: "View returns", description: "View return requests" },
  "return.manage": { title: "Manage returns", description: "Handle return requests" },
  "refund.view": { title: "View refunds", description: "View refunds" },
  "refund.manage": { title: "Manage refunds", description: "Create and manage refunds" },
  "settlement.view": { title: "View settlements", description: "View seller settlements" },
  "settlement.manage": { title: "Manage settlements", description: "Manage seller settlement payouts" },
  "accesscontrol.view": { title: "View access control", description: "View roles and permissions" },
  "accesscontrol.manage": { title: "Manage access control", description: "Edit roles, permissions, and assignments" },
  "support.view": { title: "View support", description: "View support tickets" },
  "support.create": { title: "Create ticket", description: "Create a support ticket" },
  "support.reply": { title: "Reply to support", description: "Reply to a support ticket" },
  "support.manage": { title: "Manage support", description: "Full support ticket management" },
  "seller.settings.view": { title: "View seller settings", description: "View store settings" },
  "seller.settings.manage": { title: "Manage seller settings", description: "Edit store settings" },
  "wallet.view": { title: "View wallet", description: "View wallet balance and transactions" },
  "wallet.adjust": { title: "Adjust wallet", description: "Adjust wallet balance" },
  "giftcard.view": { title: "View gift cards", description: "View gift cards" },
  "giftcard.manage": { title: "Manage gift cards", description: "Create and manage gift cards" },
};

const MODULE_FA: Record<string, string> = {
  Admin: "مدیریت",
  Product: "محصول",
  Catalog: "کاتالوگ",
  Order: "سفارش",
  Seller: "فروشنده",
  Payment: "پرداخت",
  Promotion: "پروموشن",
  Review: "نظرات",
  Story: "استوری",
  Content: "محتوا",
  PageComposition: "ترکیب صفحه",
  Fulfillment: "ارسال و تحویل",
  Return: "مرجوعی",
  Refund: "بازپرداخت",
  Settlement: "تسویه",
  AccessControl: "کنترل دسترسی",
  Support: "پشتیبانی",
  Wallet: "کیف پول",
};

const MODULE_EN: Record<string, string> = {
  Admin: "Admin",
  Product: "Product",
  Catalog: "Catalog",
  Order: "Order",
  Seller: "Seller",
  Payment: "Payment",
  Promotion: "Promotion",
  Review: "Review",
  Story: "Story",
  Content: "Content",
  PageComposition: "Page composition",
  Fulfillment: "Fulfillment",
  Return: "Return",
  Refund: "Refund",
  Settlement: "Settlement",
  AccessControl: "Access control",
  Support: "Support",
  Wallet: "Wallet",
};

/** Locale از `document.documentElement.lang` — پیش‌فرض فارسی. */
export function resolvePermissionLocale(): PermissionLocale {
  if (typeof document === "undefined") return "fa";
  const lang = (document.documentElement.lang || "").toLowerCase();
  return lang.startsWith("en") ? "en" : "fa";
}

/** تبدیل شناسهٔ نقطه‌دار به برچسب خوانا وقتی در نقشه نیست. */
export function humanizePermissionId(permissionId: string, locale: PermissionLocale = "fa"): string {
  const parts = permissionId
    .split(/[._-]+/)
    .filter(Boolean)
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1).toLowerCase());
  if (locale === "en") {
    return parts.join(" ");
  }
  return parts.join(" · ");
}

/** عنوان و توضیح انسانی مجوز. */
export function getPermissionLabel(
  permissionId: string,
  locale: PermissionLocale = resolvePermissionLocale(),
): PermissionLabel {
  const map = locale === "en" ? EN : FA;
  const hit = map[permissionId];
  if (hit) return hit;
  const title = humanizePermissionId(permissionId, locale);
  return {
    title,
    description: locale === "en" ? `Permission ${permissionId}` : `مجوز ${humanizePermissionId(permissionId, locale)}`,
  };
}

/** برچسب ماژول. */
export function getModuleLabel(module: string, locale: PermissionLocale = resolvePermissionLocale()): string {
  const map = locale === "en" ? MODULE_EN : MODULE_FA;
  return map[module] ?? module;
}
