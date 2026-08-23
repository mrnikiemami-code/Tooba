"use client";

import { useEffect, useState, type ReactNode } from "react";
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
  breadcrumbs: string[];
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
}

/**
 * پوستهٔ Workspace عمومی. صفحهٔ CRUD ماژول دامنه نیست.
 */
export function WorkspaceShell({
  title,
  subtitle,
  breadcrumbs,
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

  return (
    <div className="flex flex-col gap-3 rounded-ds border border-border bg-surface p-3">
      <header>
        <nav aria-label="breadcrumb" className="text-xs text-muted">
          {breadcrumbs.join(" / ")}
        </nav>
        <div className="mt-2 flex flex-wrap items-start justify-between gap-2">
          <div>
            <h2 className="text-lg font-semibold">{title}</h2>
            {subtitle ? <p className="text-sm text-muted">{subtitle}</p> : null}
            {readOnly ? <p className="text-sm text-warning">{messages.permissionDenied}</p> : null}
          </div>
          <div className="flex flex-wrap gap-2">
            {primary ? (
              <Button type="button" disabled={primary.permission === "denied" || primary.busy || readOnly} onClick={() => onAction?.(primary.id)}>
                {primary.label}
              </Button>
            ) : null}
            {secondary.map((action) => (
              <Button key={action.id} type="button" tone="secondary" disabled={action.permission === "denied" || action.busy} onClick={() => onAction?.(action.id)}>
                {action.label}
              </Button>
            ))}
            {destructive ? (
              <Button type="button" tone="danger" disabled={destructive.permission === "denied" || destructive.busy} onClick={() => onAction?.(destructive.id)}>
                {destructive.label}
              </Button>
            ) : null}
            {overflow.length > 0 ? (
              <select
                aria-label={messages.moreActions}
                className="min-h-11 rounded-ds border border-border bg-surface px-2 text-sm"
                defaultValue=""
                onChange={(event) => event.target.value && onAction?.(event.target.value)}
              >
                <option value="">{messages.moreActions}</option>
                {overflow.map((action) => (
                  <option key={action.id} value={action.id} disabled={action.permission === "denied"}>
                    {action.label}
                  </option>
                ))}
              </select>
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
                  item.tone === "danger" ? "bg-danger/15 text-danger" : item.tone === "warning" ? "bg-warning/15" : item.tone === "success" ? "bg-success/15" : "bg-secondary",
                )}
              >
                {item.label}
              </li>
            ))}
          </ul>
        ) : null}
      </header>
      {narrow ? (
        <select className="min-h-11 w-full rounded-ds border border-border bg-surface px-2" value={activeSectionId} onChange={(event) => onSectionChange(event.target.value)} aria-label="section">
          {sections.map((section) => (
            <option key={section.id} value={section.id}>
              {section.label}
            </option>
          ))}
        </select>
      ) : (
        <div className="flex flex-wrap gap-2" role="tablist">
          {sections.map((section) => (
            <Button key={section.id} type="button" tone={section.id === activeSectionId ? "primary" : "ghost"} onClick={() => onSectionChange(section.id)}>
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
        <div className="rounded-ds border border-warning/40 p-3" role="alert">
          <p>{conflict}</p>
          <Button type="button" tone="secondary" className="mt-2" onClick={onReloadConflict}>
            {messages.reload}
          </Button>
        </div>
      ) : null}
      {emptyKind ? <EmptyState title={emptyKind === "no-permission" ? messages.permissionDenied : emptyKind === "not-found" ? messages.notFound : emptyKind} /> : null}
      {!loading && !error && !emptyKind ? (
        <div className={cn("grid gap-3", narrow ? "grid-cols-1" : "lg:grid-cols-[minmax(0,1fr)_16rem]")}>
          <div className="flex flex-col gap-3">
            {summary ? <section className="rounded-ds border border-border p-3">{summary}</section> : null}
            <section className="rounded-ds border border-border p-3">{children}</section>
          </div>
          {narrow ? (
            <Button type="button" tone="secondary" onClick={() => setInspectorOpen(true)}>
              {messages.details}
            </Button>
          ) : (
            <aside className="flex flex-col gap-3">
              {inspector}
              <Feed title={messages.history} items={activity.map((item) => ({ id: item.id, body: item.summary, meta: `${item.actor} · ${item.at}` }))} />
              <Feed title="audit" items={audit.map((item) => ({ id: item.id, body: item.event, meta: `${item.actor} · ${item.at}` }))} />
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
