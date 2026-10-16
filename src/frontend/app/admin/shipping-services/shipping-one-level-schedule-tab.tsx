"use client";

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-088 — tab 4 «برنامه کاری و ظرفیت» of the one-level shipping shell.
 *
 * Scope rules honored here:
 *
 * - **One selected one-level method is the whole context.** There is no option selector and no
 *   provider sublevel: the tab edits the single schedule identity that the server resolved for this
 *   method.
 * - **The binding comes only from the server** (086 resolver). `method_code` is never split in the
 *   browser and a user-supplied service/option pair can never substitute for the binding.
 * - **Schedule semantics are reused, not reimplemented.** ISO weekday identity, 24-hour parsing, the
 *   half-open `[start,end)` rule, the `HH:mm:ss` wire shape, the explicit IANA timezone, capacity
 *   parsing and every Persian refusal message come from `shipping-operating-schedule-model.ts`.
 * - **Configured capacity is configuration only.** The runtime capacity enforcement is `NotEnforced`
 *   and the persistent label says so; nothing here claims remaining capacity, a reservation or an
 *   atomic slot claim.
 * - **A carrier window is pickup/dispatch availability, not a promised customer delivery.**
 * - **The selection is immutable while an operation is pending.** A generation counter is bumped on
 *   every method change so an in-flight read of the previous method can never land on the new one,
 *   and a completion notice stays tied to the identity that started the operation.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  loadAdminShippingOperatingSchedules,
  setAdminShippingOperatingScheduleActive,
  upsertAdminShippingOperatingSchedule,
  type ShippingOperatingScheduleAdminItem,
} from "./shipping-operating-schedule-api.ts";
import {
  buildScheduleWriteRequest,
  CAPACITY_CONFIG_ONLY_FA,
  COMMON_TIME_ZONE_IDS,
  DEFAULT_TIME_ZONE_ID,
  emptyWindowRow,
  formatWindowFa,
  mapScheduleErrorFa,
  NO_SCHEDULE_DISCLOSURE_FA,
  OPERATING_WINDOW_SCOPE_FA,
  scheduleStateFa,
  SHIPPING_WEEKDAYS,
  sortWindowRows,
  summarizeSchedules,
  validateShippingScheduleForm,
  windowRowsFromSchedule,
  type ShippingScheduleFormIssue,
  type ShippingWindowFormRow,
} from "./shipping-operating-schedule-model.ts";
import { adminRequestHeaders } from "../../../lib/admin/admin-result.ts";
import {
  bindingContextFa,
  bindingUnavailableFa,
  configuredCapacityFa,
  createBindingGenerationGuard,
  inactiveScheduleCountFa,
  mapHostBinding,
  ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA,
  ONE_LEVEL_CAPACITY_ENFORCEMENT_FA,
  ONE_LEVEL_INACTIVE_SCHEDULE_FA,
  ONE_LEVEL_NO_SCHEDULE_FA,
  ONE_LEVEL_WINDOW_SCOPE_FA,
  oneLevelBindingPath,
  operationStillTargetsMethod,
  resolveScheduleState,
  scheduleViewMatchesBindingService,
  scheduleWriteTarget,
  scopeSchedulesToBinding,
  type OneLevelBindingState,
} from "./shipping-one-level-schedule-model.ts";

type Props = {
  /** کد روش یک‌سطحی انتخاب‌شده در زبانهٔ «سرویس‌ها». تهی یعنی هیچ روشی انتخاب نشده است. */
  methodCode: string;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /**
   * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146: پیوند حل‌شده از درخت واقعی سرور
   * (`/v1/admin/shipping-methods`)؛ `serviceCode` و `optionCode` فیلدهای جدا و authoritative هستند و
   * هیچ تقسیم متنیِ کد روش در مرورگر انجام نمی‌شود. در حضور آن، resolver غایب صدا زده نمی‌شود.
   */
  bindingOverride?: { serviceCode: string; optionCode: string | null; isActive: boolean } | null;
};

