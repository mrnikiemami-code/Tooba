"use client";

/**
 * UI تیکت پشتیبانی — هندسه و کلاس‌های Shopeiva (#E53935، rounded-2xl، حباب گفتگو).
 * ضمیمه فایل عمداً حذف شده (DEFER attachments).
 */

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from "react";
import {
  AlertCircle,
  ArrowRight,
  CheckCircle,
  ChevronDown,
  ChevronLeft,
  Clock,
  Eye,
  Filter,
  Loader2,
  MessageSquare,
  Plus,
  Search,
  Send,
  Ticket,
  X,
} from "lucide-react";
import {
  TICKET_CATEGORIES,
  TICKET_PRIORITIES,
  formatTicketCategory,
  formatTicketDate,
  formatTicketDateShort,
  formatTicketPriority,
  formatTicketStatus,
  toPersianDigits,
  type CreateTicketInput,
  type TicketListRow,
  type TicketMessage,
  type TicketSnapshot,
} from "./support-api.ts";

const ACCENT = "#E53935";

export function ticketStatusVisual(status: string): {
  color: string;
  bg: string;
  icon: typeof AlertCircle;
  label: string;
} {
  switch (status) {
    case "Open":
      return { color: "text-red-500", bg: "bg-red-50", icon: AlertCircle, label: formatTicketStatus(status) };
    case "InProgress":
    case "WaitingForCustomer":
    case "WaitingForSeller":
      return { color: "text-amber-500", bg: "bg-amber-50", icon: Clock, label: formatTicketStatus(status) };
    case "Resolved":
    case "Closed":
      return { color: "text-emerald-500", bg: "bg-emerald-50", icon: CheckCircle, label: formatTicketStatus(status) };
    default:
      return { color: "text-gray-500", bg: "bg-gray-50", icon: MessageSquare, label: formatTicketStatus(status) };
  }
}

export function ticketPriorityClass(priority: string): string {
  switch (priority) {
    case "High":
      return "bg-red-100 text-red-600";
    case "Low":
      return "bg-emerald-100 text-emerald-600";
    default:
      return "bg-amber-100 text-amber-600";
  }
}

type ListAudience = "customer" | "seller" | "admin";

