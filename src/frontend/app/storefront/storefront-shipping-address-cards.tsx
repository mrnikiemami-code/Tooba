"use client";

/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — کارت‌های آدرس/تحویل‌گیرنده و دیالوگ آدرس جدید.
 *
 * این بخش استخراج مکانیکی از `storefront-shipping.tsx` است تا آن فایل oversized بزرگ‌تر نشود؛
 * هیچ رفتار، اعتبارسنجی، متن یا data-testid تغییر نکرده است. منطق ارسال/گروه‌ها اینجا نیست.
 */
import type { ReactNode } from "react";
import { Check, ChevronDown, Home, Mail, MapPin, Phone, Plus, User } from "lucide-react";

import { recipientDisplayName, type CustomerAddress } from "../customer-panel/customer-address-api.ts";

export type ShippingAddressForm = {
  firstName: string;
  lastName: string;
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
};

export const EMPTY_SHIPPING_ADDRESS: ShippingAddressForm = {
  firstName: "",
  lastName: "",
  recipientName: "",
  contactMobile: "",
  provinceName: "",
  cityName: "",
  postalAddress: "",
  postalCode: "",
};

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

export function StorefrontShippingAddressCards({
  address,
  onAddressChange,
  savedAddresses,
  useSavedAddress,
  savedAddressId,
  showSaved,
  onToggleSaved,
  onSelectSaved,
  onOpenNewAddress,
  provinceOptions,
  cityOptions,
  onProvinceChange,
}: {
  address: ShippingAddressForm;
  onAddressChange: (next: ShippingAddressForm) => void;
  savedAddresses: readonly CustomerAddress[] | null;
  useSavedAddress: boolean;
  savedAddressId: string | null;
  showSaved: boolean;
  onToggleSaved: () => void;
  onSelectSaved: (saved: CustomerAddress) => void;
  onOpenNewAddress: () => void;
  provinceOptions: readonly { value: string; label: string; cities: string[] }[];
  cityOptions: readonly string[];
  onProvinceChange: (provinceName: string) => void;
}) {
  return (
    <>
      {/* Address selection */}
      <div
        className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm"
        data-storefront-surface-role="card"
        data-testid="shipping-address-section"
      >
        <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-3">
          <Home className="w-4 h-4 text-primary" />
          انتخاب آدرس
        </h3>
        <div className="flex gap-3 mb-4">
          <button
            type="button"
            onClick={onOpenNewAddress}
            className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
              !useSavedAddress ? "border-primary bg-primary/5 text-primary" : "border-gray-200 text-gray-500"
            }`}
            data-testid="shipping-new-address"
          >
            <Plus className="w-4 h-4" /> آدرس جدید
          </button>
          <button
            type="button"
            onClick={onToggleSaved}
            className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
              useSavedAddress ? "border-primary bg-primary/5 text-primary" : "border-gray-200 text-gray-500"
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
                  onClick={() => onSelectSaved(saved)}
                  className={`w-full text-right p-3 md:p-4 rounded-xl border-2 transition-all ${
                    useSavedAddress && savedAddressId === saved.addressId
                      ? "border-primary bg-primary/5"
                      : "border-gray-100 hover:border-gray-200 bg-gray-50"
                  }`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="text-xs md:text-sm font-bold text-gray-900">{recipientDisplayName(saved)}</p>
                      <p className="text-[10px] md:text-xs text-gray-500">
                        {saved.provinceName}، {saved.cityName}، {saved.postalAddress}
                      </p>
                      <p className="text-[10px] text-gray-400 mt-0.5">
                        {saved.contactMobile} | کد پستی: {saved.postalCode}
                      </p>
                    </div>
                    {useSavedAddress && savedAddressId === saved.addressId ? (
                      <Check className="w-4 h-4 text-primary shrink-0 mt-1" />
                    ) : null}
                  </div>
                </button>
              ))
            )}
          </div>
        )}
      </div>

      {/* Recipient */}
      <div
        className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm"
        data-storefront-surface-role="card"
        data-testid="shipping-recipient"
      >
        <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-4">
          <MapPin className="w-4 h-4 text-primary" />
          اطلاعات تحویل گیرنده
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 md:gap-4">
          <Field label="نام">
            <div className="relative">
              <User className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                value={address.firstName}
                onChange={(e) => onAddressChange({ ...address, firstName: e.target.value })}
                data-testid="shipping-first-name"
                className="w-full pr-10 pl-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </Field>
          <Field label="نام خانوادگی">
            <input
              value={address.lastName}
              onChange={(e) => onAddressChange({ ...address, lastName: e.target.value })}
              data-testid="shipping-last-name"
              className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
            />
          </Field>
          <Field label="شماره موبایل">
            <div className="relative">
              <Phone className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                dir="ltr"
                value={address.contactMobile}
                onChange={(e) =>
                  onAddressChange({
                    ...address,
                    contactMobile: e.target.value.replace(/[^\d]/g, "").slice(0, 11),
                  })
                }
                className="w-full pr-10 pl-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </Field>
          <Field label="استان">
            <select
              value={address.provinceName}
              onChange={(e) => onProvinceChange(e.target.value)}
              className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
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
              onChange={(e) => onAddressChange({ ...address, cityName: e.target.value })}
              className="w-full px-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary disabled:opacity-50"
              data-testid="shipping-city-select"
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
            onChange={(e) => onAddressChange({ ...address, postalAddress: e.target.value })}
            className="w-full px-4 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none resize-none focus:ring-2 focus:ring-primary"
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
                  onAddressChange({
                    ...address,
                    postalCode: e.target.value.replace(/[^\d]/g, "").slice(0, 10),
                  })
                }
                className="w-full pl-10 pr-3 py-3 rounded-2xl text-sm bg-gray-50 border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </Field>
        </div>
      </div>
    </>
  );
}

export function StorefrontShippingNewAddressDialog({
  open,
  newAddress,
  onNewAddressChange,
  provinceOptions,
  provinces,
  busy,
  onCancel,
  onSubmit,
}: {
  open: boolean;
  newAddress: ShippingAddressForm;
  onNewAddressChange: (next: ShippingAddressForm) => void;
  provinceOptions: readonly { value: string; label: string; cities: string[] }[];
  provinces: readonly { label: string; cities: string[] }[];
  busy: boolean;
  onCancel: () => void;
  onSubmit: () => void;
}) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4" data-testid="shipping-new-dialog">
      <div className="bg-surface-elevated rounded-2xl max-w-lg w-full p-5 shadow-xl space-y-3" data-storefront-surface-role="overlay">
        <h4 className="font-black text-gray-900">آدرس جدید</h4>
        <input
          placeholder="نام"
          value={newAddress.firstName}
          onChange={(e) => onNewAddressChange({ ...newAddress, firstName: e.target.value })}
          data-testid="shipping-new-first-name"
          className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
        />
        <input
          placeholder="نام خانوادگی"
          value={newAddress.lastName}
          onChange={(e) => onNewAddressChange({ ...newAddress, lastName: e.target.value })}
          data-testid="shipping-new-last-name"
          className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
        />
        <input
          placeholder="موبایل"
          dir="ltr"
          value={newAddress.contactMobile}
          onChange={(e) =>
            onNewAddressChange({
              ...newAddress,
              contactMobile: e.target.value.replace(/[^\d]/g, "").slice(0, 11),
            })
          }
          className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
        />
        <select
          value={newAddress.provinceName}
          onChange={(e) => onNewAddressChange({ ...newAddress, provinceName: e.target.value, cityName: "" })}
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
          onChange={(e) => onNewAddressChange({ ...newAddress, cityName: e.target.value })}
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
          onChange={(e) => onNewAddressChange({ ...newAddress, postalAddress: e.target.value })}
          className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
          rows={3}
        />
        <input
          placeholder="کد پستی"
          dir="ltr"
          value={newAddress.postalCode}
          onChange={(e) =>
            onNewAddressChange({
              ...newAddress,
              postalCode: e.target.value.replace(/[^\d]/g, "").slice(0, 10),
            })
          }
          className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm"
        />
        <div className="flex gap-2 justify-end pt-2">
          <button type="button" className="px-4 py-2 rounded-xl text-sm" onClick={onCancel}>
            انصراف
          </button>
          <button
            type="button"
            className="px-4 py-2 rounded-xl text-sm font-bold text-white bg-primary"
            onClick={onSubmit}
            disabled={busy}
          >
            تأیید آدرس
          </button>
        </div>
      </div>
    </div>
  );
}
