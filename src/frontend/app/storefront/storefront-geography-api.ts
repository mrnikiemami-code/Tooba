/**
 * TB-SHIPPING-MVP-DESTINATION-LIVE-067 — خواندن عمومی (مشتری) فهرست مکان‌نگاری Geography.
 *
 * صفحهٔ ارسال فروشگاه برای انتخاب کشور/استان/شهر به شناسهٔ پایدار نیاز دارد. این ماژول همان مسیرهای
 * عمومی Geography را می‌خواند (`/v1/storefront/geography/places/**`)، نه مسیرهای ادمین: هیچ مجوز ادمین
 * به مشتری داده نمی‌شود و هیچ نوشتنی وجود ندارد. خروجی همیشه `id` پایدار + کد + برچسب + والد است.
 */
export type StorefrontGeographyPlace = {
  id: string;
  code: string;
  label: string;
  parentId: string | null;
  isActive: boolean;
};

export type StorefrontGeographyResult =
  | { state: "ok"; data: StorefrontGeographyPlace[] }
  | { state: "error"; data: null };

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function mapPlace(raw: unknown): StorefrontGeographyPlace | null {
  const row = asRecord(raw);
  if (!row) return null;
  const id = text(row.id ?? row.Id);
  const code = text(row.code ?? row.Code);
  if (!id || !code) return null;
  const parent = row.parentId ?? row.ParentId;
  return {
    id,
    code,
    label: text(row.label ?? row.Label) || code,
    parentId: parent == null || text(parent) === "" ? null : text(parent),
    isActive: Boolean(row.isActive ?? row.IsActive),
  };
}

async function loadPlaces(url: string): Promise<StorefrontGeographyResult> {
  try {
    const response = await fetch(url, { cache: "no-store" });
    if (!response.ok) return { state: "error", data: null };
    const payload: unknown = await response.json().catch(() => null);
    const rows = Array.isArray(payload) ? payload : [];
    return {
      state: "ok",
      data: rows.map(mapPlace).filter((x): x is StorefrontGeographyPlace => Boolean(x)),
    };
  } catch {
    return { state: "error", data: null };
  }
}

/** کشورها را از مسیر عمومی Geography می‌خواند. */
export function loadStorefrontGeographyCountries(): Promise<StorefrontGeographyResult> {
  return loadPlaces("/v1/storefront/geography/places/countries?languageId=null");
}

/** ناحیه‌های اداری (استان) یک کشور. */
export function loadStorefrontGeographyAreas(countryId: string): Promise<StorefrontGeographyResult> {
  return loadPlaces(
    `/v1/storefront/geography/places/countries/${encodeURIComponent(countryId)}/administrative-areas?languageId=null`,
  );
}

/** شهرهای یک کشور، محدود به ناحیهٔ اداری انتخاب‌شده تا سلسله‌مراتب والد/فرزند حفظ شود. */
export function loadStorefrontGeographyCities(
  countryId: string,
  administrativeAreaId?: string | null,
): Promise<StorefrontGeographyResult> {
  const query = administrativeAreaId
    ? `?administrativeAreaId=${encodeURIComponent(administrativeAreaId)}`
    : "";
  return loadPlaces(
    `/v1/storefront/geography/places/countries/${encodeURIComponent(countryId)}/cities${query}`,
  );
}
