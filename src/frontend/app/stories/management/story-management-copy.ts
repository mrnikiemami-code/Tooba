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
  confirmAction: string;
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
  loadingShops: string;
  noShopsFound: string;
  shopLoadFailed: string;
  shopScrollHint: string;
  sellerShopFixedPrefix: string;
  sellerShopFixedHint: string;
  sellerShopFallback: string;
  chooseShopTitle: string;
  chooseShopDetail: string;
  chooseShopAction: string;
  platformStorefront: string;
  destinationNone: string;
  destinationProduct: string;
  destinationShop: string;
  destinationExternal: string;
  destinationInternal: string;
  destinationUnavailable: string;
  destinationPrefix: string;
  externalLinkLabel: string;
  internalLinkLabel: string;
  ctaRequired: string;
  ctaInvalidUrl: string;
  ctaInvalidPath: string;
  productSearchPlaceholder: string;
  productSelectedPrefix: string;
  productUnknown: string;
  productSelectedFallback: string;
  productSelectedTitle: string;
  productSelectedBadge: string;
  productChangeHint: string;
  productOutsideResults: string;
  productRowsOf: string;
  pickFile: string;
  pickedFilePrefix: string;
  keepMediaHint: string;
  replaceMediaPick: string;
  replaceMediaHint: string;
  mediaReplaceFailed: string;
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
  slideNeedsOwnedMedia: string;
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
  /** Approved but not published yet — must never read as «در ویترین». */
  displayApprovedUnpublished: string;
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
  createWorkspaceHint: string;
  detailWorkspaceHint: string;
  editInfoHeading: string;
  storyFormHeading: string;
  storyFormHint: string;
  listHeading: string;
  listHint: string;
  saveForm: string;
  previewHeading: string;
  previewEmpty: string;
  previewPrevious: string;
  previewNext: string;
  previewPause: string;
  previewPlay: string;
  previewCta: string;
  previewMute: string;
  previewMediaError: string;
  previewUnmute: string;
  slidesEmptyHint: string;
  loadingProducts: string;
  loadMoreProducts: string;
  noProductsFound: string;
  productLoadFailed: string;
  productRetry: string;
  approveAndPublish: string;
  approveAndSchedule: string;
  approveConfirm: string;
  approveConfirmBody: string;
  approveBatchToast: string;
  rejectConfirmBody: string;
  rejectBatchToast: string;
  deleteBatchConfirmBody: string;
  bulkEligibleHeading: string;
  bulkSkippedHeading: string;
  bulkProcessableCount: string;
  bulkSelectedCount: string;
  bulkNoneEligible: string;
  bulkConfirmApprove: string;
  bulkConfirmReject: string;
  bulkConfirmDelete: string;
  bulkResultTitle: string;
  bulkResultClose: string;
  bulkOpApprove: string;
  bulkOpReject: string;
  bulkOpDelete: string;
  scheduleDisplay: string;
  publishNow: string;
  publishedToast: string;
  stopDisplay: string;
  returnToDraft: string;
  pendingHeading: string;
  pendingRetry: string;
  pendingContinueFailed: string;
  pendingExpandHint: string;
  pendingContinued: string;
  draftSaved: string;
  submittedToast: string;
  stoppedToast: string;
  returnedToast: string;
  actorMissing: string;
  detailLoadFailed: string;
  detailLoading: string;
  detailRetry: string;
  detailBack: string;
  stopFailed: string;
  returnFailed: string;
  pendingStorageBlocked: string;
  approvedScheduled: string;
  scheduleSaved: string;
  approvedPublished: string;
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
  confirmAction: "تأیید",
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
  loadingShops: "در حال بارگذاری فروشگاه‌ها…",
  noShopsFound: "فروشگاهی با این نام پیدا نشد.",
  shopLoadFailed: "بارگذاری فروشگاه‌ها ممکن نشد.",
  shopScrollHint: "برای دیدن فروشگاه‌های بیشتر اسکرول کنید.",
  sellerShopFixedPrefix: "فروشگاه:",
  sellerShopFixedHint: "این استوری فقط برای همین فروشگاه ثبت می‌شود.",
  sellerShopFallback: "فروشگاه فعلی شما",
  chooseShopTitle: "ابتدا فروشگاه را انتخاب کنید",
  chooseShopDetail: "برای دیدن و ساخت استوری، از پنل فروشنده فروشگاه خود را انتخاب کنید.",
  chooseShopAction: "بازگشت به پنل فروشنده",
  platformStorefront: "ویترین اصلی",
  destinationNone: "بدون لینک",
  destinationProduct: "محصول",
  destinationShop: "فروشگاه",
  destinationExternal: "پیوند بیرونی",
  destinationInternal: "صفحهٔ داخلی",
  destinationUnavailable: "مقصد در دسترس نیست",
  destinationPrefix: "مقصد:",
  externalLinkLabel: "نشانی پیوند",
  internalLinkLabel: "مسیر داخلی (مثلاً /products)",
  ctaRequired: "برای این مقصد یک مقدار معتبر وارد کنید.",
  ctaInvalidUrl: "نشانی پیوند باید با http:// یا https:// شروع شود.",
  ctaInvalidPath: "مسیر داخلی باید با / شروع شود و شامل // یا \\ نباشد.",
  productSearchPlaceholder: "جستجوی نام محصول در فهرست مجاز",
  productSelectedPrefix: "محصول انتخاب‌شده:",
  productSelectedTitle: "محصول انتخاب‌شده",
  productSelectedBadge: "انتخاب‌شده",
  productChangeHint: "برای تغییر، یک محصول دیگر را از فهرست انتخاب کنید.",
  productOutsideResults: "این محصول در نتایج فعلی نیست؛ انتخاب فعلی حفظ شده است.",
  productRowsOf: "از",
  productUnknown: "محصول (نامشخص)",
  productSelectedFallback: "محصول انتخاب‌شده",
  pickFile: "انتخاب فایل",
  pickedFilePrefix: "انتخاب‌شده:",
  keepMediaHint: "رسانهٔ فعلی حفظ می‌شود مگر اینکه بعداً مسیر تعویض رسانه را جداگانه استفاده کنید.",
  replaceMediaPick: "تعویض فایل این اسلاید",
  replaceMediaHint: "فقط اگر فایل تازه را انتخاب کنید، پس از آپلود موفق و ثبت، رسانهٔ قبلی جایگزین می‌شود.",
  mediaReplaceFailed: "آپلود فایل تازه تأیید نشد؛ رسانهٔ قبلی دست‌نخورده ماند.",
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
  slideNeedsOwnedMedia:
    "این اسلاید فقط لینک دارد و قابل ارسال نیست. فایل معتبر بارگذاری کنید یا اسلاید را حذف و دوباره اضافه کنید.",
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
  displayApprovedUnpublished: "تأییدشده، منتشر نشده",
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
  createWorkspaceHint: "عنوان، زبان و بازهٔ نمایش را ثبت کنید؛ سپس اسلایدها را اضافه کنید.",
  detailWorkspaceHint: "مشخصات، اقدام‌های بازبینی و اسلایدهای همین استوری.",
  editInfoHeading: "مشخصات استوری",
  storyFormHeading: "مشخصات استوری",
  storyFormHint: "عنوان، زبان و بازهٔ نمایش را ثبت کنید؛ سپس اسلایدها را مدیریت کنید.",
  listHeading: "فهرست استوری‌ها",
  listHint: "برای اقدام گروهی، ردیف‌ها را انتخاب کنید.",
  saveForm: "ذخیره",
  previewHeading: "پیش‌نمایش اسلایدها",
  previewEmpty: "هنوز اسلایدی نیست.",
  previewPrevious: "اسلاید قبلی",
  previewNext: "اسلاید بعدی",
  previewPause: "توقف پخش",
  previewPlay: "ادامهٔ پخش",
  previewCta: "مشاهدهٔ مقصد",
  previewMute: "صدا خاموش",
  previewMediaError: "رسانهٔ این اسلاید بارگذاری نشد.",
  previewUnmute: "صدا روشن",
  slidesEmptyHint: "پس از ذخیره، اسلاید عکس یا ویدیو و مقصد را اضافه کنید.",
  loadingProducts: "در حال بارگذاری…",
  loadMoreProducts: "نمایش محصولات بیشتر",
  noProductsFound: "محصولی برای این فروشگاه یا این جستجو پیدا نشد.",
  productLoadFailed: "دریافت فهرست محصولات ممکن نشد.",
  productRetry: "تلاش دوباره",
  approveAndPublish: "تأیید و انتشار",
  approveAndSchedule: "تأیید و زمان‌بندی",
  approveConfirm: "تأیید استوری‌ها",
  approveConfirmBody: "با تأیید، بازهٔ انتشار حفظ می‌شود و وضعیت بازبینی به‌روز می‌شود.",
  approveBatchToast: "تأیید",
  rejectConfirmBody: "یک دلیل برای همهٔ استوری‌های قابل رد اعمال می‌شود.",
  rejectBatchToast: "رد",
  deleteBatchConfirmBody: "حذف قطعی است و قابل بازگشت نیست.",
  bulkEligibleHeading: "قابل اجرا",
  bulkSkippedHeading: "اجرا نمی‌شود",
  bulkProcessableCount: "قابل اجرا",
  bulkSelectedCount: "انتخاب‌شده",
  bulkNoneEligible: "هیچ‌کدام از استوری‌های انتخاب‌شده برای این اقدام مناسب نیستند.",
  bulkConfirmApprove: "تأیید",
  bulkConfirmReject: "رد استوری‌ها",
  bulkConfirmDelete: "حذف استوری‌ها",
  bulkResultTitle: "نتیجهٔ اقدام‌ها",
  bulkResultClose: "بستن نتیجه",
  bulkOpApprove: "تأیید",
  bulkOpReject: "رد",
  bulkOpDelete: "حذف",
  scheduleDisplay: "زمان‌بندی نمایش",
  publishNow: "انتشار فوری",
  publishedToast: "منتشر شد.",
  stopDisplay: "توقف نمایش",
  returnToDraft: "بازگشت به پیش‌نویس",
  pendingHeading: "اقدام ناتمام",
  pendingRetry: "ادامه / ارسال مجدد",
  pendingContinueFailed: "ارسال مجدد ممکن نشد.",
  pendingExpandHint: "باز کردن",
  pendingContinued: "اقدام ناتمام ادامه یافت.",
  draftSaved: "ذخیره شد.",
  submittedToast: "برای تأیید ارسال شد.",
  stoppedToast: "متوقف شد.",
  returnedToast: "به پیش‌نویس برگشت.",
  actorMissing: "شناسهٔ کاربر برای ثبت امن اقدام مشخص نیست.",
  detailLoadFailed: "بارگذاری جزئیات ممکن نشد.",
  detailLoading: "در حال بارگذاری جزئیات…",
  detailRetry: "تلاش دوباره",
  detailBack: "بازگشت به فهرست",
  stopFailed: "توقف ممکن نشد.",
  returnFailed: "بازگشت ممکن نشد.",
  pendingStorageBlocked: "اقدام ناتمام قبلی هنوز باز است یا ذخیره ممکن نیست.",
  approvedScheduled: "تأیید و زمان‌بندی شد.",
  scheduleSaved: "زمان‌بندی ذخیره شد.",
  approvedPublished: "تأیید و منتشر شد.",
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
  confirmAction: "Confirm",
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
  loadingShops: "Loading shops…",
  noShopsFound: "No shop matches this search.",
  shopLoadFailed: "Shops could not be loaded.",
  shopScrollHint: "Scroll for more shops.",
  sellerShopFixedPrefix: "Shop:",
  sellerShopFixedHint: "This story is recorded only for your current shop.",
  sellerShopFallback: "Your current shop",
  chooseShopTitle: "Choose a shop first",
  chooseShopDetail: "Open the seller panel and choose your shop before viewing or creating stories.",
  chooseShopAction: "Back to seller panel",
  platformStorefront: "Main storefront",
  destinationNone: "No link",
  destinationProduct: "Product",
  destinationShop: "Shop",
  destinationExternal: "External link",
  destinationInternal: "Internal page",
  destinationUnavailable: "Destination unavailable",
  destinationPrefix: "Destination:",
  externalLinkLabel: "Link URL",
  internalLinkLabel: "Internal path (e.g. /products)",
  ctaRequired: "Enter a valid destination value.",
  ctaInvalidUrl: "Link URL must start with http:// or https://.",
  ctaInvalidPath: "Internal path must start with / and must not contain // or \\.",
  productSearchPlaceholder: "Search eligible product names",
  productSelectedPrefix: "Selected product:",
  productSelectedTitle: "Selected product",
  productSelectedBadge: "Selected",
  productChangeHint: "Choose another product from the list to change it.",
  productOutsideResults: "This product is not in the current results; the current selection is kept.",
  productRowsOf: "of",
  productUnknown: "Product (unknown)",
  productSelectedFallback: "Selected product",
  pickFile: "Choose file",
  pickedFilePrefix: "Selected:",
  keepMediaHint: "Current media is kept unless you deliberately replace it later.",
  replaceMediaPick: "Replace this slide file",
  replaceMediaHint: "Only when you pick a new file does it replace the old media, after a successful upload and update.",
  mediaReplaceFailed: "The new file upload was not confirmed; the previous media is unchanged.",
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
  slideNeedsOwnedMedia:
    "This slide is URL-only and cannot be submitted. Upload a valid file or remove the slide and add it again.",
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
  displayApprovedUnpublished: "Approved, not published",
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
  createWorkspaceHint: "Set the title, language and publication window, then add slides.",
  detailWorkspaceHint: "Story details, review actions and slides in one place.",
  editInfoHeading: "Story details",
  storyFormHeading: "Story details",
  storyFormHint: "Set the title, language and publication window, then manage slides.",
  listHeading: "Stories",
  listHint: "Select rows to run a bulk action.",
  saveForm: "Save",
  previewHeading: "Slide preview",
  previewEmpty: "No slides yet.",
  previewPrevious: "Previous slide",
  previewNext: "Next slide",
  previewPause: "Pause preview",
  previewPlay: "Resume preview",
  previewCta: "Open destination",
  previewMute: "Mute",
  previewMediaError: "This slide media could not be loaded.",
  previewUnmute: "Unmute",
  slidesEmptyHint: "After saving, add a photo or video slide and its destination.",
  loadingProducts: "Loading…",
  loadMoreProducts: "Show more products",
  noProductsFound: "No product was found for this shop or search.",
  productLoadFailed: "Products could not be loaded.",
  productRetry: "Try again",
  approveAndPublish: "Approve and publish",
  approveAndSchedule: "Approve and schedule",
  approveConfirm: "Approve stories",
  approveConfirmBody: "Approval keeps each publication window and updates review status.",
  approveBatchToast: "Approve",
  rejectConfirmBody: "One reason applies to every rejectable story in this selection.",
  rejectBatchToast: "Reject",
  deleteBatchConfirmBody: "Deletion is permanent and cannot be undone.",
  bulkEligibleHeading: "Processable",
  bulkSkippedHeading: "Will not run",
  bulkProcessableCount: "Processable",
  bulkSelectedCount: "Selected",
  bulkNoneEligible: "None of the selected stories can use this action.",
  bulkConfirmApprove: "Approve",
  bulkConfirmReject: "Reject stories",
  bulkConfirmDelete: "Delete stories",
  bulkResultTitle: "Action results",
  bulkResultClose: "Close results",
  bulkOpApprove: "Approve",
  bulkOpReject: "Reject",
  bulkOpDelete: "Delete",
  scheduleDisplay: "Schedule display",
  publishNow: "Publish now",
  publishedToast: "Published.",
  stopDisplay: "Stop display",
  returnToDraft: "Return to draft",
  pendingHeading: "Unfinished action",
  pendingRetry: "Continue / retry",
  pendingContinueFailed: "Retry could not be sent.",
  pendingExpandHint: "Open",
  pendingContinued: "Unfinished action resumed.",
  draftSaved: "Saved.",
  submittedToast: "Sent for approval.",
  stoppedToast: "Stopped.",
  returnedToast: "Returned to draft.",
  actorMissing: "A user identity is required to record this action safely.",
  detailLoadFailed: "Could not load the details.",
  detailLoading: "Loading details…",
  detailRetry: "Try again",
  detailBack: "Back to list",
  stopFailed: "Stop failed.",
  returnFailed: "Return to draft failed.",
  pendingStorageBlocked: "A previous unfinished action is still open or storage is unavailable.",
  approvedScheduled: "Approved and scheduled.",
  scheduleSaved: "Schedule saved.",
  approvedPublished: "Approved and published.",
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
