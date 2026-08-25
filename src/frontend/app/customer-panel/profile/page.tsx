"use client";

import { Camera, CircleUserRound, LockKeyhole } from "lucide-react";
import { useEffect, useState } from "react";
import { type CustomerProfilePage, loadCustomerProfile } from "../customer-api";

/**
 * فرم پروفایل Shopeiva به‌صورت خواندنی؛ چون backend ویرایش پروفایل ندارد persistence جعلی ایجاد نمی‌شود.
 */
export default function CustomerProfile() {
  const [profile, setProfile] = useState<CustomerProfilePage | null | undefined>(undefined);
  useEffect(() => {
    void loadCustomerProfile().then(setProfile);
  }, []);

  if (profile === undefined) {
    return <div className="bg-white rounded-2xl border p-8 text-center text-gray-500">در حال دریافت پروفایل...</div>;
  }
  if (!profile) {
    return <div className="bg-white rounded-2xl border p-8 text-center text-red-600">پروفایل در دسترس نیست.</div>;
  }

  return (
    <section className="max-w-3xl mx-auto bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-sm">
      <div className="h-24 bg-gradient-to-l from-blue-50 to-white border-b border-gray-100 px-6 py-5">
        <h1 className="text-xl font-black">اطلاعات حساب کاربری</h1>
        <p className="text-xs text-gray-500 mt-1">اطلاعات خواندنی نشست و آخرین سفارش</p>
      </div>
      <div className="p-5 md:p-8">
        <div className="flex flex-col items-center -mt-16 mb-8">
          <div className="relative w-24 h-24 rounded-full bg-red-50 border-4 border-white shadow flex items-center justify-center">
            <CircleUserRound className="w-12 h-12 text-[#2563EB]" />
            <span className="absolute bottom-0 left-0 w-8 h-8 bg-[#2563EB] text-white rounded-full flex items-center justify-center">
              <Camera className="w-4 h-4" />
            </span>
          </div>
          <p className="text-[11px] text-gray-400 mt-2">تصویر پروفایل در backend فعلی ذخیره نمی‌شود.</p>
        </div>
        <div className="grid md:grid-cols-2 gap-4">
          <Field label="نام نمایشی" value={profile.displayName} />
          <Field label="شماره تماس" value={profile.contactMobile ?? "ثبت نشده"} />
          <div className="md:col-span-2">
            <Field label="آخرین نشانی ارسال" value={profile.lastShippingAddress ?? "هنوز نشانی در سفارش ثبت نشده است."} />
          </div>
          <div className="md:col-span-2">
            <Field label="شناسه کاربر" value={profile.actorUserId} ltr />
          </div>
        </div>
        <div className="mt-6 rounded-xl bg-amber-50 text-amber-800 p-4 flex gap-3 text-sm">
          <LockKeyhole className="w-5 h-5 shrink-0" />
          <p>ویرایش پروفایل تا ایجاد capability معتبر backend غیرفعال است؛ هیچ تغییر ظاهری به‌عنوان ذخیره‌شده نمایش داده نمی‌شود.</p>
        </div>
      </div>
    </section>
  );
}

function Field({ label, value, ltr = false }: { label: string; value: string; ltr?: boolean }) {
  return (
    <label className="block">
      <span className="text-xs font-bold text-gray-600">{label}</span>
      <input
        readOnly
        value={value}
        dir={ltr ? "ltr" : "rtl"}
        className="mt-2 w-full h-12 rounded-xl border border-gray-200 bg-gray-50 px-4 text-sm text-gray-700"
      />
    </label>
  );
}
