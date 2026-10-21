"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — فهرست روش‌های ارسال در پیش‌نمایش طراحی.
 *
 * از همان `AppDataGrid` موجود استفاده می‌شود؛ گرید تازه یا جدول دستی ساخته نمی‌شود.
 * هیچ saved view ذخیره‌شده در مرورگر نوشته نمی‌شود (بدون store ماندگار)، و هیچ داده‌ای از سرور
 * خوانده نمی‌شود؛ ردیف‌ها فقط از fixture درون‌حافظه می‌آیند.
 */

import { useCallback, useMemo } from "react";
import { Eye, Pencil, Plus } from "lucide-react";
import {
  AppDataGrid,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../../design-system";
import {
  AppGridRowActionsCell,
  type AppGridRowAction,
} from "../../../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery } from "../../../../design-system/data-grid";
import {
  previewMethodSelectedPlaces,
  previewTextFor,
  type PreviewCalculationMode,
  type PreviewFreightPayerMode,
  type PreviewLocale,
  type PreviewMethod,
  type PreviewMethodStatus,
  type PreviewShippingTypeId,
} from "./preview-model.ts";
import { formatPreviewNumber, previewFormat, type PreviewMessages } from "./preview-i18n.ts";

const GRID_ID = "admin-shipping-services-design-preview";

type PreviewMethodRow = {
  id: string;
  method: PreviewMethod;
  name: string;
  hasName: boolean;
  type: string;
  calculation: string;
  payer: string;
  status: string;
  statusTone: "active" | "inactive";
  meta: string;
};

function shippingTypeLabel(typeId: PreviewShippingTypeId, messages: PreviewMessages): string {
  switch (typeId) {
    case "heavy":
      return messages.typeHeavy;
    case "extra-heavy":
      return messages.typeExtraHeavy;
    case "grocery":
      return messages.typeGrocery;
    default:
      return messages.typeOrdinary;
  }
}

function calculationLabel(mode: PreviewCalculationMode, messages: PreviewMessages): string {
  if (mode === "carrier") return messages.calculationCarrier;
  if (mode === "free") return messages.calculationFree;
  return messages.calculationManual;
}

function payerLabel(mode: PreviewFreightPayerMode, messages: PreviewMessages): string {
  return mode === "collect" ? messages.freightPayerCollect : messages.freightPayerPrepaid;
}

function statusLabel(status: PreviewMethodStatus, messages: PreviewMessages): string {
  return status === "active" ? messages.statusActive : messages.statusInactive;
}

export function PreviewMethodList({
  methods,
  locale,
  contentLanguageId,
  messages,
  onView,
  onEdit,
  onAdd,
}: {
  methods: readonly PreviewMethod[];
  locale: PreviewLocale;
  contentLanguageId: string;
  messages: PreviewMessages;
  onView: (method: PreviewMethod) => void;
  onEdit: (method: PreviewMethod) => void;
  onAdd: () => void;
}) {
  const rows = useMemo<PreviewMethodRow[]>(
    () =>
      methods.map((method) => {
        const name = previewTextFor(method.names, contentLanguageId).trim();
        const places = previewMethodSelectedPlaces(method).length;
        const rateRows = method.regionGroups.reduce((total, group) => total + group.rows.length, 0);
        return {
          id: method.id,
          method,
          name,
          hasName: name.length > 0,
          type: shippingTypeLabel(method.shippingTypeId, messages),
          calculation: calculationLabel(method.calculationMode, messages),
          payer: payerLabel(method.freightPayerMode, messages),
          status: statusLabel(method.status, messages),
          statusTone: method.status === "active" ? "active" : "inactive",
          meta: [
            `${messages.groupCount} ${formatPreviewNumber(method.regionGroups.length, locale)}`,
            `${messages.rowCount} ${formatPreviewNumber(rateRows, locale)}`,
            previewFormat(messages.groupPlaceCount, { count: formatPreviewNumber(places, locale) }),
          ].join(" · "),
        };
      }),
    [methods, contentLanguageId, locale, messages],
  );

  const rowActions = useMemo<AppGridRowAction<PreviewMethodRow>[]>(
    () => [
      {
        id: "view",
        label: messages.actionView,
        icon: Eye,
        testId: (row) => `shipping-preview-view-${row.id}`,
        onClick: (row) => onView(row.method),
      },
      {
        id: "edit",
        label: messages.actionEdit,
        icon: Pencil,
        testId: (row) => `shipping-preview-edit-${row.id}`,
        onClick: (row) => onEdit(row.method),
      },
    ],
    [messages.actionEdit, messages.actionView, onEdit, onView],
  );

  const columns = useMemo<GridColumnDef<PreviewMethodRow>[]>(
    () => [
      {
        id: "name",
        header: messages.colName,
        accessor: (row) => (row.hasName ? row.name : messages.notTranslated),
        cell: (row) => (
          <span className="flex flex-col gap-0.5">
            <span className={row.hasName ? "font-bold text-gray-900" : "font-bold text-amber-700"}>
              {row.hasName ? row.name : messages.notTranslated}
            </span>
            <span className="text-[11px] text-gray-500">{row.meta}</span>
          </span>
        ),
        width: 300,
        minWidth: 200,
        flex: 1.6,
      },
      {
        id: "type",
        header: messages.colType,
        accessor: (row) => row.type,
        width: 130,
        minWidth: 100,
      },
      {
        id: "calculation",
        header: messages.colCalculation,
        accessor: (row) => row.calculation,
        width: 180,
        minWidth: 130,
      },
      {
        id: "payer",
        header: messages.colFreightPayer,
        accessor: (row) => row.payer,
        width: 130,
        minWidth: 100,
      },
      {
        id: "status",
        header: messages.colStatus,
        accessor: (row) => row.status,
        cell: (row) => (
          <span
            className={
              row.statusTone === "active"
                ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700"
                : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"
            }
          >
            {row.status}
          </span>
        ),
        width: 110,
        minWidth: 90,
      },
      {
        id: "actions",
        header: messages.colActions,
        accessor: () => "",
        exportable: false,
        sortable: false,
        width: 130,
        minWidth: 120,
        maxWidth: 170,
        cell: (row) => <AppGridRowActionsCell row={row} actions={rowActions} compact />,
      },
    ],
    [messages, rowActions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [columns, rows],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: GRID_ID,
    columns,
    queryAdapter,
    locale,
  });

  return (
    <section className="space-y-3" data-testid="shipping-preview-list">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-lg font-black text-gray-900">{messages.listTitle}</h2>
          <p className="mt-1 max-w-3xl text-sm text-gray-500">{messages.listSubtitle}</p>
        </div>
        <button
          type="button"
          className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-bold text-primary-foreground"
          data-testid="shipping-preview-add-method"
          onClick={onAdd}
        >
          <Plus className="size-4" aria-hidden />
          {messages.addMethod}
        </button>
      </div>

      {rows.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-border px-4 py-6 text-center text-sm text-gray-500">
          {messages.noMethods}
        </p>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-border bg-surface-elevated">
          <div className="overflow-x-auto p-2 md:p-3">
            <AppDataGrid<PreviewMethodRow> {...gridProps} />
          </div>
        </div>
      )}
    </section>
  );
}
