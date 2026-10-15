"use client";

/**
 * TB-SHIPPING-FULL-OPERATIONS-072 — «برنامه کاری ارسال»: the Persian/RTL weekly operating-schedule
 * editor of one Shipping service, reachable from the existing /admin/shipping-services service row
 * next to the rate/coverage controls. It is one focused dialog, not a new route or a combined form.
 *
 * Carrier operating windows are pickup/dispatch availability, never a customer delivery promise.
 * Capacity is configuration-only and is labelled as such. Everything is bound to the real 071
 * GET/PUT/PATCH endpoints; nothing is fabricated or patched in memory.
 */
import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { Dialog } from "../../../design-system";
import {
  loadAdminShippingOperatingSchedules,
  setAdminShippingOperatingScheduleActive,
  upsertAdminShippingOperatingSchedule,
  type ShippingOperatingScheduleAdminItem,
} from "./shipping-operating-schedule-api.ts";
import {
  CAPACITY_CONFIG_ONLY_FA,
  COMMON_TIME_ZONE_IDS,
  DEFAULT_TIME_ZONE_ID,
  MAX_WINDOWS_PER_SCHEDULE,
  NO_SCHEDULE_DISCLOSURE_FA,
  OPERATING_WINDOW_SCOPE_FA,
  SERVICE_LEVEL_SCHEDULE_FA,
  SERVICE_LEVEL_SCHEDULE_VALUE,
  SHIPPING_WEEKDAYS,
  buildScheduleWriteRequest,
  emptyWindowRow,
  formatWindowFa,
  mapScheduleErrorFa,
  scheduleOptionKey,
  scheduleOptionLabelFa,
  scheduleStateFa,
  sortWindowRows,
  summarizeSchedules,
  validateShippingScheduleForm,
  validateScheduleOption,
  windowRowsFromSchedule,
  weekdayLabelFa,
  type ShippingScheduleFormIssue,
  type ShippingServiceOptionChoice,
  type ShippingWindowFormRow,
} from "./shipping-operating-schedule-model.ts";
import { loadShippingMethodTree } from "../shipping-services-api.ts";

type Props = {
  open: boolean;
  serviceCode: string;
  serviceName: string;
  onClose: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
};

