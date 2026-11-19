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
import { ADMIN_STORY_GRID_VIEW_KEY } from "../../admin/saved-view-store";
import { formatAdminDate, type AdminLoadState } from "../../admin/admin-api";
import { ADMIN_ACTOR_STORAGE_KEY, DEFAULT_ADMIN_ACTOR_ID } from "../../../lib/admin/admin-result";
import { loadAdminSellers, type AdminSellerRow } from "../../../features/admin-sellers/api/sellers-api";
import { readActorUserId, readSellerPartyId } from "../../vendor-panel/seller-api";
import { JalaliDatePicker } from "../../../design-system/app-data-grid/jalali-date-picker";
import {
  getAdminStory,
  getSellerStory,
  queryAdminStoriesGrid,
  querySellerStories,
  type AdminStorySnapshot,
} from "../story-api";
import { type StoryCapabilities } from "./story-capabilities";
import { STORY_COPY, STORY_ORIGIN_LABELS } from "./story-management-copy";
import { StoryReviewBadge, StoryStatusBadge } from "./StoryStatusBadge";
import { runDurableStoryMutation, runDurableUploadAdd, replayPendingStoryIntent } from "./story-executor";
import { loadStoryLocaleOptions, loadStoryProductOptions, type StoryProductOptionRow } from "./story-lookups";
import {
  isoToScheduleDraft,
  parseStoryScheduleDraft,
  type StoryScheduleDraft,
} from "./story-schedule-time";
import {
  listPendingStoryIntents,
  newStoryOccurrenceKey as mintOccurrence,
  pendingStoryLabel,
  type StoryIntentEnvelope,
} from "./story-intent";
import { readStoryScope, subscribeStoryScope } from "./story-scope";
import { createScopedStorySavedViewStore } from "./story-saved-views";

const SELLER_STORY_GRID_VIEW_KEY = "seller.stories.grid.v1";

function hasAction(row: AdminStorySnapshot, code: string): boolean {
  return (row.actionCodes ?? []).includes(code);
}

