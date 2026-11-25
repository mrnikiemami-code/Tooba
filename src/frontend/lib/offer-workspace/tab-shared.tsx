import type { ReactNode } from "react";
import { Badge } from "../../design-system";

export function TabHeading({ title, hint }: { title: string; hint?: string }) {
  return (
    <div className="mb-3">
      <h3 className="text-base font-semibold">{title}</h3>
      {hint ? <p className="mt-1 text-xs leading-5 text-muted">{hint}</p> : null}
    </div>
  );
}

export function Note({
  tone = "neutral",
  children,
}: {
  tone?: "neutral" | "warning" | "danger";
  children: ReactNode;
}) {
  const cls =
    tone === "warning"
      ? "border-warning/40 bg-warning/10"
      : tone === "danger"
        ? "border-danger/40 bg-danger/10"
        : "border-border bg-secondary/40";
  return <div className={`rounded-ds border px-3 py-2 text-xs leading-5 ${cls}`}>{children}</div>;
}

export function FieldRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="grid gap-0.5">
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="break-words text-sm font-medium">{value}</dd>
    </div>
  );
}

export function StatusPill({ status, label }: { status: string; label: string }) {
  const tone =
    status === "Active"
      ? "success"
      : status === "Suspended"
        ? "warning"
        : status === "Archived"
          ? "neutral"
          : "pending";
  return <Badge tone={tone}>{label}</Badge>;
}

export function DirtyDot({ show, label }: { show: boolean; label: string }) {
  if (!show) return null;
  return (
    <span className="inline-flex items-center gap-1 text-[11px] text-warning" title={label}>
      <span className="h-1.5 w-1.5 rounded-full bg-warning" aria-hidden="true" />
      {label}
    </span>
  );
}
