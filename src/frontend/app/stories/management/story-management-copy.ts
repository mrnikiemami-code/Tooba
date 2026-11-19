/**
 * Shared Story workspace copy — Admin and Seller stay aligned.
 * Locale catalogs follow the WorkspaceMessages pattern (fa default + en keys)
 * so UI expansion does not scatter literals through the screen.
 */

export type StoryUiLocale = "fa" | "en";

export type StoryCopy = {
  adminTitle: string;
  adminDescription: string;
  sellerTitle: string;
  sellerDescription: string;
  createButton: string;
  createModalTitle: string;
  createAndPublish: string;
  createDraft: string;
  cancel: string;
  close: string;
  details: string;
  enable: string;
  disable: string;
  submit: string;
  approve: string;
  reject: string;
  rejectPrompt: string;
  rejectReasonRequired: string;
  scheduleStart: string;
  scheduleEnd: string;
  saveSchedule: string;
  itemsHeading: string;
  addItem: string;
  editItem: string;
  editSlideTitle: string;
  newSlideTitle: string;
  saveSlideChanges: string;
  saveTitleLocale: string;
  rejectionReason: string;
  listCountSuffix: string;
  loadErrorTitle: string;
  filterAll: string;
  filterPending: string;
  originHeader: string;
  reviewHeader: string;
  sellerOwnerHeader: string;
  placementLegend: string;
  mainStorefront: string;
  forOneShop: string;
  shopSearchPlaceholder: string;
  shopPickRequired: string;
  selectedShopPrefix: string;
  sellerShopFixedPrefix: string;
  sellerShopFixedHint: string;
  sellerShopFallback: string;
  platformStorefront: string;
  destinationNone: string;
  destinationProduct: string;
  destinationShop: string;
  destinationExternal: string;
  destinationUnavailable: string;
  destinationPrefix: string;
  productSearchPlaceholder: string;
  productSelectedPrefix: string;
  productUnknown: string;
  productSelectedFallback: string;
  pickFile: string;
  pickedFilePrefix: string;
  keepMediaHint: string;
  captionLabel: string;
  durationLabel: string;
  publicationWindowHeading: string;
  publicationStartLabel: string;
  publicationEndLabel: string;
  publicationNoEndToggle: string;
  deleteStory: string;
  deleteStoryConfirm: string;
  deletedToast: string;
  deleteUnavailable: string;
  mediaRequiredHint: string;
  localPreviewLabel: string;
  displayExpired: string;
  ctaLabel: string;
  mediaLabel: string;
  slideAdded: string;
  slideUpdated: string;
  slideMissing: string;
  sendFailed: string;
  titleLabel: string;
  languageLabel: string;
  localeDefaultSuffix: string;
  savedToast: string;
  reorderToast: string;
  reorderFailed: string;
  moveUp: string;
  removeSlide: string;
  photoLabel: string;
  videoLabel: string;
  secondsSuffix: string;
  columnTitle: string;
  columnShop: string;
  columnStatus: string;
  columnSlides: string;
  columnStart: string;
  columnEnd: string;
  columnUpdated: string;
  columnActions: string;
  rowReview: string;
  rowSubmit: string;
  noEnd: string;
  displayOnStorefront: string;
  displayAwaiting: string;
  displayRejected: string;
  displayScheduled: string;
  displayStopped: string;
  displayDraft: string;
  deniedTitle: string;
  deniedDetail: string;
  breadcrumbHome: string;
  backToList: string;
  filterReviewLabel: string;
  filterApproved: string;
  filterRejected: string;
  shopNameUnavailable: string;
  locationPrefix: string;
  createWorkspaceTitle: string;
  saveForm: string;
  previewHeading: string;
  previewEmpty: string;
  slidesEmptyHint: string;
  loadingProducts: string;
  loadMoreProducts: string;
  approveAndPublish: string;
  approveAndSchedule: string;
  scheduleDisplay: string;
  stopDisplay: string;
  returnToDraft: string;
  pendingHeading: string;
  pendingRetry: string;
  pendingContinueFailed: string;
  draftSaved: string;
  submittedToast: string;
  stoppedToast: string;
  returnedToast: string;
  actorMissing: string;
  titleRequired: string;
  productIdInvalid: string;
  unsavedLeaveTitle: string;
  unsavedLeaveBody: string;
  unsavedStay: string;
  unsavedDiscard: string;
  originCreatorAdmin: string;
  originCreatorSeller: string;
};

