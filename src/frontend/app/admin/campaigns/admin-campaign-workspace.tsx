"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ArrowDown, ArrowUp, Plus, Trash2, X } from "lucide-react";
import { formatAdminMoney } from "../admin-api.ts";
import {
  addAdminCampaignMember,
  archiveAdminCampaign,
  campaignRuntimeFa,
  createAdminCampaign,
  getAdminCampaign,
  listAdminCampaignTypes,
  listAdminOfferCandidates,
  publishAdminCampaign,
  removeAdminCampaignMember,
  reorderAdminCampaignMembers,
  setAdminCampaignMemberPrice,
  updateAdminCampaign,
  type AdminCampaignDetail,
  type AdminCampaignMember,
  type AdminCampaignTranslation,
  type AdminCampaignTypeOption,
  type AdminOfferCandidate,
} from "./admin-campaigns-api.ts";

type WorkspaceTab = "info" | "members" | "pricing" | "translations";

const TABS: { id: WorkspaceTab; label: string }[] = [
  { id: "info", label: "اطلاعات کمپین" },
  { id: "members", label: "کالاها" },
  { id: "pricing", label: "قیمت‌گذاری" },
  { id: "translations", label: "ترجمه‌ها" },
];

function emptyTranslation(locale: string): AdminCampaignTranslation {
  return { locale, title: "", subtitle: "", badgeText: "" };
}

function ensureTranslations(rows: AdminCampaignTranslation[]): AdminCampaignTranslation[] {
  const fa = rows.find((row) => row.locale.toLowerCase().startsWith("fa")) ?? emptyTranslation("fa-IR");
  const en = rows.find((row) => row.locale.toLowerCase().startsWith("en")) ?? emptyTranslation("en");
  return [
    { ...fa, locale: fa.locale || "fa-IR" },
    { ...en, locale: en.locale || "en" },
  ];
}

