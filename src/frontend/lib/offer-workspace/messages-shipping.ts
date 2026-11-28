import type { OfferWorkspaceMessages } from "./messages.ts";

type ShippingMessages = Pick<
  OfferWorkspaceMessages,
  | "shippingMode"
  | "shippingModeAuto"
  | "shippingModeAutoHint"
  | "shippingModeSpecific"
  | "shippingModeSpecificHint"
  | "shippingMethods"
  | "shippingAddMethod"
  | "shippingSearchMethod"
  | "shippingNoMethodResults"
  | "shippingNoMethods"
  | "shippingMethodsError"
  | "shippingNoSelection"
  | "shippingSelectAtLeastOne"
  | "shippingRemoveMethod"
  | "shippingLockedAuto"
  | "shippingMethodNote"
  | "shippingMethodStale"
>;

export const shippingMessagesFa: ShippingMessages = {
  shippingMode: "حالت انتخاب روش ارسال",
  shippingModeAuto: "خودکار",
  shippingModeAutoHint: "تمام روش‌های مجازِ همین نوع ارسال",
  shippingModeSpecific: "اختصاصی",
  shippingModeSpecificHint: "فقط روش‌هایی که انتخاب می‌کنید",
  shippingMethods: "روش‌های ارسال",
  shippingAddMethod: "افزودن روش ارسال",
  shippingSearchMethod: "جست‌وجوی نام یا کد روش",
  shippingNoMethodResults: "روشی با این جست‌وجو پیدا نشد",
  shippingNoMethods: "روش ارسال مجازِ فعالی وجود ندارد.",
  shippingMethodsError: "فهرست روش‌های ارسال در دسترس نیست.",
  shippingNoSelection: "هنوز روشی انتخاب نشده است.",
  shippingSelectAtLeastOne: "حداقل یک روش ارسال انتخاب کنید.",
  shippingRemoveMethod: "حذف",
  shippingLockedAuto: "در حالت خودکار، سامانه همه روش‌های مجاز را اعمال می‌کند.",
  shippingMethodNote: "روش‌های غیرفعال، نامعتبر یا متعلق به فروشنده دیگر نمایش داده نمی‌شوند.",
  shippingMethodStale: "این روش دیگر فعال یا منتشرشده نیست.",
};

export const shippingMessagesEn: ShippingMessages = {
  shippingMode: "Method selection mode",
  shippingModeAuto: "Automatic",
  shippingModeAutoHint: "All allowed methods for this type",
  shippingModeSpecific: "Specific",
  shippingModeSpecificHint: "Only the methods you select",
  shippingMethods: "Shipping methods",
  shippingAddMethod: "Add shipping method",
  shippingSearchMethod: "Search method name or code",
  shippingNoMethodResults: "No method matches this search",
  shippingNoMethods: "No eligible shipping method is available.",
  shippingMethodsError: "The shipping method list is unavailable.",
  shippingNoSelection: "No method selected yet.",
  shippingSelectAtLeastOne: "Select at least one shipping method.",
  shippingRemoveMethod: "Remove",
  shippingLockedAuto: "Automatic mode applies every allowed method.",
  shippingMethodNote: "Inactive, invalid or other-seller methods are not listed.",
  shippingMethodStale: "This method is no longer active or published.",
};
