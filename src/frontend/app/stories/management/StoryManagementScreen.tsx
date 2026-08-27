"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { DataGrid, ErrorState, faWorkspaceMessages } from "../../../design-system";
import { executeGridQuery } from "../../../design-system/data-grid/query-engine";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import type { AdminLoadState } from "../../admin/admin-api";
import {
  addAdminStoryItem,
  addSellerStoryItem,
  approveAdminStory,
  createAdminStory,
  createSellerStory,
  disableAdminStory,
  enableAdminStory,
  listAdminStories,
  listSellerStories,
  rejectAdminStory,
  scheduleAdminStory,
  submitSellerStory,
  type AdminStorySnapshot,
} from "../story-api";
import {
  canEditSellerStory,
  canSubmitStory,
  type StoryCapabilities,
} from "./story-capabilities";
import { STORY_COPY, STORY_ORIGIN_LABELS } from "./story-management-copy";
import { StoryReviewBadge, StoryStatusBadge } from "./StoryStatusBadge";

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="Host هویت فعلی را مجاز تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
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

function shortId(value: string | null): string {
  if (!value) return "—";
  return value.length > 8 ? `${value.slice(0, 8)}…` : value;
}

/**
 * صفحهٔ مشترک مدیریت استوری — Admin و Seller با یک UI؛ تفاوت فقط capabilities/dataScope.
 */
