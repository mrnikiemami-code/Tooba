/**
 * TB-SHIPPING-MVP-DESTINATION-LIVE-067 — نگاشت قطعی انتخاب مشتری به شناسهٔ پایدار Geography.
 *
 * این ماژول هیچ fetch و هیچ React ندارد؛ فقط منطق خالص است تا بتوان «بدون حدس» بودن را قطعی سنجید.
 *
 * قواعد مهم:
 * - تطبیق کد/برچسب فقط **دقیق** است (بدون fuzzy، بدون زیررشته، بدون فاصله‌گذاری تقریبی).
 * - اگر بیش از یک مکان تطبیق کند، نتیجه `null` است: ابهام هرگز جمع نمی‌شود.
 * - شناسهٔ حدسی ساخته نمی‌شود؛ در نبود تطبیق، مشتری به مسیر قدیمی متنی برمی‌گردد.
 */
import type { StorefrontGeographyPlace } from "./storefront-geography-api.ts";

export type GeographyDestinationIdentifiers = {
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
};

function normalize(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

function equals(stored: string, key: string): boolean {
  return stored.trim().toLowerCase() === key.toLowerCase();
}

/** یک مکان یکتا را با تطبیق دقیق کد یا برچسب پیدا می‌کند؛ ابهام ⇒ `null`. */
function matchUnique(
  places: readonly StorefrontGeographyPlace[],
  code: string | null,
  label: string | null,
): StorefrontGeographyPlace | null {
  if (!code && !label) return null;

  const byCode = code ? places.filter((p) => equals(p.code, code)) : [];
  const candidates = byCode.length > 0
    ? byCode
    : label
      ? places.filter((p) => equals(p.label, label))
      : [];

  return candidates.length === 1 ? candidates[0] : null;
}

/**
 * کشور پایهٔ فروشگاه (ایران) را با تطبیق دقیق **کد** پیدا می‌کند. کد یک واقعیت پایدار است؛ برچسب
 * محلی‌شده می‌تواند تغییر کند، پس هرگز مبنای هویت نیست.
 */
export function findCountryByCode(
  countries: readonly StorefrontGeographyPlace[],
  countryCode: string,
): StorefrontGeographyPlace | null {
  const key = normalize(countryCode);
  if (!key) return null;
  const matches = countries.filter((c) => equals(c.code, key));
  return matches.length === 1 ? matches[0] : null;
}

/**
 * ناحیهٔ اداری متناظر با استان کاتالوگ قدیمی را پیدا می‌کند.
 *
 * کاتالوگ قدیمی فروشگاه هر استان را با یک کد پایدار (`tehran`, `alborz`, …) و یک برچسب نمایشی
 * می‌شناسد. تطبیق فقط روی همان دو کلید دقیق انجام می‌شود و در ابهام `null` برمی‌گردد تا هرگز یک
 * استان اشتباه انتخاب نشود.
 */
export function matchAreaForLegacyProvince(
  areas: readonly StorefrontGeographyPlace[],
  legacyProvince: { code: string | null; label: string | null },
): StorefrontGeographyPlace | null {
  const scoped = areas.filter((a) => a.isActive);
  return matchUnique(scoped, normalize(legacyProvince.code), normalize(legacyProvince.label));
}

/** شهر ناحیه از روی برچسب/کد انتخاب‌شده در فهرست همان ناحیه؛ ابهام ⇒ `null`. */
export function matchCityInArea(
  cities: readonly StorefrontGeographyPlace[],
  cityKey: { code?: string | null; label: string | null },
): StorefrontGeographyPlace | null {
  const scoped = cities.filter((c) => c.isActive);
  return matchUnique(scoped, normalize(cityKey.code ?? null), normalize(cityKey.label));
}

/**
 * شناسه‌های مقصد را می‌سازد، فقط وقتی سلسله‌مراتب واقعاً سازگار است: شهر باید فرزند همان ناحیه و
 * ناحیه باید فرزند همان کشور باشد. هر ناسازگاری `null` می‌شود تا سمت سرور به `DestinationNotComparable`
 * بیفتد، نه اینکه یک هویت نادرست مورد اعتماد قرار گیرد.
 */
export function buildDestinationIdentifiers(input: {
  countryId: string | null;
  area: StorefrontGeographyPlace | null;
  city: StorefrontGeographyPlace | null;
}): GeographyDestinationIdentifiers | null {
  const countryId = normalize(input.countryId);
  if (!countryId) return null;

  if (input.area && input.area.parentId !== countryId) return null;
  if (input.city && !input.area) return null;
  if (input.city && input.city.parentId !== input.area!.id) return null;

  return {
    countryId,
    administrativeAreaId: input.area?.id ?? null,
    cityId: input.city?.id ?? null,
  };
}
