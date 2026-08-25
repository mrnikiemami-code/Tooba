import { customerAuthHeaders, mapCustomerProfile, type CustomerProfilePage } from "./customer-api.ts";

/** ورودی ویرایش پروفایل؛ email/mobile/password/nationalCode ندارد. */
export interface CustomerProfileWriteInput {
  displayName: string;
  firstName?: string;
  lastName?: string;
  birthDate?: string;
  bio?: string;
}

/** خطای قابل تشخیص API پروفایل. */
export class CustomerProfileApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function sanitizeWrite(input: CustomerProfileWriteInput): Record<string, string> {
  const body: Record<string, string> = { displayName: input.displayName.trim() };
  if (input.firstName?.trim()) body.firstName = input.firstName.trim();
  if (input.lastName?.trim()) body.lastName = input.lastName.trim();
  if (input.birthDate?.trim()) body.birthDate = input.birthDate.trim();
  if (input.bio?.trim()) body.bio = input.bio.trim();
  return body;
}

/** پیام خطای کاربرپسند برای UI پروفایل. */
export function customerProfileErrorMessage(error: unknown): string {
  if (error instanceof CustomerProfileApiError) {
    if (error.status === 401) return "برای ویرایش پروفایل نشست معتبر لازم است.";
    if (error.status === 400) return "اطلاعات واردشده معتبر نیست.";
    return error.message || "ذخیرهٔ پروفایل انجام نشد.";
  }
  return "ارتباط با سرور برقرار نشد.";
}

/** پروفایل Actor جاری را به‌روز می‌کند. */
export async function saveCustomerProfile(input: CustomerProfileWriteInput): Promise<CustomerProfilePage> {
  const response = await fetch("/v1/customer/profile", {
    method: "PUT",
    headers: customerAuthHeaders(true),
    body: JSON.stringify(sanitizeWrite(input)),
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    const title = payload && typeof payload === "object" && "title" in payload
      ? String((payload as Record<string, unknown>).title)
      : "ذخیرهٔ پروفایل انجام نشد.";
    throw new CustomerProfileApiError(response.status, title);
  }
  const mapped = mapCustomerProfile(payload);
  if (!mapped) throw new CustomerProfileApiError(response.status, "پاسخ پروفایل نامعتبر است.");
  return mapped;
}

/** نام کامل Shopeiva را به displayName و اجزای اختیاری تقسیم می‌کند. */
export function profileNameFromDisplay(displayName: string): CustomerProfileWriteInput {
  const trimmed = displayName.trim();
  const parts = trimmed.split(/\s+/);
  if (parts.length <= 1) {
    return { displayName: trimmed, firstName: trimmed };
  }
  return {
    displayName: trimmed,
    firstName: parts[0],
    lastName: parts.slice(1).join(" "),
  };
}

/** فرم Shopeiva را به payload API نگاشت می‌کند. */
export function profileFormToWrite(form: {
  name: string;
  birthDate?: string;
  bio?: string;
}): CustomerProfileWriteInput {
  const base = profileNameFromDisplay(form.name);
  return {
    ...base,
    birthDate: form.birthDate?.trim() || undefined,
    bio: form.bio?.trim() || undefined,
  };
}
