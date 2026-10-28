"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { Check, ChevronLeft, CreditCard, Edit3, Package, Shield, ShoppingBag, Store, Truck } from "lucide-react";
import { toPersianDigits } from "./storefront-shipping-format.ts";
import { formatOfferAmount } from "./storefront-api.ts";
import { loadStorefrontCart, StorefrontCartApiError, type StorefrontCartLine } from "./storefront-cart-api.ts";
import { StorefrontCheckoutLimitNotice } from "./storefront-checkout-limit-notice.tsx";
import {
  createCustomerAddress,
  listCheckoutSavedAddresses,
  shippingFromCustomerAddress,
  type CustomerAddress,
} from "../customer-panel/customer-address-api.ts";
import {
  loadShippingProjection,
  saveShippingSelection,
  toCustomerShippingMessage,
  type StorefrontProvinceOption,
  type StorefrontShippingGroupSelectionInput,
  type StorefrontShippingMethod,
  type StorefrontShippingProjection,
} from "./storefront-shipping-api.ts";
import { readStoredCouponCode } from "./storefront-checkout-api.ts";
import {
  buildShippingItemGroups,
  isItemGroupingComplete,
  restoreLineMethodSelections,
} from "./storefront-shipping-item-groups.ts";
import { useCheckoutAuthGate } from "./use-checkout-auth-gate.ts";
import {
  loadStorefrontGeographyAreas,
  loadStorefrontGeographyCities,
  loadStorefrontGeographyCountries,
  type StorefrontGeographyPlace,
} from "./storefront-geography-api.ts";
import {
  buildDestinationIdentifiers,
  findCountryByCode,
  matchAreaForLegacyProvince,
  matchCityInArea,
  type GeographyDestinationIdentifiers,
} from "./storefront-shipping-destination-model.ts";
import {
  beginDestinationRefresh,
  canRetryDestinationRefresh,
  completeDestinationRefresh,
  createDestinationRefreshSnapshot,
  createRequestGeneration,
  destinationIdentityKey,
  failDestinationRefresh,
  planDestinationRefresh,
  resolveDestinationPreviewMode,
  type DestinationRefreshSnapshot,
} from "./storefront-shipping-destination-refresh.ts";
import {
  isShippingDateHorizonComplete,
  pickEligibleDeliveryDate,
  pickEligibleDeliveryWindow,
  resolveSingleGroupHorizon,
} from "./storefront-shipping-group-horizon.ts";
import {
  allGroupSchedulesComplete,
  isGroupScheduleComplete,
  mergeGroupSelections,
  resolveGroupSchedules,
  setGroupSelection,
  type StorefrontShippingGroupSelection,
} from "./storefront-shipping-group-schedule.ts";
import { StorefrontShippingGroupCards, type ShippingGroupCard, type StorefrontCapturedGroupChoice, type UnassignedSellerBlock } from "./storefront-shipping-group-cards.tsx";
import {
  buildCapturedSelectionIntent,
  capturedSelectionComplete,
  capturedSelectionContextKey,
} from "./storefront-shipping-captured-selection.ts";
import { StorefrontShippingSingleGroupSection } from "./storefront-shipping-single-group-section.tsx";
import { resolveShippingUnavailableNotices } from "./storefront-shipping-unavailable-methods.ts";
import {
  EMPTY_SHIPPING_ADDRESS,
  StorefrontShippingAddressCards,
  StorefrontShippingNewAddressDialog,
  type ShippingAddressForm,
} from "./storefront-shipping-address-cards.tsx";

type AddressForm = ShippingAddressForm;

const emptyAddress: AddressForm = EMPTY_SHIPPING_ADDRESS;

function composeRecipient(form: Pick<AddressForm, "firstName" | "lastName" | "recipientName">): string {
  const first = form.firstName.trim();
  const last = form.lastName.trim();
  if (first && last) {
    return `${first} ${last}`;
  }
  return form.recipientName.trim();
}

/**
 * مرحلهٔ ارسال Shopeiva با دادهٔ واقعی Host (روش‌ها، قیمت، حداقل تحویل، پیش‌نویس).
 */
