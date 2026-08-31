"use client";

import Link from "next/link";
import type { LucideIcon } from "lucide-react";
import { useState } from "react";
import { cn } from "../cn";

/** تعریف یک عملیات سطر — صفحه callback/مسیر را تأمین می‌کند، نه design system. */
export type AppGridRowAction<T> = {
  id: string;
  label: string;
  icon: LucideIcon;
  href?: (row: T) => string;
  onClick?: (row: T) => void | Promise<void>;
  variant?: "default" | "destructive";
  visible?: (row: T) => boolean;
  disabled?: (row: T) => boolean;
  /** پیام confirm؛ false = بدون تأیید */
  confirm?: (row: T) => string | false;
  testId?: (row: T) => string;
};

type AppGridRowActionsCellProps<T> = {
  row: T;
  actions: AppGridRowAction<T>[];
  /** دکمه‌های کوچک‌تر برای ستون عملیات باریک. */
  compact?: boolean;
};

/** سلول عملیات سطر — آیکون‌های یکنواخت با برچسب دسترس‌پذیر. */
export function AppGridRowActionsCell<T>({ row, actions, compact = false }: AppGridRowActionsCellProps<T>) {
  const [busyId, setBusyId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | undefined>();

  const visibleActions = actions.filter((action) => action.visible?.(row) ?? true);

  async function runAction(action: AppGridRowAction<T>) {
    if (action.href) return;
    const confirmMessage = action.confirm?.(row);
    if (confirmMessage && !window.confirm(confirmMessage)) {
      return;
    }
    if (!action.onClick) return;
    setBusyId(action.id);
    setMessage(undefined);
    try {
      await action.onClick(row);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "خطا");
    } finally {
      setBusyId(null);
    }
  }

  const iconSize = compact ? "size-8" : "size-10";
  const iconClass = compact ? "size-3.5" : "size-4";

  return (
    <div className="app-grid-cell-content">
      <div className={cn("relative flex items-center justify-center px-0.5", compact ? "gap-1" : "gap-2")}>
        {visibleActions.map((action) => {
          const Icon = action.icon;
          const disabled = busyId !== null || (action.disabled?.(row) ?? false);
          const className =
            action.variant === "destructive"
              ? `inline-flex ${iconSize} shrink-0 items-center justify-center rounded-full border border-danger/30 bg-surface text-danger transition-colors hover:bg-danger/10 disabled:opacity-50`
              : `inline-flex ${iconSize} shrink-0 items-center justify-center rounded-full border border-border bg-surface text-muted transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50`;
          const testId = action.testId?.(row) ?? `app-grid-action-${action.id}`;
          if (action.href) {
            return (
              <Link
                key={action.id}
                href={action.href(row)}
                className={className}
                aria-label={action.label}
                title={action.label}
                data-testid={testId}
              >
                <Icon className={iconClass} aria-hidden />
              </Link>
            );
          }
          return (
            <button
              key={action.id}
              type="button"
              disabled={disabled}
              onClick={() => void runAction(action)}
              className={className}
              aria-label={action.label}
              title={action.label}
              data-testid={testId}
            >
              <Icon className={iconClass} aria-hidden />
            </button>
          );
        })}
        {message ? <p className="absolute top-full mt-1 max-w-[10rem] text-xs text-danger">{message}</p> : null}
      </div>
    </div>
  );
}
