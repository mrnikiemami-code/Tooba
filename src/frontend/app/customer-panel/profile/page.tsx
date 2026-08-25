"use client";

import Link from "next/link";
import { Camera, LockKeyhole, Save, User } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { type CustomerProfilePage, loadCustomerProfile } from "../customer-api";
import {
  customerProfileErrorMessage,
  profileFormToWrite,
  saveCustomerProfile,
} from "../customer-profile-api";

const profileSchema = z.object({
  name: z
    .string()
    .min(3, "نام حداقل ۳ کاراکتر باید باشد")
    .regex(/^[\u0600-\u06FF\uFB8A\u067E\u0686\u06AF\u200C\u200D\s]+$/, "فقط حروف فارسی مجاز است"),
  birthDate: z.string().optional(),
  bio: z.string().max(200, "حداکثر ۲۰۰ کاراکتر").optional(),
});

type ProfileFormValues = z.infer<typeof profileSchema>;

/**
 * فرم پروفایل Shopeiva با binding زنده؛ فیلدهای Identity فقط‌خواندنی و بدون persistence جعلی.
 */
export default function CustomerProfile() {
  const [profile, setProfile] = useState<CustomerProfilePage | null | undefined>(undefined);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { name: "", birthDate: "", bio: "" },
    mode: "onChange",
  });

  useEffect(() => {
    void loadCustomerProfile().then((loaded) => {
      setProfile(loaded);
      if (loaded) {
        reset({
          name: loaded.displayName,
          birthDate: loaded.birthDate ?? "",
          bio: loaded.bio ?? "",
        });
      }
    });
  }, [reset]);

  async function onSubmit(values: ProfileFormValues) {
    if (!profile?.editable) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const updated = await saveCustomerProfile(profileFormToWrite(values));
      setProfile(updated);
      reset({
        name: updated.displayName,
        birthDate: updated.birthDate ?? "",
        bio: updated.bio ?? "",
      });
      setSuccess("اطلاعات پروفایل با موفقیت ذخیره شد.");
    } catch (cause) {
      setError(customerProfileErrorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (profile === undefined) {
    return <div className="bg-white rounded-2xl border p-8 text-center text-gray-500">در حال دریافت پروفایل...</div>;
  }
  if (!profile) {
    return <div className="bg-white rounded-2xl border p-8 text-center text-red-600">پروفایل در دسترس نیست.</div>;
  }

  const bioLength = watch("bio")?.length ?? 0;

  return (
    <div className="max-w-2xl mx-auto">
      <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-sm">
        <div className="p-4 md:p-6 border-b border-gray-100 bg-gradient-to-r from-blue-50 to-transparent">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-[#2563EB]/10 flex items-center justify-center">
              <User className="w-5 h-5 text-[#2563EB]" />
            </div>
            <div>
              <h1 className="text-lg font-black">اطلاعات پروفایل</h1>
              <p className="text-sm text-gray-500 mt-0.5">اطلاعات شخصی خود را ویرایش کنید</p>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="p-4 md:p-6 space-y-5">
          <div className="flex flex-col items-center">
            <div className="relative">
              <div className="w-28 h-28 rounded-full bg-blue-50 border-4 border-white shadow-xl flex items-center justify-center">
                <User className="w-14 h-14 text-[#2563EB]" />
              </div>
              <span className="absolute bottom-0 right-0 p-2 bg-gray-300 rounded-full text-white cursor-not-allowed" title="آپلود آواتر هنوز پشتیبانی نمی‌شود">
                <Camera className="w-4 h-4" />
              </span>
            </div>
            <p className="text-[10px] text-gray-400 mt-2">آپلود تصویر پروفایل در backend فعلی پشتیبانی نمی‌شود.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="md:col-span-2">
              <label className="text-sm font-medium text-gray-700">
                نام و نام خانوادگی <span className="text-red-500">*</span>
              </label>
              <input
                {...register("name")}
                type="text"
                placeholder="نام خود را وارد کنید"
                className={`w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border ${
                  errors.name ? "border-red-500" : "border-gray-200"
                } focus:outline-none focus:ring-2 focus:ring-[#2563EB]`}
              />
              {errors.name ? <p className="text-xs text-red-500 mt-1">{errors.name.message}</p> : null}
            </div>

            <div>
              <label className="text-sm font-medium text-gray-700">تاریخ تولد</label>
              <input
                {...register("birthDate")}
                type="text"
                placeholder="مثال: 1403/06/04"
                className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB]"
              />
              {errors.birthDate ? <p className="text-xs text-red-500 mt-1">{errors.birthDate.message}</p> : null}
            </div>

            <div>
              <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                ایمیل
                <LockKeyhole className="w-3.5 h-3.5 text-gray-400" />
              </label>
              <input
                readOnly
                value={profile.email ?? "ثبت نشده — تغییر از مسیر امن Identity"}
                dir="ltr"
                className="w-full mt-1 px-4 py-2.5 bg-gray-100 rounded-xl text-sm border border-gray-200 text-gray-600"
              />
            </div>

            <div>
              <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                شماره موبایل
                <LockKeyhole className="w-3.5 h-3.5 text-gray-400" />
              </label>
              <input
                readOnly
                value={profile.contactMobile ?? "ثبت نشده — تغییر از مسیر امن Identity"}
                dir="ltr"
                className="w-full mt-1 px-4 py-2.5 bg-gray-100 rounded-xl text-sm border border-gray-200 text-gray-600"
              />
            </div>

            <div>
              <label className="text-sm font-medium text-gray-700 flex items-center gap-1">
                کد ملی
                <LockKeyhole className="w-3.5 h-3.5 text-gray-400" />
              </label>
              <input
                readOnly
                value="در backend فعلی پشتیبانی نمی‌شود"
                className="w-full mt-1 px-4 py-2.5 bg-gray-100 rounded-xl text-sm border border-gray-200 text-gray-600"
              />
            </div>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">آدرس</label>
            <div className="mt-1 rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-600">
              {profile.lastShippingAddress ?? "آدرس‌ها در دفترچهٔ «آدرس‌های من» مدیریت می‌شوند."}
              <div className="mt-2">
                <Link href="/customer-panel/addresses" className="text-xs font-bold text-[#2563EB]">
                  مدیریت آدرس‌ها
                </Link>
              </div>
            </div>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">بیوگرافی</label>
            <textarea
              {...register("bio")}
              rows={2}
              placeholder="کمی درباره خودتان بنویسید..."
              maxLength={200}
              className={`w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border ${
                errors.bio ? "border-red-500" : "border-gray-200"
              } focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none`}
            />
            <div className="flex justify-between mt-1">
              {errors.bio ? <p className="text-xs text-red-500">{errors.bio.message}</p> : <span />}
              <span className="text-[10px] text-gray-400">{bioLength}/۲۰۰</span>
            </div>
          </div>

          {error ? <p className="text-sm text-red-600">{error}</p> : null}
          {success ? <p className="text-sm text-emerald-700">{success}</p> : null}

          <div className="flex gap-3 pt-4 border-t border-gray-100">
            <button
              type="submit"
              disabled={busy || !profile.editable}
              className={`flex-1 py-3 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-blue-700 transition-all flex items-center justify-center gap-2 ${
                busy || !profile.editable ? "opacity-70 cursor-not-allowed" : ""
              }`}
            >
              {busy ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <>
                  <Save className="w-4 h-4" />
                  ذخیره اطلاعات
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
