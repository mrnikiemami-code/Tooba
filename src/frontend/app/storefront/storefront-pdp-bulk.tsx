"use client";

import { useState, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle, Package, Send, Shield, Truck } from "lucide-react";
import { submitStorefrontBulkInquiry } from "./storefront-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";

const bulkSchema = z.object({
  fullName: z
    .string()
    .min(3, "نام حداقل ۳ کاراکتر")
    .max(50)
    .regex(/^[\u0600-\u06FF\uFB8A\u067E\u0686\u06AF\u200C\u200D\s]+$/, "فقط حروف فارسی"),
  email: z.string().email("ایمیل معتبر نیست").optional().or(z.literal("")),
  phone: z.string().regex(/^09[0-9]{9}$/, "موبایل ۱۱ رقمی معتبر نیست"),
  quantity: z.coerce.number().min(10, "حداقل ۱۰ عدد").max(1000, "حداکثر ۱۰۰۰ عدد"),
  companyName: z.string().max(100).optional().or(z.literal("")),
  address: z.string().min(10, "آدرس حداقل ۱۰ کاراکتر").max(200),
  notes: z.string().max(500).optional().or(z.literal("")),
  agreeTerms: z.boolean().refine((value) => value === true, "پذیرش قوانین الزامی است"),
});

type BulkForm = z.infer<typeof bulkSchema>;

/**
 * تب خرید عمده Shopeiva به‌صورت درخواست واقعی؛ بدون تخفیف/قیمت جعلی سمت کلاینت.
 */
export function StorefrontPdpBulk({ detail }: { detail: StorefrontProductDetailPage }) {
  const [busy, setBusy] = useState(false);
  const [doneId, setDoneId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
    reset,
  } = useForm<BulkForm>({
    resolver: zodResolver(bulkSchema),
    defaultValues: {
      fullName: "",
      email: "",
      phone: "",
      quantity: 10,
      companyName: "",
      address: "",
      notes: "",
      agreeTerms: false,
    },
  });
  const quantity = watch("quantity") || 10;

  if (doneId) {
    return (
      <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-6 text-center space-y-3" data-testid="pdp-bulk-success">
        <CheckCircle className="w-10 h-10 text-emerald-600 mx-auto" />
        <h3 className="text-lg font-bold text-emerald-800">درخواست عمده ثبت شد</h3>
        <p className="text-sm text-emerald-700">کد پیگیری داخلی: {doneId}</p>
        <p className="text-xs text-emerald-600">قیمت عمده پس از بررسی فروشگاه اعلام می‌شود؛ هیچ تخفیف نمایشی در UI محاسبه نشده است.</p>
        <button type="button" onClick={() => { setDoneId(null); reset(); }} className="text-sm font-bold text-[#2563EB]">
          ثبت درخواست جدید
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6" data-testid="pdp-bulk">
      <div>
        <h3 className="text-lg font-bold text-gray-900">خرید عمده</h3>
        <p className="text-sm text-gray-500 mt-1">
          برای «{detail.title}» درخواست عمده ثبت کنید. قیمت‌گذاری فقط پس از بررسی فروشگاه اعلام می‌شود.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        {[
          { icon: Package, title: "حداقل ۱۰ عدد", desc: "سفارش عمده از ۱۰ واحد" },
          { icon: Truck, title: "هماهنگی ارسال", desc: "پس از تأیید درخواست" },
          { icon: Shield, title: "بدون قیمت جعلی", desc: "تخفیف نمایشی محاسبه نمی‌شود" },
        ].map((item) => (
          <div key={item.title} className="flex items-center gap-2 p-3 bg-gray-50 rounded-xl border border-gray-200">
            <item.icon className="w-5 h-5 text-[#2563EB]" />
            <div>
              <p className="text-xs font-bold text-gray-700">{item.title}</p>
              <p className="text-[10px] text-gray-500">{item.desc}</p>
            </div>
          </div>
        ))}
      </div>

      <form
        className="space-y-4"
        onSubmit={handleSubmit((values) => {
          void (async () => {
            setBusy(true);
            setError(null);
            try {
              const id = await submitStorefrontBulkInquiry(detail.slug, {
                fullName: values.fullName,
                phone: values.phone,
                email: values.email || undefined,
                companyName: values.companyName || undefined,
                address: values.address,
                quantity: values.quantity,
                notes: values.notes || undefined,
              });
              setDoneId(id || "ثبت‌شده");
            } catch (cause) {
              setError(cause instanceof Error ? cause.message : "ثبت درخواست انجام نشد.");
            } finally {
              setBusy(false);
            }
          })();
        })}
      >
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Field label="نام و نام خانوادگی" error={errors.fullName?.message}>
            <input {...register("fullName")} className={inputClass} />
          </Field>
          <Field label="موبایل" error={errors.phone?.message}>
            <input {...register("phone")} dir="ltr" className={inputClass} placeholder="09xxxxxxxxx" />
          </Field>
          <Field label="ایمیل (اختیاری)" error={errors.email?.message}>
            <input {...register("email")} dir="ltr" className={inputClass} />
          </Field>
          <Field label="نام شرکت (اختیاری)" error={errors.companyName?.message}>
            <input {...register("companyName")} className={inputClass} />
          </Field>
          <Field label="تعداد" error={errors.quantity?.message}>
            <input type="number" {...register("quantity")} className={inputClass} />
          </Field>
          <div className="flex items-end text-xs text-gray-500 pb-2">
            تعداد درخواستی: {Number(quantity).toLocaleString("fa-IR")} — بدون محاسبهٔ قیمت در UI
          </div>
        </div>
        <Field label="آدرس" error={errors.address?.message}>
          <textarea {...register("address")} rows={2} className={inputClass} />
        </Field>
        <Field label="توضیحات (اختیاری)" error={errors.notes?.message}>
          <textarea {...register("notes")} rows={2} className={inputClass} />
        </Field>
        <label className="flex items-start gap-2 text-sm text-gray-600">
          <input type="checkbox" {...register("agreeTerms")} className="mt-1" />
          قوانین ثبت درخواست عمده را می‌پذیرم.
        </label>
        {errors.agreeTerms ? <p className="text-xs text-red-500">{errors.agreeTerms.message}</p> : null}
        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        <button
          type="submit"
          disabled={busy}
          className="w-full md:w-auto px-6 py-3 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-60 flex items-center justify-center gap-2"
        >
          <Send className="w-4 h-4" /> ثبت درخواست عمده
        </button>
      </form>
    </div>
  );
}

const inputClass =
  "w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB]";

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <div>
      <label className="text-sm font-medium text-gray-700">{label}</label>
      {children}
      {error ? <p className="text-xs text-red-500 mt-1">{error}</p> : null}
    </div>
  );
}
