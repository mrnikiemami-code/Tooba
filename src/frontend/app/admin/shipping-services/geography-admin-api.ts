/**
 * TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — authorized read of the Geography catalog.
 *
 * Returns stable GUID ids with display labels, so the rates/coverage dialog can submit real Geography
 * ids instead of hand-typed or invented ones. Labels are display-only; identity is always the id.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";

export type GeographyPlaceOption = {
  id: string;
  code: string;
  label: string;
  parentId: string | null;
  isActive: boolean;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function mapOption(raw: unknown): GeographyPlaceOption | null {
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

async function loadOptions(url: string, fallback: string): Promise<AdminResult<GeographyPlaceOption[]>> {
  try {
    const response = await fetch(url, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: fallback };
    }
    const rows = Array.isArray(payload) ? payload : [];
    return {
      state: "ok",
      data: rows.map(mapOption).filter((x): x is GeographyPlaceOption => Boolean(x)),
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Countries with stable ids. */
export function loadGeographyCountries(
  languageId?: string,
): Promise<AdminResult<GeographyPlaceOption[]>> {
  const qs = languageId ? `?languageId=${encodeURIComponent(languageId)}` : "";
  return loadOptions(`/v1/admin/geography/countries${qs}`, "geography.countries.load_failed");
}

/** Administrative areas of one country. */
export function loadGeographyAdministrativeAreas(
  countryId: string,
  languageId?: string,
): Promise<AdminResult<GeographyPlaceOption[]>> {
  const qs = languageId ? `?languageId=${encodeURIComponent(languageId)}` : "";
  return loadOptions(
    `/v1/admin/geography/countries/${encodeURIComponent(countryId)}/administrative-areas${qs}`,
    "geography.areas.load_failed",
  );
}

/** Cities of one country, optionally narrowed to one administrative area. */
export function loadGeographyCities(
  countryId: string,
  administrativeAreaId?: string | null,
  languageId?: string,
): Promise<AdminResult<GeographyPlaceOption[]>> {
  const params = new URLSearchParams();
  if (administrativeAreaId) params.set("administrativeAreaId", administrativeAreaId);
  if (languageId) params.set("languageId", languageId);
  const qs = params.toString() ? `?${params.toString()}` : "";
  return loadOptions(
    `/v1/admin/geography/countries/${encodeURIComponent(countryId)}/cities${qs}`,
    "geography.cities.load_failed",
  );
}

/** Service areas of one city. */
export function loadGeographyServiceAreas(
  cityId: string,
  languageId?: string,
): Promise<AdminResult<GeographyPlaceOption[]>> {
  const qs = languageId ? `?languageId=${encodeURIComponent(languageId)}` : "";
  return loadOptions(
    `/v1/admin/geography/cities/${encodeURIComponent(cityId)}/service-areas${qs}`,
    "geography.service_areas.load_failed",
  );
}
