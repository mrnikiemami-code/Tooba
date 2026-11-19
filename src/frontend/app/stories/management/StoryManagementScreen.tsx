"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Eye, Send } from "lucide-react";
import { toast } from "react-toastify";
import {
  AppDataGrid,
  Dialog,
  ErrorState,
  faWorkspaceMessages,
  adminGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import { ADMIN_STORY_GRID_VIEW_KEY, createHostSavedViewStore } from "../../admin/saved-view-store";
import { formatAdminDate, type AdminLoadState } from "../../admin/admin-api";
import {
  addAdminStoryItem,
  addSellerStoryItem,
  approveAndPublishAdminStory,
  createAdminStory,
  createSellerStory,
  getAdminStory,
  getSellerStory,
  queryAdminStoriesGrid,
  querySellerStories,
  rejectAdminStory,
  scheduleAdminStory,
  submitSellerStory,
  uploadAdminStoryMedia,
  uploadAdminStoryMediaForShop,
  uploadSellerStoryMedia,
  type AdminStorySnapshot,
} from "../story-api";
import {
  canEditSellerStory,
  canSubmitStory,
  type StoryCapabilities,
} from "./story-capabilities";
import { STORY_COPY, STORY_ORIGIN_LABELS } from "./story-management-copy";
import { StoryReviewBadge, StoryStatusBadge } from "./StoryStatusBadge";
import {
  clearStoryIntentIfCurrent,
  newStoryOccurrenceKey as mintOccurrence,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent";

const SELLER_STORY_GRID_VIEW_KEY = "seller.stories.grid.v1";

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="برای این بخش مجوز ندارید. از منوی پنل خارج نشوید؛ در صورت نیاز دوباره وارد شوید."
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function PageHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

function shopLabel(row: AdminStorySnapshot): string {
  if (row.shopDisplayName?.trim()) return row.shopDisplayName.trim();
  if (!row.sellerPartyId) return "ویترین اصلی";
  return "فروشگاه";
}

function displayState(row: AdminStorySnapshot): string {
  if (row.effectivelyPublic) return "در ویترین";
  if (row.reviewStatus === "Submitted") return "منتظر تأیید";
  if (row.reviewStatus === "Rejected") return "رد شده";
  if (row.status === "Scheduled") return "زمان‌بندی‌شده";
  if (row.status === "Disabled") return "متوقف";
  if (row.status === "Draft" || row.reviewStatus === "None") return "پیش‌نویس";
  return row.status;
}

function toDatetimeLocalValue(iso: string | null): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function fromDatetimeLocalValue(value: string): string | null {
  if (!value.trim()) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

/**
 * صفحهٔ مشترک مدیریت استوری — Admin و Seller؛ فهرست Orders-style و ویرایشگر سادهٔ کاسب.
 */
export function StoryManagementScreen({ capabilities }: { capabilities: StoryCapabilities }) {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [message, setMessage] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<AdminStorySnapshot | null>(null);
  const [reviewFilter, setReviewFilter] = useState<string>("");
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectDraft, setRejectDraft] = useState("");
  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [scheduleDraft, setScheduleDraft] = useState({ startAt: "", endAt: "" });
  const [reloadToken, setReloadToken] = useState(0);
  const [draftTitle, setDraftTitle] = useState("");
  const [slideFile, setSlideFile] = useState<File | null>(null);
  const [adminShopMode, setAdminShopMode] = useState<"platform" | "shop">("platform");
  const [adminShopId, setAdminShopId] = useState("");
  const epochRef = useRef(0);
  const inFlightRef = useRef(false);

  const bumpEpoch = useCallback(() => {
    epochRef.current += 1;
  }, []);

  const refreshGrid = useCallback(() => {
    setReloadToken((v) => v + 1);
    setState("ok");
  }, []);

  useEffect(() => {
    bumpEpoch();
    return () => bumpEpoch();
  }, [bumpEpoch, capabilities.mode]);

  useEffect(() => {
    if (!selectedId) {
      setDetail(null);
      return;
    }
    const started = epochRef.current;
    const load = async () => {
      const result =
        capabilities.mode === "seller" ? await getSellerStory(selectedId) : await getAdminStory(selectedId);
      if (started !== epochRef.current) return;
      if (result.state === "ok" && result.data) setDetail(result.data);
      else if (result.state === "denied") setState("denied");
      else {
        setDetail(null);
        toast.error(result.message ?? "بارگذاری جزئیات ممکن نشد.");
      }
    };
    void load();
  }, [selectedId, capabilities.mode, reloadToken]);

  const openDetail = useCallback((id: string) => {
    setSelectedId(id);
  }, []);

  const columns = useMemo((): GridColumnDef<AdminStorySnapshot>[] => {
    const cols: GridColumnDef<AdminStorySnapshot>[] = [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        cell: (row) => (
          <button
            type="button"
            className="flex min-h-11 items-center gap-2 text-right font-bold line-clamp-2 hover:text-[#2563EB]"
            onClick={() => openDetail(row.storyId)}
            data-testid="story-row-title"
          >
            {row.coverMediaUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={row.coverMediaUrl} alt="" className="h-10 w-10 rounded object-cover" />
            ) : (
              <span className="flex h-10 w-10 items-center justify-center rounded bg-secondary text-xs">—</span>
            )}
            <span>{row.title}</span>
          </button>
        ),
        width: 240,
        minWidth: 160,
        sticky: "start",
        filterKind: "text",
        sortable: true,
      },
      {
        id: "shopDisplayName",
        header: "فروشگاه",
        accessor: (row) => shopLabel(row),
        cell: (row) => shopLabel(row),
        width: 140,
        minWidth: 110,
        filterKind: "text",
      },
      {
        id: "displayState",
        header: "وضعیت",
        accessor: (row) => displayState(row),
        cell: (row) => (
          <span className="inline-flex flex-col gap-1">
            <span className="text-sm">{displayState(row)}</span>
            <span className="flex flex-wrap gap-1">
              <StoryStatusBadge status={row.status} />
              {row.reviewStatus !== "None" ? <StoryReviewBadge reviewStatus={row.reviewStatus} /> : null}
            </span>
          </span>
        ),
        width: 160,
        minWidth: 120,
      },
      {
        id: "items",
        header: "اسلایدها",
        accessor: (row) => row.items.length,
        cell: (row) => row.items.length.toLocaleString("fa-IR"),
        width: 90,
        minWidth: 70,
      },
      {
        id: "startAt",
        header: "شروع نمایش",
        accessor: (row) => row.startAt ?? "",
        cell: (row) => (row.startAt ? formatAdminDate(row.startAt) : "—"),
        width: 120,
        minWidth: 100,
        filterKind: "date",
      },
      {
        id: "endAt",
        header: "پایان نمایش",
        accessor: (row) => row.endAt ?? "",
        cell: (row) => (row.endAt ? formatAdminDate(row.endAt) : "بدون پایان"),
        width: 120,
        minWidth: 100,
        filterKind: "date",
      },
      {
        id: "updatedAt",
        header: "آخرین تغییر",
        accessor: (row) => row.updatedAt,
        cell: (row) => formatAdminDate(row.updatedAt),
        width: 120,
        minWidth: 100,
        filterKind: "date",
        sortable: true,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => {
          const actions: AppGridRowAction<AdminStorySnapshot>[] = [
            {
              id: "view",
              label: "بررسی",
              icon: Eye,
              onClick: (r) => openDetail(r.storyId),
            },
          ];
          if (capabilities.canSubmit && canSubmitStory(row.reviewStatus)) {
            actions.push({
              id: "submit",
              label: "ارسال برای تأیید",
              icon: Send,
              onClick: (r) => void runSubmit(r),
            });
          }
          return <AppGridRowActionsCell row={row} actions={actions} compact />;
        },
        width: 120,
        minWidth: 100,
        exportable: false,
        sortable: false,
      },
    ];
    return cols;
  }, [capabilities.canSubmit, openDetail]);

  const savedViewStore = useMemo(
    () =>
      createHostSavedViewStore(
        capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
      ),
    [capabilities.mode],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      if (capabilities.mode === "seller") {
        const filterEntries = Object.entries(query.filters ?? {}).flatMap(([field, value]) => {
          if (!value || typeof value !== "object") return [];
          const operator =
            "operator" in value && typeof value.operator === "string" ? value.operator : "contains";
          const raw =
            "query" in value && typeof value.query === "string"
              ? value.query
              : "values" in value && Array.isArray(value.values)
                ? String(value.values[0] ?? "")
                : null;
          return [{ field, operator, value: raw }];
        });
        const result = await querySellerStories({
          page: query.page,
          pageSize: query.pageSize,
          search: query.search,
          filters: filterEntries,
        });
        if (result.state === "denied") {
          setState("denied");
          return { rows: [], total: 0 };
        }
        if (result.state !== "ok" || !result.data) {
          setMessage(result.message);
          setState("error");
          return { rows: [], total: 0 };
        }
        setState("ok");
        return { rows: result.data.items, total: result.data.totalCount };
      }
      return adminGridQueryAdapter(
        (serverQuery) =>
          queryAdminStoriesGrid(serverQuery, reviewFilter ? { reviewStatus: reviewFilter } : undefined),
        () => setState("denied"),
        (detailMsg) => setMessage(detailMsg),
      )(query);
    },
    [capabilities.mode, reloadToken, reviewFilter],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  async function runSubmit(row: AdminStorySnapshot) {
    if (inFlightRef.current) return;
    const occurrenceKey = mintOccurrence();
    const envelope: StoryIntentEnvelope = {
      version: 1,
      action: "submit",
      occurrenceKey,
      routeTargetId: row.storyId,
      expectedVersion: row.versionToken,
      body: { expectedVersion: row.versionToken },
      role: "seller",
      shopId: row.sellerPartyId,
      recordedAt: new Date().toISOString(),
    };
    if (!storeStoryIntent(envelope)) {
      toast.error("ذخیرهٔ امن این اقدام در مرورگر ممکن نیست؛ چیزی ارسال نشد.");
      return;
    }
    inFlightRef.current = true;
    try {
      const result = await submitSellerStory(row.storyId, null, row.versionToken, occurrenceKey);
      if (result.state === "ok") {
        clearStoryIntentIfCurrent(envelope);
        toast.success("برای تأیید ارسال شد.");
        refreshGrid();
        if (selectedId === row.storyId) setReloadToken((v) => v + 1);
      } else {
        toast.error(result.message ?? "ارسال ممکن نشد.");
      }
    } finally {
      inFlightRef.current = false;
    }
  }

  async function createDraft() {
    if (inFlightRef.current) return;
    const title = draftTitle.trim();
    if (!title) {
      toast.error("عنوان را وارد کنید.");
      return;
    }
    if (capabilities.mode === "admin" && adminShopMode === "shop" && !adminShopId.trim()) {
      toast.error("فروشگاه را انتخاب کنید یا ویترین اصلی را بگذارید.");
      return;
    }
    const occurrenceKey = mintOccurrence();
    const envelope: StoryIntentEnvelope = {
      version: 1,
      action: "create",
      occurrenceKey,
      routeTargetId: null,
      expectedVersion: null,
      body: { title },
      role: capabilities.mode,
      shopId: capabilities.mode === "seller" ? null : adminShopMode === "shop" ? adminShopId.trim() : null,
      recordedAt: new Date().toISOString(),
    };
    if (!storeStoryIntent(envelope)) {
      toast.error("ذخیرهٔ امن پیش‌نویس در مرورگر ممکن نیست؛ چیزی ارسال نشد.");
      return;
    }
    inFlightRef.current = true;
    try {
      const payload = {
        title,
        occurrenceKey,
        sellerPartyId: capabilities.mode === "admin" && adminShopMode === "shop" ? adminShopId.trim() : null,
      };
      const result =
        capabilities.mode === "seller" ? await createSellerStory(payload) : await createAdminStory(payload);
      if (!result.ok || !result.story) {
        toast.error(result.message ?? "ساخت پیش‌نویس ممکن نشد.");
        return;
      }
      clearStoryIntentIfCurrent(envelope);
      toast.success("پیش‌نویس ذخیره شد.");
      setShowCreate(false);
      setDraftTitle("");
      setSlideFile(null);
      setSelectedId(result.story.storyId);
      refreshGrid();
    } finally {
      inFlightRef.current = false;
    }
  }

  if (state === "denied") return <Denied retry={refreshGrid} />;

  const title = capabilities.mode === "seller" ? STORY_COPY.sellerTitle : STORY_COPY.adminTitle;
  const description = capabilities.mode === "seller" ? STORY_COPY.sellerDescription : STORY_COPY.adminDescription;
  const testId = capabilities.mode === "seller" ? "seller-stories" : "admin-stories";
  const sellerEditable = detail
    ? capabilities.mode === "admin" || canEditSellerStory(detail.reviewStatus)
    : false;

  return (
    <main data-testid={testId}>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title={title} description={description} />
        <div className="flex flex-wrap items-center gap-2">
          {capabilities.canReview ? (
            <label className="flex min-h-11 items-center gap-2 text-sm text-muted">
              وضعیت بررسی
              <select
                className="min-h-11 rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm text-gray-800"
                value={reviewFilter}
                onChange={(e) => {
                  setReviewFilter(e.target.value);
                  refreshGrid();
                }}
                data-testid="admin-story-review-filter"
              >
                <option value="">{STORY_COPY.filterAll}</option>
                <option value="Submitted">{STORY_COPY.filterPending}</option>
                <option value="Approved">تأییدشده</option>
                <option value="Rejected">ردشده</option>
              </select>
            </label>
          ) : null}
          {capabilities.canCreate ? (
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() => setShowCreate(true)}
              data-testid="story-create-open"
            >
              استوری جدید
            </button>
          ) : null}
        </div>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title={STORY_COPY.loadErrorTitle} detail={message} onRetry={refreshGrid} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<AdminStorySnapshot> {...gridProps} />
          )}
        </div>
      </section>

      {detail ? (
        <section className="mt-5 rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="story-detail-workspace">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold">{detail.title}</h2>
              <p className="text-sm text-muted">{shopLabel(detail)} · {displayState(detail)}</p>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <StoryStatusBadge status={detail.status} />
                {detail.reviewStatus !== "None" ? <StoryReviewBadge reviewStatus={detail.reviewStatus} /> : null}
                {capabilities.showOrigin ? (
                  <span className="text-xs text-muted">{STORY_ORIGIN_LABELS[detail.origin] ?? detail.origin}</span>
                ) : null}
              </div>
            </div>
            <button
              type="button"
              className="min-h-11 rounded-xl px-3 py-1.5 text-sm"
              onClick={() => {
                setSelectedId(null);
                setDetail(null);
              }}
            >
              برگشت به فهرست
            </button>
          </div>

          {detail.rejectionReason ? (
            <div className="mb-4 rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800" data-testid="story-rejection-reason">
              <span className="font-bold">دلیل رد: </span>
              {detail.rejectionReason}
            </div>
          ) : null}

          <div className="mb-5 flex flex-wrap gap-2">
            {capabilities.canReview && detail.reviewStatus === "Submitted" ? (
              <>
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                  onClick={() => {
                    const key = mintOccurrence();
                    void approveAndPublishAdminStory(detail.storyId, detail.versionToken, key).then((result) => {
                      if (result.state === "ok") {
                        toast.success("تأیید و منتشر شد.");
                        refreshGrid();
                        setReloadToken((v) => v + 1);
                      } else toast.error(result.message ?? "تأیید ممکن نشد.");
                    });
                  }}
                >
                  تأیید و انتشار
                </button>
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-rose-600 px-4 py-2 text-sm font-bold text-white"
                  onClick={() => setRejectOpen(true)}
                >
                  رد با دلیل
                </button>
              </>
            ) : null}
            {capabilities.canSchedule ? (
              <button
                type="button"
                className="min-h-11 rounded-xl border border-gray-200 px-4 py-2 text-sm"
                onClick={() => {
                  setScheduleDraft({
                    startAt: toDatetimeLocalValue(detail.startAt),
                    endAt: toDatetimeLocalValue(detail.endAt),
                  });
                  setScheduleOpen(true);
                }}
              >
                زمان‌بندی نمایش
              </button>
            ) : null}
            {capabilities.canSubmit && canSubmitStory(detail.reviewStatus) ? (
              <button
                type="button"
                className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => void runSubmit(detail)}
              >
                ارسال برای تأیید
              </button>
            ) : null}
          </div>

          <h3 className="mb-2 text-sm font-bold">اسلایدها ({detail.items.length.toLocaleString("fa-IR")})</h3>
          <ul className="mb-4 space-y-2 text-sm">
            {detail.items.map((item) => (
              <li key={item.storyItemId} className="flex items-center gap-3 rounded-xl border border-gray-100 px-3 py-2">
                {item.mediaUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={item.mediaUrl} alt="" className="h-12 w-12 rounded object-cover" />
                ) : null}
                <span>{item.mediaType === "video" ? "ویدیو" : "عکس"}</span>
              </li>
            ))}
          </ul>

          {capabilities.canEdit && sellerEditable ? (
            <div className="space-y-3">
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">عکس یا ویدیو</span>
                <input
                  type="file"
                  accept="image/*,video/*"
                  className="min-h-11 w-full text-sm"
                  onChange={(e) => setSlideFile(e.target.files?.[0] ?? null)}
                  data-testid="story-slide-file"
                />
              </label>
              <button
                type="button"
                className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                disabled={!slideFile || inFlightRef.current}
                onClick={() => {
                  if (!slideFile || !detail) return;
                  void (async () => {
                    if (inFlightRef.current) return;
                    inFlightRef.current = true;
                    const uploadKey = mintOccurrence();
                    try {
                      const uploaded =
                        capabilities.mode === "seller"
                          ? await uploadSellerStoryMedia(slideFile, uploadKey)
                          : detail.sellerPartyId
                            ? await uploadAdminStoryMediaForShop(slideFile, detail.sellerPartyId, uploadKey)
                            : await uploadAdminStoryMedia(slideFile, uploadKey);
                      if (uploaded.state !== "ok" || !uploaded.data) {
                        toast.error(uploaded.message ?? "آپلود فایل ممکن نشد.");
                        return;
                      }
                      const itemKey = mintOccurrence();
                      const mediaType = uploaded.data.contentType.startsWith("video/") ? "video" : "image";
                      const added =
                        capabilities.mode === "seller"
                          ? await addSellerStoryItem(detail.storyId, {
                              mediaType,
                              mediaAssetId: uploaded.data.mediaAssetId,
                              mediaUrl: uploaded.data.displayUrl,
                              expectedVersion: detail.versionToken,
                              occurrenceKey: itemKey,
                            })
                          : await addAdminStoryItem(detail.storyId, {
                              mediaType,
                              mediaAssetId: uploaded.data.mediaAssetId,
                              mediaUrl: uploaded.data.displayUrl,
                              expectedVersion: detail.versionToken,
                              occurrenceKey: itemKey,
                            });
                      if (added.state === "ok") {
                        toast.success("اسلاید اضافه شد.");
                        setSlideFile(null);
                        refreshGrid();
                        setReloadToken((v) => v + 1);
                      } else {
                        toast.error(added.message ?? "افزودن اسلاید ممکن نشد.");
                      }
                    } finally {
                      inFlightRef.current = false;
                    }
                  })();
                }}
              >
                افزودن اسلاید
              </button>
            </div>
          ) : null}
        </section>
      ) : null}

      <Dialog open={showCreate} onClose={() => setShowCreate(false)} title="استوری جدید">
        <div className="space-y-3 p-1">
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">عنوان</span>
            <input
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={draftTitle}
              onChange={(e) => setDraftTitle(e.target.value)}
              data-testid="story-create-title"
            />
          </label>
          {capabilities.mode === "admin" ? (
            <fieldset className="space-y-2 text-sm">
              <legend className="mb-1 text-gray-600">فروشگاه</legend>
              <label className="flex min-h-11 items-center gap-2">
                <input
                  type="radio"
                  checked={adminShopMode === "platform"}
                  onChange={() => setAdminShopMode("platform")}
                />
                ویترین اصلی
              </label>
              <label className="flex min-h-11 items-center gap-2">
                <input type="radio" checked={adminShopMode === "shop"} onChange={() => setAdminShopMode("shop")} />
                فروشگاه مشخص
              </label>
              {adminShopMode === "shop" ? (
                <input
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  placeholder="شناسهٔ فروشگاه فعال"
                  value={adminShopId}
                  onChange={(e) => setAdminShopId(e.target.value)}
                  data-testid="story-admin-shop-id"
                />
              ) : null}
            </fieldset>
          ) : null}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>
              انصراف
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() => void createDraft()}
              data-testid="story-create-save"
            >
              ذخیره پیش‌نویس
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog open={rejectOpen} onClose={() => setRejectOpen(false)} title="رد با دلیل">
        <div className="space-y-3 p-1">
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">دلیل رد</span>
            <input
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={rejectDraft}
              onChange={(e) => setRejectDraft(e.target.value)}
              data-testid="story-reject-reason-input"
            />
          </label>
          <div className="flex justify-end gap-2">
            <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setRejectOpen(false)}>
              انصراف
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-rose-600 px-4 py-2 text-sm font-bold text-white"
              onClick={() => {
                if (!detail || !rejectDraft.trim()) {
                  toast.error("دلیل رد را بنویسید.");
                  return;
                }
                const key = mintOccurrence();
                void rejectAdminStory(detail.storyId, rejectDraft.trim(), detail.versionToken, key).then((result) => {
                  if (result.state === "ok") {
                    toast.success("رد شد.");
                    setRejectOpen(false);
                    refreshGrid();
                    setReloadToken((v) => v + 1);
                  } else toast.error(result.message ?? "رد ممکن نشد.");
                });
              }}
            >
              ثبت رد
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog open={scheduleOpen} onClose={() => setScheduleOpen(false)} title="زمان‌بندی نمایش">
        <div className="space-y-3 p-1">
          <p className="text-sm text-muted">اگر پایان خالی بماند، تا زمان توقف دستی در ویترین می‌ماند.</p>
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">شروع</span>
            <input
              type="datetime-local"
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={scheduleDraft.startAt}
              onChange={(e) => setScheduleDraft((c) => ({ ...c, startAt: e.target.value }))}
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">پایان (اختیاری)</span>
            <input
              type="datetime-local"
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={scheduleDraft.endAt}
              onChange={(e) => setScheduleDraft((c) => ({ ...c, endAt: e.target.value }))}
            />
          </label>
          <div className="flex justify-end gap-2">
            <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setScheduleOpen(false)}>
              انصراف
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() => {
                if (!detail) return;
                const key = mintOccurrence();
                void scheduleAdminStory(detail.storyId, {
                  startAt: fromDatetimeLocalValue(scheduleDraft.startAt),
                  endAt: fromDatetimeLocalValue(scheduleDraft.endAt),
                  expectedVersion: detail.versionToken,
                  occurrenceKey: key,
                }).then((result) => {
                  if (result.state === "ok") {
                    toast.success("زمان‌بندی ذخیره شد.");
                    setScheduleOpen(false);
                    refreshGrid();
                    setReloadToken((v) => v + 1);
                  } else toast.error(result.message ?? "زمان‌بندی ممکن نشد.");
                });
              }}
            >
              ذخیره زمان‌بندی
            </button>
          </div>
        </div>
      </Dialog>
    </main>
  );
}
