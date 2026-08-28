"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  ArrowLeft,
  Award,
  BarChart3,
  ChevronLeft,
  Clock,
  Eye,
  Hand,
  MessageSquare,
  Package,
  Search,
  Settings,
  ShoppingBag,
  Tag,
  Target,
  ThumbsUp,
  TrendingUp,
  Users,
  Wallet,
  Zap,
} from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../design-system";
import {
  formatMoney,
  formatPaymentState,
  loadSellerDashboard,
  loadSellerOffers,
  loadSellerOrders,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
  type SellerOfferListRow,
  type SellerOrderListRow,
} from "./seller-api";

function toFa(num: number | null | undefined): string {
  if (num == null || !Number.isFinite(num)) return "۰";
  return num.toLocaleString("fa-IR");
}

/**
 * داشبورد فروشنده — هندسهٔ Shopeiva؛ فقط دادهٔ Host.
 * بدون درصد فروش، درآمد، یا نمودار جعلی.
 */
export default function VendorDashboardPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [summary, setSummary] = useState<SellerDashboardSummary | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [offers, setOffers] = useState<SellerOfferListRow[]>([]);
  const [orders, setOrders] = useState<SellerOrderListRow[]>([]);
  const [period, setPeriod] = useState<"weekly" | "monthly" | "yearly">("weekly");

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void Promise.all([
      loadSellerDashboard(sellerPartyId),
      loadSellerOffers(sellerPartyId),
      loadSellerOrders(sellerPartyId),
    ]).then(([dash, offerResult, orderResult]) => {
      setSource(dash.source);
      setSummary(dash.summary);
      setMessage(dash.message);
      setDenied(Boolean(dash.denied));
      if (offerResult.source === "host") setOffers(offerResult.rows);
      if (orderResult.source === "host") setOrders(orderResult.rows);
    });
  }

  useEffect(() => {
    refresh();
  }, []);

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهدهٔ این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  const name = summary?.sellerDisplayName ?? "فروشنده";
  const loading = source === "loading";
  const orderCount = (summary?.openOrders ?? 0) + (summary?.paidOrders ?? 0);
  const productCount = summary?.activeOffers ?? 0;
  const chartLabels =
    period === "weekly"
      ? ["شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه"]
      : period === "monthly"
        ? ["هفته ۱", "هفته ۲", "هفته ۳", "هفته ۴"]
        : ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور"];

  const recentOrders = orders.slice(0, 5);
  const topOffers = offers.slice(0, 4);

  const quickActions = [
    { label: "افزودن محصول", icon: Package, href: "/vendor-panel/products/new", color: "bg-[#2563EB]" },
    { label: "مشاهده سفارشات", icon: ShoppingBag, href: "/vendor-panel/orders", color: "bg-blue-500" },
    { label: "آمار و نمودار", icon: BarChart3, href: "/vendor-panel/analytics", color: "bg-indigo-500" },
    { label: "کیف پول", icon: Wallet, href: "/vendor-panel/wallet", color: "bg-amber-500" },
    { label: "تخفیف جدید", icon: Tag, href: "/vendor-panel/coupons/new", color: "bg-rose-500" },
    { label: "تنظیمات", icon: Settings, href: "/vendor-panel/settings", color: "bg-purple-500" },
  ];

  const kpi = [
    {
      id: 1,
      label: "کل فروش",
      value: "—",
      icon: TrendingUp,
      color: "from-emerald-500 to-emerald-600",
      hint: "بدون متریک درآمد Host",
    },
    {
      id: 2,
      label: "تعداد سفارشات",
      value: loading ? "…" : toFa(orderCount),
      icon: ShoppingBag,
      color: "from-blue-500 to-blue-600",
      hint: null as string | null,
    },
    {
      id: 3,
      label: "محصولات",
      value: loading ? "…" : toFa(productCount),
      icon: Package,
      color: "from-purple-500 to-purple-600",
      hint: null,
    },
    {
      id: 4,
      label: "مشتریان",
      value: "۰",
      icon: Users,
      color: "from-amber-500 to-amber-600",
      hint: "بدون API مشتری فروشنده",
    },
  ];

  return (
    <main className="space-y-6" data-testid="seller-auth-allowed">
      {/* خوش‌آمد — هندسه Shopeiva */}
      <div className="relative bg-gradient-to-r from-[#2563EB]/10 via-white/50 to-[#2563EB]/5 rounded-2xl p-4 md:p-6 border border-[#2563EB]/20 overflow-hidden">
        <div className="absolute top-0 right-0 w-64 h-64 rounded-full bg-[#2563EB]/5 blur-3xl -translate-y-1/2 translate-x-1/3" />
        <div className="relative flex items-center justify-between flex-wrap gap-3">
          <div>
            <h1 className="text-xl md:text-2xl font-extrabold text-gray-900 flex items-center gap-2">
              <Hand className="w-6 h-6 text-[#2563EB]" />
              خوش آمدی، <span className="text-[#2563EB]">{name}</span>
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              به پنل فروشندگی خوش آمدید. از اینجا می‌توانید فروشگاه خود را مدیریت کنید.
            </p>
          </div>
          <div className="flex items-center gap-2 bg-white px-4 py-2 rounded-xl border border-gray-200 shadow-sm">
            <Award className="w-4 h-4 text-amber-500" />
            <span className="text-xs font-medium text-gray-700">فروشنده ویژه</span>
          </div>
        </div>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 md:gap-4">
          {kpi.map((stat) => {
            const Icon = stat.icon;
            return (
              <div
                key={stat.id}
                className="bg-white rounded-2xl p-4 border border-gray-200 hover:shadow-lg hover:border-[#2563EB]/30 transition-all duration-300 group"
              >
                <div className="flex items-center justify-between">
                  <div
                    className={`w-10 h-10 rounded-xl bg-gradient-to-br ${stat.color} flex items-center justify-center group-hover:scale-110 transition-transform`}
                  >
                    <Icon className="w-5 h-5 text-white" />
                  </div>
                  {/* بدون درصد ساختگی */}
                  <span className="text-xs font-medium text-gray-400">—</span>
                </div>
                <p className="text-xl font-black text-gray-900 mt-2 tabular-nums">{stat.value}</p>
                <p className="text-xs text-gray-500">{stat.label}</p>
                {stat.hint ? <p className="text-[10px] text-gray-400 mt-1">{stat.hint}</p> : null}
              </div>
            );
          })}
        </div>
      )}

      {/* نمودار + هدف — پوسته خالی بدون داده جعلی */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 bg-white rounded-2xl p-4 md:p-6 border border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h3 className="font-bold text-gray-900 flex items-center gap-2">
              <BarChart3 className="w-4 h-4 text-[#2563EB]" />
              روند فروش و بازدید
            </h3>
            <div className="flex items-center gap-1 bg-gray-100 p-1 rounded-lg">
              {(
                [
                  ["هفتگی", "weekly"],
                  ["ماهانه", "monthly"],
                  ["سالانه", "yearly"],
                ] as const
              ).map(([label, key]) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setPeriod(key)}
                  className={`px-3 py-1 rounded-lg text-[10px] font-medium transition-all ${
                    period === key ? "bg-[#2563EB] text-white" : "text-gray-600 hover:bg-white"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>
          <div
            className="w-full h-[180px] flex items-end justify-between gap-1 px-1 border border-dashed border-gray-200 rounded-xl bg-gray-50/50"
            data-testid="seller-dashboard-chart-empty"
          >
            {chartLabels.map((day) => (
              <div key={day} className="flex-1 flex flex-col items-center h-full justify-end">
                <div className="w-full flex justify-center gap-0.5 items-end flex-1">
                  <div className="w-2 sm:w-3 bg-gray-200 rounded-t" style={{ height: "4%" }} />
                  <div className="w-2 sm:w-3 bg-gray-300 rounded-t" style={{ height: "4%" }} />
                </div>
                <span className="text-[8px] sm:text-[10px] text-gray-400 whitespace-nowrap mt-1">{day}</span>
              </div>
            ))}
          </div>
          <p className="text-center text-[10px] text-gray-400 mt-2">سری زمانی فروش/بازدید در Host موجود نیست — پوسته خالی</p>
          <div className="flex items-center justify-center gap-4 mt-2 text-[10px] text-gray-500">
            <span className="flex items-center gap-1">
              <span className="w-3 h-3 rounded-full bg-[#2563EB]" />
              فروش
            </span>
            <span className="flex items-center gap-1">
              <span className="w-3 h-3 rounded-full bg-blue-400" />
              بازدید
            </span>
          </div>
        </div>

        <div className="bg-white rounded-2xl p-4 md:p-6 border border-gray-200">
          <h3 className="font-bold text-gray-900 mb-4 flex items-center gap-2">
            <Target className="w-4 h-4 text-[#2563EB]" />
            هدف و عملکرد
          </h3>
          <div className="space-y-4">
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-medium text-gray-600">هدف فروش ماهانه</span>
                <span className="text-xs font-bold text-gray-400">—</span>
              </div>
              <div className="w-full h-2.5 bg-gray-100 rounded-full overflow-hidden">
                <div className="h-full bg-gradient-to-l from-[#2563EB] to-[#60A5FA] rounded-full" style={{ width: "0%" }} />
              </div>
              <div className="flex items-center justify-between mt-2">
                <span className="text-[10px] text-gray-500">بدون متریک درآمد</span>
                <span className="text-[10px] text-gray-400">هدف: —</span>
              </div>
            </div>
            {[
              { label: "نرخ تبدیل", icon: TrendingUp, color: "text-emerald-500", bg: "bg-emerald-50" },
              { label: "رضایت مشتری", icon: ThumbsUp, color: "text-blue-500", bg: "bg-blue-50" },
              { label: "پاسخ‌گویی", icon: MessageSquare, color: "text-purple-500", bg: "bg-purple-50" },
            ].map((metric) => {
              const Icon = metric.icon;
              return (
                <div
                  key={metric.label}
                  className="flex items-center justify-between p-2.5 rounded-xl bg-gray-50 hover:bg-gray-100 transition-colors"
                >
                  <div className="flex items-center gap-2">
                    <div className={`w-8 h-8 rounded-lg ${metric.bg} flex items-center justify-center`}>
                      <Icon className={`w-4 h-4 ${metric.color}`} />
                    </div>
                    <span className="text-xs font-medium text-gray-700">{metric.label}</span>
                  </div>
                  <span className="text-sm font-bold text-gray-400">—</span>
                </div>
              );
            })}
            <div className="flex items-center gap-2 p-3 rounded-xl bg-gray-50 border border-gray-200">
              <div className="w-2 h-2 rounded-full bg-gray-400" />
              <span className="text-[11px] font-medium text-gray-600">وضعیت عملکرد تا اتصال متریک Host خالی است</span>
            </div>
          </div>
        </div>
      </div>

      {/* محصولات پرفروش / پربازدید */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="bg-white rounded-2xl p-4 border border-gray-200">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-bold text-gray-900 flex items-center gap-2">
              <TrendingUp className="w-4 h-4 text-emerald-500" />
              محصولات پرفروش
            </h3>
            <Link href="/vendor-panel/products" className="text-[10px] text-[#2563EB] hover:underline flex items-center gap-0.5">
              مشاهده همه
              <ChevronLeft className="w-3 h-3" />
            </Link>
          </div>
          <div className="space-y-2">
            {topOffers.length === 0 ? (
              <p className="text-xs text-gray-400 py-6 text-center">پیشنهاد فعالی برای نمایش نیست</p>
            ) : (
              topOffers.map((product) => (
                <Link
                  key={product.offerId}
                  href={`/vendor-panel/products/${product.offerId}`}
                  className="flex items-center gap-3 p-2 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
                >
                  <div className="w-10 h-10 bg-white rounded-lg overflow-hidden flex-shrink-0 flex items-center justify-center border border-gray-100">
                    <Package className="w-4 h-4 text-gray-400" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-medium text-gray-900 truncate">{product.productTitle}</p>
                    <div className="flex items-center gap-2 text-[10px] text-gray-500">
                      <span>موجودی {toFa(product.availableUnits)}</span>
                      <span>|</span>
                      <span className="font-bold text-[#2563EB]">{formatMoney(product.amount, product.currency)}</span>
                    </div>
                  </div>
                  <span className="text-[10px] text-gray-400">—</span>
                </Link>
              ))
            )}
          </div>
          <p className="text-[10px] text-gray-400 mt-2">رتبه‌بندی فروش در Host نیست — فهرست Offer زنده</p>
        </div>

        <div className="bg-white rounded-2xl p-4 border border-gray-200">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-bold text-gray-900 flex items-center gap-2">
              <Eye className="w-4 h-4 text-blue-500" />
              محصولات پربازدید
            </h3>
            <Link href="/vendor-panel/products" className="text-[10px] text-[#2563EB] hover:underline flex items-center gap-0.5">
              مشاهده همه
              <ChevronLeft className="w-3 h-3" />
            </Link>
          </div>
          <div className="py-8 text-center text-xs text-gray-400" data-testid="seller-dashboard-views-empty">
            متریک بازدید محصول در Host موجود نیست
          </div>
        </div>
      </div>

      {/* پرسرچ */}
      <div className="bg-white rounded-2xl p-4 border border-gray-200">
        <div className="flex items-center justify-between mb-3">
          <h3 className="font-bold text-gray-900 flex items-center gap-2">
            <Search className="w-4 h-4 text-purple-500" />
            پرسرچ‌ترین کلمات
          </h3>
          <span className="text-[10px] text-gray-400">آخرین ۷ روز</span>
        </div>
        <div className="py-6 text-center text-xs text-gray-400" data-testid="seller-dashboard-search-empty">
          دادهٔ جستجو در Host موجود نیست
        </div>
      </div>

      {/* سفارشات اخیر */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-bold text-gray-900 flex items-center gap-2">
            <Clock className="w-4 h-4 text-[#2563EB]" />
            سفارشات اخیر
          </h3>
          <Link href="/vendor-panel/orders" className="text-xs text-[#2563EB] hover:underline flex items-center gap-1">
            مشاهده همه
            <ArrowLeft className="w-3 h-3" />
          </Link>
        </div>
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-right p-3 text-xs font-medium text-gray-500 whitespace-nowrap">شناسه</th>
                  <th className="text-right p-3 text-xs font-medium text-gray-500 whitespace-nowrap">مشتری</th>
                  <th className="text-right p-3 text-xs font-medium text-gray-500 whitespace-nowrap">مبلغ</th>
                  <th className="text-right p-3 text-xs font-medium text-gray-500 whitespace-nowrap">وضعیت</th>
                  <th className="text-right p-3 text-xs font-medium text-gray-500 whitespace-nowrap">تاریخ</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {recentOrders.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="p-6 text-center text-xs text-gray-400">
                      سفارشی برای نمایش نیست
                    </td>
                  </tr>
                ) : (
                  recentOrders.map((order) => {
                    const statusLabel = formatPaymentState(order.paymentState || order.status);
                    const statusColors: Record<string, string> = {
                      پرداخت‌شده: "text-emerald-500 bg-emerald-50",
                      "در انتظار پرداخت": "text-amber-500 bg-amber-50",
                      "لغو شده": "text-red-500 bg-red-50",
                    };
                    return (
                      <tr key={order.sellerOrderId} className="hover:bg-gray-50 transition-colors">
                        <td className="p-3 font-medium text-gray-900">
                          <Link href={`/vendor-panel/orders/${order.sellerOrderId}`} className="hover:underline">
                            {order.orderNumber}
                          </Link>
                        </td>
                        <td className="p-3 text-gray-600">{order.recipientName || "—"}</td>
                        <td className="p-3 font-bold text-[#2563EB]">{formatMoney(order.payableAmount, order.currency)}</td>
                        <td className="p-3">
                          <span
                            className={`text-[10px] font-medium px-2 py-1 rounded-full ${
                              statusColors[statusLabel] || "bg-gray-100 text-gray-500"
                            }`}
                          >
                            {statusLabel}
                          </span>
                        </td>
                        <td className="p-3 text-gray-500">{order.submittedAt ? order.submittedAt.slice(0, 10) : "—"}</td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* اقدامات سریع */}
      <div>
        <h3 className="text-sm font-bold text-gray-900 mb-3 flex items-center gap-2">
          <Zap className="w-4 h-4 text-[#2563EB]" />
          اقدامات سریع
        </h3>
        <div className="grid grid-cols-3 md:grid-cols-6 gap-3">
          {quickActions.map((action) => {
            const Icon = action.icon;
            return (
              <Link
                key={action.label}
                href={action.href}
                className="bg-white rounded-2xl p-4 border border-gray-200 hover:shadow-lg hover:border-[#2563EB]/30 hover:-translate-y-1 transition-all duration-300 text-center group"
              >
                <div
                  className={`w-10 h-10 rounded-xl ${action.color} flex items-center justify-center mx-auto mb-2 group-hover:scale-110 transition-transform`}
                >
                  <Icon className="w-5 h-5 text-white" />
                </div>
                <p className="text-[10px] md:text-xs font-medium text-gray-700 truncate">{action.label}</p>
              </Link>
            );
          })}
        </div>
      </div>

      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
    </main>
  );
}
