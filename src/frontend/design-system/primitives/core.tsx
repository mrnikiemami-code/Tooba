import { cloneElement, isValidElement, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactElement, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";
import { cn } from "../cn";

type ButtonTone = "primary" | "secondary" | "danger" | "ghost";

/**
 * دکمهٔ معنایی با واریانت کنترل‌شده. رنگ خام در مصرف‌کننده نباید اضافه شود.
 */
export function Button({
  tone = "primary",
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { tone?: ButtonTone }) {
  const tones: Record<ButtonTone, string> = {
    primary: "bg-primary text-primary-foreground",
    secondary: "bg-secondary text-secondary-foreground",
    danger: "bg-danger text-white",
    ghost: "bg-transparent text-foreground",
  };
  return (
    <button
      className={cn(
        "inline-flex min-h-11 items-center justify-center rounded-ds px-4 text-sm font-medium disabled:opacity-50",
        tones[tone],
        className,
      )}
      {...props}
    />
  );
}

/**
 * دکمهٔ فقط‌آیکون. برچسب دسترس‌پذیر اجباری است و نباید خالی باشد.
 */
export function IconButton({
  label,
  className,
  children,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { label: string; children: ReactNode }) {
  if (!label.trim()) {
    throw new Error("IconButton requires a non-empty accessible label");
  }
  return (
    <button
      type="button"
      aria-label={label}
      className={cn("inline-flex min-h-11 min-w-11 items-center justify-center rounded-ds bg-secondary", className)}
      {...props}
    >
      {children}
    </button>
  );
}

/**
 * ورودی متن با پشتیبانی از جزیرهٔ LTR برای ایمیل/تلفن.
 */
export function Input({
  invalid,
  ltrIsland,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean; ltrIsland?: boolean }) {
  return (
    <input
      dir={ltrIsland ? "ltr" : undefined}
      aria-invalid={invalid || undefined}
      className={cn(
        "min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-sm",
        invalid && "border-danger",
        className,
      )}
      {...props}
    />
  );
}

/** ناحیهٔ متن چندخطی با همان قرارداد ورودی. */
export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cn("min-h-24 w-full rounded-ds border border-border bg-surface p-3 text-sm", className)} {...props} />;
}

/** انتخاب بومی؛ فهرست سفارشی سنگین در این تسک اضافه نمی‌شود. */
export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={cn("min-h-11 w-full rounded-ds border border-border bg-surface px-3 text-sm", className)} {...props} />;
}

/** چک‌باکس با برچسب مجاور. */
export function Checkbox({ label, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <label className="inline-flex items-center gap-2 text-sm">
      <input type="checkbox" className="h-4 w-4" {...props} />
      {label}
    </label>
  );
}

/** رادیو با برچسب مجاور. */
export function Radio({ label, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <label className="inline-flex items-center gap-2 text-sm">
      <input type="radio" className="h-4 w-4" {...props} />
      {label}
    </label>
  );
}

/** سوییچ دودویی مبتنی بر چک‌باکس تا وابستگی overlay اضافه نشود. */
export function Switch({ label, checked, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <label className="inline-flex items-center gap-2 text-sm">
      <input type="checkbox" role="switch" checked={checked} className="h-4 w-8" {...props} />
      {label}
    </label>
  );
}

type StatusTone = "neutral" | "info" | "success" | "warning" | "danger" | "pending";

const statusClass: Record<StatusTone, string> = {
  neutral: "bg-secondary text-secondary-foreground",
  info: "bg-info/15 text-info",
  success: "bg-success/15 text-success",
  warning: "bg-warning/15 text-warning",
  danger: "bg-danger/15 text-danger",
  pending: "bg-muted/20 text-muted",
};

/**
 * نشان وضعیت. نگاشت وضعیت دامنه باید در لایهٔ ویو انجام شود نه اینجا.
 */
export function Badge({ tone = "neutral", children }: { tone?: StatusTone; children: ReactNode }) {
  return <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs", statusClass[tone])}>{children}</span>;
}

/** برچسب فیلتر/کلمهٔ کلیدی. */
export function Chip({ children }: { children: ReactNode }) {
  return <span className="inline-flex rounded-ds bg-secondary px-2 py-1 text-xs">{children}</span>;
}

/** جداکنندهٔ افقی. */
export function Separator() {
  return <hr className="border-border" />;
}

/** سطح کارت برای ترکیب workspace نه CRUD ماژول. */
export function Card({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={cn("rounded-ds border border-border bg-surface p-4 shadow-ds", className)}>{children}</section>;
}

/** اسکلتون بارگذاری بدون timeout جعلی. */
export function Skeleton({ className }: { className?: string }) {
  return <div className={cn("h-4 animate-pulse rounded-ds bg-secondary", className)} />;
}

/** نشانگر پیشرفت نامعین. */
export function Spinner() {
  return <span role="status" aria-label="در حال بارگذاری" className="inline-block h-5 w-5 animate-spin rounded-full border-2 border-border border-t-primary" />;
}

/** هشدار درون‌صفحه با نقش مناسب. */
export function Alert({ tone = "info", children }: { tone?: Exclude<StatusTone, "pending" | "neutral">; children: ReactNode }) {
  return (
    <div role="alert" className={cn("rounded-ds border border-border p-3 text-sm", statusClass[tone])}>
      {children}
    </div>
  );
}

/** حالت خالی کنترل‌شده. */
export function EmptyState({ title, detail }: { title: string; detail?: string }) {
  return (
    <div className="rounded-ds border border-dashed border-border p-6 text-center">
      <p className="font-medium">{title}</p>
      {detail ? <p className="mt-1 text-sm text-muted">{detail}</p> : null}
    </div>
  );
}

/** حالت خطا با برچسب تلاش مجدد از درز i18n، نه متن فارسی سخت‌کد. */
export function ErrorState({
  title,
  detail,
  onRetry,
  retryLabel = "Retry",
}: {
  title: string;
  detail?: string;
  onRetry?: () => void;
  retryLabel?: string;
}) {
  return (
    <div className="rounded-ds border border-danger/40 p-6">
      <p className="font-medium text-danger">{title}</p>
      {detail ? <p className="mt-1 text-sm text-muted">{detail}</p> : null}
      {onRetry ? (
        <Button className="mt-3" tone="secondary" type="button" onClick={onRetry}>
          {retryLabel}
        </Button>
      ) : null}
    </div>
  );
}

/**
 * فیلد فرم با برچسب، راهنما و خطای مرتبط از طریق aria-describedby.
 */
export function Field({
  id,
  label,
  hint,
  error,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  children: ReactNode;
}) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;
  const control =
    isValidElement(children)
      ? cloneElement(children as ReactElement<{ id?: string; "aria-describedby"?: string; "aria-invalid"?: boolean }>, {
          id,
          "aria-describedby": describedBy,
          "aria-invalid": error ? true : undefined,
        })
      : children;
  return (
    <div className="grid gap-1">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {control}
      {hint ? (
        <p id={hintId} className="text-xs text-muted">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-xs text-danger">
          {error}
        </p>
      ) : null}
    </div>
  );
}
