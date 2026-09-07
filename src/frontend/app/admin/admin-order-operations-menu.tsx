"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { ChevronDown, MoreHorizontal } from "lucide-react";
import { toast } from "react-toastify";
import { Button, Dialog, Tooltip } from "../../design-system";
import {
  executeAdminOrderOperation,
  filterOperationsForScope,
  loadAdminOrderOperations,
  type AdminOrderOperationAction,
  type AdminOrderOperationsScope,
} from "./admin-order-operations";
import { mapAdminErrorMessage } from "./admin-error-map";

type Props = {
  checkoutId: string;
  /** برچسب دکمه؛ پیش‌فرض «عملیات». در حالت iconOnly فقط برای tooltip/aria استفاده می‌شود. */
  label?: string;
  compact?: boolean;
  /** Grid: فقط آیکون kebab بدون متن تکراری عملیات. */
  iconOnly?: boolean;
  /** Grid = whole-order؛ Detail = همهٔ actions API. */
  scope?: AdminOrderOperationsScope;
  onCompleted?: () => void;
  testId?: string;
};

/**
 * منوی یک‌دکمه‌ای عملیات سفارش — فقط actions برگشتی از API؛ portal برای جلوگیری از clip در Grid.
 * تأیید و ورودی‌های اضافی با Dialog؛ بدون confirm/prompt مرورگر.
 */
