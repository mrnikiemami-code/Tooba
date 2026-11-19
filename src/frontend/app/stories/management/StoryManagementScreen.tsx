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
import { loadSellerDevContexts, readActorUserId, readSellerPartyId } from "../../vendor-panel/seller-api";
import { JalaliDatePicker } from "../../../design-system/app-data-grid/jalali-date-picker";
import {
  getAdminStory,
  getSellerStory,
  queryAdminStoriesGrid,
  querySellerStories,
  type AdminStorySnapshot,
} from "../story-api";
import { type StoryCapabilities } from "./story-capabilities";
import {
  resolveStoryUiLocale,
  storyCopy,
  STORY_ORIGIN_LABELS_BY_LOCALE,
  type StoryCopy,
} from "./story-management-copy";
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

function Denied({ retry, copy }: { retry: () => void; copy: StoryCopy }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title={copy.deniedTitle}
        detail={copy.deniedDetail}
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function PageHeading({ title, description, homeLabel }: { title: string; description: string; homeLabel: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">
        {homeLabel} / {title}
      </p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

function shopLabel(row: AdminStorySnapshot, copy: StoryCopy): string {
  if (row.shopDisplayName?.trim()) return row.shopDisplayName.trim();
  if (!row.sellerPartyId) return copy.platformStorefront;
  return copy.shopNameUnavailable;
}

function destinationLabelForType(ctaType: string | null | undefined, copy: StoryCopy): string {
  const type = (ctaType ?? "").trim().toLowerCase();
  if (!type || type === "none") return copy.destinationNone;
  if (type === "product") return copy.destinationProduct;
  if (type === "external") return copy.destinationExternal;
  if (type === "internal" || type === "shop" || type === "seller") return copy.destinationShop;
  return copy.destinationUnavailable;
}

function displayState(row: AdminStorySnapshot, copy: StoryCopy): string {
  if (row.effectivelyPublic) return copy.displayOnStorefront;
  if (row.reviewStatus === "Submitted") return copy.displayAwaiting;
  if (row.reviewStatus === "Rejected") return copy.displayRejected;
  if (row.status === "Scheduled") return copy.displayScheduled;
  if (row.status === "Disabled") return copy.displayStopped;
  if (row.status === "Draft" || row.reviewStatus === "None") return copy.displayDraft;
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
  const uiLocale = resolveStoryUiLocale();
  const copy = storyCopy(uiLocale);
  const originLabels = STORY_ORIGIN_LABELS_BY_LOCALE[uiLocale];
  const numberLocale = uiLocale === "en" ? "en-US" : "fa-IR";
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
  const [productTotalCount, setProductTotalCount] = useState(0);
  const [productPage, setProductPage] = useState(1);
  const [productLoading, setProductLoading] = useState(false);
  const [selectedProductLabel, setSelectedProductLabel] = useState("");
  const [slideCaption, setSlideCaption] = useState("");
  const [slideDurationSec, setSlideDurationSec] = useState("5");
  const [slideCtaType, setSlideCtaType] = useState("none");
  const [slideCtaTarget, setSlideCtaTarget] = useState("");
  const [slideDialogOpen, setSlideDialogOpen] = useState(false);
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [productLabelById, setProductLabelById] = useState<Record<string, string>>({});
  const [sellerShopLabel, setSellerShopLabel] = useState("");
  const [dirty, setDirty] = useState(false);
  const slideFileInputRef = useRef<HTMLInputElement | null>(null);

  function destinationLabelForItem(item: AdminStorySnapshot["items"][number]): string {
    const ctaType = item.ctaType;
    const ctaTarget = item.ctaTarget;
    const type = (ctaType ?? "").trim().toLowerCase();
    if (!type || type === "none") return copy.destinationNone;
    if (type === "product") {
      const id = (ctaTarget ?? "").trim();
      if (!id) return copy.productUnknown;
      const owned = item.ctaProductTitle?.trim() || productLabelById[id];
      if (owned) return owned;
      return copy.productUnknown;
    }
    if (type === "external") {
      return ctaTarget?.trim() ? `${copy.destinationExternal}: ${ctaTarget}` : copy.destinationExternal;
    }
    if (type === "internal" || type === "shop" || type === "seller") {
      return ctaTarget?.trim() ? `${copy.destinationShop}: ${ctaTarget}` : copy.destinationShop;
    }
    return destinationLabelForType(ctaType, copy);
  }

  function openNewSlideDialog() {
    setEditingItemId(null);
    setSlideFile(null);
    setSlideCaption("");
    setSlideDurationSec("5");
    setSlideCtaType("none");
    setSlideCtaTarget("");
    setSelectedProductLabel("");
    setProductSearch("");
    setSlideDialogOpen(true);
  }

  function openEditSlideDialog(item: AdminStorySnapshot["items"][number]) {
    setEditingItemId(item.storyItemId);
    setSlideFile(null);
    setSlideCaption(item.caption ?? "");
    setSlideDurationSec(
      item.durationMs && item.durationMs > 0 ? String(Math.round(item.durationMs / 1000)) : "5",
    );
    setSlideCtaType(item.ctaType || "none");
    setSlideCtaTarget(item.ctaTarget ?? "");
    setSelectedProductLabel(
      item.ctaType === "product" && item.ctaTarget
        ? item.ctaProductTitle?.trim() || productLabelById[item.ctaTarget] || ""
        : "",
    );
    setProductSearch("");
    setSlideDialogOpen(true);
  }
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
    if (capabilities.mode !== "seller") {
      setSellerShopLabel("");
      return;
    }
    let cancelled = false;
    void loadSellerDevContexts()
      .then((rows) => {
        if (cancelled) return;
        const match = rows.find((row) => row.sellerPartyId === sellerShopId);
        setSellerShopLabel(match?.sellerLabel?.trim() ?? "");
      })
      .catch(() => {
        if (!cancelled) setSellerShopLabel("");
      });
    return () => {
      cancelled = true;
    };
  }, [capabilities.mode, sellerShopId, scopeTick]);

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
    setProductPage(1);
    setProductLoading(true);
    void loadStoryProductOptions({
      mode: capabilities.mode,
      shopId: shop,
      actorUserId: actorId,
      search: productSearch,
      page: 1,
      pageSize: 20,
    })
      .then((page) => {
        setProductOptions(page.items);
        setProductTotalCount(page.totalCount);
      })
      .finally(() => setProductLoading(false));
  }, [
    capabilities.mode,
    sellerShopId,
    detail?.sellerPartyId,
    adminShopId,
    adminShopMode,
    actorId,
    scopeTick,
    productSearch,
  ]);

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
        // Hydrate per-target labels from owning DTO only (scoped to this story remount).
        const nextLabels: Record<string, string> = {};
        for (const item of result.data.items) {
          if (item.ctaType === "product" && item.ctaTarget && item.ctaProductTitle?.trim()) {
            nextLabels[item.ctaTarget] = item.ctaProductTitle.trim();
          }
        }
        setProductLabelById(nextLabels);
        setSelectedProductLabel("");
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
      toast.success(copy.submittedToast);
      refreshGrid();
      if (selectedId === row.storyId) setReloadToken((v) => v + 1);
    } else if (outcome.state === "failed") toast.error(outcome.message);
    else toast.error("ارسال انجام نشد.");
  }

  async function createDraft(): Promise<string | null> {
    if (inFlightRef.current) return null;
    if (!actorId) {
      toast.error(copy.actorMissing);
      return null;
    }
    const title = draftTitle.trim();
    if (!title) {
      toast.error(copy.titleRequired);
      return null;
    }
    if (capabilities.mode === "admin" && adminShopMode === "shop" && !adminShopId.trim()) {
      toast.error(copy.shopPickRequired);
      return null;
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
      toast.success(copy.draftSaved);
      setShowCreate(false);
      setDirty(false);
      if (id) {
        openDetail(String(id));
        router.push(`${basePath}/${id}`);
        return String(id);
      }
      refreshGrid();
      return null;
    }
    if (outcome.state === "failed") toast.error(outcome.message);
    else toast.error(copy.sendFailed);
    return null;
  }

  if (state === "denied") return <Denied retry={refreshGrid} copy={copy} />;

  const title = capabilities.mode === "seller" ? copy.sellerTitle : copy.adminTitle;
  const description = capabilities.mode === "seller" ? copy.sellerDescription : copy.adminDescription;
  const testId = capabilities.mode === "seller" ? "seller-stories" : "admin-stories";
  const isListSurface = !createMode && !selectedId;
  const sellerEditable = detail ? hasAction(detail, "update") || hasAction(detail, "add_item") : false;
  const filteredShops = shopOptions.filter((row) => {
    const q = shopSearch.trim();
    if (!q) return true;
    return row.displayName.includes(q) || row.sellerPartyId.includes(q);
  });

  return (
    <main data-testid={testId}>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title={title} description={description} homeLabel={copy.breadcrumbHome} />
        <div className="flex flex-wrap items-center gap-2">
          {isListSurface && capabilities.canReview ? (
            <label className="flex min-h-11 items-center gap-2 text-sm text-muted">
              {copy.filterReviewLabel}
              <select
                className="min-h-11 rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm text-gray-800"
                value={reviewFilter}
                onChange={(e) => {
                  setReviewFilter(e.target.value);
                  refreshGrid();
                }}
                data-testid="admin-story-review-filter"
              >
                <option value="">{copy.filterAll}</option>
                <option value="Submitted">{copy.filterPending}</option>
                <option value="Approved">{copy.filterApproved}</option>
                <option value="Rejected">{copy.filterRejected}</option>
              </select>
            </label>
          ) : null}
          {isListSurface && capabilities.canCreate ? (
            <button
              type="button"
              className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() => router.push(`${basePath}/new`)}
              data-testid="story-create-open"
            >
              {copy.createButton}
            </button>
          ) : null}
        </div>
      </div>

      {pendingActs.length > 0 ? (
        <section className="mb-4 rounded-2xl border border-amber-200 bg-amber-50 p-4" data-testid="story-pending-recovery">
          <h2 className="mb-2 text-sm font-bold text-amber-900">{copy.pendingHeading}</h2>
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
                    else toast.error(copy.pendingContinueFailed);
                    });
                  }}
                >
                  {copy.pendingRetry}
                </button>
                </div>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {isListSurface ? (
        <StoryLiveGrid
          capabilities={capabilities}
          reviewFilter={reviewFilter}
          reloadToken={reloadToken}
          copy={copy}
          numberLocale={numberLocale}
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

      {createMode ? (
        <section className="mt-5 rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="story-create-workspace">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-lg font-bold">{copy.createWorkspaceTitle}</h2>
            <button
              type="button"
              className="min-h-11 rounded-xl px-3 py-1.5 text-sm"
              onClick={() => router.push(basePath)}
            >
              {copy.backToList}
            </button>
          </div>
          <div className="mb-5 space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block text-gray-600">{copy.titleLabel}</span>
              <input
                className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                value={draftTitle}
                onChange={(e) => {
                  setDraftTitle(e.target.value);
                  setDirty(true);
                }}
                data-testid="story-create-title"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block text-gray-600">{copy.languageLabel}</span>
              <select
                className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                value={editLocale}
                onChange={(e) => {
                  setEditLocale(e.target.value);
                  setDirty(true);
                }}
                data-testid="story-create-locale"
              >
                {localeOptions.map((row) => (
                  <option key={row.code} value={row.code}>
                    {row.name}
                    {row.isDefault ? copy.localeDefaultSuffix : ""}
                  </option>
                ))}
              </select>
            </label>
            {capabilities.mode === "seller" ? (
              <p className="rounded-xl bg-secondary px-3 py-2 text-sm" data-testid="story-seller-shop-fixed">
                {copy.sellerShopFixedPrefix} {sellerShopLabel.trim() || copy.sellerShopFallback}
                <span className="mt-1 block text-xs text-muted">{copy.sellerShopFixedHint}</span>
              </p>
            ) : null}
            {capabilities.mode === "admin" ? (
              <fieldset className="space-y-2 text-sm">
                <legend className="mb-1 text-gray-600">{copy.placementLegend}</legend>
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
                  {copy.mainStorefront}
                </label>
                <label className="flex min-h-11 items-center gap-2">
                  <input type="radio" checked={adminShopMode === "shop"} onChange={() => setAdminShopMode("shop")} />
                  {copy.forOneShop}
                </label>
                {adminShopMode === "shop" ? (
                  <div className="space-y-2" data-testid="story-admin-shop-picker">
                    <input
                      className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      placeholder={copy.shopSearchPlaceholder}
                      value={shopSearch}
                      onChange={(e) => setShopSearch(e.target.value)}
                      data-testid="story-admin-shop-search"
                    />
                    {adminShopName ? (
                      <p className="text-sm text-muted">
                        {copy.selectedShopPrefix} {adminShopName}
                      </p>
                    ) : (
                      <p className="text-sm text-rose-700">{copy.shopPickRequired}</p>
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
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => void createDraft()}
                data-testid="story-create-save"
              >
                {copy.saveForm}
              </button>
            </div>
          </div>
          <div className="mb-4">
            <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-sm font-bold">{copy.itemsHeading}</h3>
              {capabilities.canCreate ? (
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                  data-testid="story-slide-open-new"
                  onClick={() => {
                    void (async () => {
                      const id = await createDraft();
                      if (id) openNewSlideDialog();
                    })();
                  }}
                >
                  {copy.addItem}
                </button>
              ) : null}
            </div>
            <p className="text-sm text-muted">{copy.slidesEmptyHint}</p>
          </div>
          <div data-testid="story-create-preview">
            <h3 className="mb-2 text-sm font-bold">{copy.previewHeading}</h3>
            <p className="text-sm text-muted">{copy.previewEmpty}</p>
          </div>
        </section>
      ) : null}

      {!createMode && detail ? (
        <section className="mt-5 rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="story-detail-workspace">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold">{detail.title}</h2>
              <p className="text-sm text-muted" data-testid="story-detail-location">
                {copy.locationPrefix} {shopLabel(detail, copy)} · {displayState(detail, copy)}
              </p>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <StoryStatusBadge status={detail.status} />
                {detail.reviewStatus !== "None" ? <StoryReviewBadge reviewStatus={detail.reviewStatus} /> : null}
                {capabilities.showOrigin ? (
                  <span className="text-xs text-muted">{originLabels[detail.origin] ?? detail.origin}</span>
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
              {copy.backToList}
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
                {copy.approveAndPublish}
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
                {copy.approveAndSchedule}
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
                {copy.scheduleDisplay}
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
                      toast.success(copy.stoppedToast);
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("توقف ممکن نشد.");
                  });
                }}
              >
                {copy.stopDisplay}
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
                      toast.success(copy.returnedToast);
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error("بازگشت ممکن نشد.");
                  });
                }}
              >
                {copy.returnToDraft}
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
            <div className="mb-4 space-y-3">
              <div className="grid gap-3 md:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1 block text-gray-600">{copy.titleLabel}</span>
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
                  <span className="mb-1 block text-gray-600">{copy.languageLabel}</span>
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
                        {row.isDefault ? copy.localeDefaultSuffix : ""}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                  data-testid="story-save-title-locale"
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
                        toast.success(copy.savedToast);
                        setDirty(false);
                        setReloadToken((v) => v + 1);
                        refreshGrid();
                      } else if (outcome.state === "failed") toast.error(outcome.message);
                      else toast.error(copy.sendFailed);
                    });
                  }}
                >
                  {copy.saveTitleLocale}
                </button>
              </div>
            </div>
          ) : null}

          <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
            <h3 className="text-sm font-bold">
              {copy.itemsHeading} ({detail.items.length.toLocaleString(numberLocale)})
            </h3>
            {capabilities.canEdit && sellerEditable ? (
              <button
                type="button"
                className="min-h-11 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                data-testid="story-slide-open-new"
                onClick={() => openNewSlideDialog()}
              >
                {copy.addItem}
              </button>
            ) : null}
          </div>
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
                    <p>
                      {item.mediaType === "video" ? copy.videoLabel : copy.photoLabel}
                      {item.durationMs
                        ? ` · ${Math.round(item.durationMs / 1000).toLocaleString(numberLocale)} ${copy.secondsSuffix}`
                        : ""}
                    </p>
                    {item.caption ? <p className="text-muted">{item.caption}</p> : null}
                    <p className="text-xs text-muted" data-testid="story-slide-destination">
                      {copy.destinationPrefix} {destinationLabelForItem(item)}
                    </p>
                  </div>
                  {capabilities.canEdit && sellerEditable ? (
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        className="min-h-11 rounded-xl border border-gray-200 px-3 py-1.5 text-sm"
                        data-testid="story-slide-edit"
                        onClick={() => openEditSlideDialog(item)}
                      >
                        {copy.editItem}
                      </button>
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
                              toast.success(copy.reorderToast);
                              setReloadToken((v) => v + 1);
                            } else if (outcome.state === "failed") toast.error(outcome.message);
                            else toast.error(copy.reorderFailed);
                          });
                        }}
                      >
                        {copy.moveUp}
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
                              toast.success(copy.slideUpdated);
                              setReloadToken((v) => v + 1);
                              refreshGrid();
                            } else if (outcome.state === "failed") toast.error(outcome.message);
                            else toast.error(copy.sendFailed);
                          });
                        }}
                      >
                        {copy.removeSlide}
                      </button>
                    </div>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>

        </section>
      ) : null}

      <Dialog
        open={slideDialogOpen}
        onClose={() => {
          setSlideDialogOpen(false);
          setEditingItemId(null);
        }}
        title={editingItemId ? copy.editSlideTitle : copy.newSlideTitle}
      >
        <div className="space-y-3 p-1" data-testid="story-slide-editor">
          {!editingItemId ? (
            <div className="space-y-2 text-sm">
              <span className="mb-1 block text-gray-600">{copy.mediaLabel}</span>
              <input
                ref={slideFileInputRef}
                type="file"
                accept="image/*,video/*"
                className="sr-only"
                onChange={(e) => setSlideFile(e.target.files?.[0] ?? null)}
                data-testid="story-slide-file"
              />
              <button
                type="button"
                className="min-h-11 rounded-xl border border-gray-200 px-4 py-2 text-sm"
                onClick={() => slideFileInputRef.current?.click()}
                data-testid="story-slide-file-pick"
              >
                {slideFile ? `${copy.pickedFilePrefix} ${slideFile.name}` : copy.pickFile}
              </button>
            </div>
          ) : (
            <p className="text-sm text-muted">{copy.keepMediaHint}</p>
          )}
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">{copy.captionLabel}</span>
            <input
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={slideCaption}
              onChange={(e) => setSlideCaption(e.target.value)}
              data-testid="story-slide-caption"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">{copy.durationLabel}</span>
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
            <span className="mb-1 block text-gray-600">{copy.ctaLabel}</span>
            <select
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={slideCtaType}
              onChange={(e) => {
                setSlideCtaType(e.target.value);
                setSlideCtaTarget("");
                setSelectedProductLabel("");
              }}
              data-testid="story-slide-cta-type"
            >
              <option value="none">{copy.destinationNone}</option>
              <option value="product">{copy.destinationProduct}</option>
              <option value="external">{copy.destinationExternal}</option>
            </select>
          </label>
          {slideCtaType === "product" ? (
            <div className="space-y-2" data-testid="story-product-picker">
              <input
                className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                placeholder={copy.productSearchPlaceholder}
                value={productSearch}
                onChange={(e) => setProductSearch(e.target.value)}
                data-testid="story-product-search"
              />
              {productLoading ? <p className="text-xs text-muted">{copy.loadingProducts}</p> : null}
              <ul className="max-h-40 overflow-auto rounded-xl border border-gray-100">
                {productOptions.map((row) => (
                  <li key={`${row.productId}:${row.offerId}`}>
                    <button
                      type="button"
                      className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm hover:bg-secondary"
                      data-testid="story-product-option"
                      data-product-id={row.productId}
                      onClick={() => {
                        if (!row.productId) {
                          toast.error(copy.productIdInvalid);
                          return;
                        }
                        setSlideCtaTarget(row.productId);
                        setSelectedProductLabel(row.productTitle);
                        setProductLabelById((prev) => ({ ...prev, [row.productId]: row.productTitle }));
                      }}
                    >
                      {row.productTitle}
                    </button>
                  </li>
                ))}
              </ul>
              {productOptions.length < productTotalCount ? (
                <button
                  type="button"
                  className="min-h-11 w-full rounded-xl border border-gray-200 text-sm"
                  data-testid="story-product-load-more"
                  onClick={() => {
                    const shop =
                      capabilities.mode === "seller"
                        ? sellerShopId
                        : detail?.sellerPartyId ?? (adminShopMode === "shop" ? adminShopId : null);
                    const next = productPage + 1;
                    setProductLoading(true);
                    void loadStoryProductOptions({
                      mode: capabilities.mode,
                      shopId: shop,
                      actorUserId: actorId,
                      search: productSearch,
                      page: next,
                      pageSize: 20,
                    })
                      .then((page) => {
                        const seen = new Set(productOptions.map((r) => r.productId));
                        const appended = page.items.filter((r) => !seen.has(r.productId));
                        setProductOptions((prev) => [...prev, ...appended]);
                        setProductTotalCount(page.totalCount);
                        setProductPage(next);
                      })
                      .finally(() => setProductLoading(false));
                  }}
                >
                  {copy.loadMoreProducts} ({productOptions.length} از {productTotalCount})
                </button>
              ) : null}
              {slideCtaTarget ? (
                <p className="text-xs text-muted" data-testid="story-product-selected">
                  {copy.productSelectedPrefix} {selectedProductLabel || copy.productSelectedFallback}
                </p>
              ) : null}
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
          <div className="flex flex-wrap justify-end gap-2 pt-2">
            <button
              type="button"
              className="min-h-11 rounded-xl px-4 py-2 text-sm"
              onClick={() => {
                setSlideDialogOpen(false);
                setEditingItemId(null);
              }}
            >
              {copy.cancel}
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
              disabled={(!editingItemId && !slideFile) || inFlightRef.current}
              data-testid="story-slide-save"
              onClick={() => {
                if (!detail || !actorId) return;
                void (async () => {
                  if (inFlightRef.current) return;
                  if (editingItemId) {
                    const existing = detail.items.find((row) => row.storyItemId === editingItemId);
                    if (!existing) {
                      toast.error(copy.slideMissing);
                      return;
                    }
                    const key = mintOccurrence();
                    const body = {
                      mediaType: existing.mediaType,
                      mediaAssetId: existing.mediaAssetId,
                      mediaUrl: existing.mediaUrl,
                      caption: slideCaption.trim() || null,
                      durationMs: Math.max(1, Number(slideDurationSec) || 5) * 1000,
                      ctaType: slideCtaType,
                      ctaTarget: slideCtaType === "none" ? null : slideCtaTarget || null,
                      expectedVersion: detail.versionToken,
                      occurrenceKey: key,
                    };
                    const envelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "update_item",
                      occurrenceKey: key,
                      method: "PUT",
                      path:
                        capabilities.mode === "seller"
                          ? `/v1/seller/stories/${detail.storyId}/items/${editingItemId}`
                          : `/v1/admin/stories/${detail.storyId}/items/${editingItemId}`,
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
                    const outcome = await runDurableStoryMutation({
                      gate,
                      envelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    });
                    if (outcome.state === "ok") {
                      toast.success(copy.slideUpdated);
                      if (slideCtaType === "product" && slideCtaTarget && selectedProductLabel) {
                        setProductLabelById((prev) => ({ ...prev, [slideCtaTarget]: selectedProductLabel }));
                      }
                      setSlideDialogOpen(false);
                      setEditingItemId(null);
                      setReloadToken((v) => v + 1);
                      refreshGrid();
                    } else if (outcome.state === "failed") toast.error(outcome.message);
                    else toast.error(copy.sendFailed);
                    return;
                  }
                  if (!slideFile) return;
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
                    toast.success(copy.slideAdded);
                    if (slideCtaType === "product" && slideCtaTarget && selectedProductLabel) {
                      setProductLabelById((prev) => ({ ...prev, [slideCtaTarget]: selectedProductLabel }));
                    }
                    setSlideDialogOpen(false);
                    setSlideFile(null);
                    setSlideCaption("");
                    setSlideCtaType("none");
                    setSlideCtaTarget("");
                    refreshGrid();
                    setReloadToken((v) => v + 1);
                  } else if (outcome.state === "failed") toast.error(outcome.message);
                  else toast.error(copy.sendFailed);
                })();
              }}
            >
              {editingItemId ? copy.saveSlideChanges : copy.addItem}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog open={rejectOpen} onClose={() => setRejectOpen(false)} title={copy.reject}>
        <div className="space-y-3 p-1">
          <label className="block text-sm">
            <span className="mb-1 block text-gray-600">{copy.rejectionReason}</span>
            <input
              className="min-h-11 w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
              value={rejectDraft}
              onChange={(e) => setRejectDraft(e.target.value)}
              data-testid="story-reject-reason-input"
            />
          </label>
          <div className="flex justify-end gap-2">
            <button type="button" className="min-h-11 rounded-xl px-4 py-2 text-sm" onClick={() => setRejectOpen(false)}>
              {copy.cancel}
            </button>
            <button
              type="button"
              className="min-h-11 rounded-xl bg-rose-600 px-4 py-2 text-sm font-bold text-white"
              onClick={() => {
                if (!detail || !rejectDraft.trim()) {
                  toast.error(copy.rejectReasonRequired);
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

      <Dialog open={previewConfirm === "publish"} onClose={() => setPreviewConfirm(null)} title={copy.approveAndPublish}>
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
                      {item.mediaType === "video" ? copy.videoLabel : copy.photoLabel}
                      {item.caption ? ` · ${item.caption}` : ""}
                      {item.ctaType !== "none" ? ` · ${copy.destinationPrefix} ${destinationLabelForItem(item)}` : ""}
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
  copy,
  numberLocale,
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
  copy: StoryCopy;
  numberLocale: string;
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
        header: copy.columnTitle,
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
        // Match Orders: first data column is not sticky; actions use bridge end-pin.
        filterKind: "text",
        sortable: true,
      },
      {
        id: "shopDisplayName",
        header: copy.columnShop,
        accessor: (row) => shopLabel(row, copy),
        cell: (row) => shopLabel(row, copy),
        width: 140,
        minWidth: 110,
        filterKind: "text",
      },
      {
        id: "displayState",
        header: copy.columnStatus,
        accessor: (row) => displayState(row, copy),
        cell: (row) => (
          <span className="inline-flex flex-col gap-1">
            <span className="text-sm">{displayState(row, copy)}</span>
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
        header: copy.columnSlides,
        accessor: (row) => row.items.length,
        cell: (row) => row.items.length.toLocaleString(numberLocale),
        width: 90,
        minWidth: 70,
      },
      {
        id: "startAt",
        header: copy.columnStart,
        accessor: (row) => row.startAt ?? "",
        cell: (row) => (row.startAt ? formatAdminDate(row.startAt) : "—"),
        width: 120,
        minWidth: 100,
        filterKind: "date",
      },
      {
        id: "endAt",
        header: copy.columnEnd,
        accessor: (row) => row.endAt ?? "",
        cell: (row) => (row.endAt ? formatAdminDate(row.endAt) : copy.noEnd),
        width: 120,
        minWidth: 100,
        filterKind: "date",
      },
      {
        id: "updatedAt",
        header: copy.columnUpdated,
        accessor: (row) => row.updatedAt,
        cell: (row) => formatAdminDate(row.updatedAt),
        width: 120,
        minWidth: 100,
        filterKind: "date",
        sortable: true,
      },
      {
        id: "actions",
        header: copy.columnActions,
        accessor: () => "",
        cell: (row) => {
          const actions: AppGridRowAction<AdminStorySnapshot>[] = [
            { id: "view", label: copy.rowReview, icon: Eye, onClick: (r) => onOpenDetail(r.storyId) },
          ];
          if (capabilities.canSubmit && hasAction(row, "submit")) {
            actions.push({
              id: "submit",
              label: copy.rowSubmit,
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
  }, [capabilities.canSubmit, copy, numberLocale, onOpenDetail, onSubmit]);

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
          <ErrorState title={copy.loadErrorTitle} detail={errorMessage} onRetry={onRetry} retryLabel={faWorkspaceMessages.retry} />
        ) : (
          <AppDataGrid<AdminStorySnapshot> {...gridProps} />
        )}
      </div>
    </section>
  );
}
