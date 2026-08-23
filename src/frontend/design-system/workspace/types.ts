/**
 * ماشین حالت فرمان ناهمگام Workspace.
 * این نوع منطق دامنه یا فراخوانی API را حمل نمی‌کند؛ فقط وضعیت UI فرمان را قفل می‌کند.
 */
export type WorkspaceCommandState = "idle" | "confirming" | "submitting" | "succeeded" | "failed" | "conflicted";

/**
 * رویدادهای مجاز برای انتقال ماشین فرمان.
 * رویداد خارج از این مجموعه باید نادیده گرفته شود تا وضعیت نامعتبر ساخته نشود.
 */
export type WorkspaceCommandEvent = "confirm" | "submit" | "succeed" | "fail" | "conflict" | "reset";

/**
 * نتیجهٔ نمایش اقدام بدون SpiceDB.
 * `hidden` یعنی اقدام اصلاً در نوار فرمان نیست؛ `denied` یعنی دیده می‌شود ولی غیرفعال است.
 */
export type WorkspacePermission = "allowed" | "denied" | "hidden";

/**
 * گونهٔ خالی کنترل‌شده. پیام خالی نباید با خطای شبکه یا تعارض همزمان قاطی شود.
 */
export type WorkspaceEmptyKind = "no-data" | "no-permission" | "not-found" | "filtered" | "not-configured" | "unavailable";

/**
 * گونهٔ تعارض نسخه. معنای دامنهٔ بازگشت کالا اینجا تعریف نمی‌شود.
 */
export type WorkspaceConflictKind = "stale-version" | "concurrent-edit";

/**
 * بخش ناوبری generic. شناسه نباید به ماژول بک‌اند گره بخورد.
 */
export interface WorkspaceSection {
  /** شناسهٔ پایدار برای deep-link، نه برچسب نمایش. */
  id: string;
  /** برچسب قابل ترجمه در لایهٔ میزبان. */
  label: string;
}

/**
 * اقدام سلسله‌مراتبی Workspace: اصلی، ثانویه، مخرب، سرریز، یا زمینه‌ای.
 */
export interface WorkspaceAction {
  id: string;
  label: string;
  kind: "primary" | "secondary" | "destructive" | "overflow" | "contextual";
  permission: WorkspacePermission;
  /** در حال ارسال؛ باید کنترل را قفل کند تا دوبار کلیک ثبت نشود. */
  busy?: boolean;
  /** اقدام مخرب باید قبل از اجرا تأیید شود. */
  needsConfirmation?: boolean;
}

/**
 * رویداد عملیاتی قابل خواندن برای انسان. جایگزین Audit نیست.
 */
export interface WorkspaceActivityItem {
  id: string;
  at: string;
  actor: string;
  summary: string;
}

/**
 * ردپای حسابرسی جدا از Activity. متادیتا نباید راز یا دادهٔ حساس خام باشد.
 */
export interface WorkspaceAuditItem {
  id: string;
  at: string;
  actor: string;
  event: string;
  metadata?: string;
}

/**
 * وضعیت خلاصه در نوار وضعیت پوسته.
 */
export interface WorkspaceStatusItem {
  id: string;
  label: string;
  tone: "neutral" | "success" | "warning" | "danger";
}

/**
 * قرارداد برگشت master-detail: query فهرست باید با انتخاب جزئیات هم‌خوان بماند.
 */
export interface MasterDetailReturnState {
  /** serialization پرس‌وجوی فهرست، نه شیء دامنه. */
  listQuery: string;
  selectedId: string | null;
}

/**
 * برچسب‌های UI پوسته. متن فارسی/انگلیسی در messages جدا است، نه در primitive.
 */
export interface WorkspaceMessages {
  save: string;
  cancel: string;
  edit: string;
  retry: string;
  reload: string;
  discard: string;
  unsaved: string;
  permissionDenied: string;
  conflict: string;
  notFound: string;
  history: string;
  details: string;
  moreActions: string;
  confirmDestructive: string;
}