export const faStoryCopy: StoryCopy = {
  adminTitle: "استوری‌ها",
  adminDescription: "ساخت و انتشار استوری برای ویترین اصلی یا فروشگاه‌ها",
  sellerTitle: "استوری‌ها",
  sellerDescription: "پیش‌نویس اسلایدها و ارسال برای تأیید",
  createButton: "استوری جدید",
  createModalTitle: "ایجاد استوری",
  createAndPublish: "ایجاد و فعال‌سازی",
  createDraft: "ایجاد پیش‌نویس",
  cancel: "انصراف",
  close: "بستن",
  details: "جزئیات",
  enable: "فعال",
  disable: "غیرفعال",
  submit: "ارسال برای تأیید",
  approve: "تأیید",
  reject: "رد",
  rejectPrompt: "دلیل رد استوری را وارد کنید",
  rejectReasonRequired: "رد بدون دلیل مجاز نیست",
  scheduleStart: "شروع (اختیاری)",
  scheduleEnd: "پایان (اختیاری)",
  saveSchedule: "ذخیره زمان‌بندی",
  itemsHeading: "اسلایدها",
  addItem: "افزودن اسلاید",
  editItem: "ویرایش",
  editSlideTitle: "ویرایش اسلاید",
  newSlideTitle: "اسلاید جدید",
  saveSlideChanges: "ذخیره تغییرات",
  saveTitleLocale: "ذخیره پیش‌نویس",
  rejectionReason: "دلیل رد",
  listCountSuffix: "استوری",
  loadErrorTitle: "استوری‌ها خوانده نشد",
  filterAll: "همه",
  filterPending: "در انتظار تأیید",
  originHeader: "منبع",
  reviewHeader: "بازبینی",
  sellerOwnerHeader: "فروشنده",
  placementLegend: "محل نمایش",
  mainStorefront: "ویترین اصلی",
  forOneShop: "برای یک فروشگاه",
  shopSearchPlaceholder: "جستجوی نام فروشگاه",
  shopPickRequired: "فروشگاه را از فهرست نام‌ها انتخاب کنید.",
  selectedShopPrefix: "انتخاب‌شده:",
  sellerShopFixedPrefix: "فروشگاه:",
  sellerShopFixedHint: "این استوری فقط برای همین فروشگاه ثبت می‌شود.",
  sellerShopFallback: "فروشگاه فعلی شما",
  platformStorefront: "ویترین اصلی",
  destinationNone: "بدون لینک",
  destinationProduct: "محصول",
  destinationShop: "فروشگاه",
  destinationExternal: "پیوند",
  destinationUnavailable: "مقصد در دسترس نیست",
  destinationPrefix: "مقصد:",
  productSearchPlaceholder: "جستجوی نام محصول در فهرست مجاز",
  productSelectedPrefix: "محصول انتخاب‌شده:",
  productUnknown: "محصول (نامشخص)",
  productSelectedFallback: "محصول انتخاب‌شده",
  pickFile: "انتخاب فایل",
  pickedFilePrefix: "انتخاب‌شده:",
  keepMediaHint: "رسانهٔ فعلی حفظ می‌شود مگر اینکه بعداً مسیر تعویض رسانه را جداگانه استفاده کنید.",
  captionLabel: "زیرنویس",
  durationLabel: "مدت نمایش این عکس (ثانیه)",
  publicationWindowHeading: "بازهٔ انتشار",
  publicationStartLabel: "شروع نمایش",
  publicationEndLabel: "پایان نمایش",
  publicationNoEndToggle: "بدون پایان",
  deleteStory: "حذف استوری",
  deleteStoryConfirm: "این استوری و همهٔ اسلایدهایش حذف می‌شود. ادامه می‌دهید؟",
  deletedToast: "استوری حذف شد.",
  deleteUnavailable: "حذف در این وضعیت مجاز نیست؛ از توقف یا بازگشت به پیش‌نویس استفاده کنید.",
  mediaRequiredHint: "برای ارسال، هر اسلاید باید از بارگذاری رسانهٔ مجاز ثبت شده باشد.",
  localPreviewLabel: "پیش‌نمایش انتخاب‌شده",
  displayExpired: "منقضی",
  ctaLabel: "مقصد دکمه",
  mediaLabel: "عکس یا ویدیو",
  slideAdded: "اسلاید اضافه شد.",
  slideUpdated: "اسلاید به‌روز شد.",
  slideMissing: "اسلاید پیدا نشد.",
  sendFailed: "ارسال انجام نشد.",
  titleLabel: "عنوان",
  languageLabel: "زبان",
  localeDefaultSuffix: " (پیش‌فرض)",
  savedToast: "ذخیره شد.",
  reorderToast: "ترتیب به‌روز شد.",
  reorderFailed: "جابه‌جایی ممکن نشد.",
  moveUp: "بالاتر",
  removeSlide: "حذف",
  photoLabel: "عکس",
  videoLabel: "ویدیو",
  secondsSuffix: "ثانیه",
  columnTitle: "عنوان",
  columnShop: "فروشگاه",
  columnStatus: "وضعیت",
  columnSlides: "اسلایدها",
  columnStart: "شروع نمایش",
  columnEnd: "پایان نمایش",
  columnUpdated: "آخرین تغییر",
  columnActions: "عملیات",
  rowReview: "بررسی",
  rowSubmit: "ارسال برای تأیید",
  noEnd: "بدون پایان",
  displayOnStorefront: "در ویترین",
  displayAwaiting: "در انتظار تأیید",
  displayRejected: "رد شده",
  displayScheduled: "زمان‌بندی‌شده",
  displayStopped: "متوقف",
  displayDraft: "پیش‌نویس",
  deniedTitle: "دسترسی مجاز نیست",
  deniedDetail: "برای این بخش مجوز ندارید. از منوی پنل خارج نشوید؛ در صورت نیاز دوباره وارد شوید.",
  breadcrumbHome: "خانه",
  backToList: "برگشت به فهرست",
  filterReviewLabel: "وضعیت بررسی",
  filterApproved: "تأییدشده",
  filterRejected: "ردشده",
  shopNameUnavailable: "نام فروشگاه در دسترس نیست",
  locationPrefix: "محل نمایش:",
  createWorkspaceTitle: "استوری جدید",
  saveForm: "ذخیره",
  previewHeading: "پیش‌نمایش اسلایدها",
  previewEmpty: "هنوز اسلایدی نیست.",
  slidesEmptyHint: "پس از ذخیره، اسلاید عکس یا ویدیو و مقصد را اضافه کنید.",
  loadingProducts: "در حال بارگذاری…",
  loadMoreProducts: "نمایش محصولات بیشتر",
  approveAndPublish: "تأیید و انتشار",
  approveAndSchedule: "تأیید و زمان‌بندی",
  scheduleDisplay: "زمان‌بندی نمایش",
  stopDisplay: "توقف نمایش",
  returnToDraft: "بازگشت به پیش‌نویس",
  pendingHeading: "اقدام ناتمام",
  pendingRetry: "ادامه / ارسال مجدد",
  pendingContinueFailed: "ارسال مجدد ممکن نشد.",
  draftSaved: "ذخیره شد.",
  submittedToast: "برای تأیید ارسال شد.",
  stoppedToast: "متوقف شد.",
  returnedToast: "به پیش‌نویس برگشت.",
  actorMissing: "شناسهٔ کاربر برای ثبت امن اقدام مشخص نیست.",
  titleRequired: "عنوان را وارد کنید.",
  productIdInvalid: "این ردیف شناسهٔ محصول معتبری ندارد.",
  unsavedLeaveTitle: "تغییرات ذخیره‌نشده",
  unsavedLeaveBody: "اگر الان برگردید، تغییرات ذخیره‌نشده از بین می‌روند.",
  unsavedStay: "ادامهٔ ویرایش",
  unsavedDiscard: "خروج بدون ذخیره",
  originCreatorAdmin: "مدیر",
  originCreatorSeller: "فروشنده",
};