/** فهرست تیکت — الگوی ticketsList Shopeiva. */
export function SupportTicketsList({
  audience,
  basePath,
  rows,
  loading,
  error,
  onRetry,
  onFilterStatus,
  statusFilter,
  searchEnabled = true,
  showRequester = false,
}: {
  audience: ListAudience;
  basePath: string;
  rows: TicketListRow[];
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  onFilterStatus?: (status: string | "all") => void;
  statusFilter?: string | "all";
  searchEnabled?: boolean;
  showRequester?: boolean;
}) {
  const [searchTerm, setSearchTerm] = useState("");
  const [localFilter, setLocalFilter] = useState<string | "all">(statusFilter ?? "all");
  const [showFilters, setShowFilters] = useState(false);
  const [page, setPage] = useState(0);
  const itemsPerPage = 8;

  useEffect(() => {
    if (statusFilter != null) setLocalFilter(statusFilter);
  }, [statusFilter]);

  const filter = statusFilter ?? localFilter;

  const filtered = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    return rows.filter((row) => {
      const matchFilter = filter === "all" || row.status === filter;
      if (!matchFilter) return false;
      if (!q) return true;
      return (
        row.subject.toLowerCase().includes(q) ||
        row.ticketId.toLowerCase().includes(q) ||
        formatTicketStatus(row.status).includes(searchTerm.trim()) ||
        formatTicketPriority(row.priority).includes(searchTerm.trim())
      );
    });
  }, [rows, filter, searchTerm]);

  const pageCount = Math.max(1, Math.ceil(filtered.length / itemsPerPage));
  const safePage = Math.min(page, pageCount - 1);
  const offset = safePage * itemsPerPage;
  const currentItems = filtered.slice(offset, offset + itemsPerPage);

  const stats = {
    total: rows.length,
    open: rows.filter((t) => t.status === "Open").length,
    pending: rows.filter((t) => t.status === "InProgress" || t.status.startsWith("Waiting")).length,
    closed: rows.filter((t) => t.status === "Closed" || t.status === "Resolved").length,
  };

  function setFilter(next: string | "all") {
    setLocalFilter(next);
    setPage(0);
    onFilterStatus?.(next);
  }

  const statusOptions: Array<{ value: string | "all"; label: string }> = [
    { value: "all", label: "همه" },
    { value: "Open", label: "باز" },
    { value: "InProgress", label: "در حال بررسی" },
    { value: "WaitingForCustomer", label: "در انتظار مشتری" },
    { value: "WaitingForSeller", label: "در انتظار فروشنده" },
    { value: "Resolved", label: "حل‌شده" },
    { value: "Closed", label: "بسته‌شده" },
  ];

  return (
    <div className="space-y-4" data-testid={`${audience}-tickets-list`} dir="rtl">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-[#E53935]/10 flex items-center justify-center">
            <Ticket className="w-5 h-5 text-[#E53935]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">تیکت‌های پشتیبانی</h2>
            <p className="text-xs text-gray-500">
              {toPersianDigits(stats.total)} تیکت · {toPersianDigits(stats.open)} باز ·{" "}
              {toPersianDigits(stats.pending)} در حال بررسی
            </p>
          </div>
        </div>
        {audience !== "admin" ? (
          <Link
            href={`${basePath}/new`}
            className="px-4 py-2 bg-[#E53935] text-white rounded-xl text-xs font-bold hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center gap-1"
            data-testid={`${audience}-ticket-new`}
          >
            <Plus className="w-4 h-4" />
            تیکت جدید
          </Link>
        ) : null}
      </div>

      {searchEnabled ? (
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="relative flex-1">
            <Search className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(0);
              }}
              placeholder="جستجو در تیکت‌ها (عنوان، شناسه، وضعیت، اولویت)..."
              className="w-full pr-10 px-4 py-2.5 bg-white rounded-xl text-sm text-gray-900 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            />
            {searchTerm ? (
              <button
                type="button"
                onClick={() => setSearchTerm("")}
                className="absolute left-3 top-1/2 -translate-y-1/2 p-1 rounded-full hover:bg-gray-200"
                aria-label="پاک کردن جستجو"
              >
                <X className="w-4 h-4 text-gray-400" />
              </button>
            ) : null}
          </div>
          <div className="relative">
            <button
              type="button"
              onClick={() => setShowFilters((v) => !v)}
              className={`px-4 py-2.5 rounded-xl text-sm font-medium transition-all flex items-center gap-2 ${
                filter !== "all"
                  ? "bg-[#E53935] text-white shadow-lg shadow-[#E53935]/30"
                  : "bg-white text-gray-700 border border-gray-200 hover:border-[#E53935]/50"
              }`}
            >
              <Filter className="w-4 h-4" />
              {filter !== "all" ? formatTicketStatus(filter) : "فیلتر"}
              <ChevronDown className={`w-4 h-4 transition-transform ${showFilters ? "rotate-180" : ""}`} />
            </button>
            {showFilters ? (
              <div className="absolute top-full left-0 mt-1 bg-white rounded-xl border border-gray-200 shadow-lg z-10 min-w-[160px]">
                {statusOptions.map((opt) => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() => {
                      setFilter(opt.value);
                      setShowFilters(false);
                    }}
                    className={`block w-full text-right px-4 py-2 text-sm hover:bg-gray-100 ${
                      filter === opt.value ? "text-[#E53935] font-bold" : "text-gray-700"
                    }`}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            ) : null}
          </div>
        </div>
      ) : null}

      {error ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 flex items-center justify-between gap-3">
          <span>{error}</span>
          {onRetry ? (
            <button type="button" onClick={onRetry} className="text-[#E53935] font-bold hover:underline">
              تلاش مجدد
            </button>
          ) : null}
        </div>
      ) : null}

      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-sm text-gray-500 flex items-center justify-center gap-2">
            <Loader2 className="w-4 h-4 animate-spin text-[#E53935]" />
            در حال بارگذاری…
          </div>
        ) : currentItems.length === 0 ? (
          <div className="p-8 text-center">
            <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-3">
              <Ticket className="w-8 h-8 text-gray-300" />
            </div>
            <p className="text-sm text-gray-500">هیچ تیکتی یافت نشد</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-100">
            {currentItems.map((ticket) => {
              const visual = ticketStatusVisual(ticket.status);
              const StatusIcon = visual.icon;
              return (
                <div key={ticket.ticketId} className="p-4 hover:bg-gray-50 transition-colors group">
                  <div className="flex items-center gap-4">
                    <div className={`w-10 h-10 rounded-xl ${visual.bg} flex items-center justify-center flex-shrink-0`}>
                      <StatusIcon className={`w-3.5 h-3.5 ${visual.color}`} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="text-sm font-bold text-gray-900 truncate">{ticket.subject}</p>
                        <span className={`text-[10px] font-medium px-2 py-0.5 rounded-full ${visual.bg} ${visual.color}`}>
                          {visual.label}
                        </span>
                      </div>
                      <div className="flex items-center gap-3 mt-1 flex-wrap">
                        <span className={`text-[10px] font-medium px-2 py-0.5 rounded-full ${ticketPriorityClass(ticket.priority)}`}>
                          {formatTicketPriority(ticket.priority)}
                        </span>
                        <span className="text-xs text-gray-500">{formatTicketCategory(ticket.category)}</span>
                        {showRequester && ticket.requesterKind ? (
                          <span className="text-xs text-gray-500">{ticket.requesterKind}</span>
                        ) : null}
                        <span className="text-xs text-gray-500 flex items-center gap-0.5">
                          <MessageSquare className="w-3 h-3" />
                          {toPersianDigits(ticket.messageCount)} پیام
                        </span>
                        <span className="text-xs text-gray-500">
                          {formatTicketDateShort(ticket.lastMessageAt ?? ticket.createdAt)}
                        </span>
                      </div>
                    </div>
                    <Link
                      href={`${basePath}/${ticket.ticketId}`}
                      className="p-2 rounded-lg hover:bg-gray-100 transition-colors text-gray-400 hover:text-[#E53935]"
                      title="مشاهده"
                      data-testid={`${audience}-ticket-open-${ticket.ticketId}`}
                    >
                      <Eye className="w-4 h-4" />
                    </Link>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {filtered.length > itemsPerPage ? (
        <div className="flex flex-col items-center gap-2">
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              disabled={safePage <= 0}
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              className="flex items-center justify-center w-10 h-10 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-50"
              aria-label="صفحه قبل"
            >
              <ChevronLeft className="w-5 h-5 rotate-180" />
            </button>
            <span className="text-sm text-gray-600 px-2">
              {toPersianDigits(safePage + 1)} / {toPersianDigits(pageCount)}
            </span>
            <button
              type="button"
              disabled={safePage >= pageCount - 1}
              onClick={() => setPage((p) => Math.min(pageCount - 1, p + 1))}
              className="flex items-center justify-center w-10 h-10 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-50"
              aria-label="صفحه بعد"
            >
              <ChevronLeft className="w-5 h-5" />
            </button>
          </div>
          <div className="text-xs text-gray-500">
            نمایش {toPersianDigits(offset + 1)} تا {toPersianDigits(Math.min(offset + itemsPerPage, filtered.length))} از{" "}
            {toPersianDigits(filtered.length)} نتیجه
          </div>
        </div>
      ) : null}
    </div>
  );
}

/** فرم تیکت جدید — بدون آپلود ضمیمه (DEFER). */
export function SupportTicketForm({
  listHref,
  onSubmit,
}: {
  listHref: string;
  onSubmit: (input: CreateTicketInput) => Promise<{ ok: boolean; errorCode?: string; ticketId?: string }>;
}) {
  const router = useRouter();
  const [category, setCategory] = useState("Order");
  const [priority, setPriority] = useState("Normal");
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [relatedEntityId, setRelatedEntityId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    if (subject.trim().length < 3) {
      setError("عنوان حداقل ۳ کاراکتر باید باشد");
      return;
    }
    if (body.trim().length < 10) {
      setError("توضیحات حداقل ۱۰ کاراکتر باید باشد");
      return;
    }
    setLoading(true);
    const result = await onSubmit({
      subject: subject.trim(),
      category,
      priority,
      body: body.trim(),
      relatedEntityType: relatedEntityId.trim() ? "Order" : null,
      relatedEntityId: relatedEntityId.trim() || null,
      idempotencyKey: crypto.randomUUID(),
    });
    setLoading(false);
    if (!result.ok) {
      setError(result.errorCode ?? "ثبت تیکت ناموفق بود");
      return;
    }
    router.push(result.ticketId ? `${listHref}/${result.ticketId}` : listHref);
  }

  return (
    <div className="max-w-2xl mx-auto" data-testid="support-ticket-form" dir="rtl">
      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        <div className="p-4 md:p-6 border-b border-gray-200">
          <div className="flex items-center gap-2">
            <Ticket className="w-5 h-5 text-[#E53935]" />
            <h2 className="text-lg font-bold text-gray-900">ارسال تیکت جدید</h2>
          </div>
          <p className="text-sm text-gray-500 mt-1">مشکل یا سوال خود را مطرح کنید؛ تیم پشتیبانی پاسخ خواهد داد</p>
        </div>

        <form onSubmit={handleSubmit} className="p-4 md:p-6 space-y-4">
          <div>
            <label className="text-sm font-medium text-gray-700">
              دسته‌بندی <span className="text-red-500">*</span>
            </label>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            >
              {TICKET_CATEGORIES.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">
              عنوان <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="خلاصه مشکل خود را بنویسید"
              className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            />
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">
              توضیحات <span className="text-red-500">*</span>
            </label>
            <textarea
              rows={5}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              placeholder="مشکل خود را به طور کامل توضیح دهید..."
              className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935] resize-none"
            />
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">اولویت</label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
              className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
            >
              {TICKET_PRIORITIES.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700">شناسه سفارش مرتبط (اختیاری)</label>
            <input
              type="text"
              value={relatedEntityId}
              onChange={(e) => setRelatedEntityId(e.target.value)}
              placeholder="در صورت ارتباط با سفارش، شناسه را وارد کنید"
              className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935] font-mono"
              dir="ltr"
            />
          </div>

          {/* attachments DEFERRED — کنترل آپلود عمداً نمایش داده نمی‌شود */}

          {error ? <p className="text-xs text-red-500">{error}</p> : null}

          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={() => router.push(listHref)}
              className="px-6 py-2.5 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium hover:bg-gray-200 transition-colors flex items-center gap-2"
            >
              <ArrowRight className="w-4 h-4" />
              بازگشت
            </button>
            <button
              type="submit"
              disabled={loading}
              className={`flex-1 py-2.5 bg-[#E53935] text-white rounded-xl text-sm font-bold hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center justify-center gap-2 ${
                loading ? "opacity-70 cursor-not-allowed" : ""
              }`}
            >
              {loading ? <Loader2 className="w-5 h-5 animate-spin" /> : <Send className="w-4 h-4" />}
              ارسال تیکت
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function authorLabel(kind: string, audience: ListAudience): string {
  if (kind === "Admin" || kind === "System") return "پشتیبانی";
  if (kind === "Seller") return audience === "seller" ? "شما" : "فروشنده";
  if (kind === "Customer") return audience === "customer" ? "شما" : "مشتری";
  return kind;
}

function isSelfBubble(kind: string, audience: ListAudience): boolean {
  if (audience === "admin") return kind === "Admin" || kind === "System";
  if (audience === "seller") return kind === "Seller";
  return kind === "Customer";
}

/** جزئیات و گفتگو — حباب‌های Shopeiva. */
export function SupportTicketThread({
  audience,
  listHref,
  snapshot,
  loading,
  error,
  onReply,
  onClose,
  onReopen,
  adminControls,
}: {
  audience: ListAudience;
  listHref: string;
  snapshot: TicketSnapshot | null;
  loading?: boolean;
  error?: string | null;
  onReply: (body: string, isInternalNote?: boolean) => Promise<{ ok: boolean; errorCode?: string }>;
  onClose?: () => Promise<void>;
  onReopen?: () => Promise<void>;
  adminControls?: ReactNode;
}) {
  const [reply, setReply] = useState("");
  const [internalNote, setInternalNote] = useState(false);
  const [sending, setSending] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const messages = useMemo(() => {
    if (!snapshot) return [] as TicketMessage[];
    if (audience === "admin") return snapshot.messages;
    return snapshot.messages.filter((m) => !m.isInternalNote);
  }, [snapshot, audience]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages.length]);

  useEffect(() => {
    if (!sending) textareaRef.current?.focus();
  }, [sending]);

  async function sendReply() {
    if (!reply.trim()) {
      setActionError("لطفاً متن پاسخ را وارد کنید");
      return;
    }
    setActionError(null);
    setSending(true);
    const result = await onReply(reply.trim(), audience === "admin" ? internalNote : false);
    setSending(false);
    if (!result.ok) {
      setActionError(result.errorCode ?? "ارسال پاسخ ناموفق بود");
      return;
    }
    setReply("");
    setInternalNote(false);
  }

  if (loading && !snapshot) {
    return (
      <div className="p-8 text-center text-sm text-gray-500 flex items-center justify-center gap-2" dir="rtl">
        <Loader2 className="w-4 h-4 animate-spin text-[#E53935]" />
        در حال بارگذاری…
      </div>
    );
  }

  if (error && !snapshot) {
    return (
      <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700" dir="rtl">
        {error}
      </div>
    );
  }

  if (!snapshot) {
    return (
      <div className="p-8 text-center text-sm text-gray-500" dir="rtl">
        تیکت پیدا نشد
      </div>
    );
  }

  const visual = ticketStatusVisual(snapshot.status);
  const StatusIcon = visual.icon;
  const canReply = snapshot.status !== "Closed";
  const canClose =
    audience !== "admin" && (snapshot.status === "Open" || snapshot.status === "Resolved") && Boolean(onClose);
  const canReopen = audience !== "admin" && snapshot.status === "Closed" && Boolean(onReopen);

  return (
    <div className="max-w-3xl mx-auto space-y-4" data-testid={`${audience}-ticket-thread`} dir="rtl">
      <Link
        href={listHref}
        className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-[#E53935] transition-colors"
      >
        <ArrowRight className="w-4 h-4" />
        بازگشت به لیست تیکت‌ها
      </Link>

      <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-6">
        <div className="flex items-start justify-between gap-3 flex-wrap">
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <h2 className="text-lg font-bold text-gray-900">{snapshot.subject}</h2>
              <span
                className={`text-[10px] font-medium px-2 py-0.5 rounded-full ${visual.bg} ${visual.color} flex items-center gap-0.5`}
              >
                <StatusIcon className="w-3 h-3" />
                {visual.label}
              </span>
            </div>
            <div className="flex items-center gap-3 mt-2 flex-wrap text-xs text-gray-500">
              <span>{formatTicketCategory(snapshot.category)}</span>
              <span className="flex items-center gap-1">
                <MessageSquare className="w-3 h-3" />
                {toPersianDigits(messages.length)} پیام
              </span>
              <span>{formatTicketDate(snapshot.createdAt)}</span>
              {snapshot.relatedEntityId ? (
                <span className="font-mono" dir="ltr">
                  مرتبط: {snapshot.relatedEntityId.slice(0, 8)}
                </span>
              ) : null}
            </div>
          </div>
          <span className={`text-[10px] font-medium px-3 py-1 rounded-full ${ticketPriorityClass(snapshot.priority)}`}>
            اولویت: {formatTicketPriority(snapshot.priority)}
          </span>
        </div>

        {(canClose || canReopen) && (
          <div className="mt-4 flex gap-2 flex-wrap">
            {canClose ? (
              <button
                type="button"
                onClick={() => void onClose?.()}
                className="px-3 py-1.5 text-xs rounded-xl border border-gray-200 hover:bg-gray-50"
              >
                بستن تیکت
              </button>
            ) : null}
            {canReopen ? (
              <button
                type="button"
                onClick={() => void onReopen?.()}
                className="px-3 py-1.5 text-xs rounded-xl bg-[#E53935] text-white hover:bg-[#c62828]"
              >
                بازگشایی تیکت
              </button>
            ) : null}
          </div>
        )}

        {adminControls}
      </div>

      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        <div className="p-4 border-b border-gray-200">
          <h3 className="font-bold text-gray-900 flex items-center gap-2">
            <MessageSquare className="w-5 h-5" style={{ color: ACCENT }} />
            گفتگو
          </h3>
        </div>

        <div className="p-4 space-y-4 max-h-[400px] overflow-y-auto">
          {messages.map((msg) => {
            const self = isSelfBubble(msg.authorKind, audience);
            const note = msg.isInternalNote;
            return (
              <div key={msg.messageId} className={`flex ${self ? "justify-start" : "justify-end"}`}>
                <div
                  className={`max-w-[80%] rounded-2xl p-3 ${
                    note
                      ? "bg-amber-50 text-amber-900 border border-amber-200 rounded-bl-none"
                      : self
                        ? "bg-[#E53935] text-white rounded-br-none"
                        : "bg-gray-100 text-gray-900 rounded-bl-none"
                  }`}
                >
                  <div className="flex items-center gap-2 mb-1 flex-wrap">
                    <span className={`text-xs font-medium ${self && !note ? "text-white/80" : "text-[#E53935]"}`}>
                      {note ? "یادداشت داخلی" : authorLabel(msg.authorKind, audience)}
                    </span>
                    <span className={`text-[10px] ${self && !note ? "text-white/60" : "text-gray-400"}`}>
                      {formatTicketDate(msg.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm leading-relaxed break-words whitespace-pre-wrap">{msg.body}</p>
                </div>
              </div>
            );
          })}
          {sending ? (
            <div className="flex justify-end">
              <div className="bg-gray-100 rounded-2xl p-3 rounded-bl-none flex items-center gap-2">
                <Loader2 className="w-4 h-4 text-[#E53935] animate-spin" />
                <span className="text-xs text-gray-500">در حال ارسال...</span>
              </div>
            </div>
          ) : null}
          <div ref={messagesEndRef} />
        </div>

        {canReply ? (
          <div className="p-4 border-t border-gray-200">
            {audience === "admin" ? (
              <label className="mb-2 flex items-center gap-2 text-xs text-gray-600">
                <input
                  type="checkbox"
                  checked={internalNote}
                  onChange={(e) => setInternalNote(e.target.checked)}
                  className="rounded border-gray-300"
                />
                یادداشت داخلی (فقط اپراتور)
              </label>
            ) : null}
            <textarea
              ref={textareaRef}
              value={reply}
              onChange={(e) => setReply(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  void sendReply();
                }
              }}
              rows={3}
              maxLength={4000}
              placeholder="پاسخ خود را بنویسید... (Enter برای ارسال، Shift+Enter برای خط جدید)"
              className="w-full px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935] resize-none"
              disabled={sending}
            />
            <div className="flex justify-between items-center mt-2">
              <span className="text-[10px] text-gray-400">
                {reply.length > 0 ? `${toPersianDigits(reply.length)} کاراکتر` : "Enter برای ارسال"}
              </span>
              <button
                type="button"
                onClick={() => void sendReply()}
                disabled={sending || !reply.trim()}
                className={`px-6 py-2 bg-[#E53935] text-white rounded-xl text-sm font-bold hover:bg-[#c62828] transition-colors shadow-lg shadow-[#E53935]/30 flex items-center gap-2 ${
                  sending || !reply.trim() ? "opacity-70 cursor-not-allowed" : ""
                }`}
              >
                {sending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
                ارسال پاسخ
              </button>
            </div>
            {actionError ? <p className="text-xs text-red-500 mt-2">{actionError}</p> : null}
          </div>
        ) : (
          <div className="p-4 border-t border-gray-200 text-xs text-gray-500">این تیکت بسته‌شده و قابل پاسخ نیست.</div>
        )}
      </div>
    </div>
  );
}
