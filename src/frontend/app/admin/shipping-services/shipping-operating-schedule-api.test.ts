/**
 * TB-SHIPPING-FULL-OPERATIONS-072 — roundtrip guard for the operating-schedule client on an in-memory
 * fixture that implements the real 071 wire contract. Proves that the UI client really calls
 * `GET /v1/admin/shipping-services/{code}/operating-schedules`, `PUT` the same route with
 * `{serviceCode,timeZoneId,windows:[{weekday,startTime,endTime,capacity}],optionCode}` and
 * `PATCH /{scheduleId}/active` with `{isActive}` — without writing to the shared Development database.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  loadAdminShippingOperatingSchedules,
  scheduleErrorCode,
  setAdminShippingOperatingScheduleActive,
  upsertAdminShippingOperatingSchedule,
} from "./shipping-operating-schedule-api.ts";
import { buildScheduleWriteRequest, emptyWindowRow } from "./shipping-operating-schedule-model.ts";

type Recorded = { method: string; url: string; body: unknown; contentType: string | null };

type WindowDto = { windowId: string; weekday: number; startTime: string; endTime: string; capacity: number | null };

type ScheduleDto = {
  scheduleId: string;
  serviceCode: string;
  optionCode: string | null;
  timeZoneId: string;
  isActive: boolean;
  windows: WindowDto[];
  configuredCapacityTotal: number | null;
  createdAt: string;
  updatedAt: string;
};

/** Fixture exactly mirrors the backend wire contract: camelCase, TimeOnly as `HH:mm:ss`. */
function createFixture() {
  const schedules = new Map<string, ScheduleDto>();
  const log: Recorded[] = [];
  let counter = 0;
  let failNext: "load" | "save" | "active" | null = null;

  function json(payload: unknown, status = 200): Response {
    return new Response(JSON.stringify(payload), { status, headers: { "Content-Type": "application/json" } });
  }

  function key(optionCode: string | null): string {
    return optionCode ?? "";
  }

  async function handle(url: string, method: string, init?: RequestInit): Promise<Response> {
    const body = init?.body ? JSON.parse(String(init.body)) : null;
    log.push({ method, url, body, contentType: new Headers(init?.headers).get("Content-Type") });
    const path = url.split("?")[0];
    const fail = failNext;
    failNext = null;

    const activeMatch = /\/operating-schedules\/([^/]+)\/active$/.exec(path);
    if (activeMatch) {
      if (fail === "active") return json({ errorCode: "shipping.schedule.not_found" }, 400);
      const found = [...schedules.values()].find((s) => s.scheduleId === activeMatch[1]);
      if (!found) return json({ errorCode: "shipping.schedule.not_found" }, 400);
      found.isActive = Boolean((body as { isActive: boolean }).isActive);
      found.updatedAt = "2026-10-15T05:00:00+00:00";
      return json(found);
    }

    const listMatch = /\/shipping-services\/([^/]+)\/operating-schedules$/.exec(path);
    if (listMatch) {
      const serviceCode = decodeURIComponent(listMatch[1]!);
      if (method === "GET") {
        if (fail === "load") return json({ errorCode: "shipping.schedule.not_found" }, 400);
        return json({
          serviceCode,
          schedules: [...schedules.values()].filter((s) => s.serviceCode === serviceCode),
        });
      }
      if (method === "PUT" && body) {
        if (fail === "save") {
          return json({
            errorCode: "validation.failed",
            errors: { "Request.Windows[0].EndTime": ["shipping.validation.schedule_window_range_invalid"] },
          }, 400);
        }
        const request = body as {
          serviceCode: string;
          timeZoneId: string;
          optionCode: string | null;
          windows: { weekday: number; startTime: string; endTime: string; capacity?: number | null }[];
        };
        const id = key(request.optionCode);
        const previous = schedules.get(id);
        counter += 1;
        const saved: ScheduleDto = {
          scheduleId: previous?.scheduleId ?? `01a13deb-cd7c-7000-a11e-641338237a1${counter}`,
          serviceCode,
          optionCode: request.optionCode,
          timeZoneId: request.timeZoneId,
          isActive: previous?.isActive ?? true,
          windows: request.windows.map((w, index) => ({
            windowId: `w-${counter}-${index}`,
            weekday: w.weekday,
            startTime: w.startTime,
            endTime: w.endTime,
            capacity: w.capacity ?? null,
          })),
          configuredCapacityTotal: request.windows.some((w) => (w.capacity ?? null) != null)
            ? request.windows.reduce((total, w) => total + (w.capacity ?? 0), 0)
            : null,
          createdAt: previous?.createdAt ?? "2026-10-15T04:57:06+00:00",
          updatedAt: "2026-10-15T04:57:06+00:00",
        };
        schedules.set(id, saved);
        return json(saved);
      }
    }

    return json({ errorCode: "unexpected" }, 404);
  }

  return {
    log,
    schedules,
    install() {
      const original = globalThis.fetch;
      globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        return handle(url, (init?.method ?? "GET").toUpperCase(), init);
      }) as typeof fetch;
      return () => {
        globalThis.fetch = original;
      };
    },
    failNextRequest(kind: "load" | "save" | "active") {
      failNext = kind;
    },
  };
}

