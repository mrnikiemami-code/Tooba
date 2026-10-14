"use client";

/**
 * TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — cascading destination picker for the rates/coverage dialog.
 *
 * Every option comes from the authorized Geography admin read and carries a stable GUID id; labels are
 * display-only. Changing a parent clears dependent selections, so no inconsistent id combination can be
 * submitted. Upper-level scopes (country-wide / area-wide / city-wide) stay selectable by leaving the
 * deeper controls empty.
 */
import { useCallback, useEffect, useState } from "react";
import {
  loadGeographyAdministrativeAreas,
  loadGeographyCities,
  loadGeographyCountries,
  loadGeographyServiceAreas,
  type GeographyPlaceOption,
} from "./geography-admin-api.ts";
import {
  clearDependents,
  scopeLevelOf,
  type DestinationSelection,
} from "./shipping-rates-coverage-model.ts";

type Props = {
  selection: DestinationSelection;
  onChange: (next: DestinationSelection) => void;
  disabled?: boolean;
};

const LEVEL_FA: Record<string, string> = {
  country: "کشور (سراسری)",
  administrativeArea: "استان/ناحیهٔ اداری",
  city: "شهر",
  serviceArea: "ناحیهٔ خدمت",
};

export function ShippingDestinationPicker({ selection, onChange, disabled }: Props) {
  const [countries, setCountries] = useState<GeographyPlaceOption[]>([]);
  const [areas, setAreas] = useState<GeographyPlaceOption[]>([]);
  const [cities, setCities] = useState<GeographyPlaceOption[]>([]);
  const [serviceAreas, setServiceAreas] = useState<GeographyPlaceOption[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void (async () => {
      const result = await loadGeographyCountries();
      if (!active) return;
      if (result.state !== "ok" || !result.data) {
        setError("فهرست کشورها از Geography خوانده نشد.");
        return;
      }
      setError(null);
      setCountries(result.data);
    })();
    return () => { active = false; };
  }, []);

  const loadAreas = useCallback(async (countryId: string) => {
    if (!countryId) { setAreas([]); return; }
    const result = await loadGeographyAdministrativeAreas(countryId);
    setAreas(result.state === "ok" && result.data ? result.data : []);
    setError(result.state === "ok" ? null : "فهرست استان‌ها خوانده نشد.");
  }, []);

  const loadCities = useCallback(async (countryId: string, areaId: string | null) => {
    if (!countryId) { setCities([]); return; }
    const result = await loadGeographyCities(countryId, areaId);
    setCities(result.state === "ok" && result.data ? result.data : []);
    setError(result.state === "ok" ? null : "فهرست شهرها خوانده نشد.");
  }, []);

  const loadServiceAreas = useCallback(async (cityId: string) => {
    if (!cityId) { setServiceAreas([]); return; }
    const result = await loadGeographyServiceAreas(cityId);
    setServiceAreas(result.state === "ok" && result.data ? result.data : []);
    setError(result.state === "ok" ? null : "فهرست ناحیه‌های خدمت خوانده نشد.");
  }, []);

  useEffect(() => {
    void loadAreas(selection.countryId);
    void loadCities(selection.countryId, selection.administrativeAreaId);
  }, [loadAreas, loadCities, selection.countryId, selection.administrativeAreaId]);

  useEffect(() => {
    void loadServiceAreas(selection.cityId ?? "");
  }, [loadServiceAreas, selection.cityId]);

  const level = scopeLevelOf(selection);

  return (
    <div className="space-y-2" data-testid="shipping-destination-picker">
      <div className="grid grid-cols-2 gap-2">
        <label className="text-[11px] text-gray-600">
          کشور
          <select
            className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
            value={selection.countryId}
            disabled={disabled}
            data-testid="shipping-destination-country"
            onChange={(e) => {
              const next = clearDependents({ ...selection, countryId: e.target.value }, "country");
              onLoadFromCountry(next);
            }}
          >
            <option value="">— انتخاب کنید —</option>
            {countries.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label} ({option.code}){option.isActive ? "" : " — غیرفعال"}
              </option>
            ))}
          </select>
        </label>

        <label className="text-[11px] text-gray-600">
          استان/ناحیهٔ اداری (اختیاری)
          <select
            className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
            value={selection.administrativeAreaId ?? ""}
            disabled={disabled || !selection.countryId}
            data-testid="shipping-destination-area"
            onChange={(e) => {
              const areaId = e.target.value || null;
              onChange(clearDependents({ ...selection, administrativeAreaId: areaId }, "area"));
            }}
          >
            <option value="">— بدون استان (دامنهٔ کشوری) —</option>
            {areas.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label} ({option.code}){option.isActive ? "" : " — غیرفعال"}
              </option>
            ))}
          </select>
        </label>

        <label className="text-[11px] text-gray-600">
          شهر (اختیاری)
          <select
            className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
            value={selection.cityId ?? ""}
            disabled={disabled || !selection.countryId}
            data-testid="shipping-destination-city"
            onChange={(e) => {
              const cityId = e.target.value || null;
              onChange(clearDependents({ ...selection, cityId }, "city"));
            }}
          >
            <option value="">— بدون شهر (دامنهٔ بالاتر) —</option>
            {cities.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label} ({option.code}){option.isActive ? "" : " — غیرفعال"}
              </option>
            ))}
          </select>
        </label>

        <label className="text-[11px] text-gray-600">
          ناحیهٔ خدمت (اختیاری)
          <select
            className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
            value={selection.serviceAreaId ?? ""}
            disabled={disabled || !selection.cityId}
            data-testid="shipping-destination-service-area"
            onChange={(e) => onChange({ ...selection, serviceAreaId: e.target.value || null })}
          >
            <option value="">— بدون ناحیه (دامنهٔ شهر) —</option>
            {serviceAreas.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label} ({option.code}){option.isActive ? "" : " — غیرفعال"}
              </option>
            ))}
          </select>
        </label>
      </div>

      <p className="text-[11px] text-gray-500" data-testid="shipping-destination-level">
        سطح دامنه: {level ? LEVEL_FA[level] : "انتخاب‌نشده"} — شناسه‌ها پایدار و برگرفته از Geography هستند.
      </p>
      {error && <p className="text-[11px] text-red-600" data-testid="shipping-destination-error">{error}</p>}
    </div>
  );

  function onLoadFromCountry(next: DestinationSelection) {
    onChange(next);
    void loadAreas(next.countryId);
    void loadCities(next.countryId, null);
  }
}
