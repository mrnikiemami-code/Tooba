"use client";

import type { ReactNode } from "react";

export type StoryTone = "neutral" | "info" | "success" | "warning" | "danger";

const toneClasses: Record<StoryTone, string> = {
  neutral: "bg-secondary text-secondary-foreground",
  info: "bg-info/10 text-info",
  success: "bg-success/10 text-success",
  warning: "bg-warning/10 text-warning",
  danger: "bg-danger/10 text-danger",
};

function joinClasses(...values: Array<string | false | null | undefined>): string {
  return values.filter(Boolean).join(" ");
}

/** یک وضعیت کوچک و خوانا با رنگ معنایی — بدون نمایش enum خام. */
export function StoryStatusPill({
  label,
  tone = "neutral",
  testId,
}: {
  label: string;
  tone?: StoryTone;
  testId?: string;
}) {
  return (
    <span
      data-testid={testId}
      className={joinClasses(
        "inline-flex max-w-full items-center gap-1.5 truncate rounded-full px-2.5 py-1 text-xs font-bold",
        toneClasses[tone],
      )}
    >
      <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-current opacity-70" aria-hidden="true" />
      <span className="truncate">{label}</span>
    </span>
  );
}

/** دکمه‌های مشترک Story تا وزن بصری همهٔ اقدام‌ها یکدست بماند. */
export function storyButtonClass(
  kind: "primary" | "secondary" | "danger" | "ghost" = "secondary",
): string {
  const base =
    "inline-flex min-h-11 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-bold transition-colors disabled:cursor-not-allowed disabled:opacity-50";
  if (kind === "primary") {
    return joinClasses(base, "bg-primary text-primary-foreground shadow-sm hover:bg-primary-strong");
  }
  if (kind === "danger") {
    return joinClasses(base, "border border-danger/30 bg-danger/10 text-danger hover:bg-danger/15");
  }
  if (kind === "ghost") {
    return joinClasses(base, "text-muted hover:bg-secondary hover:text-foreground");
  }
  return joinClasses(base, "border border-border bg-surface text-foreground hover:bg-secondary");
}

/** سرصفحهٔ یکپارچهٔ فضای استوری؛ برچسب مسیر بدون لینک تزئینی. */
export function StoryPageHeader({
  breadcrumb,
  title,
  description,
  actions,
}: {
  breadcrumb: string;
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  return (
    <header className="mb-5 overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
      <div className="h-1 w-full bg-primary/80" aria-hidden="true" />
      <div className="flex flex-col gap-4 p-5 xl:flex-row xl:items-center xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-medium text-muted">
            {breadcrumb}
            <span className="px-1.5 text-border" aria-hidden="true">
              /
            </span>
            {title}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight text-foreground">{title}</h1>
          <p className="mt-1 max-w-2xl text-sm leading-6 text-muted">{description}</p>
        </div>
        {actions ? <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
    </header>
  );
}

/** کارت بخش‌ها با یک هدر مشخص، بدون تکرار پنل‌های تزئینی. */
export function StorySectionCard({
  title,
  description,
  actions,
  children,
  testId,
  className,
  bodyClassName,
  padded = true,
}: {
  title?: string;
  description?: string;
  actions?: ReactNode;
  children: ReactNode;
  testId?: string;
  className?: string;
  bodyClassName?: string;
  padded?: boolean;
}) {
  return (
    <section
      data-testid={testId}
      className={joinClasses(
        "overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm",
        className,
      )}
    >
      {title || actions ? (
        <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border px-5 py-4">
          <div className="min-w-0">
            {title ? <h2 className="text-base font-bold text-foreground">{title}</h2> : null}
            {description ? <p className="mt-1 text-xs leading-5 text-muted">{description}</p> : null}
          </div>
          {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
        </div>
      ) : null}
      <div className={joinClasses(padded ? "p-5" : "", bodyClassName)}>{children}</div>
    </section>
  );
}

/** نوار اقدام‌های یک استوری؛ تنها یک محل برای اقدام‌های آن. */
export function StoryActionBar({ children }: { children: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center gap-2 rounded-xl border border-border bg-background/40 p-3">
      {children}
    </div>
  );
}


/** حالت خالی قابل‌فهم برای بخش اسلایدها/پیش‌نمایش. */
export function StoryEmptyState({
  title,
  description,
  icon,
  action,
  compact = false,
  testId,
}: {
  title: string;
  description: string;
  icon?: ReactNode;
  action?: ReactNode;
  compact?: boolean;
  testId?: string;
}) {
  return (
    <div
      data-testid={testId}
      className={joinClasses(
        "flex flex-col items-center justify-center rounded-xl border border-dashed border-border bg-background/30 text-center",
        compact ? "gap-2 px-4 py-5" : "gap-3 px-5 py-8",
      )}
    >
      {icon ? (
        <span className="flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-muted">
          {icon}
        </span>
      ) : null}
      <div>
        <p className="text-sm font-bold text-foreground">{title}</p>
        <p className="mt-1 max-w-sm text-xs leading-5 text-muted">{description}</p>
      </div>
      {action ? <div className="pt-1">{action}</div> : null}
    </div>
  );
}