test("read an empty schedule set: a real empty answer, not a fabricated default", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const result = await loadAdminShippingOperatingSchedules("tipax");
    assert.equal(result.state, "ok");
    assert.deepEqual(result.data, { serviceCode: "tipax", schedules: [] });
    assert.equal(api.log[0]?.method, "GET");
    assert.equal(api.log[0]?.url, "/v1/admin/shipping-services/tipax/operating-schedules");
  } finally {
    restore();
  }
});

test("create a Sunday 13:00-14:00 plus 16:30-18:00 (capacity 25) schedule with the real wire shape", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const first = { ...emptyWindowRow(7), startTime: "13:00", endTime: "14:00" };
    const second = { ...emptyWindowRow(7), startTime: "16:30", endTime: "18:00", capacity: "25" };
    const saved = await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [first, second],
    }));

    assert.equal(saved.state, "ok");
    const write = api.log[0]!;
    assert.equal(write.method, "PUT");
    assert.equal(write.url, "/v1/admin/shipping-services/tipax/operating-schedules");
    assert.match(write.contentType ?? "", /application\/json/);
    assert.deepEqual(write.body, {
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [
        { weekday: 7, startTime: "13:00:00", endTime: "14:00:00", capacity: null },
        { weekday: 7, startTime: "16:30:00", endTime: "18:00:00", capacity: 25 },
      ],
    });
    assert.equal(saved.data?.windows.length, 2);
    assert.equal(saved.data?.configuredCapacityTotal, 25);

    // read-after-write از سرور، نه از حافظهٔ کلاینت.
    const readBack = await loadAdminShippingOperatingSchedules("tipax");
    assert.equal(readBack.data?.schedules.length, 1);
    assert.equal(readBack.data?.schedules[0]?.windows[0]?.startTime, "13:00:00");
    assert.equal(readBack.data?.schedules[0]?.windows[1]?.capacity, 25);
  } finally {
    restore();
  }
});

test("repeating the same body is idempotent: same schedule id and same window set", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const body = buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [{ ...emptyWindowRow(7), startTime: "13:00", endTime: "14:00" }],
    });
    const first = await upsertAdminShippingOperatingSchedule("tipax", body);
    const second = await upsertAdminShippingOperatingSchedule("tipax", body);
    assert.equal(first.data?.scheduleId, second.data?.scheduleId);
    assert.deepEqual(
      first.data?.windows.map((w) => [w.weekday, w.startTime, w.endTime]),
      second.data?.windows.map((w) => [w.weekday, w.startTime, w.endTime]),
    );
    assert.equal(api.schedules.size, 1);
  } finally {
    restore();
  }
});