export function ShippingOperatingScheduleDialog({ open, serviceCode, serviceName, onClose, onNotice }: Props) {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("loading");
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [schedules, setSchedules] = useState<ShippingOperatingScheduleAdminItem[]>([]);
  // گزینه‌های واقعی همین خدمت از درخت سرویس Fulfillment؛ هیچ گزینه‌ای اختراع نمی‌شود.
  const [options, setOptions] = useState<ShippingServiceOptionChoice[]>([]);
  const [optionsLoading, setOptionsLoading] = useState(false);
  // هویت در حال ویرایش: تهی یعنی برنامهٔ سطح خدمت (همان قرارداد wire).
  const [targetOptionCode, setTargetOptionCode] = useState<string>(SERVICE_LEVEL_SCHEDULE_VALUE);
  const [timeZoneId, setTimeZoneId] = useState(DEFAULT_TIME_ZONE_ID);
  const [rows, setRows] = useState<ShippingWindowFormRow[]>([]);
  const [issues, setIssues] = useState<ShippingScheduleFormIssue[]>([]);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async () => {
    if (!serviceCode) return;
    setState("loading");
    const result = await loadAdminShippingOperatingSchedules(serviceCode);
    if (result.state !== "ok" || !result.data) {
      setState(result.state === "denied" ? "denied" : "error");
      setMessage(result.message);
      setSchedules([]);
      return;
    }
    setSchedules(result.data.schedules);
    setState("ok");
    setMessage(undefined);
  }, [serviceCode]);

  // فهرست گزینه‌ها از همان درخت سرویس مجاز admin خوانده می‌شود؛ نبود نتیجه یعنی خدمت بدون گزینه.
  const loadOptions = useCallback(async () => {
    setOptionsLoading(true);
    const tree = await loadShippingMethodTree();
    if (tree.state !== "ok" || !tree.data) {
      setOptions([]);
      setOptionsLoading(false);
      return;
    }
    const mine = tree.data.find((item) => item.code === serviceCode);
    setOptions(
      (mine?.options ?? [])
        .filter((option) => option.code.trim().length > 0)
        .map((option) => ({ code: option.code, labelFa: option.labelFa || option.name || option.code })),
    );
    setOptionsLoading(false);
  }, [serviceCode]);

  const existing = useMemo(
    () => schedules.find((schedule) => scheduleOptionKey(schedule.optionCode) === targetOptionCode) ?? null,
    [schedules, targetOptionCode],
  );

  /** بارگذاری فرم از ردیف ذخیره‌شدهٔ همان هویت؛ نبود ردیف یعنی فرم خالی و آمادهٔ ایجاد. */
  const loadEditorFor = useCallback(
    (optionCode: string) => {
      setTargetOptionCode(optionCode);
      setIssues([]);
      const found = schedules.find((schedule) => scheduleOptionKey(schedule.optionCode) === optionCode);
      if (!found) {
        setTimeZoneId(DEFAULT_TIME_ZONE_ID);
        setRows([]);
        return;
      }
      setTimeZoneId(found.timeZoneId || DEFAULT_TIME_ZONE_ID);
      setRows(sortWindowRows(windowRowsFromSchedule(found)));
    },
    [schedules],
  );

  useEffect(() => {
    if (open) {
      setIssues([]);
      setRows([]);
      setTimeZoneId(DEFAULT_TIME_ZONE_ID);
      setTargetOptionCode(SERVICE_LEVEL_SCHEDULE_VALUE);
      void refresh();
      void loadOptions();
    }
  }, [open, refresh, loadOptions]);

  function updateRow(key: string, patch: Partial<ShippingWindowFormRow>) {
    setRows((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }

  function addRow(weekday: ShippingWindowFormRow["weekday"]) {
    setRows((prev) => [...prev, emptyWindowRow(weekday)]);
  }

  function removeRow(key: string) {
    setRows((prev) => prev.filter((row) => row.key !== key));
  }

  const optionIssues = validateScheduleOption(
    targetOptionCode === SERVICE_LEVEL_SCHEDULE_VALUE ? null : targetOptionCode,
    options,
  );

  async function save() {
    const shape = validateShippingScheduleForm({ timeZoneId, windows: rows });
    const problems = [...optionIssues, ...shape];
    if (problems.length > 0) {
      setIssues(problems);
      return;
    }

    setIssues([]);
    setBusy(true);
    const result = await upsertAdminShippingOperatingSchedule(serviceCode, buildScheduleWriteRequest({
      serviceCode,
      timeZoneId,
      optionCode: targetOptionCode === SERVICE_LEVEL_SCHEDULE_VALUE ? null : targetOptionCode,
      windows: rows,
    }));
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({
        tone: "error",
        text: mapScheduleErrorFa(result.message, "ذخیرهٔ برنامهٔ ارسال ناموفق بود؛ دوباره تلاش کنید."),
      });
      // خواندن مجدد پس از خطا: فرم روی حالت واقعی سرور بماند، نه روی خواستهٔ ناموفق.
      await refresh();
      return;
    }

    onNotice({
      tone: "success",
      text: `برنامهٔ «${scheduleOptionLabelFa(result.data!.optionCode, options)}» با ${result.data!.windows.length.toLocaleString("fa-IR")} بازه ذخیره شد.`,
    });
    await refresh();
    loadEditorFor(scheduleOptionKey(result.data!.optionCode));
  }

  async function toggle(schedule: ShippingOperatingScheduleAdminItem) {
    setBusy(true);
    const result = await setAdminShippingOperatingScheduleActive(serviceCode, schedule.scheduleId, !schedule.isActive);
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({
        tone: "error",
        text: mapScheduleErrorFa(result.message, "تغییر وضعیت برنامه ناموفق بود؛ دوباره تلاش کنید."),
      });
      await refresh();
      return;
    }
    onNotice({
      tone: "success",
      text: `برنامهٔ «${scheduleOptionLabelFa(result.data!.optionCode, options)}» ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.`,
    });
    await refresh();
  }

  const summary = summarizeSchedules(schedules);
  const targetLabel = scheduleOptionLabelFa(
    targetOptionCode === SERVICE_LEVEL_SCHEDULE_VALUE ? null : targetOptionCode,
    options,
  );

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={`برنامه کاری ارسال — ${serviceName}`}
      showCloseButton={false}
      size="xl"
    >
      <div className="max-h-[78vh] space-y-4 overflow-y-auto" data-testid="shipping-operating-schedule-dialog">
        <p className="text-xs text-gray-600">
          کد خدمت: <span dir="ltr" className="font-mono">{serviceCode}</span>
        </p>

        <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-schedule-scope-note">
          {OPERATING_WINDOW_SCOPE_FA}
        </p>

        {state === "denied" && (
          <p className="text-sm text-red-600" data-testid="shipping-schedule-denied">
            دسترسی به برنامهٔ ارسال ندارید؛ این بخش به مجوز مشاهده و مدیریت Fulfillment نیاز دارد.
          </p>
        )}
        {state === "error" && (
          <p className="text-sm text-red-600" data-testid="shipping-schedule-error">
            خطا در خواندن برنامه‌ها: {mapScheduleErrorFa(message, "دوباره تلاش کنید.")}
          </p>
        )}

        {state === "ok" && (
          <>
            <section className="space-y-2 rounded-lg border border-gray-200 p-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-bold text-gray-800">برنامه‌های ثبت‌شده</h3>
                <span className="text-[11px] text-gray-600" data-testid="shipping-schedule-summary">
                  {scheduleStateFa(schedules)}
                  {summary.configuredCapacityTotal == null
                    ? ""
                    : ` · ${CAPACITY_CONFIG_ONLY_FA}: ${summary.configuredCapacityTotal.toLocaleString("fa-IR")}`}
                </span>
              </div>
              {schedules.length === 0 ? (
                <p className="text-xs text-gray-500" data-testid="shipping-schedule-empty">
                  {NO_SCHEDULE_DISCLOSURE_FA}
                </p>
              ) : (
                <ul className="space-y-2">
                  {schedules.map((schedule) => (
                    <li
                      key={schedule.scheduleId}
                      className="space-y-1 rounded-lg border border-gray-200 p-3 text-xs text-gray-700"
                      data-testid={`shipping-schedule-row-${schedule.scheduleId}`}
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <span className="font-bold">{scheduleOptionLabelFa(schedule.optionCode, options)}</span>
                        <span className={schedule.isActive ? "font-bold text-emerald-700" : "font-bold text-gray-500"}>
                          {schedule.isActive ? "فعال" : "غیرفعال"}
                        </span>
                      </div>
                      <div className="flex flex-wrap gap-3 text-[11px] text-gray-600">
                        <span dir="ltr" className="font-mono">{schedule.timeZoneId}</span>
                        <span>بازه‌ها: {schedule.windows.length.toLocaleString("fa-IR")}</span>
                        {schedule.configuredCapacityTotal == null ? null : (
                          <span>
                            {CAPACITY_CONFIG_ONLY_FA}: {schedule.configuredCapacityTotal.toLocaleString("fa-IR")}
                          </span>
                        )}
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {schedule.windows.map((window) => (
                          <span
                            key={window.windowId}
                            className="rounded border border-gray-200 px-2 py-0.5 text-[11px]"
                            data-testid={`shipping-schedule-window-${window.windowId}`}
                          >
                            {weekdayLabelFa(window.weekday)} · {formatWindowFa(window)}
                          </span>
                        ))}
                      </div>
                      <div className="flex flex-wrap gap-2 pt-1">
                        <button
                          type="button"
                          className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                          disabled={busy}
                          onClick={() => loadEditorFor(scheduleOptionKey(schedule.optionCode))}
                          data-testid={`shipping-schedule-edit-${schedule.scheduleId}`}
                        >
                          ویرایش
                        </button>
                        <button
                          type="button"
                          className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                          disabled={busy}
                          onClick={() => void toggle(schedule)}
                          data-testid={`shipping-schedule-toggle-${schedule.scheduleId}`}
                        >
                          {schedule.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                        </button>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section className="space-y-3 rounded-lg border border-dashed border-gray-300 p-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-bold text-gray-800">
                  {existing ? "ویرایش برنامه" : "ایجاد برنامه"}
                </h3>
                <label className="text-[11px] text-gray-600">
                  هویت برنامه
                  <select
                    className="ms-2 rounded border border-gray-300 px-2 py-1 text-xs"
                    value={targetOptionCode}
                    onChange={(e) => loadEditorFor(e.target.value)}
                    data-testid="shipping-schedule-option-select"
                  >
                    <option value={SERVICE_LEVEL_SCHEDULE_VALUE}>سطح خدمت (پیش‌فرض خدمت)</option>
                    {options.map((option) => (
                      <option key={option.code} value={option.code}>
                        {option.labelFa} ({option.code})
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              {optionsLoading ? (
                <p className="text-[11px] text-gray-500">در حال خواندن گزینه‌های خدمت…</p>
              ) : null}

              {options.length === 0 ? (
                <p className="text-[11px] text-gray-600" data-testid="shipping-schedule-option-note">
                  این خدمت گزینه‌ای ندارد؛ برنامه فقط در سطح خدمت ثبت می‌شود.
                </p>
              ) : (
                <p className="text-[11px] text-gray-600" data-testid="shipping-schedule-service-level-note">
                  {SERVICE_LEVEL_SCHEDULE_FA}
                </p>
              )}

              <label className="block text-[11px] text-gray-600">
                منطقهٔ زمانی
                <input
                  className="ms-2 w-56 rounded border border-gray-300 px-2 py-1 text-xs"
                  dir="ltr"
                  list="shipping-schedule-timezones"
                  value={timeZoneId}
                  onChange={(e) => setTimeZoneId(e.target.value)}
                  data-testid="shipping-schedule-timezone-input"
                />
                <datalist id="shipping-schedule-timezones">
                  {COMMON_TIME_ZONE_IDS.map((zone) => <option key={zone} value={zone} />)}
                </datalist>
              </label>
              <p className="text-[11px] text-gray-500" data-testid="shipping-schedule-timezone-note">
                پیش‌فرض صریح <span dir="ltr" className="font-mono">{DEFAULT_TIME_ZONE_ID}</span> است و از مرورگر حدس زده نمی‌شود.
              </p>

              <div className="space-y-2" data-testid="shipping-schedule-rows">
                {rows.length === 0 ? (
                  <p className="text-[11px] text-gray-500">
                    هنوز بازه‌ای اضافه نشده؛ برای هر روز می‌توانید چند بازه بسازید (مثلاً یکشنبه 13:00–14:00).
                  </p>
                ) : null}
                {rows.map((row) => (
                  <div
                    key={row.key}
                    className="grid gap-2 rounded-lg border border-white bg-white p-2 shadow-sm sm:grid-cols-[auto_auto_auto_auto_auto]"
                    data-testid={`shipping-schedule-row-${row.key}`}
                  >
                    <label className="text-[11px] text-gray-600">
                      روز
                      <select
                        className="mt-1 block rounded border border-gray-300 px-2 py-1 text-xs"
                        value={row.weekday}
                        onChange={(e) => updateRow(row.key, { weekday: Number(e.target.value) as ShippingWindowFormRow["weekday"] })}
                        data-testid={`shipping-schedule-weekday-${row.key}`}
                      >
                        {SHIPPING_WEEKDAYS.map((day) => (
                          <option key={day.weekday} value={day.weekday}>{day.labelFa}</option>
                        ))}
                      </select>
                    </label>
                    <label className="text-[11px] text-gray-600">
                      شروع (24ساعته)
                      <input
                        className="mt-1 block w-24 rounded border border-gray-300 px-2 py-1 text-xs"
                        dir="ltr"
                        placeholder="13:00"
                        value={row.startTime}
                        onChange={(e) => updateRow(row.key, { startTime: e.target.value })}
                        data-testid={`shipping-schedule-start-${row.key}`}
                      />
                    </label>
                    <label className="text-[11px] text-gray-600">
                      پایان (24ساعته)
                      <input
                        className="mt-1 block w-24 rounded border border-gray-300 px-2 py-1 text-xs"
                        dir="ltr"
                        placeholder="14:00"
                        value={row.endTime}
                        onChange={(e) => updateRow(row.key, { endTime: e.target.value })}
                        data-testid={`shipping-schedule-end-${row.key}`}
                      />
                    </label>
                    <label className="text-[11px] text-gray-600">
                      {CAPACITY_CONFIG_ONLY_FA}
                      <input
                        className="mt-1 block w-28 rounded border border-gray-300 px-2 py-1 text-xs"
                        dir="ltr"
                        inputMode="numeric"
                        value={row.capacity}
                        onChange={(e) => updateRow(row.key, { capacity: e.target.value })}
                        data-testid={`shipping-schedule-capacity-${row.key}`}
                      />
                    </label>
                    <button
                      type="button"
                      className="self-end rounded border border-red-200 p-2 text-red-600"
                      onClick={() => removeRow(row.key)}
                      aria-label="حذف بازه"
                      data-testid={`shipping-schedule-remove-${row.key}`}
                    >
                      <Trash2 className="size-4" />
                    </button>
                  </div>
                ))}
              </div>

              <div className="flex flex-wrap gap-2">
                {SHIPPING_WEEKDAYS.map((day) => (
                  <button
                    key={day.weekday}
                    type="button"
                    className="inline-flex items-center gap-1 rounded border border-gray-300 bg-white px-2 py-1 text-[11px] font-bold text-gray-700 disabled:opacity-50"
                    disabled={rows.length >= MAX_WINDOWS_PER_SCHEDULE}
                    onClick={() => addRow(day.weekday)}
                    data-testid={`shipping-schedule-add-${day.weekday}`}
                  >
                    <Plus className="size-3.5" aria-hidden />
                    بازه در {day.labelFa}
                  </button>
                ))}
              </div>

              <p className="text-[11px] text-gray-500">
                هدف ویرایش: <span className="font-bold">{targetLabel}</span>
              </p>

              {issues.length > 0 ? (
                <ul
                  className="space-y-1 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-[11px] font-semibold text-red-700"
                  data-testid="shipping-schedule-issues"
                >
                  {issues.map((issue) => (
                    <li key={`${issue.code}-${issue.rowKey ?? "form"}-${issue.messageFa}`}>{issue.messageFa}</li>
                  ))}
                </ul>
              ) : null}

              <p className="text-[11px] text-gray-500" data-testid="shipping-schedule-disclosure">
                {NO_SCHEDULE_DISCLOSURE_FA}
              </p>
            </section>
          </>
        )}

        <div className="flex flex-wrap justify-end gap-2 border-t border-gray-100 pt-3">
          <button
            type="button"
            className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-bold text-gray-700"
            onClick={onClose}
          >
            بستن
          </button>
          <button
            type="button"
            className="rounded-lg bg-[#2563EB] px-3 py-2 text-sm font-bold text-white disabled:opacity-50"
            disabled={busy || state !== "ok"}
            onClick={() => void save()}
            data-testid="shipping-schedule-save"
          >
            {busy ? "در حال ذخیره…" : "ذخیره برنامه"}
          </button>
        </div>
      </div>
    </Dialog>
  );
}