export function StorefrontShopeivaShipping() {
  useCheckoutAuthGate("/shipping");
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const locale = useLocale();
  const [projection, setProjection] = useState<StorefrontShippingProjection | null>(null);
  const [address, setAddress] = useState<AddressForm>(emptyAddress);
  const [savedAddresses, setSavedAddresses] = useState<CustomerAddress[] | null>(null);
  const [useSavedAddress, setUseSavedAddress] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const [savedAddressId, setSavedAddressId] = useState<string | null>(null);
  const [methodCode, setMethodCode] = useState<string | null>(null);
  /** انتخاب روش هر گروه فروشنده در سبد چندگروهی؛ فقط شناسه‌های واقعی سبد و روش‌های authoritative. */
  const [lineMethodByLine, setLineMethodByLine] = useState<Record<string, string>>({});
  const [cartLines, setCartLines] = useState<StorefrontCartLine[]>([]);
  /** (150) انتخاب تاریخ/بازهٔ **هر گروه جداگانه**؛ کلید = کلید پایدار گروه. */
  const [groupSelections, setGroupSelections] = useState<Record<string, StorefrontShippingGroupSelection>>({});
  /**
   * TB-237: انتخاب روش + تاریخِ مجاز هر گروه capture‌شده، کلید = GroupKey سرور. روش با کلید متعارف
   * سرویس/گزینه **در همان گروه** ذخیره می‌شود تا دو مالک با همان Binding/ترجمه مستقل بمانند.
   */
  const [capturedChoices, setCapturedChoices] = useState<Record<string, StorefrontCapturedGroupChoice>>({});
  /**
   * TB-237: قصد کوپنِ همین context. پاک‌کردن صریح در سبد، `null` ذخیره می‌کند و همین‌جا هم «بدون
   * کوپن» است؛ هیچ `value || stored` fallback وجود ندارد.
   */
  const couponIntent = readStoredCouponCode();
  /** همان قصد کوپن برای فراخوانی‌های async طولانی (بدون تغییر هویت در طول یک تازه‌سازی). */
  const readStoredCouponCodeRef = useRef<string | null>(couponIntent);
  readStoredCouponCodeRef.current = couponIntent;
  const [deliveryDate, setDeliveryDate] = useState<string>("");
  const [deliveryTime, setDeliveryTime] = useState<string>("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [limitCode, setLimitCode] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showNewDialog, setShowNewDialog] = useState(false);
  const [newAddress, setNewAddress] = useState<AddressForm>(emptyAddress);

  // انتخاب مکانی مبتنی بر Geography: شناسهٔ پایدار کشور و ناحیه‌های اداری از مسیر عمومی خوانده می‌شود.
  // استان نمایشی همان کاتالوگ قدیمی می‌ماند تا `provinceName` (که قیمت/eligibility قدیمی را تعیین
  // می‌کند) دست‌نخورده بماند؛ Geography فقط هویت پایدار و فهرست شهر واقعی همان استان را اضافه می‌کند.
  const [geoCountryId, setGeoCountryId] = useState<string | null>(null);
  const [geoAreas, setGeoAreas] = useState<StorefrontGeographyPlace[]>([]);
  const [geoCities, setGeoCities] = useState<StorefrontGeographyPlace[]>([]);
  const LEGACY_STOREFRONT_COUNTRY = "IR";

  /**
   * TB-230: نسل درخواست. پاسخ قدیمی/بی‌ترتیب هرگز جای پاسخ جاری را نمی‌گیرد و درخواستی که
   * هویت مقصدش تغییر کرده، وضعیت «در انتظار» را هم پاک نمی‌کند.
   */
  const projectionGeneration = useRef(createRequestGeneration());
  /**
   * TB-230-R1: هویت مقصدِ تأییدشده/درخواست‌شده/شکست‌خورده جدا نگه داشته می‌شود. «شروع درخواست» هرگز
   * به‌معنای موفقیت نیست، یک بولین بارگذاری تصمیم‌گیر نیست، و خطای کهنه وضعیت درخواست تازه را
   * دست نمی‌زند. `ref` برای تصمیم همگام (مثلاً جلوگیری از کلیک تکراری Retry) و `state` برای رندر.
   */
  const destinationRefreshRef = useRef<DestinationRefreshSnapshot>(createDestinationRefreshSnapshot());
  const [destinationRefresh, setDestinationRefresh] = useState<DestinationRefreshSnapshot>(
    destinationRefreshRef.current,
  );
  const commitDestinationRefresh = useCallback((next: DestinationRefreshSnapshot) => {
    if (next === destinationRefreshRef.current) return;
    destinationRefreshRef.current = next;
    setDestinationRefresh(next);
  }, []);

  const refreshProjection = useCallback(async (
    province?: string,
    method?: string | null,
    city?: string,
    destination?: GeographyDestinationIdentifiers | null,
    options?: { rehydrateDraft?: boolean; destinationKey?: string },
  ) => {
    const sequence = projectionGeneration.current.next();
    const rehydrateDraft = options?.rehydrateDraft !== false;
    // کلید مقصد فقط وقتی داده می‌شود که این فراخوانی «تازه‌سازی مقصد» باشد؛ مسیر legacy بدون کلید
    // رفتار قبلی خودش را دارد و هیچ قفلی نمی‌گیرد.
    const trackedKey = options?.destinationKey ?? null;
    if (trackedKey !== null) {
      commitDestinationRefresh(beginDestinationRefresh(destinationRefreshRef.current, trackedKey));
    }
    try {
      const cart = await loadStorefrontCart();
      if (!projectionGeneration.current.isCurrent(sequence)) return;
      if (!cart?.cartId) {
        setError("سبد خرید پیدا نشد.");
        setProjection(null);
        if (trackedKey !== null) {
          commitDestinationRefresh(failDestinationRefresh(destinationRefreshRef.current, trackedKey));
        }
        return;
      }
      const page = await loadShippingProjection({
        cartId: cart.cartId,
        provinceName: province || undefined,
        cityName: city || undefined,
        destination: destination ?? null,
        methodCode: method || undefined,
        // TB-237: همان قصد کوپنِ همین context در خواندن هم فرستاده می‌شود تا مبلغ کالا/گروه‌ها با
        // همان تخفیفی که بعداً ثبت می‌شود هم‌خوان باشد.
        couponCode: readStoredCouponCodeRef.current,
      });
      if (!projectionGeneration.current.isCurrent(sequence)) return;
      setCartLines(cart.lines);
      setProjection(page);
      // موفقیت همین درخواست، هویت تأییدشده را جلو می‌برد و قفل شکست همان کلید را پاک می‌کند.
      if (trackedKey !== null) {
        commitDestinationRefresh(completeDestinationRefresh(destinationRefreshRef.current, trackedKey));
      }
      if (page.revalidationMessage) {
        setError(page.revalidationMessage);
      }

      const clampDate = (candidate: string | null | undefined) => {
        const min = page.minimumDeliveryDate;
        if (!min) return candidate ?? "";
        if (!candidate || candidate < min) return min;
        return candidate;
      };

      // مقصد‌محور: هیچ بازنویسی روی ورودی تایپ‌شدهٔ گیرنده/آدرس انجام نمی‌شود.
      if (!rehydrateDraft) {
        if (method) {
          setMethodCode(method);
          setDeliveryDate((prev) => clampDate(prev));
        }
        return;
      }

      if (page.draft) {
        setAddress({
          firstName: page.draft.firstName,
          lastName: page.draft.lastName,
          recipientName: page.draft.recipientName,
          contactMobile: page.draft.contactMobile,
          provinceName: page.draft.provinceName,
          cityName: page.draft.cityName,
          postalAddress: page.draft.postalAddress,
          postalCode: page.draft.postalCode,
        });
        setSavedAddressId(page.draft.savedAddressId);
        setUseSavedAddress(Boolean(page.draft.savedAddressId));
        setMethodCode(page.selectedMethodCode ?? page.draft.shippingMethodCode);
        setDeliveryDate(clampDate(page.draft.selectedDeliveryDate));
        setDeliveryTime(page.draft.selectedDeliveryTimeWindow ?? "");
        setNotes(page.draft.customerNote ?? "");
      } else if (method) {
        setMethodCode(method);
        setDeliveryDate((prev) => clampDate(prev));
      } else if (page.selectedMethodCode) {
        setMethodCode(page.selectedMethodCode);
        setDeliveryDate((prev) => clampDate(prev));
      }
    } catch (cause) {
      // فقط خطای همین درخواست جاری می‌تواند وضعیت را عوض کند؛ خطای کهنه هیچ‌چیز را دست نمی‌زند.
      if (!projectionGeneration.current.isCurrent(sequence)) return;
      if (trackedKey !== null) {
        // شکست تازه‌سازی مقصد: قیمت قبلی «جاری» نمی‌شود و قفل Retry صریح برای همان کلید می‌نشیند.
        commitDestinationRefresh(failDestinationRefresh(destinationRefreshRef.current, trackedKey));
      } else {
        setError(toCustomerShippingMessage(cause, locale));
      }
    }
  }, [commitDestinationRefresh, locale]);

  /* TB-230: بدنهٔ بالا تنها مسیر معتبر تازه‌سازی است. */

  useEffect(() => {
    // بارگذاری اولیه برای هویت «هیچ مقصد» است تا پیش‌نمایش اولیه هم به همین هویت بسته شود.
    void refreshProjection(undefined, undefined, undefined, null, { destinationKey: "" }).catch((cause: unknown) =>
      setError(toCustomerShippingMessage(cause, locale)),
    );
    void listCheckoutSavedAddresses()
      .then((result) => setSavedAddresses(result.addresses))
      .catch(() => setSavedAddresses(null));
  }, [refreshProjection, locale]);

  // انتخاب «هر خط» ذخیره‌شده هنگام بازگشت/رفرش بازیابی می‌شود؛ روش نامعتبر بازیابی نمی‌شود و
  // هیچ تبدیل خودکار روش رخ نمی‌دهد (fail-closed).
  useEffect(() => {
    const draft = projection?.draft;
    if (!draft || cartLines.length <= 1) return;
    setLineMethodByLine((prev) => ({
      ...prev,
      ...restoreLineMethodSelections({
        lines: cartLines,
        persistedGroups: draft.shippingGroups.map((group) => ({
          sellerPartyId: group.sellerPartyId,
          methodCode: group.methodCode,
          cartLineIds: group.cartLineIds ?? [],
        })),
        methods: projection.methods,
      }),
    }));
  }, [projection, cartLines]);

  const provinces: StorefrontProvinceOption[] = projection?.provinces ?? [];
  const provinceOptions = provinces.map((p) => ({ value: p.label, label: p.label, cities: p.cities }));

  // --- Geography-backed destination identity (067) ----------------------------------------------
  // کشور پایهٔ فروشگاه (ایران) با تطبیق دقیق کد؛ سپس ناحیهٔ اداری منطبق با استان انتخاب‌شده، و در
  // نهایت شهرهای همان ناحیه. هر مرحله فقط با شناسهٔ پایدار کار می‌کند و هیچ‌جا حدس زده نمی‌شود.
  useEffect(() => {
    let active = true;
    void (async () => {
      const countries = await loadStorefrontGeographyCountries();
      if (!active) return;
      const country = countries.state === "ok"
        ? findCountryByCode(countries.data, LEGACY_STOREFRONT_COUNTRY)
        : null;
      if (!country) {
        // خواندن عمومی Geography در دسترس نیست: مسیر قدیمی دست‌نخورده ادامه می‌یابد.
        setGeoCountryId(null);
        setGeoAreas([]);
        return;
      }
      setGeoCountryId(country.id);
      const areas = await loadStorefrontGeographyAreas(country.id);
      if (!active) return;
      setGeoAreas(areas.state === "ok" ? areas.data : []);
    })();
    return () => { active = false; };
  }, []);

  const legacyProvinceForSelection = provinces.find(
    (p) => p.label === address.provinceName || p.code === address.provinceName,
  ) ?? null;

  const selectedArea = useMemo(
    () => (geoAreas.length > 0
      ? matchAreaForLegacyProvince(geoAreas, {
          code: legacyProvinceForSelection?.code ?? null,
          label: address.provinceName || null,
        })
      : null),
    [geoAreas, legacyProvinceForSelection, address.provinceName],
  );

  useEffect(() => {
    let active = true;
    if (!geoCountryId || !selectedArea) {
      setGeoCities([]);
      return () => { active = false; };
    }
    void (async () => {
      const cities = await loadStorefrontGeographyCities(geoCountryId, selectedArea.id);
      if (!active) return;
      setGeoCities(cities.state === "ok" ? cities.data : []);
    })();
    return () => { active = false; };
  }, [geoCountryId, selectedArea]);

  /** شهر انتخاب‌شده در فهرست Geography همان ناحیه؛ ابهام/عدم تطبیق ⇒ null (بدون حدس). */
  const selectedCity = useMemo(
    () => (geoCities.length > 0
      ? matchCityInArea(geoCities, { label: address.cityName || null })
      : null),
    [geoCities, address.cityName],
  );

  const destinationIdentifiers = useMemo(
    () => buildDestinationIdentifiers({
      countryId: geoCountryId,
      area: selectedArea,
      city: selectedCity,
    }),
    [geoCountryId, selectedArea, selectedCity],
  );

  /**
   * TB-230 — اتصال واقعی مقصد به پیش‌نمایش capture‌شده.
   *
   * فقط وقتی پیش‌نمایش روشن است اجرا می‌شود (حالت خاموش/قدیمی دست‌نخورده می‌ماند). ملاک، کلید پایدار
   * مقصد تأییدشده است؛ رندر نامرتبط درخواست نمی‌سازد، پاسخ قدیمی دور ریخته می‌شود و تا رسیدن پاسخ تازه
   * هیچ قیمت قبلی «جاری» نمایش داده نمی‌شود. ورودی تایپ‌شدهٔ گیرنده/آدرس بازنویسی نمی‌شود.
   */
  const destinationKey = destinationIdentityKey(destinationIdentifiers);
  const capturedPreviewActive = projection?.capturedPreview != null;

  /**
   * وضعیت صریح مقصد نسبت به هویت فعلی. `current` تنها حالتی است که پول/تاریخ موجود «جاری» است؛
   * در `pending` و `failed` قیمت قبلی پنهان می‌شود و در `failed` فقط Retry صریح همان کلید هست.
   */
  const destinationPreviewMode = useMemo(
    () => resolveDestinationPreviewMode(destinationRefresh, destinationKey),
    [destinationRefresh, destinationKey],
  );

  /**
   * TB-230-R1: کارت‌ها فقط وقتی پول/تاریخ نشان می‌دهند که پیش‌نمایش موجود دقیقاً برای همین هویت مقصد
   * باشد. در انتظار پاسخ تازه «در حال محاسبه» و پس از شکست «در دسترس نیست + تلاش دوباره» می‌آید؛
   * قیمت مقصد قبلی هرگز به‌عنوان پاسخ مقصد فعلی برنمی‌گردد. ورودی تایپ‌شده دست‌نخورده می‌ماند.
   */
  const capturedPreviewForCards = useMemo(() => {
    const preview = projection?.capturedPreview ?? null;
    if (!preview || preview.state !== "ok" || destinationPreviewMode === "current") return preview;
    return {
      ...preview,
      groups: [],
      unavailableReason: destinationPreviewMode === "failed"
        ? "storefront.captured_preview.destination_failed"
        : "storefront.captured_preview.destination_pending",
    };
  }, [projection, destinationPreviewMode]);

  /**
   * TB-237: هر تغییر context (مقصد/کوپن/نسخهٔ سبد/مبدأ پاسخ) تأییدهای قبلی را باطل می‌کند تا هیچ
   * انتخاب کهنه‌ای با مبلغ/تاریخ تازه ثبت نشود؛ مشتری باید دوباره تأیید کند (بدون بازنویسی خودکار).
   */
  const capturedContextKey = capturedSelectionContextKey({
    destinationKey,
    couponCode: couponIntent,
    cartVersion: projection?.cartVersion ?? 0,
    // نسخهٔ پایدارِ انتخابِ ذخیره‌شده، خودش بخشی از context است؛ تغییر آن یعنی تأیید قبلی معتبر نیست.
    snapshotId: null,
    selectionVersion: projection?.capturedPreview?.selectionRevision ?? projection?.draft?.selectionRevision ?? null,
  });
  const capturedContextKeyRef = useRef<string | null>(null);
  useEffect(() => {
    if (capturedContextKeyRef.current === capturedContextKey) return;
    capturedContextKeyRef.current = capturedContextKey;
    setCapturedChoices((prev) => (Object.keys(prev).length === 0 ? prev : {}));
  }, [capturedContextKey]);

  const destinationContext = useRef({ province: "", city: "", method: "" });  destinationContext.current = {
    province: address.provinceName,
    city: address.cityName,
    method: methodCode ?? "",
  };

  useEffect(() => {
    const plan = planDestinationRefresh({
      capturedActive: capturedPreviewActive,
      snapshot: destinationRefresh,
      destinationKey,
      destination: destinationIdentifiers,
    });
    if (plan.action === "skip") return;
    const context = destinationContext.current;
    void refreshProjection(
      context.province,
      context.method || null,
      context.city,
      plan.destination,
      // ورودی تایپ‌شده بازنویسی نمی‌شود؛ کلید پایدار این درخواست ثبت می‌شود.
      { rehydrateDraft: !plan.preserveTypedAddress, destinationKey: plan.key },
    );
  }, [capturedPreviewActive, destinationRefresh, destinationKey, destinationIdentifiers, refreshProjection]);

  /**
   * تنها راه تکرار یک تازه‌سازی شکست‌خورده: یک Retry صریح برای همان کلید.
   * کلیک تکراری/رندر مجدد هیچ درخواست دومی نمی‌سازد (قفل همگام در همان لحظه بسته می‌شود).
   */
  const retryDestinationRefresh = useCallback(() => {
    if (!canRetryDestinationRefresh(destinationRefreshRef.current, destinationKey)) return;
    const context = destinationContext.current;
    void refreshProjection(
      context.province,
      context.method || null,
      context.city,
      destinationIdentifiers,
      { rehydrateDraft: false, destinationKey },
    );
  }, [destinationKey, destinationIdentifiers, refreshProjection]);

  /** فهرست شهر: اگر Geography برای این استان شهر واقعی داد از آن، وگرنه همان کاتالوگ قدیمی. */
  const cityOptions = useMemo(() => {
    if (selectedArea && geoCities.length > 0) {
      return geoCities.map((c) => c.label);
    }
    const match = provinces.find((p) => p.label === address.provinceName || p.code === address.provinceName);
    return match?.cities ?? [];
  }, [selectedArea, geoCities, provinces, address.provinceName]);

  const selectedMethod = projection?.methods.find((m) => m.methodCode === methodCode) ?? null;

  // --- نمایش گروهی اقلام (فروشندهٔ authoritative + روش هر خط) ---------------------------------
  // خطوط سبد و فروشندهٔ هر خط از projection واقعی سبد می‌آید؛ هیچ فروشنده/انبار جعلی ساخته نمی‌شود.
  const sellerGroups = useMemo(() => {
    const bySeller = new Map<string, { sellerPartyId: string; sellerDisplayName: string; lines: StorefrontCartLine[] }>();
    for (const line of cartLines) {
      const existing = bySeller.get(line.sellerPartyId);
      if (existing) existing.lines.push(line);
      else {
        bySeller.set(line.sellerPartyId, {
          sellerPartyId: line.sellerPartyId,
          sellerDisplayName: line.sellerDisplayName,
          lines: [line],
        });
      }
    }
    return [...bySeller.values()];
  }, [cartLines]);

  /**
   * (135) حالت «انتخاب هر خط» وقتی سبد بیش از یک خط قابل‌ارسال دارد فعال می‌شود — چه چند فروشنده،
   * چه یک فروشنده با چند خط. سبد تک‌خطی همان مسیر و همان کارت قدیمی را دست‌نخورده نگه می‌دارد.
   */
  const isGroupedCart = cartLines.length > 1;

  const methodByCode = useMemo(() => {
    const map = new Map<string, StorefrontShippingMethod>();
    for (const method of projection?.methods ?? []) map.set(method.methodCode, method);
    return map;
  }, [projection]);

  /**
   * TB-165: توضیح سطح-روش برای روش‌های بدون نرخ معتبر. تنها از پاسخ سرور؛ گیت خاموش/پاسخ قدیمی
   * (`unavailableMethods === null`) ⇒ فهرست خالی و رفتار دقیقاً قبلی.
   */
  const unavailableNotices = useMemo(
    () =>
      resolveShippingUnavailableNotices({
        unavailableMethods: projection?.unavailableMethods ?? null,
        methods: projection?.methods ?? [],
      }),
    [projection],
  );

  // گروه‌ها فقط با (فروشندهٔ واقعی خط، کد روش authoritative) ساخته می‌شوند؛ هر خط دقیقاً یک گروه.
  const itemGrouping = useMemo(
    () =>
      buildShippingItemGroups({
        lines: cartLines,
        methodCodeByLine: lineMethodByLine,
        methods: projection?.methods ?? [],
      }),
    [cartLines, lineMethodByLine, projection],
  );

  const groupSelectionsByMethod = itemGrouping.groups;

  const groupedShippingAmount = groupSelectionsByMethod.reduce((sum, group) => sum + group.method.priceAmount, 0);
  const allGroupsHaveMethod = isItemGroupingComplete(itemGrouping, cartLines);

  /** (150) افق مستقل هر گروه از ردیف authoritative سرور؛ بدون تاریخ/تلاقی در کلاینت. */
  const groupSchedules = useMemo(
    () => resolveGroupSchedules({ isGroupedCart, projection, groups: groupSelectionsByMethod }),
    [isGroupedCart, projection, groupSelectionsByMethod],
  );

  /**
   * (150) کارت‌ها به‌تفکیک **گروه ارسال واقعی** (فروشندهٔ سبد × روش canonical): دو فروشنده با روش
   * یکسان دو کارت مستقل می‌گیرند و یک فروشنده با دو روش هم دو کارت. خطوط بدون روش در بلوک انتخاب
   * روش همان فروشنده می‌مانند. شناسهٔ نمایشی از شناسهٔ پایدار سرور می‌آید (اگر ذخیره شده باشد).
   */
  const groupCards = useMemo<ShippingGroupCard[]>(
    () =>
      groupSelectionsByMethod.map((group) => ({
        key: group.key,
        identity: groupSchedules[group.key]?.shippingGroupId ?? group.key,
        sellerPartyId: group.sellerPartyId,
        sellerDisplayName: group.sellerDisplayName,
        methodCode: group.methodCode,
        methodLabel: group.method.label,
        amount: group.method.priceAmount,
        lines: group.lines,
      })),
    [groupSelectionsByMethod, groupSchedules],
  );

  const unassignedSellerBlocks = useMemo<UnassignedSellerBlock[]>(
    () =>
      sellerGroups
        .map((seller) => ({
          sellerPartyId: seller.sellerPartyId,
          sellerDisplayName: seller.sellerDisplayName,
          lines: seller.lines.filter((line) => !(lineMethodByLine[line.lineId] ?? "")),
        }))
        .filter((seller) => seller.lines.length > 0),
    [sellerGroups, lineMethodByLine],
  );

  // انتخاب هر گروه از سرور بازیابی/پاک‌سازی می‌شود؛ تغییر یک گروه بقیه را دست نمی‌زند.
  useEffect(() => {
    if (!isGroupedCart) return;
    setGroupSelections((prev) => mergeGroupSelections({ previous: prev, schedules: groupSchedules }));
  }, [isGroupedCart, groupSchedules]);

  // افق مسیر تک‌گروهی (بدون تغییر رفتار قبلی): دقیقاً فیلدهای همان projection.
  const singleGroupHorizon = useMemo(() => resolveSingleGroupHorizon(projection), [projection]);

  const shippingAmount = isGroupedCart
    ? groupedShippingAmount
    : selectedMethod?.priceAmount ?? projection?.selectedShippingAmount ?? 0;
  const payable = (projection?.subtotalExclusiveOfTax ?? 0) + shippingAmount;

  // TB-229-R1: پیش‌نمایش روشن یعنی مسیر قدیمی هیچ هزینه/تاریخی را «محاسبه‌شده» نمایش نمی‌دهد.
  // TB-230: همین پرچم (بالاتر، همراه هماهنگ‌کنندهٔ مقصد) یک‌بار تعریف شده است.

  const isValid =
    (Boolean(address.firstName.trim() && address.lastName.trim())
      || Boolean(savedAddressId && address.recipientName.trim())) &&
    Boolean(address.contactMobile.trim()) &&
    Boolean(address.provinceName.trim()) &&
    Boolean(address.cityName.trim()) &&
    Boolean(address.postalAddress.trim()) &&
    Boolean(address.postalCode.trim()) &&
    // TB-237: مسیر capture‌شده اعتبار خودش را دارد: هر گروه باید روش + تاریخِ همان روش را داشته باشد
    // و مبلغ کالای همان گروه از همان پاسخ موفق موجود باشد؛ هیچ روش/تاریخ/مبلغی حدس زده نمی‌شود.
    (capturedPreviewActive
      ? capturedSelectionComplete(projection?.capturedPreview ?? null, capturedChoices)
      : (isGroupedCart ? allGroupsHaveMethod : Boolean(methodCode))
        && (isGroupedCart
          ? allGroupSchedulesComplete({ schedules: groupSchedules, selections: groupSelections })
          : Boolean(deliveryDate) && Boolean(deliveryTime)
            && isShippingDateHorizonComplete(singleGroupHorizon, deliveryDate, deliveryTime))) &&
    (capturedPreviewActive ? true : (projection?.methods.length ?? 0) > 0);

  function selectSaved(row: CustomerAddress) {
    const mapped = shippingFromCustomerAddress(row);
    setAddress({
      firstName: mapped.firstName,
      lastName: mapped.lastName,
      recipientName: mapped.recipientName,
      contactMobile: mapped.contactMobile,
      provinceName: mapped.provinceName,
      cityName: mapped.cityName,
      postalAddress: mapped.postalAddress,
      postalCode: mapped.postalCode,
    });
    setSavedAddressId(row.addressId);
    setUseSavedAddress(true);
    setShowSaved(false);
    void refreshProjection(mapped.provinceName, methodCode, mapped.cityName).catch((cause: unknown) =>
      setError(toCustomerShippingMessage(cause, locale)),
    );
  }

  async function onSelectMethod(code: string) {
    setMethodCode(code);
    setError(null);
    try {
      await refreshProjection(address.provinceName, code, address.cityName, destinationIdentifiers);
    } catch (cause) {
      setError(toCustomerShippingMessage(cause, locale));
    }
  }

  /**
   * TB-237: انتخاب یک روش capture‌شده. تاریخ پیش‌فرض همان روش (زودترین مجاز) فقط وقتی انتخاب می‌شود
   * که قبلاً تاریخی برای همین روش نبود؛ تغییر روش تاریخِ روش قبلی را حمل نمی‌کند.
   */
  const selectCapturedMethod = useCallback((groupKey: string, methodKey: string, date: string | null) => {
    setCapturedChoices((prev) => {
      const current = prev[groupKey];
      const sameMethod = current?.methodKey === methodKey;
      const separator = methodKey.indexOf("\u001f");
      const serviceCode = separator >= 0 ? methodKey.slice(0, separator) : methodKey;
      const optionCode = separator >= 0 ? methodKey.slice(separator + 1) : "";
      return {
        ...prev,
        [groupKey]: {
          methodKey,
          serviceCode,
          optionCode: optionCode.length > 0 ? optionCode : null,
          date: date ?? (sameMethod ? current?.date ?? null : null),
        },
      };
    });
  }, []);

  async function persistAndContinue() {
    if (!projection || !isValid) return;
    const requestedGroups: StorefrontShippingGroupSelectionInput[] = isGroupedCart
      ? groupSelectionsByMethod.map((group) => ({
          shippingMethodCode: group.methodCode,
          // شناسه‌های واقعی خطوط سبد — هیچ خطی حدس زده یا دوباره اختصاص داده نمی‌شود.
          cartLineIds: group.lines.map((line) => line.lineId),
          serviceCode: group.method.serviceCode ?? null,
          optionCode: null,
          // (150) زمان‌بندی **همین گروه**؛ نه یک تاریخ سراسری برای همهٔ گروه‌ها.
          selectedDeliveryDate: groupSelections[group.key]?.deliveryDate ?? "",
          selectedDeliveryTimeWindow: groupSelections[group.key]?.deliveryTime ?? "",
        }))
      : [];
    const primaryMethodCode = isGroupedCart ? requestedGroups[0]?.shippingMethodCode ?? "" : methodCode ?? "";
    /** (150) تنها گروهِ سبد: همان shorthand قدیمی تک‌گروهی معتبر است (سرور فقط >۱ گروه را چندگروهی می‌داند). */
    const onlyGroupSelection = groupSelectionsByMethod.length === 1
      ? groupSelections[groupSelectionsByMethod[0].key]
      : undefined;
    /**
     * TB-237: قصد capture‌شده فقط از همان پاسخ موفق ساخته می‌شود: بازتاب دقیق snapshot/مبدأ/نسخه/ناحیه/
     * ردیف + مبلغ کالای همان گروه + تاریخ مجاز همان روش. هیچ روش/تاریخ/مبلغی حدس زده نمی‌شود و هیچ
     * shorthand قدیمی مخلوط نمی‌شود. نبود هر بازتاب یعنی قصد ناقص ⇒ ارسال نمی‌شود.
     */
    const capturedGroups = capturedPreviewActive
      ? buildCapturedSelectionIntent(projection.capturedPreview, capturedChoices)
      : null;
    if (capturedPreviewActive && capturedGroups === null) {
      setError(locale === "fa"
        ? "برای هر گروه یک روش و تاریخ معتبر انتخاب کنید و دوباره تلاش کنید."
        : "Choose a valid method and date for every group, then try again.");
      return;
    }
    const capturedPrimary = capturedGroups?.[0];
    const effectiveMethodCode = capturedPrimary?.shippingMethodCode ?? primaryMethodCode;
    if (!effectiveMethodCode) return;
    setBusy(true);
    setError(null);
    setLimitCode(null);
    try {
      await saveShippingSelection(projection.cartId, projection.cartVersion, {
        firstName: address.firstName,
        lastName: address.lastName,
        recipientName: composeRecipient(address),
        contactMobile: address.contactMobile,
        provinceName: address.provinceName,
        cityName: address.cityName,
        postalAddress: address.postalAddress,
        postalCode: address.postalCode,
        savedAddressId,
        shippingMethodCode: effectiveMethodCode,
        // (150) مقدار اسکالری فقط برای سازگاری مسیر تک‌گروهی ارسال می‌شود؛ در سبد چندگروهی هیچ
        // تاریخ سراسری معتبر نیست و هر گروه زمان‌بندی خودش را جدا می‌فرستد.
        selectedDeliveryDate: isGroupedCart ? onlyGroupSelection?.deliveryDate ?? "" : deliveryDate,
        selectedDeliveryTimeWindow: isGroupedCart ? onlyGroupSelection?.deliveryTime ?? "" : deliveryTime,
        customerNote: notes,
        shippingGroups: capturedPreviewActive ? null : (isGroupedCart ? requestedGroups : null),
        capturedGroups,
        // نسخهٔ انتخاب ریشه: از همان پاسخ موفق، وگرنه نسخهٔ ذخیره‌شدهٔ پیش‌نویس، وگرنه «0» (نبود).
        expectedSelectionRevision: capturedPreviewActive
          ? projection.capturedPreview?.selectionRevision ?? projection.draft?.selectionRevision ?? "0"
          : null,
        // همان قصد کوپنِ همین context؛ پاک‌کردن صریح یعنی «بدون کوپن»، نه fallback به مقدار ذخیره‌شده.
        couponCode: couponIntent,
      });
      // ارسال فقط «پیش‌نویس» می‌شود و سبد Active می‌ماند. commit/رزرو/ساخت سفارش فقط با کلیک نهایی
      // پرداخت در صفحهٔ پرداخت رخ می‌دهد؛ اینجا هیچ درخواست commit ارسال نمی‌شود.
      router.push(localizePath(`/payment?cartId=${encodeURIComponent(projection.cartId)}`));
    } catch (cause) {
      setError(toCustomerShippingMessage(cause, locale));
      setLimitCode(cause instanceof StorefrontCartApiError ? cause.errorCode : null);
    } finally {
      setBusy(false);
    }
  }

  async function submitNewAddress() {
    setBusy(true);
    setError(null);
    try {
      if (savedAddresses) {
        if (!newAddress.firstName.trim() || !newAddress.lastName.trim()) {
          throw new Error("نام و نام‌خانوادگی هر دو الزامی است.");
        }
        const created = await createCustomerAddress({
          firstName: newAddress.firstName,
          lastName: newAddress.lastName,
          recipientName: composeRecipient(newAddress),
          contactMobile: newAddress.contactMobile,
          country: "IR",
          provinceName: newAddress.provinceName,
          cityName: newAddress.cityName,
          postalCode: newAddress.postalCode,
          postalAddress: newAddress.postalAddress,
        });
        const list = await listCheckoutSavedAddresses();
        setSavedAddresses(list.addresses);
        selectSaved(created);
      } else {
        setAddress(newAddress);
        setSavedAddressId(null);
        setUseSavedAddress(false);
        await refreshProjection(newAddress.provinceName, methodCode, newAddress.cityName);
      }
      setShowNewDialog(false);
      setNewAddress(emptyAddress);
    } catch (cause) {
      setError(toCustomerShippingMessage(cause, locale));
    } finally {
      setBusy(false);
    }
  }

  if (!projection) {
    return (
      <div className="min-h-screen bg-page" data-testid="shipping-page" data-storefront-surface-role="page">
        <ShippingHero itemCount={0} subtotalLabel="—" shippingDaysLabel="—" />
        <div className="py-16 text-center">
          {error ? <p className="text-sm text-red-600">{error}</p> : <p className="text-sm text-gray-500">در حال آماده‌سازی ارسال…</p>}
          <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-primary text-white text-sm font-bold">
            بازگشت به سبد
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-page pb-10" data-testid="shipping-page" data-storefront-surface-role="page">
      <nav className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 text-xs text-gray-500 mb-2 flex flex-wrap gap-2" aria-label="مسیر صفحه">
        <Link href="/cart" className="hover:text-primary">سبد خرید</Link>
        <span>/</span>
        <span className="text-gray-800">اطلاعات ارسال</span>
      </nav>
      <ShippingHero
        itemCount={projection.itemCount}
        subtotalLabel={formatOfferAmount(projection.subtotalExclusiveOfTax, projection.currency)}
        shippingDaysLabel={projection.capturedPreview !== null
          ? "—"
          : toPersianDigits(projection.maxSellerPreparationDays + (selectedMethod?.leadDays ?? 0))}
      />

      <section className="w-full bg-section-surface" data-storefront-surface-role="section">
        <div className="max-w-[1800px] mx-auto px-4 sm:px-6 py-8 md:py-10">
          {error ? (
            <div className="mb-4 bg-red-50 border border-red-100 rounded-xl px-4 py-3" data-testid="shipping-error">
              <StorefrontCheckoutLimitNotice errorCode={limitCode} message={error} />
            </div>
          ) : null}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 md:gap-6">
            <div className="lg:col-span-2 space-y-4">
              {/* (150) آدرس و تحویل گیرنده: استخراج‌شده بدون هیچ تغییر رفتاری/بصری */}
              <StorefrontShippingAddressCards
                address={address}
                onAddressChange={setAddress}
                savedAddresses={savedAddresses}
                useSavedAddress={useSavedAddress}
                savedAddressId={savedAddressId}
                showSaved={showSaved}
                onToggleSaved={() => setShowSaved((v) => !v)}
                onSelectSaved={selectSaved}
                onOpenNewAddress={() => {
                  setShowNewDialog(true);
                  setUseSavedAddress(false);
                }}
                provinceOptions={provinceOptions}
                cityOptions={cityOptions}
                onProvinceChange={(provinceName) => {
                  setAddress((a) => ({ ...a, provinceName, cityName: "" }));
                  void refreshProjection(provinceName, methodCode);
                }}
              />

              {/* (150) یک کارت مستقل برای هر گروه ارسال واقعی؛ تاریخ/بازه داخل همان کارت.
                  TB-229-R1: وقتی پیش‌نمایش capture‌شده روشن است، همین کارت‌ها تنها نمایش ارسال‌اند
                  (سبد تک‌خطی هم از همین مسیر می‌آید) و هیچ کارت/روش/تاریخ قدیمی در کنارش رندر نمی‌شود. */}
              {isGroupedCart || projection.capturedPreview !== null ? (
                <StorefrontShippingGroupCards
                  groups={groupCards}
                  unassigned={unassignedSellerBlocks}
                  methodByLine={lineMethodByLine}
                  methods={projection.methods}
                  unavailableNotices={unavailableNotices}
                  currency={projection.currency}
   schedules={groupSchedules}
   selections={groupSelections}
                  capturedPreview={capturedPreviewForCards}
                  capturedChoices={capturedChoices}
                  onSelectCapturedMethod={selectCapturedMethod}
                  destinationRefreshState={capturedPreviewActive ? destinationPreviewMode : null}
                  onRetryDestination={retryDestinationRefresh}
                  onSelectMethod={(lineId, code) => setLineMethodByLine((prev) => ({ ...prev, [lineId]: code }))}
                  onSelectDate={(groupKey, value) =>
                    setGroupSelections((prev) => setGroupSelection(prev, groupKey, { deliveryDate: value }))
                  }
                  onSelectWindow={(groupKey, value) =>
                    setGroupSelections((prev) => setGroupSelection(prev, groupKey, { deliveryTime: value }))
                  }
                />
              ) : null}

              {/* (150) مسیر تک‌گروهی: روش + زمان تحویل؛ در سبد چندگروهی هر گروه کارت مستقل دارد. */}
              {isGroupedCart || projection.capturedPreview !== null ? null : (
                <StorefrontShippingSingleGroupSection
                  methods={projection.methods}
                  unavailableNotices={unavailableNotices}
                  currency={projection.currency}
                  methodCode={methodCode}
                  onSelectMethod={(code) => void onSelectMethod(code)}
                  horizon={singleGroupHorizon}
                  deliveryDate={deliveryDate}
                  deliveryTime={deliveryTime}
                  onSelectDate={setDeliveryDate}
                  onSelectWindow={setDeliveryTime}
                />
              )}
              {/* Notes */}
              <div className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-storefront-surface-role="card" data-testid="shipping-notes">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
                  <Edit3 className="w-4 h-4 text-primary" />
                  توضیحات سفارش <span className="text-[10px] font-normal text-gray-400">(اختیاری)</span>
                </h3>
                <textarea
                  rows={2}
                  maxLength={500}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="نکات خاص برای ارسال (مثل: زنگ واحد را بزنید، تحویل درب منزل)..."
                  className="w-full px-4 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none resize-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>

            <aside className="lg:col-span-1">
              <div className="lg:sticky lg:top-24 space-y-4">
                <div className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-storefront-surface-role="card" data-testid="shipping-summary">
                  <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-4">
                    <CreditCard className="w-4 h-4 text-primary" />
                    خلاصه سفارش
                  </h3>
                  <div className="space-y-2.5 text-xs md:text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-500">تعداد کالاها</span>
                      <span className="font-bold">{toPersianDigits(projection.itemCount)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">مبلغ کالا</span>
                      <span className="font-bold">{formatOfferAmount(projection.subtotalExclusiveOfTax, projection.currency)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">هزینه ارسال</span>
                      <span className={`font-bold ${shippingAmount === 0 ? "text-emerald-500" : "text-gray-900"}`}>
                        {capturedPreviewActive
                          ? "—"
                          : shippingAmount === 0 && (isGroupedCart ? groupSelectionsByMethod.length > 0 : selectedMethod)
                            ? "رایگان"
                            : (isGroupedCart ? allGroupsHaveMethod : selectedMethod)
                              ? formatOfferAmount(shippingAmount, projection.currency)
                              : "—"}
                      </span>
                    </div>
                    {capturedPreviewActive ? (
                      <p className="text-[10px] text-gray-500" data-testid="shipping-captured-summary-note">
                        هزینه و تاریخ ارسال در کارت هر گروه نمایش داده می‌شود؛ مبلغ نهایی پس از تکمیل مسیر
                        انتخاب/ثبت محاسبه می‌شود.
                      </p>
                    ) : null}
                    {isGroupedCart && !capturedPreviewActive ? (
                      <div className="space-y-1.5 border-t border-dashed border-gray-200 pt-2.5 mt-1" data-testid="shipping-group-fees">
                        {groupSelectionsByMethod.map((group) => (
                          <div key={`fee-${group.key}`} className="flex justify-between gap-2 text-[11px] md:text-xs">
                            <span className="text-gray-500 truncate">
                              {group.sellerDisplayName}
                              {` — ${group.method.label}`}
                            </span>
                            <span className="font-bold text-gray-800 shrink-0">
                              {formatOfferAmount(group.method.priceAmount, projection.currency)}
                            </span>
                          </div>
                        ))}
                      </div>
                    ) : null}
                    <div className="border-t border-gray-200 pt-2.5 mt-2.5 flex justify-between">
                      <span className="text-sm font-black">قابل پرداخت</span>
                      <span className="text-base font-black text-primary">
                        {capturedPreviewActive ? "—" : formatOfferAmount(payable, projection.currency)}
                      </span>
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={!isValid || busy}
                    onClick={() => void persistAndContinue()}
                    className={`mt-4 w-full py-3.5 rounded-2xl font-black text-sm flex items-center justify-center gap-2 transition-all shadow-lg ${
                      isValid && !busy
                        ? "bg-primary text-white hover:bg-primary-strong shadow-primary/25"
                        : "bg-gray-300 text-gray-500 cursor-not-allowed"
                    }`}
                    data-testid="shipping-continue"
                  >
                    {busy ? "در حال ثبت…" : "ادامه به پرداخت"}
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <div className="flex items-center justify-center gap-3 mt-3 text-[10px] text-gray-400">
                    <span className="flex items-center gap-1">
                      <Shield className="w-3 h-3" /> پرداخت امن
                    </span>
                    <span className="flex items-center gap-1">
                      <Truck className="w-3 h-3" /> ارسال با روش انتخابی شما
                    </span>
                  </div>
                </div>
              </div>
            </aside>
          </div>
        </div>
      </section>

      <StorefrontShippingNewAddressDialog
        open={showNewDialog}
        newAddress={newAddress}
        onNewAddressChange={setNewAddress}
        provinceOptions={provinceOptions}
        provinces={provinces}
        busy={busy}
        onCancel={() => setShowNewDialog(false)}
        onSubmit={() => void submitNewAddress()}
      />
    </div>
  );
}

function ShippingHero({
  itemCount,
  subtotalLabel,
  shippingDaysLabel,
}: {
  itemCount: number;
  subtotalLabel: string;
  shippingDaysLabel: string;
}) {
  const steps: { id: number; label: string; icon: typeof ShoppingBag; href: string | null }[] = [
    { id: 1, label: "سبد خرید", icon: ShoppingBag, href: "/cart" },
    { id: 2, label: "ارسال", icon: Truck, href: null },
    { id: 3, label: "پرداخت", icon: CreditCard, href: null },
  ];
  return (
    <section className="w-full bg-section-surface" data-storefront-surface-role="section">
      <div className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 md:pt-6">
        <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-primary to-primary-strong min-h-[220px] md:min-h-[240px]">
          <div className="relative z-10 flex flex-col items-center justify-center text-center p-6 pt-8 md:p-10 md:pt-12">
            <span className="inline-flex items-center gap-1.5 bg-surface/15 text-white text-[10px] md:text-xs font-bold px-3 py-1.5 rounded-full mb-4">
              <Truck className="w-3.5 h-3.5" />
              اطلاعات ارسال و تحویل
            </span>
            <h1 className="text-lg md:text-3xl font-black text-white leading-snug mb-1">اطلاعات ارسال</h1>
            <p className="text-[10px] md:text-sm text-white/80">آدرس و روش ارسال خود را انتخاب کنید</p>
          </div>
        </div>
        <div className="flex items-center justify-center gap-2 md:gap-3 -mt-6 relative z-20 mb-9" data-testid="shipping-steps">
          {steps.map((s, i) => {
            const className = `flex items-center gap-1.5 md:gap-2 px-4 md:px-5 py-2.5 md:py-3 rounded-xl text-[11px] md:text-sm font-bold ${
              s.id === 2
                ? "bg-primary text-white shadow-lg shadow-primary/30"
                : s.id < 2
                  ? "bg-red-50 text-red-600 hover:bg-red-100"
                  : "bg-gray-100 text-gray-400"
            }`;
            const content = (
              <>
                <s.icon className="w-4 h-4" />
                {s.id < 2 ? <Check className="w-3.5 h-3.5" /> : null}
                {s.label}
              </>
            );
            return (
              <div key={s.id} className="flex items-center gap-2">
                {s.href ? (
                  <Link
                    href={s.href}
                    className={className}
                    data-testid={`shipping-step-${s.id}`}
                    aria-label="بازگشت به سبد خرید"
                  >
                    {content}
                  </Link>
                ) : (
                  <div className={className} data-testid={`shipping-step-${s.id}`}>
                    {content}
                  </div>
                )}
                {i < steps.length - 1 ? <ChevronLeft className="w-4 h-4 text-red-400" /> : null}
              </div>
            );
          })}
        </div>
        <div className="grid grid-cols-3 gap-1.5 md:gap-4 mb-4">
          <SummaryCard icon={ShoppingBag} value={toPersianDigits(itemCount)} label="تعداد کالا" />
          <SummaryCard icon={CreditCard} value={subtotalLabel} label="جمع کل" />
          <SummaryCard icon={Truck} value={shippingDaysLabel} label="حداقل روز تحویل" />
        </div>
      </div>
    </section>
  );
}

function SummaryCard({
  icon: Icon,
  value,
  label,
}: {
  icon: typeof ShoppingBag;
  value: string;
  label: string;
}) {
  return (
    <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-surface border border-gray-200 shadow-lg" data-storefront-surface-role="card">
      <div className="w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
        <Icon className="w-3 h-3 md:w-5 md:h-5 text-primary" />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] md:text-2xl font-black text-gray-900 truncate">{value}</p>
        <p className="text-[7px] md:text-xs text-gray-500 truncate">{label}</p>
      </div>
    </div>
  );
}
