"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import {
  Calendar,
  Check,
  ChevronDown,
  ChevronLeft,
  Clock,
  CreditCard,
  Edit3,
  Home,
  Mail,
  MapPin,
  Package,
  Phone,
  Plus,
  Shield,
  ShoppingBag,
  Store,
  Truck,
  User,
  Zap,
} from "lucide-react";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali.ts";
import { formatOfferAmount } from "./storefront-api.ts";
import { readCartSession } from "./storefront-cart-api.ts";
import {
  createCustomerAddress,
  listCheckoutSavedAddresses,
  shippingFromCustomerAddress,
  type CustomerAddress,
} from "../customer-panel/customer-address-api.ts";
import {
  commitShippingToPayment,
  loadShippingProjection,
  saveShippingSelection,
  toCustomerShippingMessage,
  type StorefrontProvinceOption,
  type StorefrontShippingMethod,
  type StorefrontShippingProjection,
} from "./storefront-shipping-api.ts";

type AddressForm = {
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
};

const emptyAddress: AddressForm = {
  recipientName: "",
  contactMobile: "",
  provinceName: "",
  cityName: "",
  postalAddress: "",
  postalCode: "",
};

function toPersianDigits(n: string | number): string {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]!);
}

/** برچسب نمایشی تاریخ تحویل — جلالی؛ value API همچنان میلادی می‌ماند. */
function deliveryDateUi(isoDate: string): { label: string; subLabel: string } {
  const subLabel = formatJalaliDate(`${isoDate}T12:00:00`, "fa");
  const todayIso = new Date().toISOString().slice(0, 10);
  const selected = Date.parse(`${isoDate}T12:00:00Z`);
  const today = Date.parse(`${todayIso}T12:00:00Z`);
  if (!Number.isFinite(selected) || !Number.isFinite(today)) {
    return { label: subLabel, subLabel };
  }
  const delta = Math.round((selected - today) / 86_400_000);
  const label = delta === 0 ? "امروز" : delta === 1 ? "فردا" : delta === 2 ? "پس‌فردا" : subLabel;
  return { label, subLabel };
}

function iconFor(method: StorefrontShippingMethod) {
  const key = method.iconKey || method.serviceCode;
  if (key.includes("zap") || key.includes("express") || method.methodCode.includes("express")) return Zap;
  if (key.includes("store") || key.includes("person") || method.serviceCode === "in_person") return Store;
  return Package;
}

/**
 * مرحلهٔ ارسال Shopeiva با دادهٔ واقعی Host (روش‌ها، قیمت، حداقل تحویل، پیش‌نویس).
 */
