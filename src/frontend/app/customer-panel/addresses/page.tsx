"use client";

import { useEffect, useState } from "react";
import {
  Building,
  Check,
  Edit2,
  Home,
  MapPin,
  Phone,
  Plus,
  Save,
  Trash2,
  User,
  X,
} from "lucide-react";
import {
  addressBookEmptyMessage,
  addressBookErrorMessage,
  createCustomerAddress,
  deleteCustomerAddress,
  listCustomerAddresses,
  setDefaultCustomerAddress,
  updateCustomerAddress,
  type CustomerAddress,
  type CustomerAddressWriteInput,
} from "../customer-address-api";

const emptyForm: CustomerAddressWriteInput = {
  recipientName: "",
  contactMobile: "",
  country: "IR",
  provinceName: "",
  cityName: "",
  postalCode: "",
  postalAddress: "",
  buildingUnit: "",
  label: "",
  isDefault: false,
};

function toForm(address: CustomerAddress): CustomerAddressWriteInput {
  return {
    recipientName: address.recipientName,
    contactMobile: address.contactMobile,
    country: address.country,
    provinceName: address.provinceName,
    cityName: address.cityName,
    postalCode: address.postalCode,
    postalAddress: address.postalAddress,
    buildingUnit: address.buildingUnit ?? "",
    label: address.label ?? "",
    isDefault: address.isDefault,
  };
}

function addressIcon(label: string | null) {
  const value = label ?? "";
  return /کار|دفتر|اداره/.test(value) ? Building : Home;
}

/**
 * دفترچهٔ آدرس زنده با کارت و فرم Shopeiva؛ زمان ارسال و نوع ساختگی اضافه نشده است.
 */