export const enStoryCopy: StoryCopy = {
  adminTitle: "Stories",
  adminDescription: "Create, publish, and manage storefront story items",
  sellerTitle: "Stories",
  sellerDescription: "Draft items and submit for review",
  createButton: "New story",
  createModalTitle: "Create story",
  createAndPublish: "Create and publish",
  createDraft: "Create draft",
  cancel: "Cancel",
  close: "Close",
  details: "Details",
  enable: "Enable",
  disable: "Disable",
  submit: "Submit for approval",
  approve: "Approve",
  reject: "Reject",
  rejectPrompt: "Enter a rejection reason",
  rejectReasonRequired: "Rejection requires a reason",
  scheduleStart: "Start (optional)",
  scheduleEnd: "End (optional)",
  saveSchedule: "Save schedule",
  itemsHeading: "Slides",
  addItem: "Add slide",
  editItem: "Edit",
  editSlideTitle: "Edit slide",
  newSlideTitle: "New slide",
  saveSlideChanges: "Save changes",
  saveTitleLocale: "Save draft",
  rejectionReason: "Rejection reason",
  listCountSuffix: "stories",
  loadErrorTitle: "Stories could not be loaded",
  filterAll: "All",
  filterPending: "Awaiting approval",
  originHeader: "Origin",
  reviewHeader: "Review",
  sellerOwnerHeader: "Seller",
  placementLegend: "Placement",
  mainStorefront: "Main storefront",
  forOneShop: "For one shop",
  shopSearchPlaceholder: "Search shop name",
  shopPickRequired: "Pick a shop from the named list.",
  selectedShopPrefix: "Selected:",
  sellerShopFixedPrefix: "Shop:",
  sellerShopFixedHint: "This story is recorded only for your current shop.",
  sellerShopFallback: "Your current shop",
  platformStorefront: "Main storefront",
  destinationNone: "No link",
  destinationProduct: "Product",
  destinationShop: "Shop",
  destinationExternal: "Link",
  destinationUnavailable: "Destination unavailable",
  destinationPrefix: "Destination:",
  productSearchPlaceholder: "Search eligible product names",
  productSelectedPrefix: "Selected product:",
  productUnknown: "Product (unknown)",
  productSelectedFallback: "Selected product",
  pickFile: "Choose file",
  pickedFilePrefix: "Selected:",
  keepMediaHint: "Current media is kept unless you deliberately replace it later.",
  captionLabel: "Caption",
  durationLabel: "Photo display duration (seconds)",
  publicationWindowHeading: "Publication window",
  publicationStartLabel: "Starts",
  publicationEndLabel: "Ends",
  publicationNoEndToggle: "No end",
  deleteStory: "Delete story",
  deleteStoryConfirm: "This story and all its slides will be deleted. Continue?",
  deletedToast: "Story deleted.",
  deleteUnavailable: "Delete is not available in this state; use stop or return to draft.",
  mediaRequiredHint: "Each slide must be registered via allowed media upload before submit.",
  localPreviewLabel: "Selected preview",
  displayExpired: "Expired",
  ctaLabel: "Button destination",
  mediaLabel: "Photo or video",
  slideAdded: "Slide added.",
  slideUpdated: "Slide updated.",
  slideMissing: "Slide not found.",
  sendFailed: "Send failed.",
  titleLabel: "Title",
  languageLabel: "Language",
  localeDefaultSuffix: " (default)",
  savedToast: "Saved.",
  reorderToast: "Order updated.",
  reorderFailed: "Reorder failed.",
  moveUp: "Move up",
  removeSlide: "Remove",
  photoLabel: "Photo",
  videoLabel: "Video",
  secondsSuffix: "sec",
  columnTitle: "Title",
  columnShop: "Shop",
  columnStatus: "Status",
  columnSlides: "Slides",
  columnStart: "Starts",
  columnEnd: "Ends",
  columnUpdated: "Updated",
  columnActions: "Actions",
  rowReview: "Review",
  rowSubmit: "Submit for approval",
  noEnd: "No end",
  displayOnStorefront: "On storefront",
  displayAwaiting: "Awaiting approval",
  displayRejected: "Rejected",
  displayScheduled: "Scheduled",
  displayStopped: "Stopped",
  displayDraft: "Draft",
  deniedTitle: "Access denied",
  deniedDetail: "You do not have permission for this section. Stay in the panel menu; sign in again if needed.",
  breadcrumbHome: "Home",
  backToList: "Back to list",
  filterReviewLabel: "Review status",
  filterApproved: "Approved",
  filterRejected: "Rejected",
  shopNameUnavailable: "Shop name unavailable",
  locationPrefix: "Shown on:",
  createWorkspaceTitle: "New story",
  saveForm: "Save",
  previewHeading: "Slide preview",
  previewEmpty: "No slides yet.",
  slidesEmptyHint: "After saving, add a photo or video slide and its destination.",
  loadingProducts: "Loading…",
  loadMoreProducts: "Show more products",
  approveAndPublish: "Approve and publish",
  approveAndSchedule: "Approve and schedule",
  scheduleDisplay: "Schedule display",
  stopDisplay: "Stop display",
  returnToDraft: "Return to draft",
  pendingHeading: "Unfinished action",
  pendingRetry: "Continue / retry",
  pendingContinueFailed: "Retry could not be sent.",
  draftSaved: "Saved.",
  submittedToast: "Sent for approval.",
  stoppedToast: "Stopped.",
  returnedToast: "Returned to draft.",
  actorMissing: "A user identity is required to record this action safely.",
  titleRequired: "Enter a title.",
  productIdInvalid: "This row has no valid product identity.",
  unsavedLeaveTitle: "Unsaved changes",
  unsavedLeaveBody: "If you leave now, unsaved changes will be lost.",
  unsavedStay: "Keep editing",
  unsavedDiscard: "Leave without saving",
  originCreatorAdmin: "Admin",
  originCreatorSeller: "Seller",
};