export function AdminOrderOperationsMenu({
  checkoutId,
  label = "عملیات",
  compact = false,
  iconOnly = false,
  scope = "detail",
  onCompleted,
  testId,
}: Props) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [pending, setPending] = useState(false);
  const [actions, setActions] = useState<AdminOrderOperationAction[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [menuPos, setMenuPos] = useState<{ top: number; left: number; minWidth: number } | null>(null);
  const [confirmAction, setConfirmAction] = useState<AdminOrderOperationAction | null>(null);
  const [trackingAction, setTrackingAction] = useState<AdminOrderOperationAction | null>(null);
  const [trackingValue, setTrackingValue] = useState("");
  const [carrierAction, setCarrierAction] = useState<AdminOrderOperationAction | null>(null);
  const [carrierValue, setCarrierValue] = useState("پست");
  const rootRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

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
    setActions(filterOperationsForScope(result.data.actions, scope));
  }, [checkoutId, scope]);

  const updatePosition = useCallback(() => {
    const btn = buttonRef.current;
    if (!btn) return;
    const rect = btn.getBoundingClientRect();
    const minWidth = Math.max(rect.width, 192);
    const preferredLeft = rect.right - minWidth;
    const left = Math.min(Math.max(8, preferredLeft), window.innerWidth - minWidth - 8);
    const top = Math.min(rect.bottom + 4, window.innerHeight - 8);
    setMenuPos({ top, left, minWidth });
  }, []);

  useEffect(() => {
    if (!open) return;
    void refresh();
  }, [open, refresh]);

  useLayoutEffect(() => {
    if (!open) {
      setMenuPos(null);
      return;
    }
    updatePosition();
    window.addEventListener("resize", updatePosition);
    window.addEventListener("scroll", updatePosition, true);
    return () => {
      window.removeEventListener("resize", updatePosition);
      window.removeEventListener("scroll", updatePosition, true);
    };
  }, [open, updatePosition]);

  useEffect(() => {
    if (!open) return;
    function onDocClick(event: MouseEvent) {
      const target = event.target as Node;
      if (rootRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      setOpen(false);
    }
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, [open]);

  async function executeAction(
    action: AdminOrderOperationAction,
    extras?: { carrierDisplayName?: string; trackingReference?: string },
  ) {
    setPending(true);
    setError(null);
    const result = await executeAdminOrderOperation(checkoutId, {
      code: action.code,
      sellerOrderId: action.sellerOrderId,
      fulfillmentId: action.fulfillmentId,
      shipmentId: action.shipmentId,
      returnRequestId: action.returnRequestId,
      carrierDisplayName: extras?.carrierDisplayName,
      trackingReference: extras?.trackingReference,
    });
    setPending(false);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
      setError(fa);
      toast.error(fa);
      return;
    }
    toast.success(`عملیات ${action.labelFa} انجام شد.`);
    setOpen(false);
    setConfirmAction(null);
    setTrackingAction(null);
    setCarrierAction(null);
    await refresh();
    onCompleted?.();
  }

  function requestAction(action: AdminOrderOperationAction) {
    setOpen(false);
    setConfirmAction(action);
  }

  function onConfirmAccepted() {
    const action = confirmAction;
    if (!action) return;
    setConfirmAction(null);
    if (action.code === "create_shipment") {
      setCarrierValue("پست");
      setCarrierAction(action);
      return;
    }
    if (action.code === "assign_tracking") {
      setTrackingValue("");
      setTrackingAction(action);
      return;
    }
    void executeAction(action);
  }

  const buttonClass = iconOnly
    ? "inline-flex size-8 items-center justify-center rounded-full border border-border bg-surface text-muted hover:bg-secondary hover:text-foreground disabled:opacity-50"
    : compact
      ? "inline-flex h-8 items-center gap-1 rounded-full border border-border bg-surface px-2.5 text-xs font-bold text-muted hover:bg-secondary hover:text-foreground disabled:opacity-50"
      : "inline-flex items-center gap-1.5 rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 hover:bg-gray-50 disabled:opacity-50";

  const menu = open && menuPos && typeof document !== "undefined"
    ? createPortal(
        <div
          ref={menuRef}
          role="menu"
          className="fixed z-[80] rounded-xl border border-gray-200 bg-white py-1 shadow-lg"
          style={{ top: menuPos.top, left: menuPos.left, minWidth: menuPos.minWidth }}
          data-testid={`admin-order-ops-menu-${checkoutId}`}
        >
          {loading ? (
            <p className="px-3 py-2 text-xs text-gray-500">در حال بارگذاری…</p>
          ) : actions.length === 0 ? (
            <p className="px-3 py-2 text-xs text-gray-500">هیچ عملیاتی مجاز نیست</p>
          ) : (
            actions.map((action) => (
              <button
                key={`${action.code}-${action.sellerOrderId ?? ""}-${action.shipmentId ?? ""}-${action.returnRequestId ?? ""}`}
                type="button"
                role="menuitem"
                disabled={pending}
                className="block w-full px-3 py-2 text-right text-xs font-semibold text-gray-800 hover:bg-gray-50 disabled:opacity-50"
                onClick={() => requestAction(action)}
                data-testid={`admin-order-ops-action-${action.code}`}
              >
                {action.labelFa}
              </button>
            ))
          )}
          {error ? <p className="border-t border-gray-100 px-3 py-2 text-xs text-danger">{error}</p> : null}
        </div>,
        document.body,
      )
    : null;

  const trigger = (
    <button
      ref={buttonRef}
      type="button"
      className={buttonClass}
      disabled={pending}
      aria-expanded={open}
      aria-haspopup="menu"
      aria-label={label}
      onClick={() => setOpen((value) => !value)}
      data-testid={`admin-order-ops-trigger-${checkoutId}`}
    >
      {iconOnly || compact ? <MoreHorizontal className="size-3.5" aria-hidden /> : null}
      {!iconOnly ? <span>{label}</span> : null}
      {!iconOnly && !compact ? <ChevronDown className="size-3.5" aria-hidden /> : null}
    </button>
  );

  return (
    <div ref={rootRef} className="relative inline-flex" data-testid={testId ?? `admin-order-ops-${checkoutId}`}>
      {iconOnly ? <Tooltip label={label}>{trigger}</Tooltip> : trigger}
      {menu}
      <Dialog
        title="تأیید عملیات"
        open={confirmAction !== null}
        onClose={() => setConfirmAction(null)}
        showCloseButton={false}
      >
        <p className="text-sm text-gray-700">
          {confirmAction?.confirmMessageFa || `اجرای «${confirmAction?.labelFa ?? ""}»؟`}
        </p>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" tone="secondary" onClick={() => setConfirmAction(null)}>
            انصراف
          </Button>
          <Button type="button" tone="primary" onClick={onConfirmAccepted} data-testid="admin-order-ops-confirm">
            تأیید
          </Button>
        </div>
      </Dialog>
      <Dialog
        title="ایجاد مرسوله"
        open={carrierAction !== null}
        onClose={() => setCarrierAction(null)}
        showCloseButton={false}
      >
        <label className="block space-y-1 text-sm">
          <span className="text-xs font-bold text-gray-600">حمل‌کننده</span>
          <select
            value={carrierValue}
            onChange={(e) => setCarrierValue(e.target.value)}
            className="w-full rounded-lg border border-gray-200 bg-white px-3 py-2"
            data-testid="admin-order-ops-carrier"
          >
            {["پست", "تیپاکس", "پیک موتوری", "سایر"].map((option) => (
              <option key={option} value={option}>{option}</option>
            ))}
          </select>
        </label>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" tone="secondary" onClick={() => setCarrierAction(null)}>انصراف</Button>
          <Button
            type="button"
            tone="primary"
            disabled={pending || !carrierValue.trim()}
            onClick={() => {
              if (!carrierAction) return;
              void executeAction(carrierAction, { carrierDisplayName: carrierValue.trim() });
            }}
          >
            ایجاد
          </Button>
        </div>
      </Dialog>
      <Dialog
        title="ثبت کد رهگیری"
        open={trackingAction !== null}
        onClose={() => setTrackingAction(null)}
        showCloseButton={false}
      >
        <label className="block space-y-1 text-sm">
          <span className="text-xs font-bold text-gray-600">کد پیگیری</span>
          <input
            value={trackingValue}
            onChange={(e) => setTrackingValue(e.target.value)}
            className="w-full rounded-lg border border-gray-200 px-3 py-2"
            dir="ltr"
            data-testid="admin-order-ops-tracking"
          />
        </label>
        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" tone="secondary" onClick={() => setTrackingAction(null)}>انصراف</Button>
          <Button
            type="button"
            tone="primary"
            disabled={pending || !trackingValue.trim()}
            onClick={() => {
              if (!trackingAction) return;
              void executeAction(trackingAction, { trackingReference: trackingValue.trim() });
            }}
          >
            ثبت
          </Button>
        </div>
      </Dialog>
    </div>
  );
}
