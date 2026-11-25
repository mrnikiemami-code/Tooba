import type { ReactNode } from "react";
import { AlertTriangle, Info } from "lucide-react";
import styles from "./offer-workspace.module.css";

export function SectionTitle({ title, desc }: { title: string; desc?: string }) {
  return (
    <div className={styles.cardHdMain}>
      <h2 className={styles.cardTitle}>{title}</h2>
      {desc ? <p className={styles.cardDesc}>{desc}</p> : null}
    </div>
  );
}

export function Note({
  tone = "neutral",
  children,
}: {
  tone?: "neutral" | "info" | "warning" | "danger" | "ok";
  children: ReactNode;
}) {
  const cls =
    tone === "warning"
      ? styles.bannerWarn
      : tone === "danger"
        ? styles.bannerDanger
        : tone === "info"
          ? styles.bannerInfo
          : tone === "ok"
            ? styles.bannerOk
            : "";
  return (
    <div className={`${styles.banner} ${cls}`} role={tone === "danger" ? "alert" : undefined}>
      {tone === "danger" || tone === "warning" ? <AlertTriangle size={14} aria-hidden /> : tone === "info" ? <Info size={14} aria-hidden /> : null}
      <div>{children}</div>
    </div>
  );
}

export function FieldRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className={styles.field}>
      <span className={styles.fieldLabel}>{label}</span>
      <div className={`${styles.ctl} ${styles.ctlReadonly}`} style={{ display: "flex", alignItems: "center" }}>
        {value}
      </div>
    </div>
  );
}

export function StatusPill({ status, label }: { status: string; label: string }) {
  const cls =
    status === "Active"
      ? styles.pillOk
      : status === "Suspended"
        ? styles.pillWarn
        : status === "Archived"
          ? styles.pillNeutral
          : styles.pillWarn;
  return <span className={`${styles.pill} ${cls}`}>{label}</span>;
}

export function DirtyDot({ show, label }: { show: boolean; label: string }) {
  if (!show) return null;
  return (
    <span className={styles.tabDot} title={label} aria-label={label} role="img" />
  );
}

export function ReadOnlyChip({ children }: { children: ReactNode }) {
  return <span className={styles.chip}>{children}</span>;
}

export function InfoIcon() {
  return <Info size={14} aria-hidden />;
}
