"use client";

import Link from "next/link";
import { useState } from "react";
import {
  CheckCircle2,
  ChevronLeft,
  Clock,
  Headphones,
  Mail,
  MapPin,
  Phone,
  RefreshCw,
  Shield,
  Truck,
} from "lucide-react";
import type { StorefrontCategoryItem } from "./storefront-model.ts";

/**
 * فوتر Shopeiva با نوار اعتماد، خبرنامهٔ نمایشی و ستون‌های قالب. عضویت خبرنامه جهش سرور نیست.
 */
export function StorefrontShopeivaFooter({ categories }: { categories: StorefrontCategoryItem[] }) {
  const [email, setEmail] = useState("");
  const [subscribed, setSubscribed] = useState(false);

  const features = [
    { icon: Truck, title: "ارسال سریع", desc: "به سراسر کشور" },
    { icon: Shield, title: "ضمانت اصالت", desc: "تضمین کیفیت کالا" },
    { icon: RefreshCw, title: "۷ روز بازگشت", desc: "بازگشت بدون قید و شرط" },
    { icon: Headphones, title: "پشتیبانی ۲۴/۷", desc: "همیشه در کنار شما" },
  ];

  return (
    <footer className="relative mt-10 bg-white border-t border-gray-200">
      <div className="border-b border-gray-200 bg-gray-50/50">
        <div className="max-w-[1800px] mx-auto px-3 sm:px-4 py-4 md:py-5">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 md:gap-4">
            {features.map((item) => (
              <div key={item.title} className="flex items-center justify-center gap-2 md:gap-3 group">
                <div className="w-10 h-10 md:w-12 md:h-12 rounded-xl bg-[#2563EB]/10 flex items-center justify-center group-hover:bg-[#2563EB] transition-all shrink-0">
                  <item.icon className="w-4 h-4 md:w-5 md:h-5 text-[#2563EB] group-hover:text-white transition" />
                </div>
                <div>
                  <h4 className="text-xs md:text-sm font-bold text-gray-900">{item.title}</h4>
                  <p className="text-[10px] md:text-xs text-gray-500 mt-0.5">{item.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-l from-[#2563EB] to-[#1d4ed8]" />
        <div className="relative max-w-[1800px] mx-auto px-3 sm:px-4 py-8 md:py-10">
          <div className="flex flex-col md:flex-row items-center justify-between gap-4 md:gap-6">
            <div className="text-white text-center md:text-right">
              <div className="flex items-center gap-2 justify-center md:justify-start mb-1 md:mb-2">
                <Mail className="w-5 h-5 md:w-6 md:h-6" />
                <h3 className="text-lg md:text-2xl font-bold">عضویت در خبرنامه</h3>
              </div>
              <p className="text-xs md:text-sm text-white/90 max-w-md">از پیشنهادهای فروشگاهی و تازه‌های ویترین باخبر شوید</p>
            </div>
            <form
              className="w-full md:w-auto flex flex-col sm:flex-row gap-2 max-w-md"
              onSubmit={(event) => {
                event.preventDefault();
                if (email.trim()) {
                  setSubscribed(true);
                }
              }}
            >
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="ایمیل خود را وارد کنید..."
                className="w-full px-4 py-2.5 bg-white/95 text-gray-900 rounded-xl text-sm"
              />
              <button type="submit" className="px-5 py-2.5 bg-zinc-900 text-white rounded-xl text-sm font-bold min-w-[100px]">
                {subscribed ? (
                  <span className="inline-flex items-center gap-1">
                    <CheckCircle2 className="w-4 h-4" /> عضو شدید
                  </span>
                ) : (
                  "عضویت"
                )}
              </button>
            </form>
          </div>
        </div>
      </div>

      <div className="max-w-[1800px] mx-auto px-3 sm:px-4 py-10 md:py-12">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 md:gap-5 mb-8">
          <div className="bg-gray-50 rounded-2xl p-4 md:p-5 border border-gray-200 min-h-[220px]">
            <h4 className="font-bold text-gray-900 mb-3 text-sm md:text-base flex items-center gap-2">
              <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
              دسته‌بندی‌ها
            </h4>
            <div className="space-y-2">
              {categories.slice(0, 6).map((category) => (
                <Link
                  key={category.categoryId}
                  href={`/products?categoryId=${category.categoryId}`}
                  className="flex items-center gap-2 text-sm text-gray-700 hover:text-[#2563EB]"
                >
                  {category.name}
                </Link>
              ))}
            </div>
            <Link href="/products" className="inline-flex items-center gap-1 text-xs text-[#2563EB] font-bold mt-3 pt-2 border-t border-gray-200 w-full">
              مشاهده همه
              <ChevronLeft className="w-3 h-3" />
            </Link>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4 md:p-5 border border-gray-200 min-h-[220px]">
            <h4 className="font-bold text-gray-900 mb-3 text-sm md:text-base flex items-center gap-2">
              <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
              خدمات مشتریان
            </h4>
            <ul className="space-y-2.5 text-sm text-gray-600">
              <li>رویه ارسال سفارش</li>
              <li>رویه بازگشت کالا</li>
              <li>شیوه‌های پرداخت</li>
              <li>پیگیری سفارش</li>
            </ul>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4 md:p-5 border border-gray-200 min-h-[220px]">
            <h4 className="font-bold text-gray-900 mb-3 text-sm md:text-base flex items-center gap-2">
              <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
              دسترسی سریع
            </h4>
            <ul className="space-y-2.5 text-sm text-gray-600">
              <li>درباره ما</li>
              <li>تماس با ما</li>
              <li>سؤالات متداول</li>
              <li>قوانین و مقررات</li>
            </ul>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4 md:p-5 border border-gray-200 min-h-[220px]">
            <h4 className="font-bold text-gray-900 mb-3 text-sm md:text-base flex items-center gap-2">
              <span className="w-1 h-5 bg-[#2563EB] rounded-full" />
              تماس با ما
            </h4>
            <ul className="space-y-3 text-sm text-gray-600">
              <li className="flex gap-2">
                <MapPin className="w-4 h-4 text-[#2563EB] shrink-0" />
                تهران، خیابان ولیعصر
              </li>
              <li className="flex gap-2">
                <Phone className="w-4 h-4 text-[#2563EB] shrink-0" />
                ۰۲۱-۹۱۰۰۰۰۰۰
              </li>
              <li className="flex gap-2">
                <Clock className="w-4 h-4 text-[#2563EB] shrink-0" />
                شنبه تا پنج‌شنبه ۹ الی ۱۸
              </li>
            </ul>
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-4 border-t border-gray-200 pt-6">
          <div className="flex gap-3">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/badges/enamad.png" alt="نماد اعتماد" className="h-12 w-auto bg-white rounded-lg border p-1" />
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/badges/namad-01.png" alt="ساماندهی" className="h-12 w-auto bg-white rounded-lg border p-1" />
          </div>
          <p className="text-xs text-gray-500">ویترین Tooba روی قالب خریداری‌شدهٔ Shopeiva · دادهٔ زنده Catalog/Offer</p>
        </div>
      </div>
    </footer>
  );
}
