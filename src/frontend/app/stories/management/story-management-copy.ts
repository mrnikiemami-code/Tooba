/** برچسب‌های مشترک مدیریت استوری — Admin و Seller یک زبان بصری دارند. */

export const STORY_STATUS_LABELS: Record<string, string> = {
  Draft: "پیش‌نویس",
  Scheduled: "زمان‌بندی",
  Active: "فعال",
  Expired: "منقضی",
  Disabled: "غیرفعال",
};

export const STORY_ORIGIN_LABELS: Record<string, string> = {
  Admin: "ادمین",
  Seller: "فروشنده",
};

export const STORY_REVIEW_LABELS: Record<string, string> = {
  None: "—",
  Submitted: "در انتظار بازبینی",
  Approved: "تأییدشده",
  Rejected: "ردشده",
};

export const STORY_COPY = {
  adminTitle: "استوری‌ها",
  adminDescription: "ایجاد، فعال‌سازی و آیتم‌های Story ویترین",
  sellerTitle: "استوری‌ها",
  sellerDescription: "پیش‌نویس، آیتم‌ها و ارسال برای بازبینی",
  createButton: "استوری جدید",
  createModalTitle: "ایجاد استوری",
  createAndPublish: "ایجاد و فعال‌سازی",
  createDraft: "ایجاد پیش‌نویس",
  cancel: "انصراف",
  close: "بستن",
  details: "جزئیات",
  enable: "فعال",
  disable: "غیرفعال",
  submit: "ارسال برای بازبینی",
  approve: "تأیید",
  reject: "رد",
  rejectPrompt: "دلیل رد استوری را وارد کنید",
  rejectReasonRequired: "رد بدون دلیل مجاز نیست",
  scheduleStart: "شروع (اختیاری)",
  scheduleEnd: "پایان (اختیاری)",
  saveSchedule: "ذخیره زمان‌بندی",
  itemsHeading: "آیتم‌ها",
  addItem: "افزودن آیتم",
  rejectionReason: "دلیل رد",
  listCountSuffix: "استوری",
  loadErrorTitle: "استوری‌ها خوانده نشد",
  filterAll: "همه",
  filterPending: "در انتظار بازبینی",
  originHeader: "منبع",
  reviewHeader: "بازبینی",
  sellerOwnerHeader: "فروشنده",
} as const;