/** Status / origin / review labels per locale. */
export const STORY_STATUS_LABELS_BY_LOCALE: Record<StoryUiLocale, Record<string, string>> = {
  fa: {
    Draft: "پیش‌نویس",
    Scheduled: "زمان‌بندی",
    Active: "فعال",
    Expired: "منقضی",
    Disabled: "غیرفعال",
  },
  en: {
    Draft: "Draft",
    Scheduled: "Scheduled",
    Active: "Active",
    Expired: "Expired",
    Disabled: "Disabled",
  },
};

export const STORY_ORIGIN_LABELS_BY_LOCALE: Record<StoryUiLocale, Record<string, string>> = {
  fa: { Admin: "مدیر", Seller: "فروشنده" },
  en: { Admin: "Admin", Seller: "Seller" },
};

export const STORY_REVIEW_LABELS_BY_LOCALE: Record<StoryUiLocale, Record<string, string>> = {
  fa: {
    None: "—",
    Submitted: "در انتظار بازبینی",
    Approved: "تأییدشده",
    Rejected: "ردشده",
  },
  en: {
    None: "—",
    Submitted: "Awaiting review",
    Approved: "Approved",
    Rejected: "Rejected",
  },
};

/** Default fa catalogs kept for modules that still import the historical constants. */
export const STORY_STATUS_LABELS = STORY_STATUS_LABELS_BY_LOCALE.fa;
export const STORY_ORIGIN_LABELS = STORY_ORIGIN_LABELS_BY_LOCALE.fa;
export const STORY_REVIEW_LABELS = STORY_REVIEW_LABELS_BY_LOCALE.fa;

export function resolveStoryUiLocale(pathname?: string | null): StoryUiLocale {
  const path =
    pathname ??
    (typeof window !== "undefined" ? window.location.pathname : "") ??
    "";
  const seg = path.split("/").filter(Boolean)[0]?.toLowerCase();
  return seg === "en" ? "en" : "fa";
}

export function storyCopy(locale: StoryUiLocale = "fa"): StoryCopy {
  return locale === "en" ? enStoryCopy : faStoryCopy;
}

/** Back-compat default (fa). Prefer storyCopy(resolveStoryUiLocale()). */
export const STORY_COPY = faStoryCopy;