export function ShippingOneLevelScheduleTab({ methodCode, onNotice, bindingOverride = null }: Props) {
  const [bindingLoading, setBindingLoading] = useState(false);
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);
  const [schedulesState, setSchedulesState] = useState<"ok" | "denied" | "error" | null>(null);
  const [view, setView] = useState<{ serviceCode: string; schedules: ShippingOperatingScheduleAdminItem[] } | null>(null);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | null>(null);

  const [timeZoneId, setTimeZoneId] = useState<string>(DEFAULT_TIME_ZONE_ID);
  const [rows, setRows] = useState<ShippingWindowFormRow[]>([]);
  const [issues, setIssues] = useState<ShippingScheduleFormIssue[]>([]);
  const [dirty, setDirty] = useState(false);

  const guard = useRef(createBindingGenerationGuard());
  const resolvedBinding = binding?.state === "resolved" ? binding.binding : null;

  const emit = useCallback(
    (next: { tone: "success" | "error"; text: string }) => {
      setNotice(next);
      onNotice(next);
    },
    [onNotice],
  );

  const refresh = useCallback(async (serviceCode: string, isCurrent: () => boolean) => {
    setSchedulesState(null);
    const result = await loadAdminShippingOperatingSchedules(serviceCode);
    if (!isCurrent()) return;
    if (result.state !== "ok" || !result.data) {
      setSchedulesState(result.state === "denied" ? "denied" : "error");
      setView(null);
      return;
    }
    setView(result.data);
    setSchedulesState("ok");
  }, []);

  useEffect(() => {
    // بازنشانی کامل ویرایشگر هنگام تغییر روش انتخاب‌شده.
    setTimeZoneId(DEFAULT_TIME_ZONE_ID);
    setRows([]);
    setIssues([]);
    setDirty(false);
    setNotice(null);
    setView(null);
    setSchedulesState(null);
    setBinding(null);

    if (!methodCode) {
      setBindingLoading(false);
      return;
    }

    // پیوند authoritative از درخت واقعی سرور (بدون تقسیم متنی کد روش در مرورگر).
    if (bindingOverride) {
      const isCurrent = guard.current.begin();
      setBinding({
        state: "resolved",
        binding: {
          methodCode,
          serviceCode: bindingOverride.serviceCode,
          optionCode: bindingOverride.optionCode,
          isActive: bindingOverride.isActive,
        },
      });
      setBindingLoading(false);
      void refresh(bindingOverride.serviceCode, isCurrent);
      return;
    }

    const isCurrent = guard.current.begin();
    setBindingLoading(true);
    void (async () => {
      let next: OneLevelBindingState;
      try {
        const response = await fetch(oneLevelBindingPath(methodCode), {
          headers: adminRequestHeaders(),
          cache: "no-store",
        });
        const payload = await response.json().catch(() => null);
        next = response.ok ? mapHostBinding(payload) : { state: "untrusted" };
      } catch {
        next = { state: "untrusted" };
      }
      if (!isCurrent()) return;
      setBinding(next);
      setBindingLoading(false);
      if (next.state === "resolved") {
        void refresh(next.binding.serviceCode, isCurrent);
      }
    })();
  }, [methodCode, refresh, bindingOverride?.serviceCode, bindingOverride?.optionCode, bindingOverride?.isActive]);

  const state = resolveScheduleState({
    hasMethod: methodCode.length > 0,
    bindingLoading,
    bindingResolved: resolvedBinding != null,
    bindingKnown: binding != null,
    schedulesLoading: false,
    schedulesState,
  });

  const scoped = useMemo(
    () => (resolvedBinding ? scopeSchedulesToBinding(view?.schedules ?? null, resolvedBinding) : []),
    [resolvedBinding, view],
  );
  const stored = scoped[0] ?? null;
  const summary = useMemo(() => summarizeSchedules(scoped), [scoped]);

  /** بارگذاری فرم از تنها برنامهٔ همین هویت؛ نبود برنامه یعنی فرم خالی و آمادهٔ ایجاد. */
  const loadEditorFrom = useCallback((schedule: ShippingOperatingScheduleAdminItem | null) => {
    setIssues([]);
    setDirty(false);
    if (!schedule) {
      setTimeZoneId(DEFAULT_TIME_ZONE_ID);
      setRows([]);
      return;
    }
    setTimeZoneId(schedule.timeZoneId || DEFAULT_TIME_ZONE_ID);
    setRows(sortWindowRows(windowRowsFromSchedule(schedule)));
  }, []);

  useEffect(() => {
    if (state === "ready") loadEditorFrom(stored);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state, stored?.scheduleId]);

  function updateRow(key: string, patch: Partial<ShippingWindowFormRow>) {
    setRows((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
    setIssues([]);
    setDirty(true);
  }

  function addRow(weekday: ShippingWindowFormRow["weekday"]) {
    setRows((prev) => [...prev, emptyWindowRow(weekday)]);
    setIssues([]);
    setDirty(true);
  }

  function removeRow(key: string) {
    setRows((prev) => prev.filter((row) => row.key !== key));
    setIssues([]);
    setDirty(true);
  }

  async function save() {
    if (!resolvedBinding) return;
    const problems = validateShippingScheduleForm({ timeZoneId, windows: rows });
    if (problems.length > 0) {
      setIssues(problems);
      return;
    }
    setIssues([]);
    const target = scheduleWriteTarget(resolvedBinding);
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await upsertAdminShippingOperatingSchedule(
      target.serviceCode,
      buildScheduleWriteRequest({
        serviceCode: target.serviceCode,
        timeZoneId,
        // هویت گزینه فقط از پیوند سرور می‌آید؛ هیچ‌گاه از کد روش یا ورودی کاربر.
        optionCode: target.optionCode,
        windows: rows,
      }),
    );
    setBusy(false);
    if (!operationStillTargetsMethod(startedFor, methodCode)) {
      // روش عوض شده است؛ نتیجه به روش جدید نسبت داده نمی‌شود و اعلان به هویت اصلی می‌ماند.
      return;
    }
    if (result.state !== "ok") {
      emit({ tone: "error", text: mapScheduleErrorFa(result.message, "ذخیرهٔ برنامهٔ این روش ناموفق بود؛ دوباره تلاش کنید.") });
      // خواندن مجدد پس از خطا: فرم روی حالت واقعی سرور بماند، نه روی خواستهٔ ناموفق.
      if (isCurrent()) await refresh(target.serviceCode, isCurrent);
      return;
    }
    setDirty(false);
    emit({
      tone: "success",
      text: `برنامهٔ روش ${startedFor} با ${result.data!.windows.length.toLocaleString("fa-IR")} بازه ذخیره شد.`,
    });
    if (isCurrent()) await refresh(target.serviceCode, isCurrent);
  }

  async function toggle(schedule: ShippingOperatingScheduleAdminItem) {
    if (!resolvedBinding) return;
    const target = scheduleWriteTarget(resolvedBinding);
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await setAdminShippingOperatingScheduleActive(target.serviceCode, schedule.scheduleId, !schedule.isActive);
    setBusy(false);
    if (!operationStillTargetsMethod(startedFor, methodCode)) return;
    if (result.state !== "ok") {
      emit({ tone: "error", text: mapScheduleErrorFa(result.message, "تغییر وضعیت برنامهٔ این روش ناموفق بود.") });
      return;
    }
    emit({
      tone: "success",
      text: `برنامهٔ روش ${startedFor} ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.`,
    });
    if (isCurrent()) await refresh(target.serviceCode, isCurrent);
  }

  if (state === "no-method") {
    return (
      <div className="rounded-ds border border-dashed border-border p-6" data-testid="shipping-one-level-schedule-no-method">
        <p className="font-medium">هیچ روشی انتخاب نشده است</p>
        <p className="mt-1 text-sm text-muted">
          برای دیدن برنامه کاری و ظرفیت، ابتدا در زبانهٔ «سرویس‌ها» یک روش یک‌سطحی را انتخاب کنید.
        </p>
      </div>
    );
  }

  if (state === "binding-loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-schedule-binding-loading">
        در حال خواندن پیوند سرویس/گزینهٔ این روش…
      </p>
    );
  }

  if (state === "binding-unavailable") {
    return (
      <div className="rounded-ds border border-border p-6" data-testid="shipping-one-level-schedule-binding-unavailable">
        <p className="font-medium">برنامه کاری و ظرفیت این روش در دسترس نیست</p>
        <p className="mt-1 text-sm text-muted">{bindingUnavailableFa(binding, methodCode)}</p>
        <p className="mt-2 text-xs text-muted">
          نگاشت کد روش به سرویس/گزینه فقط سمت سرور انجام می‌شود؛ این صفحه هیچ حدسی نمی‌زند.
        </p>
      </div>
    );
  }

  if (state === "loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-schedule-loading">
        در حال بارگذاری برنامه‌های این روش…
      </p>
    );
  }

  if (state === "denied") {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-schedule-denied">
        دسترسی لازم برای مشاهده یا تغییر برنامهٔ این روش را ندارید.
      </p>
    );
  }

  if (state === "error" || !resolvedBinding) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-schedule-error">
        خطا در خواندن برنامه‌های این روش؛ دوباره تلاش کنید.
      </p>
    );
  }

  // پاسخِ خدمتِ دیگر هرگز زیر نام این روش رندر یا ویرایش نمی‌شود.
  if (!scheduleViewMatchesBindingService(view, resolvedBinding)) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-schedule-service-mismatch">
        پاسخ سرور به خدمت این روش تعلق ندارد؛ برای احتیاط هیچ برنامه‌ای نمایش داده نمی‌شود.
      </p>
    );
  }

  return (
    <div className="space-y-4" data-testid="shipping-one-level-schedule-tab">
      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium" data-testid="shipping-one-level-schedule-context">
          {bindingContextFa(resolvedBinding)}
        </p>
        <p className="mt-1 text-xs text-muted">
          هر روش یک ردیف مستقل است. این بخش روی همان روش انتخاب‌شده عمل می‌کند و هیچ سطح دوم پیکربندی نمی‌سازد.
        </p>
      </div>

      {notice && (
        <p
          className={notice.tone === "success" ? "text-sm text-emerald-700" : "text-sm text-red-600"}
          data-testid="shipping-one-level-schedule-notice"
        >
          {notice.text}
        </p>
      )}

      <div className="space-y-2 rounded-ds border border-border p-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h3 className="text-sm font-bold">برنامهٔ ثبت‌شدهٔ این روش</h3>
          <span className="text-[11px] text-muted" data-testid="shipping-one-level-schedule-summary">
            {scheduleStateFa(scoped)}
          </span>
        </div>

        {scoped.length === 0 ? (
          <p className="text-xs text-muted" data-testid="shipping-one-level-schedule-empty">
            {ONE_LEVEL_NO_SCHEDULE_FA}
          </p>
        ) : (
          <>
            <p className="text-[11px] text-muted" data-testid="shipping-one-level-schedule-inactive-note">
              {ONE_LEVEL_INACTIVE_SCHEDULE_FA} ({inactiveScheduleCountFa(scoped)})
            </p>
            {scoped.map((schedule) => (
              <div
                key={schedule.scheduleId}
                className="space-y-2 rounded-ds border border-border p-3 text-xs"
                data-testid={`shipping-one-level-schedule-row-${schedule.scheduleId}`}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span dir="ltr" className="font-mono">{schedule.timeZoneId}</span>
                  <span
                    className={schedule.isActive ? "font-bold text-emerald-700" : "font-bold text-muted"}
                    data-testid={`shipping-one-level-schedule-active-${schedule.scheduleId}`}
                  >
                    {schedule.isActive ? "فعال" : "غیرفعال"}
                  </span>
                </div>
                <div className="flex flex-wrap gap-3 text-[11px] text-muted">
                  <span>بازه‌ها: {schedule.windows.length.toLocaleString("fa-IR")}</span>
                  <span data-testid={`shipping-one-level-schedule-capacity-${schedule.scheduleId}`}>
                    {configuredCapacityFa(schedule.configuredCapacityTotal)}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  {schedule.windows.map((window) => (
                    <span
                      key={window.windowId}
                      className="rounded-ds border border-border px-2 py-0.5 text-[11px]"
                      data-testid={`shipping-one-level-schedule-window-${window.windowId}`}
                    >
                      {SHIPPING_WEEKDAYS.find((row) => row.weekday === window.weekday)?.labelFa ?? `روز ${window.weekday}`} · {formatWindowFa(window)}
                    </span>
                  ))}
                </div>
                <button
                  type="button"
                  className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
                  disabled={busy}
                  onClick={() => void toggle(schedule)}
                  data-testid={`shipping-one-level-schedule-toggle-${schedule.scheduleId}`}
                >
                  {schedule.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                </button>
              </div>
            ))}
          </>
        )}
      </div>

      <p className="rounded-ds bg-secondary p-3 text-[11px] text-secondary-foreground" data-testid="shipping-one-level-schedule-scope-note">
        {ONE_LEVEL_WINDOW_SCOPE_FA} {OPERATING_WINDOW_SCOPE_FA}
      </p>

      <p className="rounded-ds bg-secondary p-3 text-[11px] text-secondary-foreground" data-testid="shipping-one-level-schedule-capacity-note">
        {ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA} — {ONE_LEVEL_CAPACITY_ENFORCEMENT_FA}
      </p>

      <section className="space-y-3 rounded-ds border border-dashed border-border p-3">
        <h3 className="text-sm font-bold">ویرایش برنامهٔ این روش</h3>
        <p className="text-[11px] text-muted">
          برنامه روی همان خدمتی ثبت می‌شود که این روش به آن پیوند دارد؛ پیوند از سرور می‌آید و قابل‌تغییر نیست.
          ذخیره، مجموعهٔ بازه‌های همین هویت را جایگزین می‌کند و برنامهٔ هیچ روش دیگری تغییر نمی‌کند.
        </p>

        <label className="block text-[11px] text-muted">
          منطقهٔ زمانی (IANA)
          <input
            className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
            dir="ltr"
            list="shipping-one-level-timezones"
            value={timeZoneId}
            onChange={(e) => { setTimeZoneId(e.target.value); setIssues([]); setDirty(true); }}
            data-testid="shipping-one-level-schedule-timezone"
          />
          <datalist id="shipping-one-level-timezones">
            {COMMON_TIME_ZONE_IDS.map((zone) => <option key={zone} value={zone} />)}
          </datalist>
        </label>

        <div className="space-y-2">
          {rows.length === 0 && (
            <p className="text-xs text-muted" data-testid="shipping-one-level-schedule-editor-empty">
              {NO_SCHEDULE_DISCLOSURE_FA}
            </p>
          )}
          {rows.map((row) => (
            <div key={row.key} className="grid gap-2 sm:grid-cols-4" data-testid={`shipping-one-level-schedule-row-edit-${row.key}`}>
              <label className="text-[11px] text-muted">
                روز هفته
                <select
                  className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                  value={row.weekday}
                  disabled={busy}
                  onChange={(e) => updateRow(row.key, { weekday: Number(e.target.value) as ShippingWindowFormRow["weekday"] })}
                  data-testid={`shipping-one-level-schedule-weekday-${row.key}`}
                >
                  {SHIPPING_WEEKDAYS.map((day) => (
                    <option key={day.weekday} value={day.weekday}>{day.labelFa}</option>
                  ))}
                </select>
              </label>
              <label className="text-[11px] text-muted">
                شروع
                <input
                  className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                  dir="ltr"
                  type="time"
                  value={row.startTime}
                  onChange={(e) => updateRow(row.key, { startTime: e.target.value })}
                  data-testid={`shipping-one-level-schedule-start-${row.key}`}
                />
              </label>
              <label className="text-[11px] text-muted">
                پایان
                <input
                  className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                  dir="ltr"
                  type="time"
                  value={row.endTime}
                  onChange={(e) => updateRow(row.key, { endTime: e.target.value })}
                  data-testid={`shipping-one-level-schedule-end-${row.key}`}
                />
              </label>
              <label className="text-[11px] text-muted">
                ظرفیت تنظیم‌شده
                <input
                  className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                  dir="ltr"
                  inputMode="numeric"
                  placeholder="خالی = تعیین‌نشده"
                  value={row.capacity}
                  onChange={(e) => updateRow(row.key, { capacity: e.target.value })}
                  data-testid={`shipping-one-level-schedule-capacity-input-${row.key}`}
                />
              </label>
              <button
                type="button"
                className="rounded-ds border border-border px-3 py-1 text-xs sm:col-span-4"
                disabled={busy}
                onClick={() => removeRow(row.key)}
                data-testid={`shipping-one-level-schedule-remove-${row.key}`}
              >
                حذف بازه
              </button>
            </div>
          ))}
        </div>

        <div className="flex flex-wrap gap-2">
          {SHIPPING_WEEKDAYS.map((day) => (
            <button
              key={day.weekday}
              type="button"
              className="rounded-ds border border-border px-2 py-1 text-[11px] disabled:opacity-50"
              disabled={busy}
              onClick={() => addRow(day.weekday)}
              data-testid={`shipping-one-level-schedule-add-${day.weekday}`}
            >
              + {day.labelFa}
            </button>
          ))}
        </div>

        <p className="text-[10px] text-muted">
          {CAPACITY_CONFIG_ONLY_FA} — خالی‌بودن ظرفیت یعنی «تعیین‌نشده»، نه صفر و نه رزرو.
        </p>

        {issues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-schedule-issues">
            {issues.map((issue) => <li key={`${issue.code}-${issue.rowKey ?? "form"}`}>{issue.messageFa}</li>)}
          </ul>
        )}

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
            disabled={busy}
            onClick={() => void save()}
            data-testid="shipping-one-level-schedule-save"
          >
            {busy ? "در حال ذخیره…" : "ذخیرهٔ برنامه"}
          </button>
          <button
            type="button"
            className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
            disabled={busy || !dirty}
            onClick={() => loadEditorFrom(stored)}
            data-testid="shipping-one-level-schedule-cancel"
          >
            انصراف از تغییرات
          </button>
        </div>
      </section>
    </div>
  );
}
