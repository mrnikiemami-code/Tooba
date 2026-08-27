"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Calendar,
  CheckCircle,
  ChevronLeft,
  ChevronRight,
  Copy,
  Edit2,
  Filter,
  Percent,
  Plus,
  Power,
  Search,
  Tag,
  X,
  XCircle,
} from "lucide-react";
import {
  activateSellerPromotion,
  deactivateSellerPromotion,
  loadSellerPromotions,
  readSellerPartyId,
  DEFAULT_SELLER_PARTY_ID,
  type SellerPromotionRow,
} from "../seller-api";

const toPersianDigits = (num: number | string) => {
  const digits = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"];
  return String(num).replace(/\d/g, (d) => digits[Number(d)] ?? d);
};

function statusLabel(status: string): string {
  switch (status) {
    case "Active":
      return "فعال";
    case "Expired":
      return "منقضی";
    default:
      return "پیش‌نویس";
  }
}

function discountLabel(row: SellerPromotionRow): { value: string; type: string } {
  if (row.discountKind === "FixedAmountOff") {
    return {
      value: toPersianDigits(row.fixedAmount.toLocaleString("fa-IR")),
      type: "تومان",
    };
  }
  const percent = Math.round(row.percentageRate * 100);
  return { value: `${toPersianDigits(percent)}٪`, type: "درصد" };
}

function formatDate(iso: string | null): string {
  if (!iso) {
    return "بدون پایان";
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return "—";
  }
  return toPersianDigits(date.toLocaleDateString("fa-IR"));
}

/**
 * فهرست زندهٔ تخفیف‌های فروشنده — الگوی بصری Shopeiva CouponsList.
 */
