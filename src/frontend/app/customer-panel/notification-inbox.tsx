"use client";

/**
 * اینباکس اعلان — قفل بصری Shopeiva notifications.jsx با بایندینگ Host واقعی.
 * بازخورد پس از read/delete/mark-all: react-toastify مطابق منبع شاپیوا (نه inline flash).
 */

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import {
  Bell,
  Calendar,
  Check,
  CheckCircle,
  ChevronDown,
  Filter,
  Package,
  RotateCcw,
  ShoppingBag,
  Truck,
  Wallet,
  X,
  XCircle,
  type LucideIcon,
} from "lucide-react";
import {
  dismissNotification,
  loadNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type NotificationItem,
  type NotificationRecipient,
} from "./notification-api";

type VisualItem = NotificationItem & {
  read: boolean;
  date: string;
  time: string;
  desc: string;
  icon: LucideIcon;
  color: string;
  bgColor: string;
  borderColor: string;
};

const filterOptions = [
  { value: "all", label: "همه" },
  { value: "unread", label: "خوانده نشده" },
  { value: "read", label: "خوانده شده" },
  { value: "order", label: "سفارشات" },
  { value: "offer", label: "تخفیف‌ها" },
  { value: "ticket", label: "تیکت‌ها" },
] as const;

function toPersianDigits(num: number | string): string {
  if (num !== 0 && !num) return "۰";
  const digits = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"];
  return String(num).replace(/\d/g, (d) => digits[parseInt(d, 10)]!);
}

function visualFor(item: NotificationItem): VisualItem {
  const type = item.type || item.category || "order";
  let icon: LucideIcon = Bell;
  let color = "text-blue-500";
  let bgColor = "bg-blue-50 dark:bg-blue-900/20";
  let borderColor = "border-blue-500/30";

  if (type === "payment.succeeded" || (type.includes("succeeded") && type.includes("payment"))) {
    icon = CheckCircle;
    color = "text-emerald-500";
    bgColor = "bg-emerald-50 dark:bg-emerald-900/20";
    borderColor = "border-emerald-500/30";
  } else if (type === "payment.failed" || (type.includes("payment") && type.includes("failed"))) {
    icon = XCircle;
    color = "text-red-500";
    bgColor = "bg-red-50 dark:bg-red-900/20";
    borderColor = "border-red-500/30";
  } else if (
    type.includes("wallet") ||
    type === "wallet.payment.succeeded" ||
    type === "wallet.refund.credited" ||
    type === "wallet.gift_card.redeemed" ||
    type === "wallet.admin_adjustment"
  ) {
    icon = Wallet;
    color = "text-violet-500";
    bgColor = "bg-violet-50 dark:bg-violet-900/20";
    borderColor = "border-violet-500/30";
  } else if (type === "shipment.dispatched" || type.includes("shipment")) {
    icon = Truck;
    color = "text-blue-500";
    bgColor = "bg-blue-50 dark:bg-blue-900/20";
    borderColor = "border-blue-500/30";
  } else if (type.includes("fulfillment")) {
    icon = Package;
    color = "text-blue-500";
    bgColor = "bg-blue-50 dark:bg-blue-900/20";
    borderColor = "border-blue-500/30";
  } else if (type.includes("return") || type.includes("refund")) {
    icon = RotateCcw;
    color = "text-amber-500";
    bgColor = "bg-amber-50 dark:bg-amber-900/20";
    borderColor = "border-amber-500/30";
  } else if (type.includes("order") || type.includes("paid")) {
    icon = ShoppingBag;
    color = "text-emerald-500";
    bgColor = "bg-emerald-50 dark:bg-emerald-900/20";
    borderColor = "border-emerald-500/30";
  }

  return {
    ...item,
    read: item.isRead,
    date: item.displayDate,
    time: item.displayTime,
    desc: item.body,
    icon,
    color,
    bgColor,
    borderColor,
  };
}

