import { formatAdminStatus } from "./admin-api";

export const SUPPLY_STATUS_VALUES = [
  "Reserved",
  "AvailableForReacquire",
  "Unavailable",
  "PartiallyUnavailable",
  "Fulfilled",
  "NotApplicable",
] as const;

export type AdminSupplyStatusKind = (typeof SUPPLY_STATUS_VALUES)[number];

export type AdminSupplyLineShortage = {
  itemTitle: string | null;
  unitCode: string | null;
  required: number;
  available: number;
  shortage: number;
};

export function formatAdminSupplyStatus(status: string | null | undefined): string {
  return formatAdminStatus(status || "NotApplicable");
}

export function adminSupplyMessageFa(status: string | null | undefined): string {
  switch (status) {
    case "Reserved":
      return "موجودی موردنیاز این سفارش رزرو شده است.";
    case "AvailableForReacquire":
      return "رزرو قبلی فعال نیست، اما موجودی لازم در حال حاضر قابل تأمین است.";
    case "Unavailable":
    case "PartiallyUnavailable":
      return "یک یا چند قلم این سفارش در حال حاضر قابل تأمین نیست.";
    case "Fulfilled":
      return "موجودی موردنیاز این سفارش تأمین و تکمیل شده است.";
    default:
      return "تأمین موجودی برای این سفارش موضوعیت ندارد.";
  }
}

export function adminSupplyBadgeClass(status: string | null | undefined): string {
  switch (status) {
    case "Reserved":
    case "Fulfilled":
      return "bg-emerald-50 text-emerald-700";
    case "AvailableForReacquire":
      return "bg-amber-50 text-amber-800";
    case "Unavailable":
    case "PartiallyUnavailable":
      return "bg-red-50 text-red-700";
    default:
      return "bg-gray-100 text-gray-700";
  }
}

export const supplyStatusEnumOptions = SUPPLY_STATUS_VALUES.map((value) => ({
  value,
  label: formatAdminSupplyStatus(value),
}));
