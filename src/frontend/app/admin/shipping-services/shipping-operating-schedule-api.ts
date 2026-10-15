/**
 * TB-SHIPPING-FULL-OPERATIONS-072 — admin client for the Shipping-owned weekly operating schedule
 * (carrier pickup / dispatch availability), addressed by the opaque Fulfillment service `code`.
 *
 * The route service code is authoritative: the body never re-declares ownership, and every write is
 * followed by a real re-read instead of an in-memory patch. A schedule is never fabricated: an empty
 * list means "no explicit hours configured", which is a real answer.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";

export type ShippingOperatingWindowWrite = {
  weekday: number;
  /** Local time on the wire as `HH:mm:ss` (backend contract: `TimeOnly`). */
  startTime: string;
  /** Local time on the wire as `HH:mm:ss`; exclusive. */
  endTime: string;
  /** Configuration-only capacity; `null`/omitted means "not configured". */
  capacity?: number | null;
};

export type ShippingOperatingScheduleWriteRequest = {
  serviceCode: string;
  timeZoneId: string;
  windows: ShippingOperatingWindowWrite[];
  /** `null` means the service-level schedule; otherwise the Fulfillment option this schedule describes. */
  optionCode?: string | null;
};

export type ShippingOperatingWindowAdminItem = {
  windowId: string;
  weekday: number;
  startTime: string;
  endTime: string;
  capacity: number | null;
};

export type ShippingOperatingScheduleAdminItem = {
  scheduleId: string;
  serviceCode: string;
  optionCode: string | null;
  timeZoneId: string;
  isActive: boolean;
  windows: ShippingOperatingWindowAdminItem[];
  /** Configured (not enforced) capacity total; `null` means no window declares a capacity. */
  configuredCapacityTotal: number | null;
  createdAt: string;
  updatedAt: string;
};

export type ShippingOperatingSchedulesView = {
  serviceCode: string;
  schedules: ShippingOperatingScheduleAdminItem[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function pick<T extends Record<string, unknown>>(row: T, camel: string, pascal: string): unknown {
  return row[camel] ?? row[pascal];
}

function optionalText(value: unknown): string | null {
  const raw = text(value);
  return raw ? raw : null;
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function integerOrNull(value: unknown): number | null {
  const parsed = numberOrNull(value);
  return parsed == null ? null : Math.trunc(parsed);
}

function mapWindow(raw: unknown): ShippingOperatingWindowAdminItem | null {
  const row = asRecord(raw);
  if (!row) return null;
  const windowId = text(pick(row, "windowId", "WindowId"));
  const weekday = integerOrNull(pick(row, "weekday", "Weekday"));
  const startTime = text(pick(row, "startTime", "StartTime"));
  const endTime = text(pick(row, "endTime", "EndTime"));
  if (!windowId || weekday == null || !startTime || !endTime) return null;
  return {
    windowId,
    weekday,
    startTime,
    endTime,
    capacity: integerOrNull(pick(row, "capacity", "Capacity")),
  };
}

function mapSchedule(raw: unknown): ShippingOperatingScheduleAdminItem | null {
  const row = asRecord(raw);
  if (!row) return null;
  const scheduleId = text(pick(row, "scheduleId", "ScheduleId"));
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  const timeZoneId = text(pick(row, "timeZoneId", "TimeZoneId"));
  if (!scheduleId || !serviceCode) return null;
  const windowsRaw = pick(row, "windows", "Windows");
  return {
    scheduleId,
    serviceCode,
    optionCode: optionalText(pick(row, "optionCode", "OptionCode")),
    timeZoneId,
    isActive: Boolean(pick(row, "isActive", "IsActive")),
    windows: (Array.isArray(windowsRaw) ? windowsRaw : [])
      .map(mapWindow)
      .filter((x): x is ShippingOperatingWindowAdminItem => Boolean(x)),
    configuredCapacityTotal: integerOrNull(pick(row, "configuredCapacityTotal", "ConfiguredCapacityTotal")),
    createdAt: text(pick(row, "createdAt", "CreatedAt")),
    updatedAt: text(pick(row, "updatedAt", "UpdatedAt")),
  };
}

function mapView(raw: unknown): ShippingOperatingSchedulesView | null {
  const row = asRecord(raw);
  if (!row) return null;
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  if (!serviceCode) return null;
  const schedules = pick(row, "schedules", "Schedules");
  return {
    serviceCode,
    schedules: (Array.isArray(schedules) ? schedules : [])
      .map(mapSchedule)
      .filter((x): x is ShippingOperatingScheduleAdminItem => Boolean(x)),
  };
}

/**
 * Stable error code of a failed write.
 *
 * A shape failure is reported by the platform as `validation.failed` with the real, stable child code
 * inside the `errors` map (`shipping.validation.schedule_window_overlap` and friends). Surfacing only
 * the envelope would hide the actionable code, so the first non-envelope child code wins.
 */
export function scheduleErrorCode(payload: unknown): string {
  const row = asRecord(payload);
  if (!row) return "";
  const direct = text(pick(row, "errorCode", "ErrorCode"));
  if (direct && direct !== "validation.failed") return direct;
  const errors = asRecord(pick(row, "errors", "Errors"));
  if (errors) {
    for (const value of Object.values(errors)) {
      const list = Array.isArray(value) ? value : [value];
      for (const item of list) {
        const code = text(item);
        if (code && code !== "validation.failed") return code;
      }
    }
  }
  return direct || text(pick(row, "title", "Title"));
}

function base(serviceCode: string): string {
  return `/v1/admin/shipping-services/${encodeURIComponent(serviceCode)}/operating-schedules`;
}

/** Reads every stored schedule of one service (service-level plus per-option rows). */
export async function loadAdminShippingOperatingSchedules(
  serviceCode: string,
): Promise<AdminResult<ShippingOperatingSchedulesView>> {
  try {
    const response = await fetch(base(serviceCode), { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: scheduleErrorCode(payload) || "shipping_schedule.load_failed",
      };
    }
    const mapped = mapView(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_schedule.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/**
 * Creates or fully replaces the schedule of exactly one identity (service code plus optional option).
 * The backend replaces the whole window set of that identity only; other identities are untouched.
 */
export async function upsertAdminShippingOperatingSchedule(
  serviceCode: string,
  body: ShippingOperatingScheduleWriteRequest,
): Promise<AdminResult<ShippingOperatingScheduleAdminItem>> {
  try {
    const response = await fetch(base(serviceCode), {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        serviceCode,
        timeZoneId: body.timeZoneId,
        windows: body.windows,
        optionCode: body.optionCode ?? null,
      }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: scheduleErrorCode(payload) || "shipping_schedule.save_failed",
      };
    }
    const mapped = mapSchedule(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_schedule.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Activates or deactivates one stored schedule. */
export async function setAdminShippingOperatingScheduleActive(
  serviceCode: string,
  scheduleId: string,
  isActive: boolean,
): Promise<AdminResult<ShippingOperatingScheduleAdminItem>> {
  try {
    const response = await fetch(`${base(serviceCode)}/${encodeURIComponent(scheduleId)}/active`, {
      method: "PATCH",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ isActive }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: scheduleErrorCode(payload) || "shipping_schedule.active_failed",
      };
    }
    const mapped = mapSchedule(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_schedule.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