export function StoryManagementScreen({ capabilities }: { capabilities: StoryCapabilities }) {
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<AdminStorySnapshot[]>([]);
  const [message, setMessage] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [reviewFilter, setReviewFilter] = useState<string>("");
  const [rejectDraft, setRejectDraft] = useState("");
  const [draft, setDraft] = useState({
    title: "",
    locale: "fa",
    coverMediaUrl: "/images/stories/1.jpg",
    ctaType: "internal",
    ctaTarget: "/products",
  });
  const [itemDraft, setItemDraft] = useState({ mediaType: "image", mediaUrl: "/images/stories/2.jpg" });
  const [scheduleDraft, setScheduleDraft] = useState({ startAt: "", endAt: "" });

  const refresh = useCallback(() => {
    const request =
      capabilities.mode === "seller"
        ? listSellerStories()
        : listAdminStories(reviewFilter ? { reviewStatus: reviewFilter } : undefined);
    void request.then((result) => {
      setState(result.state);
      setRows(result.data ?? []);
      setMessage(result.message);
    });
  }, [capabilities.mode, reviewFilter]);

  useEffect(refresh, [refresh]);

  const selected = rows.find((row) => row.storyId === selectedId) ?? null;

  useEffect(() => {
    if (!selected) {
      setScheduleDraft({ startAt: "", endAt: "" });
      setRejectDraft("");
      return;
    }
    setScheduleDraft({
      startAt: toDatetimeLocalValue(selected.startAt),
      endAt: toDatetimeLocalValue(selected.endAt),
    });
    setRejectDraft("");
  }, [selected]);

  const columns = useMemo((): GridColumnDef<AdminStorySnapshot>[] => {
    const cols: GridColumnDef<AdminStorySnapshot>[] = [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        cell: (row) => (
          <button type="button" className="text-right font-bold line-clamp-2 hover:text-[#2563EB]" onClick={() => setSelectedId(row.storyId)}>
            {row.title}
          </button>
        ),
        width: 200,
        minWidth: 140,
        maxWidth: 280,
        sticky: "start",
        filterKind: "text",
        sortable: true,
      },
      {
        id: "status",
        header: "وضعیت",
        accessor: (row) => row.status,
        cell: (row) => <StoryStatusBadge status={row.status} />,
        width: 110,
        minWidth: 90,
        maxWidth: 140,
        filterKind: "status",
      },
    ];

    if (capabilities.showOrigin) {
      cols.push({
        id: "origin",
        header: STORY_COPY.originHeader,
        accessor: (row) => row.origin,
        cell: (row) => STORY_ORIGIN_LABELS[row.origin] ?? row.origin,
        width: 100,
        minWidth: 80,
        maxWidth: 130,
        filterKind: "status",
      });
    }

    if (capabilities.canReview || capabilities.canSubmit) {
      cols.push({
        id: "reviewStatus",
        header: STORY_COPY.reviewHeader,
        accessor: (row) => row.reviewStatus,
        cell: (row) => <StoryReviewBadge reviewStatus={row.reviewStatus} />,
        width: 140,
        minWidth: 110,
        maxWidth: 180,
        filterKind: "status",
      });
    }

    if (capabilities.showSellerOwner) {
      cols.push({
        id: "sellerPartyId",
        header: STORY_COPY.sellerOwnerHeader,
        accessor: (row) => row.sellerPartyId ?? "",
        cell: (row) => <span dir="ltr" className="text-xs">{shortId(row.sellerPartyId)}</span>,
        width: 110,
        minWidth: 90,
        maxWidth: 140,
      });
    }

    cols.push(
      {
        id: "locale",
        header: "locale",
        accessor: (row) => row.locale ?? "",
        cell: (row) => <span dir="ltr">{row.locale ?? "—"}</span>,
        width: 90,
        minWidth: 70,
        maxWidth: 120,
      },
      {
        id: "market",
        header: "market",
        accessor: (row) => row.market ?? "",
        cell: (row) => <span dir="ltr">{row.market ?? "—"}</span>,
        width: 90,
        minWidth: 70,
        maxWidth: 120,
      },
      {
        id: "displayOrder",
        header: "ترتیب",
        accessor: (row) => row.displayOrder,
        cell: (row) => row.displayOrder.toLocaleString("fa-IR"),
        width: 80,
        minWidth: 60,
        maxWidth: 100,
        sortable: true,
      },
      {
        id: "items",
        header: "آیتم",
        accessor: (row) => row.items.length,
        cell: (row) => row.items.length.toLocaleString("fa-IR"),
        width: 70,
        minWidth: 50,
        maxWidth: 90,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        cell: (row) => (
          <span className="flex flex-wrap gap-2">
            {capabilities.canPublish || capabilities.canDisable ? (
              row.status === "Active" && capabilities.canDisable ? (
                <button
                  type="button"
                  className="rounded-lg bg-amber-600 px-3 py-1.5 text-xs text-white"
                  onClick={() => void disableAdminStory(row.storyId).then(refresh)}
                >
                  {STORY_COPY.disable}
                </button>
              ) : capabilities.canPublish ? (
                <button
                  type="button"
                  className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs text-white"
                  onClick={() => void enableAdminStory(row.storyId).then(refresh)}
                >
                  {STORY_COPY.enable}
                </button>
              ) : null
            ) : null}
            {capabilities.canReview && row.reviewStatus === "Submitted" ? (
              <>
                <button
                  type="button"
                  className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs text-white"
                  onClick={() =>
                    void approveAdminStory(row.storyId).then((result) => {
                      if (result.state === "ok") refresh();
                      else setMessage(result.message);
                    })
                  }
                >
                  {STORY_COPY.approve}
                </button>
                <button
                  type="button"
                  className="rounded-lg bg-rose-600 px-3 py-1.5 text-xs text-white"
                  onClick={() => {
                    const reason = window.prompt(STORY_COPY.rejectPrompt);
                    if (reason == null) return;
                    if (!reason.trim()) {
                      setMessage(STORY_COPY.rejectReasonRequired);
                      return;
                    }
                    void rejectAdminStory(row.storyId, reason.trim()).then((result) => {
                      if (result.state === "ok") refresh();
                      else setMessage(result.message);
                    });
                  }}
                >
                  {STORY_COPY.reject}
                </button>
              </>
            ) : null}
            {capabilities.canSubmit && canSubmitStory(row.reviewStatus) ? (
              <button
                type="button"
                className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs text-white"
                onClick={() =>
                  void submitSellerStory(row.storyId).then((result) => {
                    if (result.state === "ok") refresh();
                    else setMessage(result.message);
                  })
                }
              >
                {STORY_COPY.submit}
              </button>
            ) : null}
            <button type="button" className="rounded-lg border border-gray-200 px-3 py-1.5 text-xs" onClick={() => setSelectedId(row.storyId)}>
              {STORY_COPY.details}
            </button>
          </span>
        ),
        width: capabilities.canReview ? 280 : 200,
        minWidth: 160,
        maxWidth: 360,
      },
    );

    return cols;
  }, [capabilities, refresh]);

  const queryAdapter = useMemo(() => async (query: GridServerQuery) => executeGridQuery(rows, columns, query), [rows, columns]);

  if (state === "denied") return <Denied retry={refresh} />;

  const title = capabilities.mode === "seller" ? STORY_COPY.sellerTitle : STORY_COPY.adminTitle;
  const description = capabilities.mode === "seller" ? STORY_COPY.sellerDescription : STORY_COPY.adminDescription;
  const testId = capabilities.mode === "seller" ? "seller-stories" : "admin-stories";
  const sellerEditable = selected
    ? capabilities.mode === "admin" || canEditSellerStory(selected.reviewStatus)
    : false;

  return (
    <main data-testid={testId}>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <PageHeading title={title} description={description} />
        <div className="flex flex-wrap items-center gap-2">
          {capabilities.canReview ? (
            <label className="flex items-center gap-2 text-sm text-muted">
              فیلتر بازبینی
              <select
                className="rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm text-gray-800"
                value={reviewFilter}
                onChange={(e) => setReviewFilter(e.target.value)}
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
            <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" onClick={() => setShowCreate(true)}>
              {STORY_COPY.createButton}
            </button>
          ) : null}
        </div>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="border-b border-border px-5 py-3 text-sm text-muted">
          {rows.length.toLocaleString("fa-IR")} {STORY_COPY.listCountSuffix}
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title={STORY_COPY.loadErrorTitle} detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <DataGrid columns={columns} queryAdapter={queryAdapter} />
          )}
        </div>
      </section>

      {selected ? (
        <section className="mt-5 rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold">{selected.title}</h2>
              <p className="text-sm text-muted" dir="ltr">{selected.storyId}</p>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <StoryStatusBadge status={selected.status} />
                {(capabilities.canReview || capabilities.canSubmit) && selected.reviewStatus !== "None" ? (
                  <StoryReviewBadge reviewStatus={selected.reviewStatus} />
                ) : null}
                {capabilities.showOrigin ? (
                  <span className="text-xs text-muted">{STORY_ORIGIN_LABELS[selected.origin] ?? selected.origin}</span>
                ) : null}
              </div>
            </div>
            <button type="button" className="rounded-xl px-3 py-1.5 text-sm" onClick={() => setSelectedId(null)}>
              {STORY_COPY.close}
            </button>
          </div>

          {capabilities.canSubmit && selected.rejectionReason ? (
            <div className="mb-4 rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800" data-testid="story-rejection-reason">
              <span className="font-bold">{STORY_COPY.rejectionReason}: </span>
              {selected.rejectionReason}
            </div>
          ) : null}

          {capabilities.canReview && selected.reviewStatus === "Submitted" ? (
            <div className="mb-5 flex flex-wrap items-end gap-3">
              <label className="block min-w-[16rem] flex-1 text-sm">
                <span className="mb-1 block text-gray-600">{STORY_COPY.rejectPrompt}</span>
                <input
                  className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={rejectDraft}
                  onChange={(e) => setRejectDraft(e.target.value)}
                  data-testid="story-reject-reason-input"
                />
              </label>
              <button
                type="button"
                className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                onClick={() =>
                  void approveAdminStory(selected.storyId).then((result) => {
                    if (result.state === "ok") refresh();
                    else setMessage(result.message);
                  })
                }
              >
                {STORY_COPY.approve}
              </button>
              <button
                type="button"
                className="rounded-xl bg-rose-600 px-4 py-2 text-sm font-bold text-white"
                onClick={() => {
                  if (!rejectDraft.trim()) {
                    setMessage(STORY_COPY.rejectReasonRequired);
                    return;
                  }
                  void rejectAdminStory(selected.storyId, rejectDraft.trim()).then((result) => {
                    if (result.state === "ok") refresh();
                    else setMessage(result.message);
                  });
                }}
              >
                {STORY_COPY.reject}
              </button>
            </div>
          ) : null}

          {capabilities.canSubmit && canSubmitStory(selected.reviewStatus) ? (
            <button
              type="button"
              className="mb-5 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={() =>
                void submitSellerStory(selected.storyId).then((result) => {
                  if (result.state === "ok") refresh();
                  else setMessage(result.message);
                })
              }
            >
              {STORY_COPY.submit}
            </button>
          ) : null}

          {capabilities.canSchedule ? (
            <>
              <div className="mb-4 grid gap-3 md:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1 block text-gray-600">{STORY_COPY.scheduleStart}</span>
                  <input
                    type="datetime-local"
                    className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    value={scheduleDraft.startAt}
                    onChange={(e) => setScheduleDraft((current) => ({ ...current, startAt: e.target.value }))}
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1 block text-gray-600">{STORY_COPY.scheduleEnd}</span>
                  <input
                    type="datetime-local"
                    className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    value={scheduleDraft.endAt}
                    onChange={(e) => setScheduleDraft((current) => ({ ...current, endAt: e.target.value }))}
                  />
                </label>
              </div>
              <button
                type="button"
                className="mb-5 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() =>
                  void scheduleAdminStory(selected.storyId, {
                    startAt: fromDatetimeLocalValue(scheduleDraft.startAt),
                    endAt: fromDatetimeLocalValue(scheduleDraft.endAt),
                  }).then((result) => {
                    if (result.state === "ok") refresh();
                    else setMessage(result.message);
                  })
                }
              >
                {STORY_COPY.saveSchedule}
              </button>
            </>
          ) : null}

          <h3 className="mb-2 text-sm font-bold">
            {STORY_COPY.itemsHeading} ({selected.items.length.toLocaleString("fa-IR")})
          </h3>
          <ul className="mb-4 space-y-2 text-sm">
            {selected.items.map((item) => (
              <li key={item.storyItemId} className="rounded-xl border border-gray-100 px-3 py-2" dir="ltr">
                {item.mediaType} — {item.mediaUrl ?? "—"}
              </li>
            ))}
          </ul>

          {capabilities.canEdit && sellerEditable ? (
            <>
              <div className="grid gap-3 md:grid-cols-3">
                <label className="block text-sm">
                  <span className="mb-1 block text-gray-600">mediaType</span>
                  <select
                    className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    value={itemDraft.mediaType}
                    onChange={(e) => setItemDraft((current) => ({ ...current, mediaType: e.target.value }))}
                  >
                    <option value="image">image</option>
                    <option value="video">video</option>
                  </select>
                </label>
                <label className="block text-sm md:col-span-2">
                  <span className="mb-1 block text-gray-600">mediaUrl</span>
                  <input
                    className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    dir="ltr"
                    value={itemDraft.mediaUrl}
                    onChange={(e) => setItemDraft((current) => ({ ...current, mediaUrl: e.target.value }))}
                  />
                </label>
              </div>
              <button
                type="button"
                className="mt-3 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-bold text-white"
                onClick={() => {
                  const add =
                    capabilities.mode === "seller"
                      ? addSellerStoryItem(selected.storyId, itemDraft)
                      : addAdminStoryItem(selected.storyId, itemDraft);
                  void add.then((result) => {
                    if (result.state === "ok") refresh();
                    else setMessage(result.message);
                  });
                }}
              >
                {STORY_COPY.addItem}
              </button>
            </>
          ) : null}
          {message ? <p className="mt-3 text-sm text-red-600">{message}</p> : null}
        </section>
      ) : null}

      {showCreate ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50 p-4">
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h2 className="mb-4 text-lg font-bold">{STORY_COPY.createModalTitle}</h2>
            <div className="space-y-3">
              {([
                ["title", "عنوان"],
                ["locale", "locale"],
                ["coverMediaUrl", "coverMediaUrl"],
                ["ctaType", "ctaType"],
                ["ctaTarget", "ctaTarget"],
              ] as const).map(([key, label]) => (
                <label key={key} className="block text-sm">
                  <span className="mb-1 block text-gray-600">{label}</span>
                  <input
                    className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    dir={key === "title" ? "rtl" : "ltr"}
                    value={draft[key]}
                    onChange={(e) => setDraft((current) => ({ ...current, [key]: e.target.value }))}
                  />
                </label>
              ))}
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>
                {STORY_COPY.cancel}
              </button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => {
                  const create =
                    capabilities.mode === "seller" ? createSellerStory(draft) : createAdminStory(draft);
                  void create.then(async (result) => {
                    if (!result.ok || !result.story) {
                      setMessage(result.message);
                      return;
                    }
                    if (capabilities.canPublish) {
                      await enableAdminStory(result.story.storyId);
                    }
                    setShowCreate(false);
                    setSelectedId(result.story.storyId);
                    refresh();
                  });
                }}
              >
                {capabilities.canPublish ? STORY_COPY.createAndPublish : STORY_COPY.createDraft}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
  );
}