function toLocalInput(iso: string | null | undefined): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function fromLocalInput(value: string): string | null {
  if (!value.trim()) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

function discountPreview(base: number, campaign: number | null): string | null {
  if (campaign == null || !(base > 0) || !(campaign > 0)) return null;
  if (campaign >= base) return null;
  const pct = Math.round((1 - campaign / base) * 100);
  return pct > 0 ? `${pct.toLocaleString("fa-IR")}٪ تخفیف` : null;
}

function runtimeBadgeClass(key: string): string {
  switch (key) {
    case "active":
      return "bg-emerald-50 text-emerald-700";
    case "scheduled":
      return "bg-blue-50 text-blue-700";
    case "expired":
      return "bg-slate-100 text-slate-600";
    case "archived":
      return "bg-amber-50 text-amber-800";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

function OfferPickerDialog({
  open,
  excludeIds,
  onClose,
  onPick,
}: {
  open: boolean;
  excludeIds: Set<string>;
  onClose: () => void;
  onPick: (sellerOfferId: string) => void;
}) {
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<AdminOfferCandidate[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    const timer = window.setTimeout(() => {
      void listAdminOfferCandidates({ search, skip: 0, take: 40 }).then((result) => {
        setLoading(false);
        if (!result.ok) {
          setError(result.message);
          return;
        }
        setError(undefined);
        setItems(result.data.items.filter((row) => !excludeIds.has(row.sellerOfferId)));
      });
    }, 250);
    return () => window.clearTimeout(timer);
  }, [excludeIds, open, search]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" data-testid="campaign-offer-picker">
      <div className="max-h-[85vh] w-full max-w-2xl overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-xl">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 className="text-base font-black">افزودن کالا</h2>
          <button type="button" className="rounded-lg border p-1.5" onClick={onClose} aria-label="بستن">
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="border-b border-border p-4">
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-xl border border-border px-3 py-2 text-sm"
            placeholder="جستجوی عنوان کالا یا فروشنده…"
            data-testid="campaign-offer-search"
            autoFocus
          />
        </div>
        <div className="max-h-[50vh] overflow-y-auto p-2">
          {loading ? <p className="p-4 text-sm text-muted">در حال جستجو…</p> : null}
          {error ? <p className="p-4 text-sm text-red-600">{error}</p> : null}
          {!loading && !error && items.length === 0 ? (
            <p className="p-4 text-sm text-muted">کالای مناسبی پیدا نشد.</p>
          ) : null}
          <ul className="space-y-1">
            {items.map((item) => (
              <li key={item.sellerOfferId}>
                <button
                  type="button"
                  className="flex w-full items-start justify-between gap-3 rounded-xl px-3 py-2 text-right hover:bg-slate-50"
                  onClick={() => onPick(item.sellerOfferId)}
                  data-testid={`campaign-offer-pick-${item.sellerOfferId}`}
                >
                  <div>
                    <p className="font-bold text-sm">{item.productTitle}</p>
                    <p className="mt-0.5 text-xs text-muted">{item.sellerDisplayName}</p>
                    {!item.inStock ? (
                      <p className="mt-1 text-xs font-bold text-amber-700">ناموجود</p>
                    ) : null}
                  </div>
                  <div className="shrink-0 text-left text-xs">
                    <p className="font-bold">{formatAdminMoney(item.baseAmount, item.currency)}</p>
                    <p className="text-muted">{item.availableUnits.toLocaleString("fa-IR")} عدد</p>
                  </div>
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}

export function AdminCampaignWorkspace({ campaignId }: { campaignId?: string }) {
  const router = useRouter();
  const isCreate = !campaignId;
  const [tab, setTab] = useState<WorkspaceTab>("info");
  const [detail, setDetail] = useState<AdminCampaignDetail | null>(null);
  const [types, setTypes] = useState<AdminCampaignTypeOption[]>([]);
  const [promotionTypeId, setPromotionTypeId] = useState("");
  const [startAt, setStartAt] = useState("");
  const [endAt, setEndAt] = useState("");
  const [priority, setPriority] = useState("100");
  const [translations, setTranslations] = useState<AdminCampaignTranslation[]>(ensureTranslations([]));
  const [priceDrafts, setPriceDrafts] = useState<Record<string, string>>({});
  const [pickerOpen, setPickerOpen] = useState(false);
  const [message, setMessage] = useState<string>();
  const [messageTone, setMessageTone] = useState<"error" | "success">("error");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(!isCreate);
  const [denied, setDenied] = useState(false);
  const titleRef = useRef<HTMLInputElement>(null);
  const startRef = useRef<HTMLInputElement>(null);

  const applyDetail = useCallback((next: AdminCampaignDetail) => {
    setDetail(next);
    setPromotionTypeId(next.promotionTypeId);
    setStartAt(toLocalInput(next.startAt));
    setEndAt(toLocalInput(next.endAt));
    setPriority(String(next.priority));
    setTranslations(ensureTranslations(next.translations));
    const drafts: Record<string, string> = {};
    for (const member of next.members) {
      drafts[member.sellerOfferId] =
        member.campaignAmount != null && Number.isFinite(member.campaignAmount)
          ? String(member.campaignAmount)
          : "";
    }
    setPriceDrafts(drafts);
  }, []);

  const load = useCallback(async (id: string) => {
    setLoading(true);
    const [campaign, typeResult] = await Promise.all([getAdminCampaign(id), listAdminCampaignTypes("fa-IR")]);
    setLoading(false);
    if (!campaign.ok) {
      setDenied(Boolean(campaign.denied));
      setMessageTone("error");
      setMessage(campaign.message);
      return;
    }
    setDenied(false);
    if (typeResult.ok) setTypes(typeResult.data);
    applyDetail(campaign.data);
  }, [applyDetail]);

  useEffect(() => {
    void listAdminCampaignTypes("fa-IR").then((result) => {
      if (!result.ok) return;
      setTypes(result.data);
      setPromotionTypeId((current) => current || result.data[0]?.promotionTypeId || "");
    });
  }, []);

  useEffect(() => {
    if (campaignId) void load(campaignId);
  }, [campaignId, load]);

  const members = detail?.members ?? [];
  const excludeIds = useMemo(() => new Set(members.map((m) => m.sellerOfferId)), [members]);
  const faTitle = translations.find((row) => row.locale.toLowerCase().startsWith("fa"))?.title ?? "";

  const validateWindow = (): string | null => {
    const startIso = fromLocalInput(startAt);
    const endIso = fromLocalInput(endAt);
    if (!startIso) return "زمان شروع الزامی است.";
    if (endIso && new Date(startIso).getTime() >= new Date(endIso).getTime()) {
      return "زمان شروع باید قبل از پایان باشد.";
    }
    if (!faTitle.trim()) return "عنوان فارسی الزامی است.";
    const prio = Number(priority);
    if (!Number.isFinite(prio)) return "اولویت نامعتبر است.";
    if (isCreate && !promotionTypeId) return "نوع کمپین را انتخاب کنید.";
    return null;
  };

  const buildTranslationsPayload = () =>
    translations.map((row) => ({
      locale: row.locale,
      title: row.title.trim(),
      subtitle: row.subtitle.trim(),
      badgeText: row.badgeText.trim(),
    }));

  const save = async () => {
    const error = validateWindow();
    if (error) {
      setMessageTone("error");
      setMessage(error);
      if (!faTitle.trim()) titleRef.current?.focus();
      else startRef.current?.focus();
      return;
    }
    const startIso = fromLocalInput(startAt)!;
    const endIso = fromLocalInput(endAt);
    setBusy(true);
    if (isCreate) {
      const created = await createAdminCampaign({
        promotionTypeId,
        startAt: startIso,
        endAt: endIso,
        priority: Number(priority),
        translations: buildTranslationsPayload(),
      });
      setBusy(false);
      if (!created.ok) {
        setMessageTone("error");
        setMessage(created.message);
        return;
      }
      router.replace(`/admin/campaigns/${created.data.campaignId}`);
      return;
    }
    const updated = await updateAdminCampaign(campaignId!, {
      startAt: startIso,
      endAt: endIso,
      priority: Number(priority),
      translations: buildTranslationsPayload(),
    });
    setBusy(false);
    if (!updated.ok) {
      setMessageTone("error");
      setMessage(updated.message);
      return;
    }
    applyDetail(updated.data);
    setMessageTone("success");
    setMessage("تغییرات ذخیره شد.");
  };

  const publish = async () => {
    if (!campaignId) return;
    setBusy(true);
    const result = await publishAdminCampaign(campaignId);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
    setMessageTone("success");
    setMessage("کمپین منتشر شد.");
  };

  const archive = async () => {
    if (!campaignId) return;
    if (!window.confirm("این کمپین بایگانی شود؟")) return;
    setBusy(true);
    const result = await archiveAdminCampaign(campaignId);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
    setMessageTone("success");
    setMessage("کمپین بایگانی شد.");
  };

  const addMember = async (sellerOfferId: string) => {
    if (!campaignId) return;
    setPickerOpen(false);
    setBusy(true);
    const result = await addAdminCampaignMember(campaignId, sellerOfferId);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
    setMessageTone("success");
    setMessage("کالا افزوده شد.");
  };

  const removeMember = async (sellerOfferId: string) => {
    if (!campaignId) return;
    setBusy(true);
    const result = await removeAdminCampaignMember(campaignId, sellerOfferId);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
  };

  const moveMember = async (member: AdminCampaignMember, direction: -1 | 1) => {
    if (!campaignId) return;
    const ordered = members.map((row) => row.sellerOfferId);
    const index = ordered.indexOf(member.sellerOfferId);
    const swap = ordered[index + direction];
    if (!swap) return;
    [ordered[index], ordered[index + direction]] = [swap, member.sellerOfferId];
    setBusy(true);
    const result = await reorderAdminCampaignMembers(campaignId, ordered);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
  };

  const savePrice = async (member: AdminCampaignMember) => {
    if (!campaignId) return;
    const raw = priceDrafts[member.sellerOfferId]?.trim() ?? "";
    const amount = Number(raw);
    if (!raw || !Number.isFinite(amount) || amount <= 0) {
      setMessageTone("error");
      setMessage("قیمت کمپین باید عددی بزرگ‌تر از صفر باشد.");
      return;
    }
    setBusy(true);
    const result = await setAdminCampaignMemberPrice(campaignId, member.sellerOfferId, amount, {
      currency: member.currency,
    });
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    applyDetail(result.data);
    setMessageTone("success");
    setMessage(
      amount >= member.baseAmount
        ? "قیمت ذخیره شد؛ نسبت به قیمت عادی تخفیفی ندارد."
        : "قیمت کمپین ذخیره شد.",
    );
  };

  const setTranslationField = (
    localePrefix: "fa" | "en",
    field: keyof Omit<AdminCampaignTranslation, "locale">,
    value: string,
  ) => {
    setTranslations((current) =>
      ensureTranslations(current).map((row) =>
        row.locale.toLowerCase().startsWith(localePrefix) ? { ...row, [field]: value } : row,
      ),
    );
  };

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-campaign-workspace">
        <p>دسترسی به کمپین مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={() => campaignId && void load(campaignId)}>
          تلاش دوباره
        </button>
      </main>
    );
  }

  if (loading) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-campaign-workspace">
        <p className="text-sm text-muted">در حال بارگذاری…</p>
      </main>
    );
  }

  const runtimeKey = detail?.runtimeLabel ?? "draft";
  const typeLabel =
    types.find((row) => row.promotionTypeId === promotionTypeId)?.displayName
    ?? detail?.promotionTypeDisplayName
    ?? "—";

  return (
    <main data-testid="admin-campaign-workspace">
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/campaigns" className="text-sm text-muted hover:underline">
            بازگشت به فهرست
          </Link>
          <h1 className="mt-1 text-xl font-black">
            {isCreate ? "کمپین جدید" : faTitle || "ویرایش کمپین"}
          </h1>
          <div className="mt-2 flex flex-wrap items-center gap-2 text-sm">
            <span className={`rounded-full px-2.5 py-0.5 text-xs font-bold ${runtimeBadgeClass(runtimeKey)}`}>
              {campaignRuntimeFa(runtimeKey)}
            </span>
            <span className="text-muted">{typeLabel}</span>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className="rounded-xl border px-4 py-2 text-sm font-bold"
            disabled={busy}
            onClick={() => void save()}
            data-testid="campaign-save"
          >
            {isCreate ? "ایجاد پیش‌نویس" : "ذخیره"}
          </button>
          {!isCreate && detail?.lifecycleStatus === "Draft" ? (
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              disabled={busy}
              onClick={() => void publish()}
              data-testid="campaign-publish"
            >
              انتشار
            </button>
          ) : null}
          {!isCreate && detail?.lifecycleStatus !== "Archived" ? (
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm font-bold text-amber-800"
              disabled={busy}
              onClick={() => void archive()}
              data-testid="campaign-archive"
            >
              بایگانی
            </button>
          ) : null}
        </div>
      </div>

      {message ? (
        <p
          className={`mb-4 text-sm ${messageTone === "success" ? "text-emerald-700" : "text-red-600"}`}
          data-testid="campaign-workspace-notice"
        >
          {message}
        </p>
      ) : null}

      {!isCreate ? (
        <div className="mb-4 flex flex-wrap gap-2 border-b border-border" role="tablist" aria-label="بخش‌های کمپین" data-testid="campaign-tabs">
          {TABS.map((item) => (
            <button
              key={item.id}
              type="button"
              role="tab"
              aria-selected={tab === item.id}
              className={`px-4 py-2.5 text-sm font-bold border-b-2 ${
                tab === item.id ? "border-[#2563EB] text-[#2563EB]" : "border-transparent text-muted"
              }`}
              onClick={() => setTab(item.id)}
              data-testid={`campaign-tab-${item.id}`}
            >
              {item.label}
            </button>
          ))}
        </div>
      ) : null}

      {(isCreate || tab === "info") && (
        <section className="space-y-4 rounded-2xl border border-border bg-surface-elevated p-4 md:p-6" data-testid="campaign-tab-info">
          <label className="block text-sm">
            <span className="mb-1 block font-bold">نوع کمپین</span>
            <select
              value={promotionTypeId}
              disabled={!isCreate || busy}
              onChange={(e) => setPromotionTypeId(e.target.value)}
              className="w-full max-w-md rounded-xl border border-border px-3 py-2"
              data-testid="campaign-type"
            >
              {types.map((type) => (
                <option key={type.promotionTypeId} value={type.promotionTypeId}>
                  {type.displayName}
                </option>
              ))}
            </select>
          </label>
          <div className="grid gap-4 md:grid-cols-3">
            <label className="block text-sm">
              <span className="mb-1 block font-bold">شروع</span>
              <input
                ref={startRef}
                type="datetime-local"
                value={startAt}
                onChange={(e) => setStartAt(e.target.value)}
                className="w-full rounded-xl border border-border px-3 py-2"
                data-testid="campaign-start"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-bold">پایان</span>
              <input
                type="datetime-local"
                value={endAt}
                onChange={(e) => setEndAt(e.target.value)}
                className="w-full rounded-xl border border-border px-3 py-2"
                data-testid="campaign-end"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-bold">اولویت</span>
              <input
                type="number"
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className="w-full rounded-xl border border-border px-3 py-2"
                data-testid="campaign-priority"
              />
            </label>
          </div>
          {isCreate ? (
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block text-sm">
                <span className="mb-1 block font-bold">عنوان فارسی</span>
                <input
                  ref={titleRef}
                  value={faTitle}
                  onChange={(e) => setTranslationField("fa", "title", e.target.value)}
                  className="w-full rounded-xl border border-border px-3 py-2"
                  data-testid="campaign-title-fa"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block font-bold">عنوان انگلیسی</span>
                <input
                  value={translations.find((row) => row.locale.toLowerCase().startsWith("en"))?.title ?? ""}
                  onChange={(e) => setTranslationField("en", "title", e.target.value)}
                  className="w-full rounded-xl border border-border px-3 py-2"
                  data-testid="campaign-title-en"
                />
              </label>
            </div>
          ) : null}
        </section>
      )}

      {!isCreate && tab === "members" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-4 md:p-6" data-testid="campaign-tab-members">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
            <p className="text-sm text-muted">کالاهای عضو کمپین را اضافه، حذف یا جابه‌جا کنید.</p>
            <button
              type="button"
              className="inline-flex items-center gap-2 rounded-xl bg-[#2563EB] px-3 py-2 text-sm font-bold text-white"
              disabled={busy}
              onClick={() => setPickerOpen(true)}
              data-testid="campaign-add-member"
            >
              <Plus className="h-4 w-4" />
              افزودن کالا
            </button>
          </div>
          {members.length === 0 ? (
            <p className="text-sm text-muted">هنوز کالایی اضافه نشده است.</p>
          ) : (
            <ul className="space-y-2">
              {members.map((member, index) => (
                <li
                  key={member.sellerOfferId}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border px-3 py-3"
                  data-testid={`campaign-member-${member.sellerOfferId}`}
                >
                  <div>
                    <p className="font-bold text-sm">{member.productTitle}</p>
                    <p className="mt-0.5 text-xs text-muted">{member.sellerDisplayName}</p>
                    <p className="mt-1 text-xs">
                      {formatAdminMoney(member.baseAmount, member.currency)}
                      {" · "}
                      {member.inStock
                        ? `${member.availableUnits.toLocaleString("fa-IR")} موجود`
                        : "ناموجود"}
                    </p>
                  </div>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      className="rounded-lg border p-1.5"
                      disabled={busy || index === 0}
                      onClick={() => void moveMember(member, -1)}
                      aria-label="بالا"
                      data-testid={`campaign-member-up-${member.sellerOfferId}`}
                    >
                      <ArrowUp className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      className="rounded-lg border p-1.5"
                      disabled={busy || index === members.length - 1}
                      onClick={() => void moveMember(member, 1)}
                      aria-label="پایین"
                      data-testid={`campaign-member-down-${member.sellerOfferId}`}
                    >
                      <ArrowDown className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      className="rounded-lg border p-1.5 text-red-700"
                      disabled={busy}
                      onClick={() => void removeMember(member.sellerOfferId)}
                      aria-label="حذف"
                      data-testid={`campaign-member-remove-${member.sellerOfferId}`}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}

      {!isCreate && tab === "pricing" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-4 md:p-6" data-testid="campaign-tab-pricing">
          {members.length === 0 ? (
            <p className="text-sm text-muted">ابتدا در زبانهٔ کالاها عضو اضافه کنید.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px] text-sm">
                <thead className="bg-slate-50 text-right text-muted">
                  <tr>
                    <th className="px-3 py-2 font-bold">کالا</th>
                    <th className="px-3 py-2 font-bold">قیمت عادی</th>
                    <th className="px-3 py-2 font-bold">قیمت کمپین</th>
                    <th className="px-3 py-2 font-bold">تخفیف</th>
                    <th className="px-3 py-2 font-bold">اقدام</th>
                  </tr>
                </thead>
                <tbody>
                  {members.map((member) => {
                    const draft = priceDrafts[member.sellerOfferId] ?? "";
                    const draftNum = Number(draft);
                    const preview = discountPreview(
                      member.baseAmount,
                      Number.isFinite(draftNum) && draft ? draftNum : member.campaignAmount,
                    );
                    const noDiscount =
                      Number.isFinite(draftNum) && draft !== "" && draftNum >= member.baseAmount;
                    return (
                      <tr key={member.sellerOfferId} className="border-t border-border">
                        <td className="px-3 py-3">
                          <p className="font-bold">{member.productTitle}</p>
                          <p className="text-xs text-muted">{member.sellerDisplayName}</p>
                        </td>
                        <td className="px-3 py-3">{formatAdminMoney(member.baseAmount, member.currency)}</td>
                        <td className="px-3 py-3">
                          <input
                            type="number"
                            min={1}
                            step="1"
                            value={draft}
                            onChange={(e) =>
                              setPriceDrafts((current) => ({
                                ...current,
                                [member.sellerOfferId]: e.target.value,
                              }))
                            }
                            className="w-36 rounded-xl border border-border px-2 py-1.5"
                            data-testid={`campaign-price-${member.sellerOfferId}`}
                          />
                        </td>
                        <td className="px-3 py-3">
                          {preview ? (
                            <span className="text-emerald-700">{preview}</span>
                          ) : noDiscount ? (
                            <span className="text-amber-700">بدون تخفیف مؤثر</span>
                          ) : (
                            "—"
                          )}
                        </td>
                        <td className="px-3 py-3">
                          <button
                            type="button"
                            className="rounded-lg border px-3 py-1.5 font-bold"
                            disabled={busy}
                            onClick={() => void savePrice(member)}
                            data-testid={`campaign-price-save-${member.sellerOfferId}`}
                          >
                            ذخیره قیمت
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      ) : null}

      {!isCreate && tab === "translations" ? (
        <section className="grid gap-4 md:grid-cols-2" data-testid="campaign-tab-translations">
          {(
            [
              { prefix: "fa" as const, label: "فارسی", localeHint: "fa-IR" },
              { prefix: "en" as const, label: "انگلیسی", localeHint: "en" },
            ] as const
          ).map((lang) => {
            const row =
              translations.find((item) => item.locale.toLowerCase().startsWith(lang.prefix))
              ?? emptyTranslation(lang.localeHint);
            return (
              <div key={lang.prefix} className="space-y-3 rounded-2xl border border-border bg-surface-elevated p-4">
                <h2 className="font-black">{lang.label}</h2>
                <label className="block text-sm">
                  <span className="mb-1 block font-bold">عنوان</span>
                  <input
                    ref={lang.prefix === "fa" ? titleRef : undefined}
                    value={row.title}
                    onChange={(e) => setTranslationField(lang.prefix, "title", e.target.value)}
                    className="w-full rounded-xl border border-border px-3 py-2"
                    data-testid={`campaign-trans-title-${lang.prefix}`}
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1 block font-bold">زیرعنوان</span>
                  <input
                    value={row.subtitle}
                    onChange={(e) => setTranslationField(lang.prefix, "subtitle", e.target.value)}
                    className="w-full rounded-xl border border-border px-3 py-2"
                    data-testid={`campaign-trans-subtitle-${lang.prefix}`}
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1 block font-bold">متن نشان</span>
                  <input
                    value={row.badgeText}
                    onChange={(e) => setTranslationField(lang.prefix, "badgeText", e.target.value)}
                    className="w-full rounded-xl border border-border px-3 py-2"
                    data-testid={`campaign-trans-badge-${lang.prefix}`}
                  />
                </label>
              </div>
            );
          })}
        </section>
      ) : null}

      <OfferPickerDialog
        open={pickerOpen}
        excludeIds={excludeIds}
        onClose={() => setPickerOpen(false)}
        onPick={(id) => void addMember(id)}
      />
    </main>
  );
}
