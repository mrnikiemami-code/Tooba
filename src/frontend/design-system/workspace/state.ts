import type { MasterDetailReturnState, WorkspaceCommandEvent, WorkspaceCommandState, WorkspacePermission } from "./types";

/**
 * بخش را کثیف می‌کند تا خروج بدون ذخیره قابل تشخیص باشد.
 */
export function markSectionDirty(dirty: ReadonlySet<string>, sectionId: string): Set<string> {
  return new Set(dirty).add(sectionId);
}

/** پس از ذخیره یا انصراف، کثیفی بخش را پاک می‌کند. */
export function clearSectionDirty(dirty: ReadonlySet<string>, sectionId: string): Set<string> {
  const next = new Set(dirty);
  next.delete(sectionId);
  return next;
}

/** اگر حداقل یک بخش ذخیره نشده باشد، خروج از Workspace باید تأیید شود. */
export function hasUnsavedChanges(dirty: ReadonlySet<string>): boolean {
  return dirty.size > 0;
}

/**
 * جابه‌جایی بخش وقتی دادهٔ ذخیره نشده وجود دارد مسدود می‌شود.
 * همان بخش دوباره انتخاب شود، گارد لازم نیست.
 */
export function shouldBlockNavigation(dirty: ReadonlySet<string>, currentSectionId: string, targetSectionId: string): boolean {
  return hasUnsavedChanges(dirty) && currentSectionId !== targetSectionId;
}

/**
 * اجازهٔ اقدام را از پرچم‌های لایهٔ ویژگی می‌سازد. SpiceDB اینجا صدا زده نمی‌شود.
 */
export function resolveWorkspaceAction(
  canView: boolean,
  canEdit: boolean,
  canExecute: boolean,
  kind: "view" | "edit" | "execute",
): WorkspacePermission {
  if (!canView) {
    return "hidden";
  }
  if (kind === "view") {
    return "allowed";
  }
  if (kind === "edit") {
    return canEdit ? "allowed" : "denied";
  }
  return canExecute ? "allowed" : "denied";
}

/**
 * انتقال قطعی ماشین فرمان. رویداد نامعتبر وضعیت فعلی را نگه می‌دارد تا UI پاره نشود.
 */
export function nextCommandState(current: WorkspaceCommandState, event: WorkspaceCommandEvent): WorkspaceCommandState {
  switch (event) {
    case "confirm":
      return current === "idle" ? "confirming" : current;
    case "submit":
      return current === "idle" || current === "confirming" ? "submitting" : current;
    case "succeed":
      return current === "submitting" ? "succeeded" : current;
    case "fail":
      return current === "submitting" ? "failed" : current;
    case "conflict":
      return current === "submitting" ? "conflicted" : current;
    case "reset":
      return "idle";
    default:
      return current;
  }
}

/** بخش فعال را برای deep-link پایدار سریال می‌کند. */
export function serializeWorkspaceNavigation(sectionId: string): string {
  return JSON.stringify({ sectionId });
}

/** ورودی خراب را رد می‌کند تا ناوبری به بخش نامعلوم نرود. */
export function deserializeWorkspaceNavigation(raw: string): { sectionId: string } {
  const parsed = JSON.parse(raw) as { sectionId?: unknown };
  if (typeof parsed.sectionId !== "string" || parsed.sectionId.length === 0) {
    throw new Error("workspace navigation requires sectionId");
  }
  return { sectionId: parsed.sectionId };
}

/** query فهرست و شناسهٔ انتخاب را با هم نگه می‌دارد تا برگشت از جزئیات فیلتر را از دست ندهد. */
export function serializeMasterDetailReturn(state: MasterDetailReturnState): string {
  return JSON.stringify(state);
}

/** مقادیر ناقص را به query خالی و انتخاب تهی کاهش می‌دهد، نه به شیء دامنه. */
export function deserializeMasterDetailReturn(raw: string): MasterDetailReturnState {
  const parsed = JSON.parse(raw) as MasterDetailReturnState;
  return { listQuery: parsed.listQuery ?? "", selectedId: parsed.selectedId ?? null };
}
