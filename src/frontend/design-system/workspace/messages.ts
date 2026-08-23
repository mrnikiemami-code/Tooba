import type { WorkspaceMessages } from "./types";

/**
 * کاتالوگ پیام Workspace. فارسی پیش‌فرض ویترین است؛ کلید انگلیسی قرارداد i18n است.
 */
export const faWorkspaceMessages: WorkspaceMessages = {
  save: "ذخیره",
  cancel: "انصراف",
  edit: "ویرایش",
  retry: "تلاش دوباره",
  reload: "بارگذاری مجدد",
  discard: "دور ریختن",
  unsaved: "تغییرات ذخیره‌نشده",
  permissionDenied: "اجازه نیست",
  conflict: "تداخل نسخه",
  notFound: "یافت نشد",
  history: "تاریخچه",
  details: "جزئیات",
  moreActions: "اقدام‌های بیشتر",
  confirmDestructive: "این اقدام برگشت‌پذیر نیست. ادامه؟",
};

/** کاتالوگ انگلیسی هم‌کلید با فارسی؛ جهت را میزبان Workspace انتخاب می‌کند. */
export const enWorkspaceMessages: WorkspaceMessages = {
  save: "Save",
  cancel: "Cancel",
  edit: "Edit",
  retry: "Retry",
  reload: "Reload",
  discard: "Discard",
  unsaved: "Unsaved changes",
  permissionDenied: "Permission denied",
  conflict: "Version conflict",
  notFound: "Not found",
  history: "History",
  details: "Details",
  moreActions: "More actions",
  confirmDestructive: "This action cannot be undone. Continue?",
};
