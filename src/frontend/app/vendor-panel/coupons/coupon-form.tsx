"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, Gift, Percent, Save, Tag } from "lucide-react";
import {
  createSellerPromotion,
  DEFAULT_SELLER_PARTY_ID,
  loadSellerPromotion,
  readSellerPartyId,
  updateSellerPromotion,
  type SellerPromotionRow,
  type UpsertSellerPromotionInput,
} from "../seller-api";

type FormState = {
  name: string;
  code: string;
  discount: string;
  type: "درصد" | "تومان";
  expires: string;
  minimumSubtotal: string;
};

const emptyForm: FormState = {
  name: "",
  code: "",
  discount: "",
  type: "درصد",
  expires: "",
  minimumSubtotal: "",
};

function fromRow(row: SellerPromotionRow): FormState {
  const isFixed = row.discountKind === "FixedAmountOff";
  return {
    name: row.name,
    code: row.couponCode ?? "",
    discount: isFixed
      ? String(row.fixedAmount)
      : String(Math.round(row.percentageRate * 100)),
    type: isFixed ? "تومان" : "درصد",
    expires: row.effectiveTo ? row.effectiveTo.slice(0, 10) : "",
    minimumSubtotal: row.minimumSubtotal != null ? String(row.minimumSubtotal) : "",
  };
}

function toInput(form: FormState): UpsertSellerPromotionInput {
  const discountValue = Number(form.discount.replace(/,/g, ""));
  const minSpend = form.minimumSubtotal.trim()
    ? Number(form.minimumSubtotal.replace(/,/g, ""))
    : null;
  return {
    name: form.name.trim() || form.code.trim(),
    couponCode: form.code.trim().toUpperCase(),
    discountKind: form.type === "تومان" ? "FixedAmountOff" : "PercentageOff",
    discountValue,
    effectiveFrom: new Date().toISOString(),
    effectiveTo: form.expires ? new Date(`${form.expires}T23:59:59Z`).toISOString() : null,
    currency: form.type === "تومان" ? "IRR" : null,
    minimumSubtotal: minSpend != null && Number.isFinite(minSpend) ? minSpend : null,
  };
}

/**
 * فرم ایجاد/ویرایش تخفیف فروشنده — الگوی بصری Shopeiva CouponForm.
 */