function resolveActorId(mode: "admin" | "seller"): string {
  if (mode === "seller") return readActorUserId() ?? "";
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

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

/**
 * صفحهٔ مشترک مدیریت استوری — Admin و Seller؛ فهرست Orders-style و ویرایشگر سادهٔ کاسب.
 */
export function StoryManagementScreen({
  capabilities,
  initialStoryId = null,
  createMode = false,
  navigate,
}: {
  capabilities: StoryCapabilities;
  initialStoryId?: string | null;
  createMode?: boolean;
  /** Optional navigation (pages wire Next router; tests inject a stub). */
  navigate?: (href: string) => void;
}) {
  const router = {
    push: (href: string) => {
      if (navigate) navigate(href);
      else if (typeof window !== "undefined") window.history.pushState({}, "", href);
    },
  };
  const basePath = capabilities.mode === "seller" ? "/vendor-panel/stories" : "/admin/stories";
  // continue as single body — keep previous Body signature by aliasing
  return (
    <StoryManagementScreenBody
      capabilities={capabilities}
      initialStoryId={initialStoryId}
      createMode={createMode}
      navigate={navigate}
      router={router}
      basePath={basePath}
    />
  );
}

function StoryManagementScreenBody({
  capabilities,
  initialStoryId = null,
  createMode = false,
  navigate,
  router,
  basePath,
}: {
  capabilities: StoryCapabilities;
  initialStoryId?: string | null;
  createMode?: boolean;
  navigate?: (href: string) => void;
  router: { push: (href: string) => void };
  basePath: string;
}) {
  void navigate;
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [message, setMessage] = useState<string>();
  const [showCreate, setShowCreate] = useState(createMode);
  const [selectedId, setSelectedId] = useState<string | null>(initialStoryId);
  const [detail, setDetail] = useState<AdminStorySnapshot | null>(null);
  const [reviewFilter, setReviewFilter] = useState<string>("");
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectDraft, setRejectDraft] = useState("");
  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [scheduleMode, setScheduleMode] = useState<"schedule" | "approve_schedule">("schedule");
  const [scheduleDraft, setScheduleDraft] = useState<StoryScheduleDraft>({
    startDayIso: "",
    startTime: "10:00",
    endDayIso: "",
    endTime: "",
  });
  const [scheduleFieldError, setScheduleFieldError] = useState<string | null>(null);
  const [scopeTick, setScopeTick] = useState(0);
  const [recoveryFile, setRecoveryFile] = useState<File | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const [draftTitle, setDraftTitle] = useState("");
  const [slideFile, setSlideFile] = useState<File | null>(null);
  const [adminShopMode, setAdminShopMode] = useState<"platform" | "shop">("platform");
  const [adminShopId, setAdminShopId] = useState("");
  const [adminShopName, setAdminShopName] = useState("");
  const [shopOptions, setShopOptions] = useState<AdminSellerRow[]>([]);
  const [shopSearch, setShopSearch] = useState("");
  const [pendingActs, setPendingActs] = useState<StoryIntentEnvelope[]>([]);
  const [previewConfirm, setPreviewConfirm] = useState<"publish" | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editLocale, setEditLocale] = useState("");
  const [localeOptions, setLocaleOptions] = useState<Array<{ code: string; name: string; isDefault: boolean }>>([]);
  const [productOptions, setProductOptions] = useState<StoryProductOptionRow[]>([]);
  const [productSearch, setProductSearch] = useState("");
  const [slideCaption, setSlideCaption] = useState("");
  const [slideDurationSec, setSlideDurationSec] = useState("5");
  const [slideCtaType, setSlideCtaType] = useState("none");
  const [slideCtaTarget, setSlideCtaTarget] = useState("");
  const [dirty, setDirty] = useState(false);
  const epochRef = useRef(0);
  const inFlightRef = useRef(false);
  const scope = readStoryScope(capabilities.mode);
  const sellerShopId = scope.shopId;
  const actorId = scope.actorUserId || resolveActorId(capabilities.mode);
  const gate = { inFlightRef, epochRef };

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
  }, [bumpEpoch, capabilities.mode, sellerShopId, actorId, selectedId, scopeTick]);

  useEffect(() => {
    return subscribeStoryScope(capabilities.mode, () => {
      bumpEpoch();
      setDetail(null);
      setSelectedId(null);
      setShowCreate(false);
      setPendingActs([]);
      setScopeTick((v) => v + 1);
      setReloadToken((v) => v + 1);
    });
  }, [capabilities.mode, bumpEpoch]);

  useEffect(() => {
    const pending = listPendingStoryIntents(capabilities.mode, sellerShopId, actorId)
      .filter((row) => row.state === "ok")
      .map((row) => row.envelope);
    setPendingActs(pending);
  }, [capabilities.mode, sellerShopId, actorId, reloadToken, scopeTick]);

  useEffect(() => {
    if (capabilities.mode !== "admin") return;
    void loadAdminSellers().then((result) => {
      if (result.state === "ok" && result.data) {
        setShopOptions(result.data.filter((row) => row.status.toLowerCase() === "active"));
      }
    });
  }, [capabilities.mode]);

  useEffect(() => {
    void loadStoryLocaleOptions({
      mode: capabilities.mode,
      shopId: sellerShopId,
      actorUserId: actorId,
    }).then((rows) => {
      setLocaleOptions(rows);
      const def = rows.find((row) => row.isDefault) ?? rows[0];
      if (def) setEditLocale((prev) => prev || def.code);
    });
  }, [capabilities.mode, sellerShopId, actorId, scopeTick]);

  useEffect(() => {
    const shop =
      capabilities.mode === "seller"
        ? sellerShopId
        : detail?.sellerPartyId ?? (adminShopMode === "shop" ? adminShopId : null);
    void loadStoryProductOptions({
      mode: capabilities.mode,
      shopId: shop,
      actorUserId: actorId,
    }).then((rows) => setProductOptions(rows));
  }, [capabilities.mode, sellerShopId, detail?.sellerPartyId, adminShopId, adminShopMode, actorId, scopeTick]);

  useEffect(() => {
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!dirty) return;
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [dirty]);

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
      if (result.state === "ok" && result.data) {
        setDetail(result.data);
        setEditTitle(result.data.title);
        setEditLocale(result.data.locale || editLocale);
        setDirty(false);
      } else if (result.state === "denied") setState("denied");
      else {
        setDetail(null);
        toast.error(result.message ?? "بارگذاری جزئیات ممکن نشد.");
      }
    };
    void load();
  }, [selectedId, capabilities.mode, reloadToken]);

  const openDetail = useCallback(
    (id: string) => {
      setSelectedId(id);
      router.push(`${basePath}/${encodeURIComponent(id)}`);
    },
    [basePath, router],
  );

  async function runSubmit(row: AdminStorySnapshot) {
    if (inFlightRef.current) return;
    if (!actorId) {
      toast.error("شناسهٔ کاربر برای ثبت امن اقدام مشخص نیست.");
      return;
    }
    const occurrenceKey = mintOccurrence();
    const body = { expectedVersion: row.versionToken, occurrenceKey };
    const envelope: StoryIntentEnvelope = {
      version: 2,
      action: "submit",
      occurrenceKey,
      method: "POST",
      path: `/v1/seller/stories/${row.storyId}/submit`,
      routeTargetId: row.storyId,
      expectedVersion: row.versionToken,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: "seller",
      shopId: row.sellerPartyId,
      actorUserId: actorId,
      recordedAt: new Date().toISOString(),
    };
    const outcome = await runDurableStoryMutation({
      gate,
      envelope,
      scope: { role: "seller", shopId: row.sellerPartyId, actorUserId: actorId },
    });
    if (outcome.state === "ok") {
      toast.success("برای تأیید ارسال شد.");
      refreshGrid();
      if (selectedId === row.storyId) setReloadToken((v) => v + 1);
    } else if (outcome.state === "failed") toast.error(outcome.message);
    else toast.error("ارسال انجام نشد.");
  }

  async function createDraft() {
    if (inFlightRef.current) return;
    if (!actorId) {
      toast.error("شناسهٔ کاربر برای ثبت امن اقدام مشخص نیست.");
      return;
    }
    const title = draftTitle.trim();
    if (!title) {
      toast.error("عنوان را وارد کنید.");
      return;
    }
    if (capabilities.mode === "admin" && adminShopMode === "shop" && !adminShopId.trim()) {
      toast.error("فروشگاه را از فهرست نام‌ها انتخاب کنید.");
      return;
    }
    const occurrenceKey = mintOccurrence();
    const shopId =
      capabilities.mode === "admin" && adminShopMode === "shop" ? adminShopId.trim() : sellerShopId;
    const body = {
      title,
      occurrenceKey,
      sellerPartyId: capabilities.mode === "admin" && adminShopMode === "shop" ? adminShopId.trim() : null,
    };
    const envelope: StoryIntentEnvelope = {
      version: 2,
      action: "create",
      occurrenceKey,
      method: "POST",
      path: capabilities.mode === "seller" ? "/v1/seller/stories" : "/v1/admin/stories",
      routeTargetId: null,
      expectedVersion: null,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: capabilities.mode,
      shopId,
      actorUserId: actorId,
      recordedAt: new Date().toISOString(),
    };
    const outcome = await runDurableStoryMutation({
      gate,
      envelope,
      scope: { role: capabilities.mode, shopId, actorUserId: actorId },
    });
    if (outcome.state === "ok") {
      const payload = outcome.data as { storyId?: string; data?: { storyId?: string } };
      const id = payload?.storyId ?? payload?.data?.storyId;
      toast.success("پیش‌نویس ذخیره شد.");
      setShowCreate(false);
      setDraftTitle("");
      setSlideFile(null);
      if (id) openDetail(String(id));
      refreshGrid();
    } else if (outcome.state === "failed") toast.error(outcome.message);
    else toast.error("ارسال انجام نشد.");
  }

  if (state === "denied") return <Denied retry={refreshGrid} />;

  const title = capabilities.mode === "seller" ? STORY_COPY.sellerTitle : STORY_COPY.adminTitle;
  const description = capabilities.mode === "seller" ? STORY_COPY.sellerDescription : STORY_COPY.adminDescription;
  const testId = capabilities.mode === "seller" ? "seller-stories" : "admin-stories";
  const sellerEditable = detail ? hasAction(detail, "update") || hasAction(detail, "add_item") : false;
  const filteredShops = shopOptions.filter((row) => {
    const q = shopSearch.trim();
    if (!q) return true;
    return row.displayName.includes(q) || row.sellerPartyId.includes(q);
  });

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
              onClick={() => {
                setShowCreate(true);
                router.push(`${basePath}/new`);
              }}
              data-testid="story-create-open"
            >
              استوری جدید
            </button>
          ) : null}
        </div>
      </div>

      {pendingActs.length > 0 ? (
        <section className="mb-4 rounded-2xl border border-amber-200 bg-amber-50 p-4" data-testid="story-pending-recovery">
          <h2 className="mb-2 text-sm font-bold text-amber-900">اقدام ناتمام</h2>
          <ul className="space-y-2 text-sm text-amber-900">
            {pendingActs.map((act) => (
              <li key={`${act.action}:${act.occurrenceKey}`} className="flex flex-wrap items-center justify-between gap-2">
                <span>{pendingStoryLabel(act)}</span>
                <div className="flex flex-wrap items-center gap-2">
                  {(act.action === "upload" || act.action === "upload_add") && !act.children[0]?.done ? (
                    <input
                      type="file"
                      accept="image/*,video/*"
                      className="min-h-11 text-sm"
                      data-testid="story-recovery-file"
                      onChange={(e) => setRecoveryFile(e.target.files?.[0] ?? null)}
                    />
                  ) : null}
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-amber-700 px-3 py-1.5 text-white"
                  data-testid="story-recovery-retry"
                  onClick={() => {
                    void replayPendingStoryIntent({
                      gate,
                      envelope: act,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                      uploadFile: recoveryFile,
                    }).then((outcome) => {
                      if (outcome.state === "ok") {
                        toast.success("اقدام ناتمام با موفقیت ادامه یافت.");
                        setRecoveryFile(null);
                        refreshGrid();
                        setReloadToken((v) => v + 1);
                      } else if (outcome.state === "failed") toast.error(outcome.message);
                      else toast.error("ارسال مجدد ممکن نشد.");
                    });
                  }}
                >
                  ادامه / ارسال مجدد
                </button>
                </div>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {!selectedId && !showCreate ? (
        <StoryLiveGrid
          capabilities={capabilities}
          reviewFilter={reviewFilter}
          reloadToken={reloadToken}
          onDenied={() => setState("denied")}
          onError={(detailMsg) => {
            setMessage(detailMsg);
            setState("error");
          }}
          onOk={() => setState("ok")}
          onOpenDetail={openDetail}
          onSubmit={(row) => void runSubmit(row)}
          errorMessage={message}
          onRetry={refreshGrid}
          showError={state === "error"}
        />
      ) : null}

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
                router.push(basePath);
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
            {hasAction(detail, "approve_and_publish") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                onClick={() => setPreviewConfirm("publish")}
              >
                تأیید و انتشار
              </button>
            ) : null}
            {hasAction(detail, "approve_and_schedule") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl border border-emerald-600 px-4 py-2 text-sm font-bold text-emerald-700"
                onClick={() => {
                  setScheduleMode("approve_schedule");
                  const start = isoToScheduleDraft(detail.startAt);
                  const end = isoToScheduleDraft(detail.endAt);
                  setScheduleFieldError(null);
                  setScheduleDraft({
                    startDayIso: start.dayIso,
                    startTime: start.time || "10:00",
                    endDayIso: end.dayIso,
                    endTime: end.time,
                  });
                  setScheduleOpen(true);
                }}
              >
                تأیید و زمان‌بندی
              </button>
            ) : null}
            {hasAction(detail, "reject") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl bg-rose-600 px-4 py-2 text-sm font-bold text-white"
                onClick={() => setRejectOpen(true)}
              >
                رد با دلیل
              </button>
            ) : null}
            {hasAction(detail, "schedule") || hasAction(detail, "publish_now") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl border border-gray-200 px-4 py-2 text-sm"
                onClick={() => {
                  setScheduleMode("schedule");
                  const start = isoToScheduleDraft(detail.startAt);
                  const end = isoToScheduleDraft(detail.endAt);
                  setScheduleFieldError(null);
                  setScheduleDraft({
                    startDayIso: start.dayIso,
                    startTime: start.time || "10:00",
                    endDayIso: end.dayIso,
                    endTime: end.time,
                  });
                  setScheduleOpen(true);
                }}
              >
                زمان‌بندی نمایش
              </button>
            ) : null}
            {hasAction(detail, "stop") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl border border-rose-200 px-4 py-2 text-sm text-rose-700"
                onClick={() => {
                  if (!actorId) return;
                  const key = mintOccurrence();
                  const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "stop",
                    occurrenceKey: key,
                    method: "POST",
                    path:
                      capabilities.mode === "seller"
                        ? `/v1/seller/stories/${detail.storyId}/stop`
                        : `/v1/admin/stories/${detail.storyId}/disable`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: capabilities.mode,
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      toast.success("متوقف شد.");
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("توقف ممکن نشد.");
                  });
                }}
              >
                توقف نمایش
              </button>
            ) : null}
            {hasAction(detail, "return_to_draft") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl border border-gray-200 px-4 py-2 text-sm"
                onClick={() => {
                  if (!actorId) return;
                  const key = mintOccurrence();
                  const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "return_to_draft",
                    occurrenceKey: key,
                    method: "POST",
                    path: `/v1/seller/stories/${detail.storyId}/return-to-draft`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: "seller",
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: "seller", shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      toast.success("به پیش‌نویس برگشت.");
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("بازگشت ممکن نشد.");
                  });
                }}
              >
                بازگشت به پیش‌نویس
              </button>
            ) : null}
            {hasAction(detail, "submit") ? (
              <button
                type="button"
                className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => void runSubmit(detail)}
              >
                ارسال برای تأیید
              </button>
            ) : null}
          </div>

          {capabilities.canEdit && sellerEditable ? (
            <div className="mb-4 grid gap-3 md:grid-cols-2">
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">عنوان</span>
                <input
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={editTitle}
                  onChange={(e) => {
                    setEditTitle(e.target.value);
                    setDirty(true);
                  }}
                  data-testid="story-edit-title"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">زبان</span>
                <select
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={editLocale}
                  onChange={(e) => {
                    setEditLocale(e.target.value);
                    setDirty(true);
                  }}
                  data-testid="story-edit-locale"
                >
                  {localeOptions.map((row) => (
                    <option key={row.code} value={row.code}>
                      {row.name}
                      {row.isDefault ? " (پیش‌فرض)" : ""}
                    </option>
                  ))}
                </select>
              </label>
              <button
                type="button"
                className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white md:col-span-2"
                onClick={() => {
                  if (!detail || !actorId) return;
                  const key = mintOccurrence();
                  const body = {
                    title: editTitle.trim(),
                    locale: editLocale || null,
                    expectedVersion: detail.versionToken,
                    occurrenceKey: key,
                  };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "update",
                    occurrenceKey: key,
                    method: "PUT",
                    path:
                      capabilities.mode === "seller"
                        ? `/v1/seller/stories/${detail.storyId}`
                        : `/v1/admin/stories/${detail.storyId}`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: capabilities.mode,
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      toast.success("ذخیره شد.");
                      setDirty(false);
                      setReloadToken((v) => v + 1);
                      refreshGrid();
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("ارسال انجام نشد.");
                  });
                }}
              >
                ذخیره عنوان و زبان
              </button>
            </div>
          ) : null}

          <h3 className="mb-2 text-sm font-bold">اسلایدها ({detail.items.length.toLocaleString("fa-IR")})</h3>
          <ul className="mb-4 space-y-2 text-sm">
            {detail.items.map((item, index) => (
              <li key={item.storyItemId} className="rounded-xl border border-gray-100 px-3 py-2" data-testid="story-slide-row">
                <div className="flex flex-wrap items-center gap-3">
                  {item.mediaType === "video" && item.mediaUrl ? (
                    // eslint-disable-next-line jsx-a11y/media-has-caption
                    <video src={item.mediaUrl} className="h-16 w-16 rounded object-cover" muted playsInline controls />
                  ) : item.mediaUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={item.mediaUrl} alt="" className="h-16 w-16 rounded object-cover" />
                  ) : null}
                  <div className="min-w-0 flex-1">
                    <p>{item.mediaType === "video" ? "ویدیو" : "عکس"}</p>
                    {item.caption ? <p className="text-muted">{item.caption}</p> : null}
                  </div>
                  {capabilities.canEdit && sellerEditable ? (
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        className="min-h-11 rounded-xl border border-gray-200 px-3 py-1.5 text-sm"
                        disabled={index === 0}
                        onClick={() => {
                          if (!actorId) return;
                          const ids = detail.items.map((row) => row.storyItemId);
                          const next = [...ids];
                          const tmp = next[index - 1]!;
                          next[index - 1] = next[index]!;
                          next[index] = tmp;
                          const key = mintOccurrence();
                          const body = { itemIds: next, expectedVersion: detail.versionToken, occurrenceKey: key };
                          const envelope: StoryIntentEnvelope = {
                            version: 2,
                            action: "reorder_items",
                            occurrenceKey: key,
                            method: "PUT",
                            path:
                              capabilities.mode === "seller"
                                ? `/v1/seller/stories/${detail.storyId}/items/reorder`
                                : `/v1/admin/stories/${detail.storyId}/items/reorder`,
                            routeTargetId: detail.storyId,
                            expectedVersion: detail.versionToken,
                            bodyText: JSON.stringify(body),
                            uploadMeta: null,
                            addBodyText: null,
                            uploadedAssetId: null,
                            children: [],
                            role: capabilities.mode,
                            shopId: detail.sellerPartyId ?? sellerShopId,
                            actorUserId: actorId,
                            recordedAt: new Date().toISOString(),
                          };
                          void runDurableStoryMutation({
                            gate,
                            envelope,
                            scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                          }).then((outcome) => {
                            if (outcome.state === "ok") {
                              toast.success("ترتیب به‌روز شد.");
                              setReloadToken((v) => v + 1);
                            } else if (outcome.state === "failed") toast.error(outcome.message);
                            else toast.error("جابه‌جایی ممکن نشد.");
                          });
                        }}
                      >
                        بالاتر
                      </button>
                      <button
                        type="button"
                        className="min-h-11 rounded-xl border border-rose-200 px-3 py-1.5 text-sm text-rose-700"
                        onClick={() => {
                          if (!actorId) return;
                          const key = mintOccurrence();
                          const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                          const envelope: StoryIntentEnvelope = {
                            version: 2,
                            action: "remove_item",
                            occurrenceKey: key,
                            method: "DELETE",
                            path:
                              capabilities.mode === "seller"
                                ? `/v1/seller/stories/${detail.storyId}/items/${item.storyItemId}`
                                : `/v1/admin/stories/${detail.storyId}/items/${item.storyItemId}`,
                            routeTargetId: detail.storyId,
                            expectedVersion: detail.versionToken,
                            bodyText: JSON.stringify(body),
                            uploadMeta: null,
                            addBodyText: null,
                            uploadedAssetId: null,
                            children: [],
                            role: capabilities.mode,
                            shopId: detail.sellerPartyId ?? sellerShopId,
                            actorUserId: actorId,
                            recordedAt: new Date().toISOString(),
                          };
                          void runDurableStoryMutation({
                            gate,
                            envelope,
                            scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                          }).then((outcome) => {
                            if (outcome.state === "ok") {
                              toast.success("اسلاید حذف شد.");
                              setReloadToken((v) => v + 1);
                              refreshGrid();
                            } else if (outcome.state === "failed") toast.error(outcome.message);
                            else toast.error("حذف ممکن نشد.");
                          });
                        }}
                      >
                        حذف
                      </button>
                    </div>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>

          {capabilities.canEdit && sellerEditable ? (
            <div className="space-y-3" data-testid="story-slide-editor">
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
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">زیرنویس</span>
                <input
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={slideCaption}
                  onChange={(e) => setSlideCaption(e.target.value)}
                  data-testid="story-slide-caption"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">مدت نمایش عکس (ثانیه)</span>
                <input
                  type="number"
                  min={1}
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={slideDurationSec}
                  onChange={(e) => setSlideDurationSec(e.target.value)}
                  data-testid="story-slide-duration"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block text-gray-600">مقصد دکمه</span>
                <select
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={slideCtaType}
                  onChange={(e) => {
                    setSlideCtaType(e.target.value);
                    setSlideCtaTarget("");
                  }}
                  data-testid="story-slide-cta-type"
                >
                  <option value="none">بدون دکمه</option>
                  <option value="product">محصول فروشگاه</option>
                  <option value="external">آدرس وب‌سایت</option>
                </select>
              </label>
              {slideCtaType === "product" ? (
                <div className="space-y-2" data-testid="story-product-picker">
                  <input
                    className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    placeholder="جستجوی نام محصول"
                    value={productSearch}
                    onChange={(e) => setProductSearch(e.target.value)}
                  />
                  <ul className="max-h-40 overflow-auto rounded-xl border border-gray-100">
                    {productOptions
                      .filter((row) => !productSearch.trim() || row.productTitle.includes(productSearch.trim()))
                      .slice(0, 20)
                      .map((row) => (
                        <li key={row.offerId}>
                          <button
                            type="button"
                            className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm hover:bg-secondary"
                            onClick={() => {
                              if (!row.productId) {
                                toast.error("این ردیف شناسهٔ محصول معتبری ندارد.");
                                return;
                              }
                              setSlideCtaTarget(row.productId);
                            }}
                          >
                            {row.productTitle}
                          </button>
                        </li>
                      ))}
                  </ul>
                  {slideCtaTarget ? <p className="text-xs text-muted">محصول انتخاب شد.</p> : null}
                </div>
              ) : null}
              {slideCtaType === "external" ? (
                <input
                  className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  dir="ltr"
                  placeholder="https://..."
                  value={slideCtaTarget}
                  onChange={(e) => setSlideCtaTarget(e.target.value)}
                  data-testid="story-slide-cta-external"
                />
              ) : null}
              <button
                type="button"
                className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                disabled={!slideFile || inFlightRef.current}
                onClick={() => {
                  if (!slideFile || !detail || !actorId) return;
                  void (async () => {
                    if (inFlightRef.current) return;
                    const uploadKey = mintOccurrence();
                    const itemKey = mintOccurrence();
                    const plannedAdd = {
                      caption: slideCaption.trim() || null,
                      durationSec: Math.max(1, Number(slideDurationSec) || 5),
                      ctaType: slideCtaType,
                      ctaTarget: slideCtaType === "none" ? null : slideCtaTarget || null,
                      expectedVersion: detail.versionToken,
                      itemOccurrenceKey: itemKey,
                    };
                    // Lock + store begin before async digest (executor owns the mutex).
                    const uploadEnvelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "upload_add",
                      occurrenceKey: uploadKey,
                      method: "POST",
                      path:
                        capabilities.mode === "seller"
                          ? "/v1/seller/media/upload-story"
                          : detail.sellerPartyId
                            ? "/v1/admin/media/upload-for-shop"
                            : "/v1/admin/media/upload-story-platform",
                      routeTargetId: detail.storyId,
                      expectedVersion: detail.versionToken,
                      bodyText: JSON.stringify(plannedAdd),
                      uploadMeta: {
                        fileName: slideFile.name,
                        fileType: slideFile.type,
                        fileSize: slideFile.size,
                        digestHex: "pending",
                      },
                      addBodyText: null,
                      uploadedAssetId: null,
                      children: [
                        { step: "upload", occurrenceKey: uploadKey, done: false },
                        { step: "add", occurrenceKey: itemKey, done: false },
                      ],
                      role: capabilities.mode,
                      shopId: detail.sellerPartyId ?? sellerShopId,
                      actorUserId: actorId,
                      recordedAt: new Date().toISOString(),
                    };
                    const outcome = await runDurableUploadAdd({
                      gate,
                      envelope: uploadEnvelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                      uploadFile: slideFile,
                    });
                    if (outcome.state === "ok") {
                      toast.success("اسلاید اضافه شد.");
                      setSlideFile(null);
                      setSlideCaption("");
                      setSlideCtaType("none");
                      setSlideCtaTarget("");
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("ارسال انجام نشد.");
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
                  onChange={() => {
                    setAdminShopMode("platform");
                    setAdminShopId("");
                    setAdminShopName("");
                  }}
                />
                ویترین اصلی
              </label>
              <label className="flex min-h-11 items-center gap-2">
                <input type="radio" checked={adminShopMode === "shop"} onChange={() => setAdminShopMode("shop")} />
                فروشگاه مشخص
              </label>
              {adminShopMode === "shop" ? (
                <div className="space-y-2" data-testid="story-admin-shop-picker">
                  <input
                    className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    placeholder="جستجوی نام فروشگاه"
                    value={shopSearch}
                    onChange={(e) => setShopSearch(e.target.value)}
                    data-testid="story-admin-shop-search"
                  />
                  {adminShopName ? (
                    <p className="text-sm text-muted">انتخاب‌شده: {adminShopName}</p>
                  ) : (
                    <p className="text-sm text-rose-700">فروشگاه را از فهرست نام‌ها انتخاب کنید.</p>
                  )}
                  <ul className="max-h-48 overflow-auto rounded-xl border border-gray-100">
                    {filteredShops.map((row) => (
                      <li key={row.sellerPartyId}>
                        <button
                          type="button"
                          className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm hover:bg-secondary"
                          onClick={() => {
                            setAdminShopId(row.sellerPartyId);
                            setAdminShopName(row.displayName);
                          }}
                          data-testid="story-admin-shop-option"
                        >
                          {row.displayName}
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
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
                if (!actorId) return;
                const key = mintOccurrence();
                const body = {
                  reason: rejectDraft.trim(),
                  expectedVersion: detail.versionToken,
                  occurrenceKey: key,
                };
                const envelope: StoryIntentEnvelope = {
                  version: 2,
                  action: "reject",
                  occurrenceKey: key,
                  method: "POST",
                  path: `/v1/admin/stories/${detail.storyId}/reject`,
                  routeTargetId: detail.storyId,
                  expectedVersion: detail.versionToken,
                  bodyText: JSON.stringify(body),
                  uploadMeta: null,
                  addBodyText: null,
                  uploadedAssetId: null,
                  children: [],
                  role: "admin",
                  shopId: detail.sellerPartyId,
                  actorUserId: actorId,
                  recordedAt: new Date().toISOString(),
                };
                void runDurableStoryMutation({
                  gate,
                  envelope,
                  scope: { role: "admin", shopId: null, actorUserId: actorId },
                }).then((outcome) => {
                  if (outcome.state === "ok") {
                    toast.success("رد شد.");
                    setRejectOpen(false);
                    refreshGrid();
                    setReloadToken((v) => v + 1);
                  } else if (outcome.state === "failed") toast.error(outcome.message);
                  else toast.error("رد ممکن نشد.");
                });
              }}
            >
              ثبت رد
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={scheduleOpen}
        onClose={() => setScheduleOpen(false)}
        title={scheduleMode === "approve_schedule" ? "تأیید و زمان‌بندی" : "زمان‌بندی نمایش"}
      >
        <div className="space-y-3 p-1">
          <p className="text-sm text-muted">زمان به وقت تهران. اگر پایان خالی بماند تا توقف دستی می‌ماند.</p>
          {scheduleFieldError ? (
            <p className="text-sm text-rose-700" role="alert" data-testid="story-schedule-error">{scheduleFieldError}</p>
          ) : null}
          <label className="block text-sm" data-testid="story-schedule-start-wrap">
            <span className="mb-1 block text-gray-600">روز و ساعت شروع</span>
            <JalaliDatePicker
              ariaLabel="روز شروع"
              value={scheduleDraft.startDayIso || undefined}
              onChange={(iso) => setScheduleDraft((c) => ({ ...c, startDayIso: iso ?? "" }))}
            />
            <input type="time" className="mt-2 min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm" value={scheduleDraft.startTime} onChange={(e) => setScheduleDraft((c) => ({ ...c, startTime: e.target.value }))} data-testid="story-schedule-start" />
          </label>
          <label className="block text-sm" data-testid="story-schedule-end-wrap">
            <span className="mb-1 block text-gray-600">روز و ساعت پایان (اختیاری)</span>
            <JalaliDatePicker
              ariaLabel="روز پایان"
              value={scheduleDraft.endDayIso || undefined}
              onChange={(iso) => setScheduleDraft((c) => ({ ...c, endDayIso: iso ?? "" }))}
            />
            <input type="time" className="mt-2 min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm" value={scheduleDraft.endTime} onChange={(e) => setScheduleDraft((c) => ({ ...c, endTime: e.target.value }))} data-testid="story-schedule-end" />
          </label>
          <div className="flex justify-end gap-2">
            <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setScheduleOpen(false)}>
              انصراف
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() => {
                if (!detail || !actorId) return;
                const key = mintOccurrence();
                const parsed = parseStoryScheduleDraft(scheduleDraft);
                if (!parsed.ok) {
                  setScheduleFieldError(parsed.message);
                  return;
                }
                setScheduleFieldError(null);
                const startAt = parsed.startAt;
                const endAt = parsed.endAt;
                const body =
                  scheduleMode === "approve_schedule"
                    ? { expectedVersion: detail.versionToken, startAt, endAt, occurrenceKey: key }
                    : { startAt, endAt, expectedVersion: detail.versionToken, occurrenceKey: key };
                const envelope: StoryIntentEnvelope = {
                  version: 2,
                  action: scheduleMode === "approve_schedule" ? "approve_schedule" : "schedule",
                  occurrenceKey: key,
                  method: "POST",
                  path:
                    scheduleMode === "approve_schedule"
                      ? `/v1/admin/stories/${detail.storyId}/approve-and-schedule`
                      : `/v1/admin/stories/${detail.storyId}/schedule`,
                  routeTargetId: detail.storyId,
                  expectedVersion: detail.versionToken,
                  bodyText: JSON.stringify(body),
                  uploadMeta: null,
                  addBodyText: null,
                  uploadedAssetId: null,
                  children: [],
                  role: "admin",
                  shopId: detail.sellerPartyId,
                  actorUserId: actorId,
                  recordedAt: new Date().toISOString(),
                };
                void runDurableStoryMutation({
                  gate,
                  envelope,
                  scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                }).then((outcome) => {
                  if (outcome.state === "blocked") {
                    toast.error(
                      outcome.reason === "storage"
                        ? "اقدام ناتمام قبلی هنوز باز است یا ذخیره ممکن نیست."
                        : "ارسال انجام نشد.",
                    );
                    return;
                  }
                  if (outcome.state === "failed") {
                    toast.error(outcome.message);
                    return;
                  }
                  toast.success(scheduleMode === "approve_schedule" ? "تأیید و زمان‌بندی شد." : "زمان‌بندی ذخیره شد.");
                  setScheduleOpen(false);
                  refreshGrid();
                  setReloadToken((v) => v + 1);
                });
              }}
            >
              {scheduleMode === "approve_schedule" ? "تأیید و زمان‌بندی" : "ذخیره زمان‌بندی"}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog open={previewConfirm === "publish"} onClose={() => setPreviewConfirm(null)} title="تأیید انتشار">
        <div className="space-y-3 p-1" data-testid="story-publish-preview">
          {detail ? (
            <>
              <p className="text-sm text-muted">پیش‌نمایش نسخهٔ بررسی‌شده قبل از انتشار:</p>
              <p className="font-bold">{detail.title}</p>
              <ul className="space-y-2 text-sm">
                {detail.items.map((item) => (
                  <li key={item.storyItemId} className="flex items-center gap-3 rounded-xl border border-gray-100 px-3 py-2">
                    {item.mediaType === "video" && item.mediaUrl ? (
                      // eslint-disable-next-line jsx-a11y/media-has-caption
                      <video src={item.mediaUrl} className="h-16 w-16 rounded object-cover" muted playsInline />
                    ) : item.mediaUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={item.mediaUrl} alt="" className="h-16 w-16 rounded object-cover" />
                    ) : null}
                    <span>
                      {item.mediaType === "video" ? "ویدیو" : "عکس"}
                      {item.caption ? ` · ${item.caption}` : ""}
                      {item.ctaType !== "none" ? ` · مقصد: ${item.ctaType}` : ""}
                    </span>
                  </li>
                ))}
              </ul>
              <div className="flex justify-end gap-2">
                <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setPreviewConfirm(null)}>
                  انصراف
                </button>
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                  onClick={() => {
                    if (!detail || !actorId) return;
                    const key = mintOccurrence();
                    const envelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "approve_publish",
                      occurrenceKey: key,
                      method: "POST",
                      path: `/v1/admin/stories/${detail.storyId}/approve-and-publish`,
                      routeTargetId: detail.storyId,
                      expectedVersion: detail.versionToken,
                      bodyText: JSON.stringify({ expectedVersion: detail.versionToken, occurrenceKey: key }),
                      uploadMeta: null,
                      addBodyText: null,
                      uploadedAssetId: null,
                      children: [],
                      role: "admin",
                      shopId: detail.sellerPartyId,
                      actorUserId: actorId,
                      recordedAt: new Date().toISOString(),
                    };
                    void runDurableStoryMutation({
                      gate,
                      envelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    }).then((outcome) => {
                      if (outcome.state === "ok") {
                        toast.success("تأیید و منتشر شد.");
                        setPreviewConfirm(null);
                        refreshGrid();
                        setReloadToken((v) => v + 1);
                      } else if (outcome.state === "failed") toast.error(outcome.message);
                      else toast.error("ارسال انجام نشد.");
                    });
                  }}
                >
                  تأیید انتشار
                </button>
              </div>
            </>
          ) : null}
        </div>
      </Dialog>
    </main>
  );
}

function StoryLiveGrid({
  capabilities,
  reviewFilter,
  reloadToken,
  onDenied,
  onError,
  onOk,
  onOpenDetail,
  onSubmit,
  errorMessage,
  onRetry,
  showError,
}: {
  capabilities: StoryCapabilities;
  reviewFilter: string;
  reloadToken: number;
  onDenied: () => void;
  onError: (message?: string) => void;
  onOk: () => void;
  onOpenDetail: (id: string) => void;
  onSubmit: (row: AdminStorySnapshot) => void;
  errorMessage?: string;
  onRetry: () => void;
  showError: boolean;
}) {
  const actorId = resolveActorId(capabilities.mode);
  const sellerShopId = capabilities.mode === "seller" ? readSellerPartyId() : null;
  const columns = useMemo((): GridColumnDef<AdminStorySnapshot>[] => {
    return [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        cell: (row) => (
          <button
            type="button"
            className="flex min-h-11 items-center gap-2 text-right font-bold line-clamp-2 hover:text-[#2563EB]"
            onClick={() => onOpenDetail(row.storyId)}
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
            { id: "view", label: "بررسی", icon: Eye, onClick: (r) => onOpenDetail(r.storyId) },
          ];
          if (capabilities.canSubmit && hasAction(row, "submit")) {
            actions.push({
              id: "submit",
              label: "ارسال برای تأیید",
              icon: Send,
              onClick: (r) => onSubmit(r),
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
  }, [capabilities.canSubmit, onOpenDetail, onSubmit]);

  const savedViewStore = useMemo(
    () =>
      createScopedStorySavedViewStore({
        role: capabilities.mode,
        actorUserId: actorId || "anon",
        shopId: sellerShopId,
        gridKey: capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
      }),
    [capabilities.mode, actorId, sellerShopId],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      if (capabilities.mode === "seller") {
        const result = await querySellerStories(query);
        if (result.state === "denied") {
          onDenied();
          return { rows: [], total: 0 };
        }
        if (result.state !== "ok" || !result.data) {
          onError(result.message);
          return { rows: [], total: 0 };
        }
        onOk();
        return { rows: result.data.items, total: result.data.totalCount };
      }
      return adminGridQueryAdapter(
        (serverQuery) =>
          queryAdminStoriesGrid(serverQuery, reviewFilter ? { reviewStatus: reviewFilter } : undefined),
        onDenied,
        onError,
      )(query);
    },
    [capabilities.mode, reloadToken, reviewFilter, onDenied, onError, onOk],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
      <div className="p-2 md:p-4">
        {showError ? (
          <ErrorState title={STORY_COPY.loadErrorTitle} detail={errorMessage} onRetry={onRetry} retryLabel={faWorkspaceMessages.retry} />
        ) : (
          <AppDataGrid<AdminStorySnapshot> {...gridProps} />
        )}
      </div>
    </section>
  );
}