export function CouponsList() {
  const sellerPartyId =
    typeof window !== "undefined"
      ? (readSellerPartyId(window.location.search) ?? DEFAULT_SELLER_PARTY_ID)
      : DEFAULT_SELLER_PARTY_ID;
  const [rows, setRows] = useState<SellerPromotionRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState("all");
  const [isFilterOpen, setIsFilterOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(0);
  const [busyId, setBusyId] = useState<string | null>(null);
  const itemsPerPage = 4;

  const refresh = useCallback(() => {
    setLoading(true);
    void loadSellerPromotions(sellerPartyId).then((result) => {
      setLoading(false);
      if (result.denied) {
        setError("دسترسی به این فروشنده مجاز نیست.");
        setRows([]);
        return;
      }
      if (result.source !== "host") {
        setError(result.message ?? "خواندن تخفیف‌ها ناموفق بود.");
        setRows([]);
        return;
      }
      setError(null);
      setRows(result.rows);
    });
  }, [sellerPartyId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const searchResults = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    if (!term) {
      return rows;
    }
    return rows.filter((row) => {
      const hay = `${row.couponCode ?? ""} ${row.name} ${statusLabel(row.status)} ${row.discountKind}`.toLowerCase();
      return hay.includes(term);
    });
  }, [rows, searchTerm]);

  const filteredCoupons = searchResults.filter((row) => {
    if (filterStatus === "all") {
      return true;
    }
    return statusLabel(row.status) === filterStatus;
  });

  const pageCount = Math.max(1, Math.ceil(filteredCoupons.length / itemsPerPage));
  const offset = currentPage * itemsPerPage;
  const currentItems = filteredCoupons.slice(offset, offset + itemsPerPage);

  const stats = {
    total: rows.length,
    active: rows.filter((c) => c.status === "Active").length,
    expired: rows.filter((c) => c.status === "Expired").length,
  };

  async function onActivate(id: string) {
    setBusyId(id);
    const result = await activateSellerPromotion(sellerPartyId, id);
    setBusyId(null);
    if (result.ok) {
      refresh();
    } else {
      setError(result.errorCode);
    }
  }

  async function onDeactivate(id: string) {
    if (!confirm("آیا از غیرفعال‌سازی این کد تخفیف مطمئن هستید؟")) {
      return;
    }
    setBusyId(id);
    const result = await deactivateSellerPromotion(sellerPartyId, id);
    setBusyId(null);
    if (result.ok) {
      refresh();
    } else {
      setError(result.errorCode);
    }
  }

  function handleCopy(code: string) {
    void navigator.clipboard.writeText(code);
  }

  return (
    <div className="space-y-4" data-testid="seller-coupons-live">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-[#E53935]/10 flex items-center justify-center">
            <Tag className="w-5 h-5 text-[#E53935]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">تخفیف‌ها</h2>
            <p className="text-xs text-gray-500">
              {toPersianDigits(stats.total)} کد · {toPersianDigits(stats.active)} فعال ·{" "}
              {toPersianDigits(stats.expired)} منقضی
            </p>
          </div>
        </div>
        <Link
          href="/vendor-panel/coupons/new"
          className="px-4 py-2 bg-[#E53935] text-white rounded-xl text-xs font-bold hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center gap-1"
        >
          <Plus className="w-4 h-4" />
          تخفیف جدید
        </Link>
      </div>

      <div className="grid grid-cols-3 gap-2">
        <div className="bg-white rounded-xl p-3 text-center border border-gray-200">
          <p className="text-lg font-black text-gray-900">{toPersianDigits(stats.total)}</p>
          <p className="text-[10px] text-gray-500">کل کدها</p>
        </div>
        <div className="bg-white rounded-xl p-3 text-center border border-gray-200">
          <p className="text-lg font-black text-emerald-500">{toPersianDigits(stats.active)}</p>
          <p className="text-[10px] text-gray-500">فعال</p>
        </div>
        <div className="bg-white rounded-xl p-3 text-center border border-gray-200">
          <p className="text-lg font-black text-red-500">{toPersianDigits(stats.expired)}</p>
          <p className="text-[10px] text-gray-500">منقضی</p>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setCurrentPage(0);
            }}
            placeholder="جستجو در کدهای تخفیف..."
            className="w-full pr-10 px-4 py-2.5 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
          />
          {searchTerm ? (
            <button
              type="button"
              onClick={() => {
                setSearchTerm("");
                setCurrentPage(0);
              }}
              className="absolute left-3 top-1/2 -translate-y-1/2 p-1 rounded-full hover:bg-gray-200 transition-colors"
            >
              <X className="w-4 h-4 text-gray-400" />
            </button>
          ) : null}
        </div>
        <div className="relative">
          <button
            type="button"
            onClick={() => setIsFilterOpen(!isFilterOpen)}
            className={`px-4 py-2.5 rounded-xl text-sm font-medium transition-all flex items-center gap-2 ${
              filterStatus !== "all"
                ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                : "bg-white text-gray-700 border border-gray-200 hover:border-[#E53935]/50"
            }`}
          >
            <Filter className="w-4 h-4" />
            {filterStatus !== "all" ? filterStatus : "فیلتر"}
          </button>
          {isFilterOpen ? (
            <div className="absolute top-full left-0 mt-1 bg-white rounded-xl border border-gray-200 shadow-lg z-10 min-w-[140px]">
              {["all", "فعال", "منقضی", "پیش‌نویس"].map((status) => (
                <button
                  key={status}
                  type="button"
                  onClick={() => {
                    setFilterStatus(status);
                    setIsFilterOpen(false);
                    setCurrentPage(0);
                  }}
                  className={`block w-full text-right px-4 py-2 text-sm hover:bg-gray-100 transition-colors ${
                    filterStatus === status ? "text-[#E53935] font-bold" : "text-gray-700"
                  }`}
                >
                  {status === "all" ? "همه" : status}
                </button>
              ))}
            </div>
          ) : null}
        </div>
      </div>

      {error ? <p className="text-sm text-red-600">{error}</p> : null}
      {loading ? <p className="text-sm text-gray-500">در حال بارگذاری…</p> : null}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {!loading && currentItems.length === 0 ? (
          <div className="col-span-2 bg-white rounded-2xl p-8 text-center border border-gray-200">
            <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-3">
              <Tag className="w-8 h-8 text-gray-300" />
            </div>
            <p className="text-sm text-gray-500">هیچ کد تخفیفی یافت نشد</p>
          </div>
        ) : (
          currentItems.map((coupon) => {
            const isActive = coupon.status === "Active";
            const label = statusLabel(coupon.status);
            const discount = discountLabel(coupon);
            const editable = coupon.status !== "Active";
            return (
              <div
                key={coupon.promotionId}
                className={`bg-white rounded-2xl p-4 border-2 transition-all duration-300 hover:shadow-xl ${
                  isActive ? "border-[#E53935] shadow-[#E53935]/10" : "border-gray-200 opacity-70"
                }`}
                data-testid={`seller-coupon-card-${coupon.promotionId}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div
                      className={`w-12 h-12 rounded-xl ${isActive ? "bg-[#E53935]/10" : "bg-gray-100"} flex items-center justify-center`}
                    >
                      <Percent className={`w-6 h-6 ${isActive ? "text-[#E53935]" : "text-gray-400"}`} />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="font-bold text-gray-900 font-mono" dir="ltr">
                          {coupon.couponCode ?? "—"}
                        </p>
                        {coupon.couponCode ? (
                          <button
                            type="button"
                            onClick={() => handleCopy(coupon.couponCode!)}
                            className="p-1 rounded hover:bg-gray-100 transition-colors"
                            title="کپی"
                          >
                            <Copy className="w-3.5 h-3.5 text-gray-400 hover:text-[#E53935] transition-colors" />
                          </button>
                        ) : null}
                      </div>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-sm font-bold text-[#E53935]">{discount.value}</span>
                        <span className="text-[10px] text-gray-400">|</span>
                        <span className="text-[10px] text-gray-500">{discount.type}</span>
                      </div>
                      <p className="text-[10px] text-gray-400 mt-0.5">{coupon.name}</p>
                    </div>
                  </div>
                  <div className="text-left">
                    <span
                      className={`text-[10px] font-medium px-2 py-0.5 rounded-full ${
                        isActive
                          ? "bg-emerald-100 text-emerald-600"
                          : coupon.status === "Draft"
                            ? "bg-amber-100 text-amber-700"
                            : "bg-red-100 text-red-600"
                      }`}
                    >
                      {label}
                    </span>
                    <div className="flex items-center gap-1 mt-1 text-[10px] text-gray-400">
                      <Calendar className="w-3 h-3" />
                      {formatDate(coupon.effectiveTo)}
                    </div>
                  </div>
                </div>
                <div className="flex items-center justify-between mt-3 pt-3 border-t border-gray-200">
                  <div className="flex items-center gap-1 text-[10px] text-gray-400">
                    {isActive ? (
                      <CheckCircle className="w-3.5 h-3.5 text-emerald-500" />
                    ) : (
                      <XCircle className="w-3.5 h-3.5 text-gray-400" />
                    )}
                    بدون آمار مصرف جعلی
                  </div>
                  <div className="flex items-center gap-1">
                    {editable ? (
                      <Link
                        href={`/vendor-panel/coupons/${coupon.promotionId}/edit`}
                        className="p-1.5 rounded hover:bg-gray-100 transition-colors text-gray-400 hover:text-blue-500"
                        title="ویرایش"
                      >
                        <Edit2 className="w-3.5 h-3.5" />
                      </Link>
                    ) : null}
                    {coupon.status === "Draft" || coupon.status === "Expired" ? (
                      <button
                        type="button"
                        disabled={busyId === coupon.promotionId}
                        onClick={() => void onActivate(coupon.promotionId)}
                        className="p-1.5 rounded hover:bg-emerald-50 transition-colors text-gray-400 hover:text-emerald-600"
                        title="فعال‌سازی"
                      >
                        <Power className="w-3.5 h-3.5" />
                      </button>
                    ) : null}
                    {isActive ? (
                      <button
                        type="button"
                        disabled={busyId === coupon.promotionId}
                        onClick={() => void onDeactivate(coupon.promotionId)}
                        className="p-1.5 rounded hover:bg-red-50 transition-colors text-gray-400 hover:text-red-500"
                        title="غیرفعال"
                      >
                        <XCircle className="w-3.5 h-3.5" />
                      </button>
                    ) : null}
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      {filteredCoupons.length > itemsPerPage ? (
        <div className="flex flex-col items-center gap-3">
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              disabled={currentPage <= 0}
              onClick={() => setCurrentPage((p) => Math.max(0, p - 1))}
              className="flex items-center justify-center w-8 h-8 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-50"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
            {Array.from({ length: pageCount }, (_, i) => (
              <button
                key={i}
                type="button"
                onClick={() => setCurrentPage(i)}
                className={`flex items-center justify-center w-8 h-8 rounded-lg text-sm font-medium transition-colors ${
                  currentPage === i
                    ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                    : "text-gray-700 hover:bg-gray-100"
                }`}
              >
                {toPersianDigits(i + 1)}
              </button>
            ))}
            <button
              type="button"
              disabled={currentPage >= pageCount - 1}
              onClick={() => setCurrentPage((p) => Math.min(pageCount - 1, p + 1))}
              className="flex items-center justify-center w-8 h-8 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-50"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
