import { useLayoutEffect, type RefObject } from "react";
import type { GridApi, ICellRendererParams } from "ag-grid-community";

type OverflowTooltipHost = Pick<ICellRendererParams, "setTooltip" | "api">;

function isElementOverflowing(element: HTMLElement): boolean {
  return element.scrollWidth > element.clientWidth || element.scrollHeight > element.clientHeight;
}

function hasOverflow(root: HTMLElement): boolean {
  const marked = root.querySelectorAll<HTMLElement>("[data-overflow-measure], .truncate");
  if (marked.length > 0) {
    for (const element of marked) {
      if (isElementOverflowing(element)) return true;
    }
    return false;
  }
  return isElementOverflowing(root);
}

/** ثبت tooltip فقط وقتی محتوای سلول سفارشی overflow دارد (AG Grid whenTruncated برای renderer کافی نیست). */
export function useOverflowTooltip(
  host: OverflowTooltipHost,
  text: string,
  rootRef: RefObject<HTMLElement | null>,
) {
  useLayoutEffect(() => {
    const root = rootRef.current;
    const setTooltip = host.setTooltip;
    if (!root || !setTooltip) return;

    const apply = () => {
      const label = text.trim();
      if (!label) {
        setTooltip("", () => false);
        return;
      }
      setTooltip(label, () => hasOverflow(root));
    };

    apply();
    const api = host.api as GridApi | undefined;
    api?.addEventListener("columnResized", apply);
    api?.addEventListener("gridColumnsChanged", apply);
    api?.addEventListener("firstDataRendered", apply);

    return () => {
      api?.removeEventListener("columnResized", apply);
      api?.removeEventListener("gridColumnsChanged", apply);
      api?.removeEventListener("firstDataRendered", apply);
      setTooltip("", () => false);
    };
  }, [host.api, host.setTooltip, rootRef, text]);
}

export function gridTooltipText(value: unknown, formatted?: string | null): string | undefined {
  const resolved = formatted ?? (value == null ? "" : String(value));
  const trimmed = resolved.trim();
  return trimmed.length > 0 ? trimmed : undefined;
}