export function NotificationInbox({
  kind,
  emptyHint,
}: {
  kind: NotificationRecipient;
  emptyHint?: string;
}) {
  const [items, setItems] = useState<VisualItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<(typeof filterOptions)[number]["value"]>("all");
  const [showFilters, setShowFilters] = useState(false);

  const reload = useCallback(async () => {
    try {
      const rows = await loadNotifications(kind);
      setItems(rows.map(visualFor));
      setError(null);
    } catch {
      setItems([]);
      setError("بارگذاری اعلان‌ها از Host ناموفق بود.");
    }
  }, [kind]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const unreadCount = useMemo(() => (items ?? []).filter((n) => !n.read).length, [items]);

  const handleMarkAsRead = async (id: string) => {
    try {
      await markNotificationRead(kind, id);
      setItems((prev) => (prev ?? []).map((n) => (n.id === id ? { ...n, read: true, isRead: true } : n)));
      toast.success("به عنوان خوانده شد علامت‌گذاری شد");
    } catch {
      toast.error("علامت‌گذاری خوانده‌شدن ناموفق بود");
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await dismissNotification(kind, id);
      setItems((prev) => (prev ?? []).filter((n) => n.id !== id));
      toast.info("اطلاعیه حذف شد");
    } catch {
      toast.error("حذف اطلاعیه ناموفق بود");
    }
  };

  const handleMarkAllRead = async () => {
    if (unreadCount === 0) {
      toast.info("همه اطلاعیه‌ها قبلاً خوانده شده‌اند");
      return;
    }
    try {
      await markAllNotificationsRead(kind);
      setItems((prev) => (prev ?? []).map((n) => ({ ...n, read: true, isRead: true })));
      toast.success("همه اطلاعیه‌ها خوانده شدند");
    } catch {
      toast.error("خواندن همه ناموفق بود");
    }
  };

  const filteredItems = useMemo(() => {
    const list = items ?? [];
    return list.filter((item) => {
      if (filter === "all") return true;
      if (filter === "unread") return !item.read;
      if (filter === "read") return item.read;
      return item.category === filter || item.type === filter;
    });
  }, [filter, items]);

  const stats = useMemo(() => {
    const list = items ?? [];
    return {
      total: list.length,
      unread: list.filter((n) => !n.read).length,
      orders: list.filter(
        (n) =>
          n.category === "order" ||
          n.type.includes("order") ||
          n.type.includes("payment") ||
          n.type.includes("wallet") ||
          n.type.includes("shipment") ||
          n.type.includes("fulfillment") ||
          n.type.includes("return") ||
          n.type.includes("refund"),
      ).length,
      offers: list.filter((n) => n.category === "offer" || n.type === "offer").length,
    };
  }, [items]);

  if (items === null) {
    return (
      <div className="space-y-4" data-testid={`notifications-inbox-${kind}-loading`}>
        <div className="bg-white dark:bg-[#111] rounded-2xl p-10 text-center border border-gray-200 dark:border-gray-800">
          <p className="text-sm text-gray-500">در حال بارگذاری اعلان‌ها…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4" data-testid={`notifications-inbox-${kind}`}>
      {error ? (
        <div
          className="rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-xs font-medium px-3 py-2"
          role="alert"
        >
          {error}
        </div>
      ) : null}

      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-[#E53935]/10 flex items-center justify-center">
            <Bell className="w-5 h-5 text-[#E53935]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900 dark:text-white">اطلاعیه‌ها</h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {toPersianDigits(stats.total)} اطلاعیه · {toPersianDigits(stats.unread)} خوانده نشده
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {unreadCount > 0 && (
            <button
              type="button"
              onClick={() => void handleMarkAllRead()}
              className="px-3 py-1.5 bg-[#E53935] text-white rounded-xl text-xs font-medium hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center gap-1"
              data-testid="notifications-mark-all-read"
            >
              <Check className="w-3.5 h-3.5" />
              همه خوانده شد
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-4 gap-2">
        <div className="bg-white dark:bg-[#111] rounded-xl p-3 text-center border border-gray-200 dark:border-gray-800">
          <p className="text-lg font-black text-gray-900 dark:text-white">{toPersianDigits(stats.total)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">کل</p>
        </div>
        <div className="bg-white dark:bg-[#111] rounded-xl p-3 text-center border border-gray-200 dark:border-gray-800">
          <p className="text-lg font-black text-[#E53935]">{toPersianDigits(stats.unread)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">خوانده نشده</p>
        </div>
        <div className="bg-white dark:bg-[#111] rounded-xl p-3 text-center border border-gray-200 dark:border-gray-800">
          <p className="text-lg font-black text-blue-500">{toPersianDigits(stats.orders)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">سفارشات</p>
        </div>
        <div className="bg-white dark:bg-[#111] rounded-xl p-3 text-center border border-gray-200 dark:border-gray-800">
          <p className="text-lg font-black text-amber-500">{toPersianDigits(stats.offers)}</p>
          <p className="text-[10px] text-gray-500 dark:text-gray-400">تخفیف‌ها</p>
        </div>
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => setShowFilters(!showFilters)}
          className="px-3 py-1.5 bg-white dark:bg-[#111] rounded-xl text-xs font-medium text-gray-700 dark:text-gray-300 border border-gray-200 dark:border-gray-700 hover:border-[#E53935]/50 transition-colors flex items-center gap-1"
        >
          <Filter className="w-3.5 h-3.5" />
          فیلتر
          <ChevronDown className={`w-3.5 h-3.5 transition-transform ${showFilters ? "rotate-180" : ""}`} />
        </button>
        {filter !== "all" && (
          <button
            type="button"
            onClick={() => setFilter("all")}
            className="px-3 py-1.5 bg-[#E53935] text-white rounded-xl text-xs font-medium flex items-center gap-1"
          >
            {filterOptions.find((f) => f.value === filter)?.label}
            <X className="w-3 h-3" />
          </button>
        )}
      </div>

      {showFilters && (
        <div className="flex flex-wrap gap-1.5 p-3 bg-white dark:bg-[#111] rounded-xl border border-gray-200 dark:border-gray-700">
          {filterOptions.map((option) => (
            <button
              key={option.value}
              type="button"
              onClick={() => {
                setFilter(option.value);
                setShowFilters(false);
              }}
              className={`px-3 py-1 rounded-lg text-xs font-medium transition-all ${
                filter === option.value
                  ? "bg-[#E53935] text-white"
                  : "bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700"
              }`}
            >
              {option.label}
              {option.value !== "all" && (
                <span className="mr-1 text-[9px] opacity-70">
                  (
                  {toPersianDigits(
                    (items ?? []).filter((n) => {
                      if (option.value === "unread") return !n.read;
                      if (option.value === "read") return n.read;
                      return n.category === option.value || n.type === option.value;
                    }).length,
                  )}
                  )
                </span>
              )}
            </button>
          ))}
        </div>
      )}

      <div className="space-y-2">
        {filteredItems.length === 0 ? (
          <div className="bg-white dark:bg-[#111] rounded-2xl p-10 text-center border border-gray-200 dark:border-gray-800">
            <div className="w-16 h-16 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center mx-auto mb-3">
              <Bell className="w-8 h-8 text-gray-300 dark:text-gray-600" />
            </div>
            <p className="text-sm text-gray-500 dark:text-gray-400">هیچ اطلاعیه‌ای وجود ندارد</p>
            {emptyHint ? <p className="text-xs text-gray-400 mt-2">{emptyHint}</p> : null}
          </div>
        ) : (
          filteredItems.map((item) => {
            const Icon = item.icon;
            return (
              <div
                key={item.id}
                className={`bg-white dark:bg-[#111] rounded-2xl p-4 border-2 transition-all duration-300 hover:shadow-md ${
                  item.read ? "border-gray-200 dark:border-gray-800" : `border-[#E53935]/30 ${item.bgColor}`
                }`}
                data-testid={`notification-row-${item.id}`}
                data-notification-type={item.type}
                data-read={item.read ? "true" : "false"}
              >
                <div className="flex items-start gap-3">
                  <div className={`w-10 h-10 rounded-xl ${item.bgColor} flex items-center justify-center flex-shrink-0`}>
                    <Icon className={`w-5 h-5 ${item.color}`} />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between flex-wrap gap-2">
                      <div className="flex items-center gap-2">
                        <p
                          className={`text-sm font-medium ${
                            item.read ? "text-gray-600 dark:text-gray-400" : "text-gray-900 dark:text-white"
                          }`}
                        >
                          {item.title}
                        </p>
                        {!item.read && <span className="w-2 h-2 rounded-full bg-[#E53935] animate-pulse" />}
                      </div>
                      <div className="flex items-center gap-2 text-[10px] text-gray-400">
                        <Calendar className="w-3 h-3" />
                        <span>{item.date}</span>
                        <span>•</span>
                        <span>{item.time}</span>
                      </div>
                    </div>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{item.desc}</p>
                    <div className="flex items-center gap-2 mt-2.5">
                      {!item.read && (
                        <button
                          type="button"
                          onClick={() => void handleMarkAsRead(item.id)}
                          className="text-[10px] text-[#E53935] hover:underline font-medium flex items-center gap-1"
                        >
                          <Check className="w-3 h-3" />
                          علامت خوانده شد
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => void handleDelete(item.id)}
                        className="text-[10px] text-gray-400 hover:text-red-500 transition-colors flex items-center gap-1"
                      >
                        <X className="w-3 h-3" />
                        حذف
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
