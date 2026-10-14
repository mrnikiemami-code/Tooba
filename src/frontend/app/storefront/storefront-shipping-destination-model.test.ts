/**
 * TB-SHIPPING-MVP-DESTINATION-LIVE-067 — آزمون‌های متمرکز هویت مقصد مشتری.
 *
 * اثبات: (۱) یک مسیر شهر معتبر شناسهٔ پایدار می‌سازد، (۲) مشتری قدیمی بدون شناسه دست‌نخورده می‌ماند،
 * (۳) سلسله‌مراتب ناسازگار هرگز «قابل اعتماد» نمی‌شود، (۴) ابهام جمع نمی‌شود.
 */
import assert from "node:assert/strict";
import test from "node:test";
import type { StorefrontGeographyPlace } from "./storefront-geography-api.ts";
import {
  buildDestinationIdentifiers,
  findCountryByCode,
  matchAreaForLegacyProvince,
  matchCityInArea,
} from "./storefront-shipping-destination-model.ts";

const IR: StorefrontGeographyPlace = {
  id: "c0000000-0000-4000-8000-00000000000a",
  code: "IR",
  label: "ایران",
  parentId: null,
  isActive: true,
};

const ALBORZ: StorefrontGeographyPlace = {
  id: "a0000000-0000-4000-8000-00000000000b",
  code: "alborz",
  label: "البرز",
  parentId: IR.id,
  isActive: true,
};

const KARAJ: StorefrontGeographyPlace = {
  id: "c1000000-0000-4000-8000-00000000000c",
  code: "karaj",
  label: "کرج",
  parentId: ALBORZ.id,
  isActive: true,
};

const TEHRAN_AREA: StorefrontGeographyPlace = {
  id: "a0000000-0000-4000-8000-00000000000d",
  code: "tehran",
  label: "تهران",
  parentId: IR.id,
  isActive: true,
};

const TEHRAN_CITY: StorefrontGeographyPlace = {
  id: "c2000000-0000-4000-8000-00000000000e",
  code: "tehran",
  label: "تهران",
  parentId: TEHRAN_AREA.id,
  isActive: true,
};

test("country base is found by exact stable code only, never by label", () => {
  assert.equal(findCountryByCode([IR], "IR")?.id, IR.id);
  assert.equal(findCountryByCode([IR], "ir")?.id, IR.id, "کد بدون حساسیت به بزرگی/کوچکی");
  assert.equal(findCountryByCode([IR], "ایران"), null, "برچسب محلی‌شده مبنا نیست");
  assert.equal(findCountryByCode([IR], "IRQ"), null, "تطبیق زیررشته‌ای ممنوع است");
});

test("legacy province maps to its Geography area by exact code, and the city then carries real ids", () => {
  const area = matchAreaForLegacyProvince([ALBORZ, TEHRAN_AREA], { code: "alborz", label: "البرز" });
  assert.equal(area?.id, ALBORZ.id);

  const city = matchCityInArea([KARAJ], { label: "کرج" });
  assert.equal(city?.id, KARAJ.id);

  const destination = buildDestinationIdentifiers({ countryId: IR.id, area, city });
  assert.deepEqual(destination, {
    countryId: IR.id,
    administrativeAreaId: ALBORZ.id,
    cityId: KARAJ.id,
  });
});

test("legacy client without a Geography area still gets a valid country-level destination or none", () => {
  // استان ناشناخته در Geography: هیچ ناحیه‌ای تطبیق نمی‌کند ⇒ هویت سطح کشور، بدون حدس.
  const area = matchAreaForLegacyProvince([ALBORZ], { code: "unknown_province", label: "ناشناخته" });
  assert.equal(area, null);

  const destination = buildDestinationIdentifiers({ countryId: IR.id, area, city: null });
  assert.deepEqual(destination, { countryId: IR.id, administrativeAreaId: null, cityId: null });

  // مشتری قدیمی‌تر از این هم: هیچ شناسه‌ای ندارد ⇒ null، یعنی مسیر قدیمی متنی دست‌نخورده ادامه می‌یابد.
  assert.equal(buildDestinationIdentifiers({ countryId: null, area: null, city: null }), null);
});

test("malformed hierarchy is rejected instead of being silently trusted", () => {
  // شهر از استان دیگر: والد شهر با ناحیهٔ انتخاب‌شده نمی‌خواند.
  assert.equal(
    buildDestinationIdentifiers({ countryId: IR.id, area: ALBORZ, city: TEHRAN_CITY }),
    null,
  );

  // ناحیه از کشور دیگر.
  assert.equal(
    buildDestinationIdentifiers({
      countryId: "c0000000-0000-4000-8000-00000000ffff",
      area: ALBORZ,
      city: null,
    }),
    null,
  );

  // شهر بدون استان والد.
  assert.equal(buildDestinationIdentifiers({ countryId: IR.id, area: null, city: KARAJ }), null);
});

test("ambiguous or fuzzy area/city matches are never collapsed", () => {
  const duplicateAreas: StorefrontGeographyPlace[] = [
    ALBORZ,
    { ...ALBORZ, id: "a0000000-0000-4000-8000-00000000000f" },
  ];
  assert.equal(
    matchAreaForLegacyProvince(duplicateAreas, { code: "alborz", label: "البرز" }),
    null,
    "دو ناحیهٔ هم‌کد ⇒ ابهام ⇒ هیچ انتخابی",
  );

  assert.equal(
    matchAreaForLegacyProvince([ALBORZ], { code: null, label: "الب" }),
    null,
    "تطبیق تقریبی/زیررشته‌ای ممنوع است",
  );

  assert.equal(
    matchCityInArea([{ ...KARAJ, id: "c3000000-0000-4000-8000-000000000010" }, KARAJ], { label: "کرج" }),
    null,
    "دو شهر هم‌نام ⇒ ابهام ⇒ هیچ انتخابی",
  );

  const inactive: StorefrontGeographyPlace = { ...KARAJ, isActive: false };
  assert.equal(matchCityInArea([inactive], { label: "کرج" }), null, "مکان غیرفعال انتخاب نمی‌شود");
});
