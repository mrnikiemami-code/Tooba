"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { cn } from "../cn";
import { Button, EmptyState, ErrorState, Skeleton, Spinner } from "../primitives/core";
import { Dialog, Drawer } from "../primitives/overlays";
import { faWorkspaceMessages } from "./messages";
import { hasUnsavedChanges } from "./state";
import type {
  WorkspaceAction,
  WorkspaceActivityItem,
  WorkspaceAuditItem,
  WorkspaceEmptyKind,
  WorkspaceMessages,
  WorkspaceSection,
  WorkspaceStatusItem,
} from "./types";

/**
 * قرارداد پوسته: عنوان، ناوبری بخش، فرمان، خلاصه، محتوا، بازرس و گارد ذخیره نشده.
 * این props صفحهٔ CRUD ماژول دامنه نیست و نباید به API محصول/سفارش گره بخورد.
 */
export interface WorkspaceShellProps {
  title: string;
  subtitle?: string;
  breadcrumbs?: string[];
  statusItems?: WorkspaceStatusItem[];
  sections: WorkspaceSection[];
  activeSectionId: string;
  onSectionChange: (sectionId: string) => void;
  actions?: WorkspaceAction[];
  onAction?: (actionId: string) => void;
  summary?: ReactNode;
  children: ReactNode;
  inspector?: ReactNode;
  activity?: WorkspaceActivityItem[];
  audit?: WorkspaceAuditItem[];
  messages?: WorkspaceMessages;
  loading?: boolean;
  emptyKind?: WorkspaceEmptyKind | null;
  error?: string | null;
  onRetry?: () => void;
  readOnly?: boolean;
  conflict?: string | null;
  onReloadConflict?: () => void;
  dirtySections?: ReadonlySet<string>;
  pendingSectionId?: string | null;
  onDiscardNavigation?: () => void;
  onStay?: () => void;
  forceNarrow?: boolean;
  leading?: ReactNode;
  flush?: boolean;
}

/**
 * پوستهٔ Workspace عمومی. صفحهٔ CRUD ماژول دامنه نیست.
 */