export function CouponForm({ promotionId }: { promotionId?: string }) {
  const router = useRouter();
  const sellerPartyId =
    typeof window !== "undefined"
      ? (readSellerPartyId(window.location.search) ?? DEFAULT_SELLER_PARTY_ID)
      : DEFAULT_SELLER_PARTY_ID;
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [bootLoading, setBootLoading] = useState(Boolean(promotionId));
  const isEdit = Boolean(promotionId);

  useEffect(() => {
    if (!promotionId) {
      return;
    }
    void loadSellerPromotion(sellerPartyId, promotionId).then((result) => {
      setBootLoading(false);
      if (result.detail) {
        setForm(fromRow(result.detail));
        return;
      }
      setError(result.message ?? "پروموشن پیدا نشد.");
    });
  }, [promotionId, sellerPartyId]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    if (form.code.trim().length < 3) {
      setError("کد تخفیف حداقل ۳ کاراکتر باید باشد.");
      return;
    }
    if (!form.discount.trim()) {
      setError("مقدار تخفیف را وارد کنید.");
      return;
    }
    setIsLoading(true);
    const input = toInput(form);
    const result = isEdit && promotionId
      ? await updateSellerPromotion(sellerPartyId, promotionId, input)
      : await createSellerPromotion(sellerPartyId, input);
    setIsLoading(false);
    if (!result.ok) {
      setError(result.errorCode);
      return;
    }
    router.push("/vendor-panel/coupons");
  }

  if (bootLoading) {
    return <p className="text-sm text-gray-500">در حال بارگذاری…</p>;
  }

  return (
    <div className="max-w-2xl mx-auto" data-testid="seller-coupon-form">
      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-lg">
        <div className="p-4 md:p-6 border-b border-gray-200 bg-gradient-to-r from-[#E53935]/5 to-transparent">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-[#E53935]/10 flex items-center justify-center">
              <Gift className="w-6 h-6 text-[#E53935]" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-900">
                {isEdit ? "ویرایش کد تخفیف" : "ایجاد کد تخفیف جدید"}
              </h2>
              <p className="text-sm text-gray-500 mt-0.5">اطلاعات کد تخفیف را وارد کنید</p>
            </div>
          </div>
        </div>

        <form onSubmit={(e) => void onSubmit(e)} className="p-4 md:p-6 space-y-5">
          <div>
            <label className="text-sm font-medium text-gray-700">
              نام نمایشی <span className="text-red-500">*</span>
            </label>
            <input
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              type="text"
              placeholder="مثلاً تخفیف تابستان"
              className="mt-1 w-full px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            />
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">
              کد تخفیف <span className="text-red-500">*</span>
            </label>
            <div className="relative mt-1">
              <Tag className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                value={form.code}
                onChange={(e) => setForm((f) => ({ ...f, code: e.target.value.toUpperCase() }))}
                type="text"
                placeholder="مثلاً SUMMER20"
                className="w-full pr-10 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935] font-mono uppercase tracking-wider"
              />
            </div>
            <p className="text-[10px] text-gray-400 mt-1">حداقل ۳ و حداکثر ۲۰ کاراکتر</p>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">تاریخ انقضا</label>
            <input
              type="date"
              value={form.expires}
              onChange={(e) => setForm((f) => ({ ...f, expires: e.target.value }))}
              className="mt-1 w-full px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium text-gray-700">
                مقدار تخفیف <span className="text-red-500">*</span>
              </label>
              <div className="relative mt-1">
                <Percent className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  value={form.discount}
                  onChange={(e) => setForm((f) => ({ ...f, discount: e.target.value }))}
                  type="text"
                  placeholder={form.type === "درصد" ? "مثلاً ۲۰" : "مثلاً ۵۰۰۰۰"}
                  className="w-full pr-10 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                />
              </div>
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">
                نوع تخفیف <span className="text-red-500">*</span>
              </label>
              <select
                value={form.type}
                onChange={(e) =>
                  setForm((f) => ({ ...f, type: e.target.value === "تومان" ? "تومان" : "درصد" }))
                }
                className="mt-1 w-full px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
              >
                <option value="درصد">درصد (%)</option>
                <option value="تومان">تومان (ریال)</option>
              </select>
            </div>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">حداقل مبلغ سبد (اختیاری)</label>
            <input
              value={form.minimumSubtotal}
              onChange={(e) => setForm((f) => ({ ...f, minimumSubtotal: e.target.value }))}
              type="text"
              placeholder="مثلاً ۱۰۰۰۰۰"
              className="mt-1 w-full px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            />
          </div>

          <div className="bg-amber-50 rounded-xl p-4 border border-amber-200 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-amber-700">نکته مهم</p>
              <p className="text-xs text-amber-600/80 mt-0.5">
                پس از فعال‌سازی، فیلدهای اقتصادی قابل ویرایش نیستند. سقف مصرف هنوز در این موج اعمال نمی‌شود.
              </p>
            </div>
          </div>

          {error ? <p className="text-sm text-red-600">{error}</p> : null}

          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={() => router.back()}
              className="px-6 py-2.5 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium hover:bg-gray-200 transition-colors"
            >
              بازگشت
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className={`flex-1 py-2.5 bg-[#E53935] text-white rounded-xl text-sm font-bold hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center justify-center gap-2 ${
                isLoading ? "opacity-70 cursor-not-allowed" : ""
              }`}
            >
              {isLoading ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <>
                  <Save className="w-4 h-4" />
                  {isEdit ? "ذخیره تغییرات" : "ایجاد کد تخفیف"}
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
