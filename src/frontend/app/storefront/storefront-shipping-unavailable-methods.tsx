"use client";

/**
 * TB-165 — نمایش فقط-توضیحیِ روش‌های بدون نرخ معتبر.
 *
 * این بلوک هیچ دکمه/رادیویی/گزینهٔ کلیک‌شدنی ندارد (`aria-disabled`، بدون مبلغ)؛ فقط دلیل سرور را
 * روی همان روش توضیح می‌دهد. انتخاب روش همچنان تنها از `methods` واجد شرایط سرور می‌آید.
 */
import { AlertCircle } from "lucide-react";

import type { StorefrontShippingUnavailableNotice } from "./storefront-shipping-unavailable-methods.ts";

export function StorefrontShippingUnavailableMethods({
  notices,
  testIdPrefix = "shipping-unavailable",
}: {
  notices: readonly StorefrontShippingUnavailableNotice[];
  testIdPrefix?: string;
}) {
  if (notices.length === 0) return null;
  return (
    <div className="mt-3" data-testid={`${testIdPrefix}-methods`} data-shipping-unavailable-count={notices.length}>
      <p className="text-[11px] font-bold text-gray-700 mb-2 flex items-center gap-1.5">
        <AlertCircle className="w-3.5 h-3.5 text-amber-600" aria-hidden="true" />
        روش‌های ارسال بدون نرخ معتبر
      </p>
      <ul className="space-y-1.5" aria-label="روش‌های ارسال بدون نرخ معتبر" role="list">
        {notices.map((notice) => (
          <li
            key={notice.methodCode}
            role="listitem"
            aria-disabled="true"
            data-testid={`${testIdPrefix}-method-${notice.methodCode}`}
            data-reason-code={notice.reasonCode}
            data-shipping-unavailable="true"
            className="rounded-xl border border-dashed border-amber-200 bg-amber-50/60 p-2.5 text-right"
          >
            <p className="text-[11px] font-bold text-gray-700">
              {notice.label}
              <span className="mr-1 font-normal text-gray-400">({notice.methodCode})</span>
            </p>
            <p className="mt-0.5 text-[10px] text-amber-800 leading-5">{notice.reasonText}</p>
            <p className="mt-0.5 text-[10px] font-bold text-gray-500">غیرقابل انتخاب</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