test("service-level and option-level schedules coexist without overwriting each other", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [{ ...emptyWindowRow(7), startTime: "13:00", endTime: "14:00" }],
    }));
    await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "express",
      windows: [{ ...emptyWindowRow(1), startTime: "09:00", endTime: "10:00" }],
    }));
    await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: null,
      windows: [{ ...emptyWindowRow(6), startTime: "08:00", endTime: "12:00" }],
    }));

    const view = await loadAdminShippingOperatingSchedules("tipax");
    assert.equal(view.data?.schedules.length, 3);
    assert.deepEqual(
      view.data?.schedules.map((s) => s.optionCode).sort(),
      [null, "express", "standard"].sort((a, b) => String(a).localeCompare(String(b))),
    );
  } finally {
    restore();
  }
});

test("active toggle persists and refreshes through PATCH {scheduleId}/active", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const saved = await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [{ ...emptyWindowRow(7), startTime: "13:00", endTime: "14:00" }],
    }));
    const id = saved.data!.scheduleId;

    const off = await setAdminShippingOperatingScheduleActive("tipax", id, false);
    assert.equal(off.state, "ok");
    assert.equal(off.data?.isActive, false);
    const patch = api.log.at(-1)!;
    assert.equal(patch.method, "PATCH");
    assert.equal(patch.url, `/v1/admin/shipping-services/tipax/operating-schedules/${id}/active`);
    assert.deepEqual(patch.body, { isActive: false });

    const on = await setAdminShippingOperatingScheduleActive("tipax", id, true);
    assert.equal(on.data?.isActive, true);
    const view = await loadAdminShippingOperatingSchedules("tipax");
    assert.equal(view.data?.schedules[0]?.isActive, true);
  } finally {
    restore();
  }
});

test("stable failure codes are surfaced, including the child code of a validation envelope", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    api.failNextRequest("save");
    const rejected = await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
      serviceCode: "tipax",
      timeZoneId: "Asia/Tehran",
      optionCode: "standard",
      windows: [{ ...emptyWindowRow(7), startTime: "14:00", endTime: "13:00" }],
    }));
    assert.equal(rejected.state, "error");
    assert.equal(rejected.status, 400);
    // کد فرزند پایدار باید دیده شود، نه پوشش عمومی validation.failed.
    assert.equal(rejected.message, "shipping.validation.schedule_window_range_invalid");
    assert.equal(api.schedules.size, 0);

    api.failNextRequest("active");
    const badToggle = await setAdminShippingOperatingScheduleActive("tipax", "missing", false);
    assert.equal(badToggle.state, "error");
    assert.equal(badToggle.message, "shipping.schedule.not_found");
  } finally {
    restore();
  }
});

test("401/403 map to denied and unreachable network maps to a stable error", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    for (const status of [401, 403]) {
      globalThis.fetch = (async () => new Response(null, { status })) as typeof fetch;
      assert.equal((await loadAdminShippingOperatingSchedules("tipax")).state, "denied");
      assert.equal((await upsertAdminShippingOperatingSchedule("tipax", buildScheduleWriteRequest({
        serviceCode: "tipax",
        timeZoneId: "Asia/Tehran",
        optionCode: null,
        windows: [{ ...emptyWindowRow(7), startTime: "13:00", endTime: "14:00" }],
      }))).state, "denied");
      assert.equal((await setAdminShippingOperatingScheduleActive("tipax", "x", true)).state, "denied");
    }

    globalThis.fetch = (async () => {
      throw new TypeError("fetch failed");
    }) as typeof fetch;
    const unreachable = await loadAdminShippingOperatingSchedules("tipax");
    assert.equal(unreachable.state, "error");
    assert.equal(unreachable.message, "host-unreachable");
  } finally {
    restore();
  }
});

test("scheduleErrorCode prefers the actionable child code over the validation envelope", () => {
  assert.equal(scheduleErrorCode({ errorCode: "shipping.schedule.not_found" }), "shipping.schedule.not_found");
  assert.equal(
    scheduleErrorCode({ errorCode: "validation.failed", errors: { Windows: ["shipping.validation.schedule_window_overlap"] } }),
    "shipping.validation.schedule_window_overlap",
  );
  assert.equal(scheduleErrorCode({ title: "boom" }), "boom");
  assert.equal(scheduleErrorCode(null), "");
});