export default function CustomerAddressesPage() {
  const [rows, setRows] = useState<CustomerAddress[] | null | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CustomerAddressWriteInput>(emptyForm);
  const [busy, setBusy] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  async function refresh() {
    const list = await listCustomerAddresses();
    setRows(list);
    setError(null);
  }

  useEffect(() => {
    void refresh().catch((cause) => {
      setRows(null);
      setError(addressBookErrorMessage(cause));
    });
  }, []);

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm);
    setShowForm(true);
  }

  function openEdit(address: CustomerAddress) {
    setEditingId(address.addressId);
    setForm(toForm(address));
    setShowForm(true);
  }

  function closeForm() {
    setShowForm(false);
    setEditingId(null);
    setForm(emptyForm);
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      if (editingId) await updateCustomerAddress(editingId, form);
      else await createCustomerAddress(form);
      closeForm();
      await refresh();
    } catch (cause) {
      setError(addressBookErrorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function onDefault(addressId: string) {
    setBusy(true);
    setError(null);
    try {
      await setDefaultCustomerAddress(addressId);
      await refresh();
    } catch (cause) {
      setError(addressBookErrorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function onDelete(addressId: string) {
    setDeletingId(addressId);
    setError(null);
    try {
      await deleteCustomerAddress(addressId);
      await refresh();
    } catch (cause) {
      setError(addressBookErrorMessage(cause));
    } finally {
      setDeletingId(null);
    }
  }

  if (rows === undefined) {
    return <div className="rounded-2xl border bg-white p-8 text-center text-gray-500">در حال دریافت آدرس‌ها...</div>;
  }
  if (rows === null) {
    return <div role="alert" className="rounded-2xl border border-red-100 bg-white p-8 text-center text-red-600">{error}</div>;
  }

  const empty = addressBookEmptyMessage(rows.length);
  const defaultCount = rows.filter((row) => row.isDefault).length;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-[#2563EB]/10 flex items-center justify-center">
            <MapPin className="w-5 h-5 text-[#2563EB]" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-gray-900">آدرس‌های من</h1>
            <p className="text-xs text-gray-500">
              {rows.length.toLocaleString("fa-IR")} آدرس · {defaultCount.toLocaleString("fa-IR")} پیش‌فرض
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="px-4 py-2 bg-[#2563EB] text-white rounded-xl text-xs font-bold hover:bg-blue-700 transition-all shadow-lg shadow-[#2563EB]/30 flex items-center gap-1"
        >
          <Plus className="w-4 h-4" />
          آدرس جدید
        </button>
      </div>

      {error ? <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl p-3">{error}</p> : null}

      {empty ? (
        <div className="bg-white rounded-2xl p-8 text-center border border-gray-200">
          <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-3">
            <MapPin className="w-8 h-8 text-gray-300" />
          </div>
          <p className="text-sm text-gray-500">{empty}</p>
          <button
            type="button"
            onClick={openCreate}
            className="mt-3 px-4 py-2 bg-[#2563EB] text-white rounded-xl text-xs font-bold"
          >
            افزودن آدرس جدید
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {rows.map((addr) => {
            const Icon = addressIcon(addr.label);
            const deleting = deletingId === addr.addressId;
            return (
              <article
                key={addr.addressId}
                className={`bg-white rounded-2xl overflow-hidden border-2 transition-all ${
                  addr.isDefault ? "border-[#2563EB] shadow-lg shadow-[#2563EB]/10" : "border-gray-200 hover:border-[#2563EB]/30 hover:shadow-md"
                } ${deleting ? "opacity-50" : ""}`}
              >
                <div className={`p-3 border-b flex items-center justify-between ${addr.isDefault ? "bg-blue-50 border-blue-100" : "bg-gray-50 border-gray-100"}`}>
                  <div className="flex items-center gap-2">
                    <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${addr.isDefault ? "bg-[#2563EB]" : "bg-gray-500"}`}>
                      <Icon className="w-4 h-4 text-white" />
                    </div>
                    <p className="text-sm font-bold text-gray-900">{addr.label ?? "آدرس"}</p>
                  </div>
                  {addr.isDefault ? (
                    <span className="text-[8px] font-bold bg-[#2563EB] text-white px-2 py-0.5 rounded-full">پیش‌فرض</span>
                  ) : null}
                </div>
                <div className="p-3 space-y-2">
                  <p className="text-xs text-gray-600 leading-relaxed">{addr.postalAddress}</p>
                  <div className="flex flex-wrap items-center gap-2 text-[10px] text-gray-500">
                    <span className="flex items-center gap-0.5"><User className="w-3 h-3" />{addr.recipientName}</span>
                    <span className="w-px h-3 bg-gray-300" />
                    <span className="flex items-center gap-0.5"><Phone className="w-3 h-3" />{addr.contactMobile}</span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2 text-[10px] text-gray-400">
                    <span className="flex items-center gap-0.5 bg-gray-100 px-2 py-0.5 rounded-full">
                      <MapPin className="w-3 h-3" />
                      {addr.cityName}، {addr.provinceName}
                    </span>
                  </div>
                  <div className="flex items-center justify-end pt-2 border-t border-gray-100 gap-0.5">
                    {!addr.isDefault ? (
                      <button
                        type="button"
                        onClick={() => void onDefault(addr.addressId)}
                        disabled={busy}
                        className="p-1 text-gray-400 hover:text-[#2563EB] rounded hover:bg-gray-100"
                        title="تنظیم پیش‌فرض"
                        aria-label="تنظیم پیش‌فرض"
                      >
                        <Check className="w-3.5 h-3.5" />
                      </button>
                    ) : null}
                    <button
                      type="button"
                      onClick={() => openEdit(addr)}
                      className="p-1 text-gray-400 hover:text-blue-500 rounded hover:bg-gray-100"
                      aria-label="ویرایش آدرس"
                    >
                      <Edit2 className="w-3.5 h-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => void onDelete(addr.addressId)}
                      disabled={deleting}
                      className="p-1 text-gray-400 hover:text-red-500 rounded hover:bg-red-50 disabled:opacity-50"
                      aria-label="حذف آدرس"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      )}

      {showForm ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 border border-gray-200 shadow-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB]/10 flex items-center justify-center">
                  {editingId ? <Edit2 className="w-4 h-4 text-[#2563EB]" /> : <Plus className="w-4 h-4 text-[#2563EB]" />}
                </div>
                <h2 className="text-lg font-bold">{editingId ? "ویرایش آدرس" : "آدرس جدید"}</h2>
              </div>
              <button type="button" onClick={closeForm} className="p-2 rounded-lg hover:bg-gray-100" aria-label="بستن">
                <X className="w-5 h-5 text-gray-500" />
              </button>
            </div>
            <form onSubmit={(event) => void onSubmit(event)} className="space-y-4">
              <Field label="عنوان آدرس" value={form.label ?? ""} onChange={(label) => setForm({ ...form, label })} placeholder="مثلاً خانه، محل کار" required={false} />
              <label className="text-sm font-medium text-gray-700 block">
                آدرس کامل
                <textarea
                  required
                  value={form.postalAddress}
                  onChange={(event) => setForm({ ...form, postalAddress: event.target.value })}
                  rows={2}
                  placeholder="آدرس کامل را وارد کنید"
                  className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none"
                />
              </label>
              <div className="grid grid-cols-2 gap-4">
                <Field label="کشور" value={form.country} onChange={(country) => setForm({ ...form, country })} />
                <Field label="استان" value={form.provinceName} onChange={(provinceName) => setForm({ ...form, provinceName })} />
                <Field label="شهر" value={form.cityName} onChange={(cityName) => setForm({ ...form, cityName })} />
                <Field label="کد پستی" value={form.postalCode} onChange={(postalCode) => setForm({ ...form, postalCode })} ltr />
                <Field label="واحد / پلاک" value={form.buildingUnit ?? ""} onChange={(buildingUnit) => setForm({ ...form, buildingUnit })} required={false} />
                <Field label="نام گیرنده" value={form.recipientName} onChange={(recipientName) => setForm({ ...form, recipientName })} />
                <Field label="شماره تماس" value={form.contactMobile} onChange={(contactMobile) => setForm({ ...form, contactMobile })} ltr />
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isDefault"
                  checked={form.isDefault === true}
                  onChange={(event) => setForm({ ...form, isDefault: event.target.checked })}
                  className="w-4 h-4 accent-[#2563EB] rounded"
                />
                <label htmlFor="isDefault" className="text-sm text-gray-700">تنظیم به عنوان آدرس پیش‌فرض</label>
              </div>
              <div className="flex gap-3 pt-4 border-t border-gray-200">
                <button type="button" onClick={closeForm} className="px-6 py-2.5 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium">
                  انصراف
                </button>
                <button
                  type="submit"
                  disabled={busy}
                  className="flex-1 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-60 flex items-center justify-center gap-2"
                >
                  <Save className="w-4 h-4" />
                  {editingId ? "ویرایش آدرس" : "افزودن آدرس"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder,
  ltr,
  required = true,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  ltr?: boolean;
  required?: boolean;
}) {
  return (
    <label className="text-sm font-medium text-gray-700 block">
      {label}
      <input
        required={required}
        value={value}
        placeholder={placeholder}
        dir={ltr ? "ltr" : undefined}
        onChange={(event) => onChange(event.target.value)}
        className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB]"
      />
    </label>
  );
}