export function StorefrontShopeivaShipping() {
  const router = useRouter();
  const [projection, setProjection] = useState<StorefrontShippingProjection | null>(null);
  const [address, setAddress] = useState<AddressForm>(emptyAddress);
  const [savedAddresses, setSavedAddresses] = useState<CustomerAddress[] | null>(null);
  const [useSavedAddress, setUseSavedAddress] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const [savedAddressId, setSavedAddressId] = useState<string | null>(null);
  const [methodCode, setMethodCode] = useState<string | null>(null);
  const [deliveryDate, setDeliveryDate] = useState<string>("");
  const [deliveryTime, setDeliveryTime] = useState<string>("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showNewDialog, setShowNewDialog] = useState(false);
  const [newAddress, setNewAddress] = useState<AddressForm>(emptyAddress);

  const refreshProjection = useCallback(async (province?: string, method?: string | null) => {
    const session = readCartSession();
    if (!session.cartId) {
      setError("سبد خرید پیدا نشد.");
      setProjection(null);
      return;
    }
    const page = await loadShippingProjection({
      cartId: session.cartId,
      provinceName: province || undefined,
      methodCode: method || undefined,
    });
    setProjection(page);
    if (page.revalidationMessage) {
      setError(page.revalidationMessage);
    }

    const clampDate = (candidate: string | null | undefined) => {
      const min = page.minimumDeliveryDate;
      if (!min) return candidate ?? "";
      if (!candidate || candidate < min) return min;
      return candidate;
    };

    if (page.draft) {
      setAddress({
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
  }, []);

  useEffect(() => {
    void refreshProjection().catch((cause: unknown) => setError(toCustomerShippingMessage(cause)));
    void listCheckoutSavedAddresses()
      .then((result) => setSavedAddresses(result.addresses))
      .catch(() => setSavedAddresses(null));
  }, [refreshProjection]);

  const provinces: StorefrontProvinceOption[] = projection?.provinces ?? [];
  const provinceOptions = provinces.map((p) => ({ value: p.label, label: p.label, cities: p.cities }));
  const cityOptions = useMemo(() => {
    const match = provinces.find((p) => p.label === address.provinceName || p.code === address.provinceName);
    return match?.cities ?? [];
  }, [provinces, address.provinceName]);

  const selectedMethod = projection?.methods.find((m) => m.methodCode === methodCode) ?? null;
  const shippingAmount = selectedMethod?.priceAmount ?? projection?.selectedShippingAmount ?? 0;
  const payable = (projection?.subtotalExclusiveOfTax ?? 0) + shippingAmount;

  const isValid =
    Boolean(address.recipientName.trim()) &&
    Boolean(address.contactMobile.trim()) &&
    Boolean(address.provinceName.trim()) &&
    Boolean(address.cityName.trim()) &&
    Boolean(address.postalAddress.trim()) &&
    Boolean(address.postalCode.trim()) &&
    Boolean(methodCode) &&
    Boolean(deliveryDate) &&
    Boolean(deliveryTime) &&
    (projection?.methods.length ?? 0) > 0;

  function selectSaved(row: CustomerAddress) {
    const mapped = shippingFromCustomerAddress(row);
    setAddress({
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
    void refreshProjection(mapped.provinceName, methodCode).catch((cause: unknown) =>
      setError(toCustomerShippingMessage(cause)),
    );
  }

  async function onSelectMethod(code: string) {
    setMethodCode(code);
    setError(null);
    try {
      await refreshProjection(address.provinceName, code);
    } catch (cause) {
      setError(toCustomerShippingMessage(cause));
    }
  }

  async function persistAndContinue() {
    if (!projection || !methodCode || !isValid) return;
    setBusy(true);
    setError(null);
    try {
      await saveShippingSelection(projection.cartId, projection.cartVersion, {
        recipientName: address.recipientName,
        contactMobile: address.contactMobile,
        provinceName: address.provinceName,
        cityName: address.cityName,
        postalAddress: address.postalAddress,
        postalCode: address.postalCode,
        savedAddressId,
        shippingMethodCode: methodCode,
        selectedDeliveryDate: deliveryDate,
        selectedDeliveryTimeWindow: deliveryTime,
        customerNote: notes,
      });
      const committed = await commitShippingToPayment(projection.cartId, projection.cartVersion);
      if (!committed.checkoutId) {
        throw new Error("شناسهٔ سفارش برنگشت.");
      }
      router.push(`/payment?checkoutId=${encodeURIComponent(committed.checkoutId)}`);
    } catch (cause) {
      setError(toCustomerShippingMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function submitNewAddress() {
    setBusy(true);
    setError(null);
    try {
      if (savedAddresses) {
        const created = await createCustomerAddress({
          recipientName: newAddress.recipientName,
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
        await refreshProjection(newAddress.provinceName, methodCode);
      }
      setShowNewDialog(false);
      setNewAddress(emptyAddress);
    } catch (cause) {
      setError(toCustomerShippingMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (!projection) {
    return (
      <div className="min-h-screen bg-white" data-testid="shipping-page">
        <ShippingHero itemCount={0} subtotalLabel="—" shippingDaysLabel="—" />
        <div className="py-16 text-center">
          {error ? <p className="text-sm text-red-600">{error}</p> : <p className="text-sm text-gray-500">در حال آماده‌سازی ارسال…</p>}
          <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-[#E53935] text-white text-sm font-bold">
            بازگشت به سبد
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white pb-10" data-testid="shipping-page">
      <nav className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 text-xs text-gray-500 mb-2 flex flex-wrap gap-2" aria-label="مسیر صفحه">
        <Link href="/cart" className="hover:text-[#E53935]">سبد خرید</Link>
        <span>/</span>
        <span className="text-gray-800">اطلاعات ارسال</span>
      </nav>
      <ShippingHero
        itemCount={projection.itemCount}
        subtotalLabel={formatOfferAmount(projection.subtotalExclusiveOfTax, projection.currency)}
        shippingDaysLabel={toPersianDigits(projection.maxSellerPreparationDays + (selectedMethod?.leadDays ?? 0))}
      />

      <section className="w-full bg-white">
        <div className="max-w-[1800px] mx-auto px-4 sm:px-6 py-8 md:py-10">
          {error ? (
            <p className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl px-4 py-3" data-testid="shipping-error">
              {error}
            </p>
          ) : null}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 md:gap-6">
            <div className="lg:col-span-2 space-y-4">
              {/* Address selection */}
              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-address-section">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
                  <Home className="w-4 h-4 text-[#E53935]" />
                  انتخاب آدرس
                </h3>
                <div className="flex gap-3 mb-4">
                  <button
                    type="button"
                    onClick={() => {
                      setShowNewDialog(true);
                      setUseSavedAddress(false);
                    }}
                    className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                      !useSavedAddress ? "border-[#E53935] bg-[#E53935]/5 text-[#E53935]" : "border-gray-200 text-gray-500"
                    }`}
                    data-testid="shipping-new-address"
                  >
                    <Plus className="w-4 h-4" /> آدرس جدید
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowSaved((v) => !v)}
                    className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                      useSavedAddress ? "border-[#E53935] bg-[#E53935]/5 text-[#E53935]" : "border-gray-200 text-gray-500"
                    }`}
                    data-testid="shipping-saved-toggle"
                  >
                    <Home className="w-4 h-4" /> آدرس‌های من
                    <ChevronDown className={`w-3 h-3 transition-transform ${showSaved ? "rotate-180" : ""}`} />
                  </button>
                </div>
                {showSaved && (
                  <div className="space-y-2" data-testid="shipping-saved-list">
                    {(savedAddresses ?? []).length === 0 ? (
                      <p className="text-xs text-gray-500">نشانی ذخیره‌شده‌ای در دسترس نیست.</p>
                    ) : (
                      (savedAddresses ?? []).map((saved) => (
                        <button
                          key={saved.addressId}
                          type="button"
                          onClick={() => selectSaved(saved)}
                          className={`w-full text-right p-3 md:p-4 rounded-xl border-2 transition-all ${
                            useSavedAddress && savedAddressId === saved.addressId
                              ? "border-[#E53935] bg-[#E53935]/5"
                              : "border-gray-100 hover:border-gray-200 bg-gray-50"
                          }`}
                        >
                          <div className="flex items-start justify-between gap-2">
                            <div className="min-w-0">
                              <p className="text-xs md:text-sm font-bold text-gray-900">{saved.recipientName}</p>
                              <p className="text-[10px] md:text-xs text-gray-500">
                                {saved.provinceName}، {saved.cityName}، {saved.postalAddress}
                              </p>
                              <p className="text-[10px] text-gray-400 mt-0.5">
                                {saved.contactMobile} | کد پستی: {saved.postalCode}
                              </p>
                            </div>
                            {useSavedAddress && savedAddressId === saved.addressId ? (
                              <Check className="w-4 h-4 text-[#E53935] shrink-0 mt-1" />
                            ) : null}
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                )}
              </div>

              {/* Recipient */}
              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-recipient">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-4">
                  <MapPin className="w-4 h-4 text-[#E53935]" />
                  اطلاعات تحویل گیرنده
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3 md:gap-4">
                  <Field label="نام و نام خانوادگی">
                    <div className="relative">
                      <User className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        value={address.recipientName}
                        onChange={(e) => setAddress((a) => ({ ...a, recipientName: e.target.value }))}
                        className="w-full pr-10 pl-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935]"
                      />
                    </div>
                  </Field>
                  <Field label="شماره موبایل">
                    <div className="relative">
                      <Phone className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        dir="ltr"
                        value={address.contactMobile}
                        onChange={(e) =>
                          setAddress((a) => ({
                            ...a,
                            contactMobile: e.target.value.replace(/[^\d]/g, "").slice(0, 11),
                          }))
                        }
                        className="w-full pr-10 pl-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935]"
                      />
                    </div>
                  </Field>
                  <Field label="استان">
                    <select
                      value={address.provinceName}
                      onChange={(e) => {
                        const provinceName = e.target.value;
                        setAddress((a) => ({ ...a, provinceName, cityName: "" }));
                        void refreshProjection(provinceName, methodCode);
                      }}
                      className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935]"
                    >
                      <option value="">انتخاب استان</option>
                      {provinceOptions.map((p) => (
                        <option key={p.label} value={p.label}>
                          {p.label}
                        </option>
                      ))}
                    </select>
                  </Field>
                  <Field label="شهر">
                    <select
                      value={address.cityName}
                      disabled={!address.provinceName}
                      onChange={(e) => setAddress((a) => ({ ...a, cityName: e.target.value }))}
                      className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935] disabled:opacity-50"
                    >
                      <option value="">{address.provinceName ? "انتخاب شهر" : "ابتدا استان را انتخاب کنید"}</option>
                      {cityOptions.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </select>
                  </Field>
                </div>
                <Field label="آدرس کامل" className="mt-3">
                  <textarea
                    rows={3}
                    value={address.postalAddress}
                    onChange={(e) => setAddress((a) => ({ ...a, postalAddress: e.target.value }))}
                    className="w-full px-4 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none resize-none focus:ring-2 focus:ring-[#E53935]"
                  />
                </Field>
                <div className="mt-3 max-w-xs">
                  <Field label="کد پستی">
                    <div className="relative">
                      <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        dir="ltr"
                        maxLength={10}
                        value={address.postalCode}
                        onChange={(e) =>
                          setAddress((a) => ({
                            ...a,
                            postalCode: e.target.value.replace(/[^\d]/g, "").slice(0, 10),
                          }))
                        }
                        className="w-full pl-10 pr-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935]"
                      />
                    </div>
                  </Field>
                </div>
              </div>

              {/* Methods */}
              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-methods">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
                  <Package className="w-4 h-4 text-[#E53935]" />
                  روش ارسال
                </h3>
                {projection.methods.length === 0 ? (
                  <p className="text-sm text-gray-500" data-testid="shipping-methods-empty">
                    در حال حاضر روش ارسالی برای این مقصد فعال نیست.
                  </p>
                ) : (
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                    {projection.methods.map((m) => {
                      const Icon = iconFor(m);
                      const selected = methodCode === m.methodCode;
                      return (
                        <button
                          key={m.methodCode}
                          type="button"
                          onClick={() => void onSelectMethod(m.methodCode)}
                          className={`relative flex items-start gap-3 p-4 rounded-2xl border-2 text-right transition-all ${
                            selected
                              ? "border-[#E53935] bg-[#E53935]/5 shadow-md shadow-[#E53935]/10"
                              : "border-gray-200 hover:border-gray-300 bg-gray-50"
                          }`}
                          data-testid={`shipping-method-${m.methodCode}`}
                        >
                          <div
                            className={`w-11 h-11 rounded-xl flex items-center justify-center shrink-0 ${
                              selected ? "bg-[#E53935]/10" : "bg-white"
                            }`}
                          >
                            <Icon className={`w-5 h-5 ${selected ? "text-[#E53935]" : "text-gray-400"}`} />
                          </div>
                          <div className="min-w-0 flex-1">
                            <p className={`text-xs md:text-sm font-bold ${selected ? "text-[#E53935]" : "text-gray-700"}`}>
                              {m.label}
                            </p>
                            <p className={`text-[11px] md:text-sm font-black mt-0.5 ${selected ? "text-[#E53935]" : "text-gray-900"}`}>
                              {m.isFree || m.priceAmount === 0
                                ? "رایگان"
                                : `${formatOfferAmount(m.priceAmount, projection.currency)}`}
                            </p>
                            <p className="text-[9px] text-gray-400 mt-0.5 flex items-center gap-0.5">
                              <Clock className="w-2.5 h-2.5" /> {m.estimationLabel}
                            </p>
                          </div>
                          {selected ? <div className="absolute top-2 left-2 w-3 h-3 rounded-full bg-[#E53935]" /> : null}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>

              {/* Delivery */}
              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-delivery">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
                  <Calendar className="w-4 h-4 text-[#E53935]" />
                  زمان تحویل
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs md:text-sm font-bold text-gray-700 mb-2 pr-1">تاریخ تحویل</label>
                    {projection.deliveryDates.length === 0 ? (
                      <p className="text-xs text-gray-500 bg-gray-50 border border-dashed border-gray-200 rounded-xl p-3">
                        ابتدا روش ارسال را انتخاب کنید تا نزدیک‌ترین تاریخ مجاز فروشگاه نمایش داده شود.
                      </p>
                    ) : (
                      <>
                        {projection.minimumDeliveryDate ? (
                          <p className="text-[11px] text-gray-500 mb-2">
                            زودتر از{" "}
                            <span className="font-bold text-gray-700">
                              {formatJalaliDate(`${projection.minimumDeliveryDate}T12:00:00`, "fa")}
                            </span>{" "}
                            قابل انتخاب نیست.
                          </p>
                        ) : null}
                        <div className="flex flex-wrap gap-2">
                          {projection.deliveryDates.map((d) => {
                            const disabled = Boolean(
                              projection.minimumDeliveryDate && d.value < projection.minimumDeliveryDate,
                            );
                            const selected = deliveryDate === d.value;
                            const ui = deliveryDateUi(d.value);
                            return (
                              <button
                                key={d.value}
                                type="button"
                                disabled={disabled}
                                onClick={() => {
                                  if (disabled) return;
                                  setDeliveryDate(d.value);
                                }}
                                title={disabled ? "قبل از حداقل زمان فروشگاه مجاز نیست" : ui.subLabel}
                                className={`flex-1 min-w-[5.5rem] text-center p-3 rounded-2xl border-2 transition-all disabled:opacity-40 disabled:cursor-not-allowed ${
                                  selected
                                    ? "border-[#E53935] bg-[#E53935]/5 text-[#E53935]"
                                    : "border-gray-200 text-gray-500 bg-gray-50"
                                }`}
                                data-testid={`shipping-date-${d.value}`}
                              >
                                <p className="text-xs md:text-sm font-bold">{ui.label}</p>
                                <p className="text-[10px] md:text-xs opacity-70" dir="ltr">
                                  {ui.subLabel}
                                </p>
                              </button>
                            );
                          })}
                        </div>
                      </>
                    )}
                  </div>
                  <div>
                    <label className="block text-xs md:text-sm font-bold text-gray-700 mb-2 pr-1">ساعت تحویل</label>
                    <select
                      value={deliveryTime}
                      onChange={(e) => setDeliveryTime(e.target.value)}
                      className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-[#E53935]"
                      data-testid="shipping-time"
                    >
                      <option value="">محدوده زمانی را انتخاب کنید</option>
                      {projection.deliveryTimeWindows.map((t) => (
                        <option key={t.value} value={t.value}>
                          {t.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>

              {/* Notes */}
              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-notes">
                <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
                  <Edit3 className="w-4 h-4 text-[#E53935]" />
                  توضیحات سفارش <span className="text-[10px] font-normal text-gray-400">(اختیاری)</span>
                </h3>
                <textarea
                  rows={2}
                  maxLength={500}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="نکات خاص برای ارسال (مثل: زنگ واحد را بزنید، تحویل درب منزل)..."
                  className="w-full px-4 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none resize-none focus:ring-2 focus:ring-[#E53935]"
                />
              </div>
            </div>

            <aside className="lg:col-span-1">
              <div className="lg:sticky lg:top-24 space-y-4">
                <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="shipping-summary">
                  <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-4">
                    <CreditCard className="w-4 h-4 text-[#E53935]" />
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
                        {shippingAmount === 0 && selectedMethod
                          ? "رایگان"
                          : selectedMethod
                            ? formatOfferAmount(shippingAmount, projection.currency)
                            : "—"}
                      </span>
                    </div>
                    <div className="border-t border-gray-200 pt-2.5 mt-2.5 flex justify-between">
                      <span className="text-sm font-black">قابل پرداخت</span>
                      <span className="text-base font-black text-[#E53935]">
                        {formatOfferAmount(payable, projection.currency)}
                      </span>
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={!isValid || busy}
                    onClick={() => void persistAndContinue()}
                    className={`mt-4 w-full py-3.5 rounded-2xl font-black text-sm flex items-center justify-center gap-2 transition-all shadow-lg ${
                      isValid && !busy
                        ? "bg-[#E53935] text-white hover:bg-[#d32f2f] shadow-[#E53935]/25"
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
                      <Truck className="w-3 h-3" /> ارسال از پیکربندی فروشگاه
                    </span>
                  </div>
                </div>
              </div>
            </aside>
          </div>
        </div>
      </section>

      {showNewDialog ? (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4" data-testid="shipping-new-dialog">
          <div className="bg-white rounded-2xl max-w-lg w-full p-5 shadow-xl space-y-3">
            <h4 className="font-black text-gray-900">آدرس جدید</h4>
            <input
              placeholder="نام و نام خانوادگی"
              value={newAddress.recipientName}
              onChange={(e) => setNewAddress((a) => ({ ...a, recipientName: e.target.value }))}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
            />
            <input
              placeholder="موبایل"
              dir="ltr"
              value={newAddress.contactMobile}
              onChange={(e) =>
                setNewAddress((a) => ({ ...a, contactMobile: e.target.value.replace(/[^\d]/g, "").slice(0, 11) }))
              }
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
            />
            <select
              value={newAddress.provinceName}
              onChange={(e) => setNewAddress((a) => ({ ...a, provinceName: e.target.value, cityName: "" }))}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
            >
              <option value="">استان</option>
              {provinceOptions.map((p) => (
                <option key={p.label} value={p.label}>
                  {p.label}
                </option>
              ))}
            </select>
            <select
              value={newAddress.cityName}
              onChange={(e) => setNewAddress((a) => ({ ...a, cityName: e.target.value }))}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
            >
              <option value="">شهر</option>
              {(provinces.find((p) => p.label === newAddress.provinceName)?.cities ?? []).map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            <textarea
              placeholder="آدرس کامل"
              value={newAddress.postalAddress}
              onChange={(e) => setNewAddress((a) => ({ ...a, postalAddress: e.target.value }))}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
              rows={3}
            />
            <input
              placeholder="کد پستی"
              dir="ltr"
              value={newAddress.postalCode}
              onChange={(e) =>
                setNewAddress((a) => ({ ...a, postalCode: e.target.value.replace(/[^\d]/g, "").slice(0, 10) }))
              }
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
            />
            <div className="flex gap-2 justify-end pt-2">
              <button type="button" className="px-4 py-2 rounded-xl text-sm" onClick={() => setShowNewDialog(false)}>
                انصراف
              </button>
              <button
                type="button"
                className="px-4 py-2 rounded-xl text-sm font-bold text-white bg-[#E53935]"
                onClick={() => void submitNewAddress()}
                disabled={busy}
              >
                تأیید آدرس
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Field({
  label,
  children,
  className = "",
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={`space-y-1.5 ${className}`}>
      <label className="block text-xs md:text-sm font-bold text-gray-700 pr-1">{label}</label>
      {children}
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
  const steps = [
    { id: 1, label: "سبد خرید", icon: ShoppingBag },
    { id: 2, label: "ارسال", icon: Truck },
    { id: 3, label: "پرداخت", icon: CreditCard },
  ];
  return (
    <section className="w-full bg-white">
      <div className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 md:pt-6">
        <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-[#E53935] to-[#b71c1c] min-h-[220px] md:min-h-[240px]">
          <div className="relative z-10 flex flex-col items-center justify-center text-center p-6 pt-8 md:p-10 md:pt-12">
            <span className="inline-flex items-center gap-1.5 bg-white/15 text-white text-[10px] md:text-xs font-bold px-3 py-1.5 rounded-full mb-4">
              <Truck className="w-3.5 h-3.5" />
              اطلاعات ارسال و تحویل
            </span>
            <h1 className="text-lg md:text-3xl font-black text-white leading-snug mb-1">اطلاعات ارسال</h1>
            <p className="text-[10px] md:text-sm text-white/80">آدرس و روش ارسال خود را انتخاب کنید</p>
          </div>
        </div>
        <div className="flex items-center justify-center gap-2 md:gap-3 -mt-6 relative z-20 mb-9">
          {steps.map((s, i) => (
            <div key={s.id} className="flex items-center gap-2">
              <div
                className={`flex items-center gap-1.5 md:gap-2 px-4 md:px-5 py-2.5 md:py-3 rounded-xl text-[11px] md:text-sm font-bold ${
                  s.id === 2
                    ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                    : s.id < 2
                      ? "bg-red-50 text-red-600"
                      : "bg-gray-100 text-gray-400"
                }`}
              >
                <s.icon className="w-4 h-4" />
                {s.id < 2 ? <Check className="w-3.5 h-3.5" /> : null}
                {s.label}
              </div>
              {i < steps.length - 1 ? <ChevronLeft className="w-4 h-4 text-red-400" /> : null}
            </div>
          ))}
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
    <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-white border border-gray-200 shadow-lg">
      <div className="w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl bg-[#E53935]/10 flex items-center justify-center shrink-0">
        <Icon className="w-3 h-3 md:w-5 md:h-5 text-[#E53935]" />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] md:text-2xl font-black text-gray-900 truncate">{value}</p>
        <p className="text-[7px] md:text-xs text-gray-500 truncate">{label}</p>
      </div>
    </div>
  );
}
