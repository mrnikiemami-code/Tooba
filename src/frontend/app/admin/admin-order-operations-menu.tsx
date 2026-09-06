"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ChevronDown, MoreHorizontal } from "lucide-react";
import {
  executeAdminOrderOperation,
  loadAdminOrderOperations,
  type AdminOrderOperationAction,
} from "./admin-order-operations";
import { mapAdminErrorMessage } from "./admin-error-map";

type Props = {
  checkoutId: string;
  /** برچسب دکمه؛ پیش‌فرض «عملیات». */
  label?: string;
  compact?: boolean;
  onCompleted?: () => void;
  testId?: string;
};

function promptForExtras(action: AdminOrderOperationAction): {
  carrierDisplayName?: string;
  trackingReference?: string;
  cancelled?: boolean;
} {
  if (action.code === "create_shipment") {
    const carrier = window.prompt("نام حمل‌کننده را وارد کنید:", "پست");
    if (!carrier || !carrier.trim()) return { cancelled: true };
    return { carrierDisplayName: carrier.trim() };
  }
  if (action.code === "assign_tracking") {
    const tracking = window.prompt("کد پیگیری را وارد کنید:");
    if (!tracking || !tracking.trim()) return { cancelled: true };
    return { trackingReference: tracking.trim() };
  }
  return {};
}

/** منوی یک‌دکمه‌ای عملیات سفارش — فقط actions برگشتی از API. */
export function AdminOrderOperationsMenu({
  checkoutId,
  label = "عملیات",
  compact = false,
  onCompleted,
  testId,
}: Props) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [pending, setPending] = useState(false);
  const [actions, setActions] = useState<AdminOrderOperationAction[]>([]);
  const [error, setError] = useState<string | null>(null);
  const rootRef = useRef<HTMLDivElement>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await loadAdminOrderOperations(checkoutId);
    setLoading(false);
    if (result.state === "denied") {
      setActions([]);
      setError(mapAdminErrorMessage("admin.authorization.denied", "fa"));
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setActions([]);
      setError(result.message ?? mapAdminErrorMessage(null, "fa"));
      return;
    }
    setActions(result.data.actions);
  }, [checkoutId]);

  useEffect(() => {
    if (!open) return;
    void refresh();
  }, [open, refresh]);

  useEffect(() => {
    if (!open) return;
    function onDocClick(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, [open]);

  async function runAction(action: AdminOrderOperationAction) {
    if (action.requiresConfirm) {
      const message = action.confirmMessageFa || `اجرای «${action.labelFa}»؟`;
      if (!window.confirm(message)) return;
    }
    const extras = promptForExtras(action);
    if (extras.cancelled) return;

    setPending(true);
    setError(null);
    const result = await executeAdminOrderOperation(checkoutId, {
      code: action.code,
      sellerOrderId: action.sellerOrderId,
      fulfillmentId: action.fulfillmentId,
      shipmentId: action.shipmentId,
      returnRequestId: action.returnRequestId,
      carrierDisplayName: extras.carrierDisplayName,
      trackingReference: extras.trackingReference,
    });
    setPending(false);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      setError(/^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw);
      return;
    }
    setOpen(false);
    await refresh();
    onCompleted?.();
  }

  const buttonClass = compact
    ? "inline-flex h-8 items-center gap-1 rounded-full border border-border bg-surface px-2.5 text-xs font-bold text-muted hover:bg-secondary hover:text-foreground disabled:opacity-50"
    : "inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50 disabled:opacity-50";

  return (
    <div ref={rootRef} className="relative inline-flex" data-testid={testId ?? `admin-order-ops-${checkoutId}`}>
      <button
        type="button"
        className={buttonClass}
        disabled={pending}
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((value) => !value)}
        data-testid={`admin-order-ops-trigger-${checkoutId}`}
      >
        {compact ? <MoreHorizontal className="size-3.5" aria-hidden /> : null}
        <span>{label}</span>
        {!compact ? <ChevronDown className="size-3.5" aria-hidden /> : null}
      </button>
      {open ? (
        <div
          role="menu"
          className="absolute end-0 top-full z-40 mt-1 min-w-[12rem] rounded-xl border border-gray-200 bg-white py-1 shadow-lg"
          data-testid={`admin-order-ops-menu-${checkoutId}`}
        >
          {loading ? (
            <p className="px-3 py-2 text-xs text-gray-500">در حال بارگذاری…</p>
          ) : actions.length === 0 ? (
            <p className="px-3 py-2 text-xs text-gray-500">عملیات مجازی نیست</p>
          ) : (
            actions.map((action) => (
              <button
                key={`${action.code}-${action.sellerOrderId ?? ""}-${action.shipmentId ?? ""}-${action.returnRequestId ?? ""}`}
                type="button"
                role="menuitem"
                disabled={pending}
                className="block w-full px-3 py-2 text-right text-xs font-semibold text-gray-800 hover:bg-gray-50 disabled:opacity-50"
                onClick={() => void runAction(action)}
                data-testid={`admin-order-ops-action-${action.code}`}
              >
                {action.labelFa}
              </button>
            ))
          )}
          {error ? <p className="border-t border-gray-100 px-3 py-2 text-xs text-danger">{error}</p> : null}
        </div>
      ) : null}
    </div>
  );
}
