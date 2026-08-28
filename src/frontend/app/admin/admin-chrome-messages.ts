/**
 * برچسب‌های ناوبری و عملگرهای Admin — بدون انگلیسی خام در UI فارسی.
 */

export type AdminChromeLocale = "fa" | "en";

export type AdminNavLabels = {
  groupOps: string;
  groupMarket: string;
  groupModeration: string;
  groupSystem: string;
  dashboard: string;
  products: string;
  catalogCategories: string;
  catalogAttributes: string;
  categorySchema: string;
  orders: string;
  fulfillments: string;
  returns: string;
  settlement: string;
  payouts: string;
  content: string;
  stories: string;
  pageComposition: string;
  sellers: string;
  customers: string;
  reviews: string;
  tickets: string;
  giftCards: string;
  wallets: string;
  promotions: string;
  settings: string;
  accessControl: string;
  operations: string;
  signOut: string;
  openMenu: string;
  closeMenu: string;
};

const faNav: AdminNavLabels = {
  groupOps: "عملیات",
  groupMarket: "بازار",
  groupModeration: "نظارت",
  groupSystem: "سامانه",
  dashboard: "داشبورد",
  products: "کاتالوگ / محصولات",
  catalogCategories: "دسته‌بندی‌ها",
  catalogAttributes: "تعاریف ویژگی",
  categorySchema: "طرح ویژگی رده",
  orders: "سفارش‌ها و پرداخت",
  fulfillments: "ارسال و تحویل",
  returns: "مرجوعی و بازپرداخت",
  settlement: "تسویه فروشندگان",
  payouts: "صف پرداخت به فروشنده",
  content: "محتوا / بلاگ",
  stories: "استوری‌ها",
  pageComposition: "ترکیب صفحهٔ خانه",
  sellers: "فروشندگان",
  customers: "مشتریان",
  reviews: "نظرات",
  tickets: "تیکت پشتیبانی",
  giftCards: "کارت هدیه",
  wallets: "کیف پول مشتریان",
  promotions: "پروموشن‌ها",
  settings: "تنظیمات",
  accessControl: "کنترل دسترسی",
  operations: "عملیات",
  signOut: "خروج",
  openMenu: "باز کردن منو",
  closeMenu: "بستن منو",
};

const enNav: AdminNavLabels = {
  groupOps: "Operations",
  groupMarket: "Marketplace",
  groupModeration: "Moderation",
  groupSystem: "System",
  dashboard: "Dashboard",
  products: "Catalog / Products",
  catalogCategories: "Categories",
  catalogAttributes: "Attribute definitions",
  categorySchema: "Category attribute schema",
  orders: "Orders & payments",
  fulfillments: "Shipping & fulfillment",
  returns: "Returns & refunds",
  settlement: "Seller settlement",
  payouts: "Seller payout queue",
  content: "Content / blog",
  stories: "Stories",
  pageComposition: "Home page composition",
  sellers: "Sellers",
  customers: "Customers",
  reviews: "Reviews",
  tickets: "Support tickets",
  giftCards: "Gift cards",
  wallets: "Customer wallets",
  promotions: "Promotions",
  settings: "Settings",
  accessControl: "Access control",
  operations: "Operations",
  signOut: "Sign out",
  openMenu: "Open menu",
  closeMenu: "Close menu",
};

/** locale Admin از lang سند یا پیش‌فرض فارسی. */
export function resolveAdminChromeLocale(): AdminChromeLocale {
  if (typeof document === "undefined") return "fa";
  const lang = (document.documentElement.lang || "").toLowerCase();
  return lang.startsWith("en") ? "en" : "fa";
}

export function adminNavLabels(locale: AdminChromeLocale = "fa"): AdminNavLabels {
  return locale === "en" ? enNav : faNav;
}
