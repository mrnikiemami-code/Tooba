/**
 * حالت سبک VIEW / EDIT برای فرم‌های Admin.
 * مجوز از Host/SpiceDB می‌آید؛ این لایه فقط UI را هدایت می‌کند.
 */

"use client";

import { useCallback, useEffect, useReducer } from "react";

export type AdminFormModeKind = "view" | "edit";

export interface AdminFormModeState {
  mode: AdminFormModeKind;
  canView: boolean;
  canEdit: boolean;
  isDirty: boolean;
}

export interface AdminFormModeController extends AdminFormModeState {
  onEdit: () => void;
  onCancel: () => void;
  onSaved: () => void;
  markDirty: () => void;
  clearDirty: () => void;
  resetToView: () => void;
  /** اگر dirty باشد و کاربر تأیید نکند، false برمی‌گرداند. */
  confirmDiscardIfDirty: (confirmFn?: () => boolean) => boolean;
}

export function createAdminFormModeState(
  canView: boolean,
  canEdit: boolean,
  mode: AdminFormModeKind = "view",
): AdminFormModeState {
  return {
    mode: canView ? mode : "view",
    canView,
    canEdit: canView && canEdit,
    isDirty: false,
  };
}

/** ورود به EDIT فقط با canEdit. */
export function enterAdminEditMode(state: AdminFormModeState): AdminFormModeState {
  if (!state.canView || !state.canEdit) return state;
  return { ...state, mode: "edit", isDirty: false };
}

/** انصراف: دور ریختن dirty و بازگشت به VIEW. */
export function cancelAdminEditMode(state: AdminFormModeState): AdminFormModeState {
  return { ...state, mode: "view", isDirty: false };
}

/** پس از ذخیرهٔ موفق. */
export function completeAdminSave(state: AdminFormModeState): AdminFormModeState {
  return { ...state, mode: "view", isDirty: false };
}

export function markAdminFormDirty(state: AdminFormModeState): AdminFormModeState {
  if (state.mode !== "edit") return state;
  return { ...state, isDirty: true };
}

export function clearAdminFormDirty(state: AdminFormModeState): AdminFormModeState {
  return { ...state, isDirty: false };
}

const DEFAULT_DISCARD_CONFIRM = () =>
  typeof window !== "undefined"
    ? window.confirm("تغییرات ذخیره‌نشده از بین می‌رود. ادامه؟")
    : true;

/**
 * کنترلر سبک بدون وابستگی به فریم‌ورک فرم؛ صفحهٔ Admin state را نگه می‌دارد.
 */
export function reduceAdminFormMode(
  state: AdminFormModeState,
  action:
    | { type: "capabilities"; canView: boolean; canEdit: boolean }
    | { type: "edit" }
    | { type: "cancel" }
    | { type: "saved" }
    | { type: "dirty" }
    | { type: "clearDirty" }
    | { type: "resetView" },
): AdminFormModeState {
  switch (action.type) {
    case "capabilities":
      return createAdminFormModeState(
        action.canView,
        action.canEdit,
        state.mode === "edit" && action.canEdit ? "edit" : "view",
      );
    case "edit":
      return enterAdminEditMode(state);
    case "cancel":
      return cancelAdminEditMode(state);
    case "saved":
      return completeAdminSave(state);
    case "dirty":
      return markAdminFormDirty(state);
    case "clearDirty":
      return clearAdminFormDirty(state);
    case "resetView":
      return { ...state, mode: "view", isDirty: false };
    default:
      return state;
  }
}

/**
 * Hook سبک برای VIEW/EDIT Admin.
 * استفاده: const form = useAdminFormMode({ canView, canEdit });
 */
export function useAdminFormMode(options: {
  canView: boolean;
  canEdit: boolean;
}): AdminFormModeController {
  const [state, dispatch] = useReducer(
    reduceAdminFormMode,
    undefined,
    () => createAdminFormModeState(options.canView, options.canEdit),
  );

  useEffect(() => {
    dispatch({ type: "capabilities", canView: options.canView, canEdit: options.canEdit });
  }, [options.canView, options.canEdit]);

  const onEdit = useCallback(() => {
    dispatch({ type: "edit" });
  }, []);

  const onCancel = useCallback(() => {
    dispatch({ type: "cancel" });
  }, []);

  const onSaved = useCallback(() => {
    dispatch({ type: "saved" });
  }, []);

  const markDirty = useCallback(() => {
    dispatch({ type: "dirty" });
  }, []);

  const clearDirty = useCallback(() => {
    dispatch({ type: "clearDirty" });
  }, []);

  const resetToView = useCallback(() => {
    dispatch({ type: "resetView" });
  }, []);

  const confirmDiscardIfDirty = useCallback(
    (confirmFn: () => boolean = DEFAULT_DISCARD_CONFIRM) => {
      if (!state.isDirty) return true;
      return confirmFn();
    },
    [state.isDirty],
  );

  return {
    ...state,
    onEdit,
    onCancel,
    onSaved,
    markDirty,
    clearDirty,
    resetToView,
    confirmDiscardIfDirty,
  };
}