export function WorkspaceShell({
  title,
  subtitle,
  breadcrumbs = [],
  statusItems = [],
  sections,
  activeSectionId,
  onSectionChange,
  actions = [],
  onAction,
  summary,
  children,
  inspector,
  activity = [],
  audit = [],
  messages = faWorkspaceMessages,
  loading,
  emptyKind,
  error,
  onRetry,
  readOnly,
  conflict,
  onReloadConflict,
  dirtySections = new Set(),
  pendingSectionId,
  onDiscardNavigation,
  onStay,
  forceNarrow,
  leading,
  flush,
}: WorkspaceShellProps) {
  const [narrow, setNarrow] = useState(false);
  const [inspectorOpen, setInspectorOpen] = useState(false);

  useEffect(() => {
    if (forceNarrow != null) {
      setNarrow(forceNarrow);
      return;
    }
    const media = window.matchMedia("(max-width: 767px)");
    const sync = () => setNarrow(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, [forceNarrow]);

  const primary = actions.find((action) => action.kind === "primary" && action.permission !== "hidden");
  const secondary = actions.filter((action) => action.kind === "secondary" && action.permission !== "hidden");
  const destructive = actions.find((action) => action.kind === "destructive" && action.permission !== "hidden");
  const overflow = actions.filter((action) => (action.kind === "overflow" || action.kind === "contextual") && action.permission !== "hidden");
  const [overflowOpen, setOverflowOpen] = useState(false);
  const overflowRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!overflowOpen) return;
    const onDoc = (event: MouseEvent) => {
      if (!overflowRef.current?.contains(event.target as Node)) setOverflowOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [overflowOpen]);

  const splitOverflow = primary && overflow.length > 0 ? overflow : [];
  const loneOverflow = !primary ? overflow : [];

  return (
    <div className={cn("flex w-full flex-col gap-5 bg-background", flush ? "p-4 md:p-6" : "rounded-ds border border-border bg-surface p-6 shadow-md md:p-8")}>
      <header>
        <nav aria-label="breadcrumb" className="text-sm text-muted">
          {breadcrumbs.join(" / ")}
        </nav>
        <div className="mt-3 flex flex-wrap items-start justify-between gap-3">
          <div className="flex min-w-0 items-start gap-4">
            {leading}
            <div className="min-w-0">
              <h2 className="text-3xl font-semibold tracking-tight">{title}</h2>
              {subtitle ? <p className="mt-1 text-base text-muted">{subtitle}</p> : null}
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2" data-testid="workspace-action-cluster">
            {primary ? (
              <div className="inline-flex overflow-hidden rounded-xl shadow-sm" data-testid="workspace-primary-split" ref={splitOverflow.length ? overflowRef : undefined}>
                <Button
                  type="button"
                  className={cn("rounded-none", splitOverflow.length > 0 ? "rounded-s-xl" : "rounded-xl")}
                  disabled={primary.permission === "denied" || primary.busy || readOnly}
                  onClick={() => onAction?.(primary.id)}
                  data-testid={`workspace-action-${primary.id}`}
                >
                  {primary.label}
                </Button>
                {splitOverflow.length > 0 ? (
                  <div className="relative border-s border-white/20">
                    <button
                      type="button"
                      className="inline-flex min-h-11 min-w-11 items-center justify-center bg-primary px-2 text-sm text-primary-foreground hover:brightness-95 disabled:opacity-50"
                      aria-label={messages.moreActions}
                      aria-expanded={overflowOpen}
                      data-testid="workspace-primary-split-toggle"
                      disabled={readOnly}
                      onClick={() => setOverflowOpen((open) => !open)}
                    >
                      ▾
                    </button>
                    {overflowOpen ? (
                      <ul
                        className="absolute end-0 z-20 mt-1 min-w-[11rem] overflow-hidden rounded-xl border border-border bg-surface py-1 shadow-lg"
                        role="menu"
                        data-testid="workspace-primary-split-menu"
                      >
                        {splitOverflow.map((action) => (
                          <li key={action.id} role="none">
                            <button
                              type="button"
                              role="menuitem"
                              disabled={action.permission === "denied" || action.busy}
                              className="flex w-full px-3 py-2 text-start text-sm hover:bg-secondary disabled:opacity-40"
                              onClick={() => {
                                setOverflowOpen(false);
                                onAction?.(action.id);
                              }}
                            >
                              {action.label}
                            </button>
                          </li>
                        ))}
                      </ul>
                    ) : null}
                  </div>
                ) : null}
              </div>
            ) : null}
            {secondary.map((action) => (
              <Button
                key={action.id}
                type="button"
                tone="secondary"
                disabled={action.permission === "denied" || action.busy}
                onClick={() => onAction?.(action.id)}
                data-testid={`workspace-action-${action.id}`}
              >
                {action.label}
              </Button>
            ))}
            {destructive ? (
              <Button type="button" tone="danger" disabled={destructive.permission === "denied" || destructive.busy} onClick={() => onAction?.(destructive.id)}>
                {destructive.label}
              </Button>
            ) : null}
            {loneOverflow.length > 0 ? (
              <div className="relative" ref={overflowRef}>
                <Button type="button" tone="secondary" aria-expanded={overflowOpen} onClick={() => setOverflowOpen((open) => !open)}>
                  {messages.moreActions}
                </Button>
                {overflowOpen ? (
                  <ul className="absolute end-0 z-20 mt-1 min-w-[11rem] overflow-hidden rounded-xl border border-border bg-surface py-1 shadow-lg" role="menu">
                    {loneOverflow.map((action) => (
                      <li key={action.id} role="none">
                        <button
                          type="button"
                          role="menuitem"
                          disabled={action.permission === "denied" || action.busy}
                          className="flex w-full px-3 py-2 text-start text-sm hover:bg-secondary disabled:opacity-40"
                          onClick={() => {
                            setOverflowOpen(false);
                            onAction?.(action.id);
                          }}
                        >
                          {action.label}
                        </button>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
            ) : null}
          </div>
        </div>
        {statusItems.length > 0 ? (
          <ul className="mt-2 flex flex-wrap gap-2">
            {statusItems.map((item) => (
              <li
                key={item.id}
                className={cn(
                  "rounded-ds px-2 py-1 text-xs",
                  item.tone === "danger"
                    ? "bg-danger/15 text-danger"
                    : item.tone === "warning"
                      ? "bg-warning/15"
                      : item.tone === "success"
                        ? "bg-success/15"
                        : "bg-secondary",
                )}
              >
                {item.label}
              </li>
            ))}
          </ul>
        ) : null}
      </header>
      {summary ? (
        <section className="rounded-ds border border-border bg-surface p-3" data-testid="workspace-summary-strip">
          {summary}
        </section>
      ) : null}
      {narrow ? (
        <select className="min-h-11 w-full rounded-ds border border-border bg-surface px-2" value={activeSectionId} onChange={(event) => onSectionChange(event.target.value)} aria-label="section">
          {sections.map((section) => (
            <option key={section.id} value={section.id}>
              {section.label}
            </option>
          ))}
        </select>
      ) : (
        <div className="flex flex-nowrap gap-2 overflow-x-auto pb-1" role="tablist">
          {sections.map((section) => (
            <Button
              key={section.id}
              type="button"
              className="shrink-0"
              tone={section.id === activeSectionId ? "primary" : "ghost"}
              onClick={() => onSectionChange(section.id)}
            >
              {section.label}
            </Button>
          ))}
        </div>
      )}
      {loading ? (
        <div aria-busy="true">
          <Spinner />
          <Skeleton className="mt-2 h-24 w-full" />
        </div>
      ) : null}
      {error ? <ErrorState title={error} onRetry={onRetry} retryLabel={messages.retry} /> : null}
      {conflict ? (
        <div className="rounded-ds border border-warning bg-warning/10 p-5" role="alert">
          <p className="text-lg font-semibold">این محصول را کاربر دیگری تغییر داده است.</p>
          <p className="mt-1 text-base">نسخهٔ تازه را بارگذاری کنید، تغییرات را بازبینی کنید، یا پیش‌نویس محلی را کنار بگذارید.</p>
          <p className="mt-1 text-sm text-muted">{conflict}</p>
          <div className="mt-4 flex flex-wrap gap-2">
            <Button type="button" onClick={onReloadConflict}>
              بارگذاری نسخهٔ تازه
            </Button>
            <Button type="button" tone="secondary" onClick={onReloadConflict}>
              بازبینی تغییرات
            </Button>
            <Button type="button" tone="ghost" onClick={onReloadConflict}>
              صرف‌نظر از پیش‌نویس محلی
            </Button>
          </div>
        </div>
      ) : null}
      {emptyKind ? <EmptyState title={emptyKind === "no-permission" ? messages.permissionDenied : emptyKind === "not-found" ? messages.notFound : emptyKind} /> : null}
      {!loading && !error && !emptyKind ? (
        <div className={cn("grid gap-4", narrow ? "grid-cols-1" : "xl:grid-cols-[minmax(0,1fr)_20rem]")}>
          <div className="flex flex-col gap-3">
            <section className="rounded-ds border border-border p-3 md:p-4">{children}</section>
          </div>
          {narrow ? (
            <Button type="button" tone="secondary" onClick={() => setInspectorOpen(true)}>
              {messages.details}
            </Button>
          ) : (
            <aside className="flex flex-col gap-3">
              {inspector}
              <Feed title={messages.history} items={activity.map((item) => ({ id: item.id, body: item.summary, meta: `${item.actor} · ${item.at}` }))} />
              <Feed title="حسابرسی" items={audit.map((item) => ({ id: item.id, body: item.event, meta: `${item.actor} · ${item.at}` }))} />
            </aside>
          )}
        </div>
      ) : null}
      {narrow && primary ? (
        <div className="sticky bottom-0 border-t border-border bg-surface py-2">
          <Button type="button" className="w-full" disabled={primary.permission === "denied" || readOnly} onClick={() => onAction?.(primary.id)}>
            {primary.label}
          </Button>
        </div>
      ) : null}
      <Drawer title={messages.details} open={inspectorOpen} onClose={() => setInspectorOpen(false)}>
        {inspector}
        <Feed title={messages.history} items={activity.map((item) => ({ id: item.id, body: item.summary, meta: `${item.actor} · ${item.at}` }))} />
      </Drawer>
      <Dialog title={messages.unsaved} open={Boolean(pendingSectionId) && hasUnsavedChanges(dirtySections)} onClose={() => onStay?.()}>
        <p className="text-sm">{messages.unsaved}</p>
        <div className="mt-3 flex gap-2">
          <Button type="button" tone="secondary" onClick={onStay}>
            {messages.cancel}
          </Button>
          <Button type="button" tone="danger" onClick={onDiscardNavigation}>
            {messages.discard}
          </Button>
        </div>
      </Dialog>
    </div>
  );
}

function Feed({ title, items }: { title: string; items: Array<{ id: string; body: string; meta: string }> }) {
  if (items.length === 0) {
    return null;
  }
  return (
    <section className="rounded-ds border border-border p-3 text-sm">
      <h3 className="font-medium">{title}</h3>
      <ol className="mt-2 space-y-2">
        {items.map((item) => (
          <li key={item.id}>
            <p>{item.body}</p>
            <p className="text-xs text-muted">{item.meta}</p>
          </li>
        ))}
      </ol>
    </section>
  );
}
