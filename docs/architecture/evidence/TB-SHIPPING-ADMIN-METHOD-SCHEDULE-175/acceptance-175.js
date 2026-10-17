/**
 * TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175 — isolated owner-API acceptance for the method-owned
 * operating schedule (Admin «برنامه کاری حمل») on a disposable loopback Host.
 *
 * Proves, through the real authorized Shipping owner endpoints only:
 *   - the schedule identity is (serviceCode, optionCode) from the method binding, never the method text;
 *   - a method-owned row is written, re-read and replaced without touching the sibling option;
 *   - a service-level (legacy) row stays auditable and is never mutated by a method edit;
 *   - an active schedule with zero windows is a real "restricted-empty" row, not "unrestricted";
 *   - unknown option, body/path service mismatch, cross-service PATCH and anonymous writes fail closed.
 *
 * No geography, price, carrier calendar, slot or availability is invented here. The customer
 * delivery promise is deliberately NOT asserted as changed by these writes.
 *
 * Usage:
 *   TOOBA_175_SCRATCH_PASSWORD=... node acceptance-175.js
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const BASE = process.env.TOOBA_175_HOST || "http://127.0.0.1:5429";
const EVIDENCE = __dirname;
const SERVICE = "tipax";
const OPTION = "standard";
const SIBLING = "express";
const OTHER_SERVICE = "post";
const UNKNOWN_OPTION = "scratch_unknown_175";
const TZ = "Asia/Tehran";

const checks = [];
const trace = [];
const record = (id, ok, detail = "") => {
    checks.push({ id, ok, detail });
    console.log(`${ok ? "PASS" : "FAIL"} ${id} ${detail}`);
};
const prop = (o, ...names) => {
    if (!o || typeof o !== "object") return undefined;
    for (const n of names) if (o[n] !== undefined && o[n] !== null) return o[n];
    return undefined;
};
const unwrap = (j) => (j && typeof j === "object" && j.data !== undefined ? j.data : j);
const errorCode = (p) => (p && typeof p === "object"
    ? p.errorCode || p.code || (p.error && p.error.code) || null
    : null);

function psql(sql) {
    const container = process.env.TOOBA_175_CONTAINER || "tooba-tb175-pg";
    const user = process.env.TOOBA_175_USER || "scratch175";
    const database = process.env.TOOBA_175_DATABASE || "tooba_175_scratch";
    const password = process.env.TOOBA_175_SCRATCH_PASSWORD || "";
    return execFileSync(
        "docker",
        ["exec", "-e", `PGPASSWORD=${password}`, container, "psql", "-U", user, "-d", database, "-tAc", sql],
        { encoding: "utf8" }).trim();
}

const adminHeaders = (actorId) => ({
    Accept: "application/json",
    "Accept-Language": "fa-IR",
    "X-Tooba-Dev-Actor-User-Id": actorId,
    "Content-Type": "application/json",
});

async function call(name, url, init) {
    const res = await fetch(`${BASE}${url}`, init);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { }
    trace.push(`${name} ${url} -> ${res.status} :: ${text.slice(0, 700).replace(/\s+/g, " ")}`);
    return { status: res.status, json, text };
}

const schedulesUrl = (service) =>
    `/v1/admin/shipping-services/${encodeURIComponent(service)}/operating-schedules`;
const listSchedules = (actorId, service = SERVICE) =>
    call("LIST", schedulesUrl(service), { headers: adminHeaders(actorId) });
const putSchedule = (actorId, service, body) =>
    call("PUT", schedulesUrl(service), {
        method: "PUT",
        headers: adminHeaders(actorId),
        body: JSON.stringify(body),
    });
const windowOf = (weekday, startTime, endTime, capacity = null) => ({
    weekday, startTime, endTime, capacity,
});
const rowsFor = (view, service, option) => (prop(view, "schedules") || []).filter(
    (row) => prop(row, "serviceCode") === service && (prop(row, "optionCode") ?? null) === option);
const windowShape = (row) => (prop(row, "windows") || []).map(
    (w) => `${prop(w, "weekday")}:${prop(w, "startTime")}-${prop(w, "endTime")}`).join(",");

(async () => {
    // ---------- 0. Authorized Development Admin actor ----------
    const dev = await call("DEV_CONTEXT", "/v1/admin/dev-context", { headers: { Accept: "application/json" } });
    const actorUserId = prop(unwrap(dev.json) || {}, "actorUserId");
    record("dev_context_available", dev.status === 200 && !!actorUserId, `http=${dev.status}`);

    await call("BOOTSTRAP", "/v1/admin/access-control/bootstrap", {
        method: "POST", headers: adminHeaders(actorUserId), body: "{}",
    });
    const caps = await call("CAPABILITIES", "/v1/admin/access-control/me/capabilities", { headers: adminHeaders(actorUserId) });
    const perms = ((unwrap(caps.json) || {}).permissions || [])
        .map((p) => (typeof p === "string" ? p : prop(p, "permissionId")));
    record("fulfillment_manage_granted", perms.includes("fulfillment.manage"), `granted=${perms.length}`);

    // ---------- 1. Baseline: no schedule for this identity => unrestricted ----------
    const baseline = await listSchedules(actorUserId);
    const baselineView = unwrap(baseline.json) || {};
    record("baseline_read_ok", baseline.status === 200, `http=${baseline.status}`);
    record("baseline_no_own_schedule",
        rowsFor(baselineView, SERVICE, OPTION).length === 0,
        `own=${rowsFor(baselineView, SERVICE, OPTION).length} all=${(prop(baselineView, "schedules") || []).length}`);
    record("baseline_sibling_absent",
        rowsFor(baselineView, SERVICE, SIBLING).length === 0,
        `sibling=${rowsFor(baselineView, SERVICE, SIBLING).length}`);
    record("baseline_db_rows_zero",
        psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}'`) === "0",
        "no persisted tipax schedule before the write");

    // ---------- 2. Write the method-owned row (Monday 09:00-12:00) ----------
    const created = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: OPTION,
        timeZoneId: TZ,
        windows: [windowOf(1, "09:00:00", "12:00:00")],
    });
    const createdRow = unwrap(created.json) || {};
    record("standard_schedule_created",
        created.status === 200
        && prop(createdRow, "optionCode") === OPTION
        && prop(createdRow, "timeZoneId") === TZ
        && windowShape(createdRow) === "1:09:00:00-12:00:00",
        `http=${created.status} option=${prop(createdRow, "optionCode")} windows=${windowShape(createdRow)}`);
    const createdId = prop(createdRow, "scheduleId");

    // ---------- 3. Re-read from the server: exactly this identity ----------
    const reread = await listSchedules(actorUserId);
    const rereadView = unwrap(reread.json) || {};
    const own = rowsFor(rereadView, SERVICE, OPTION);
    record("standard_reopened_from_server",
        reread.status === 200 && own.length === 1
        && windowShape(own[0]) === "1:09:00:00-12:00:00"
        && prop(own[0], "isActive") === true,
        `rows=${own.length} windows=${own[0] ? windowShape(own[0]) : "none"}`);
    record("sibling_not_touched_by_standard_write",
        rowsFor(rereadView, SERVICE, SIBLING).length === 0
        && psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}' and option_code='${SIBLING}'`) === "0",
        "express remains undeclared");
    record("db_identity_exact_one_row",
        psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}' and option_code='${OPTION}'`) === "1"
        && psql(`select count(*) from shipping.operating_windows where schedule_id='${createdId}'`) === "1",
        "one schedule row, one window row");

    // ---------- 4. Sibling gets its own different schedule ----------
    const siblingPut = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: SIBLING,
        timeZoneId: TZ,
        windows: [windowOf(2, "10:00:00", "11:00:00")],
    });
    record("sibling_schedule_created", siblingPut.status === 200, `http=${siblingPut.status}`);
    const afterSibling = await listSchedules(actorUserId);
    const afterSiblingView = unwrap(afterSibling.json) || {};
    const ownAfterSibling = rowsFor(afterSiblingView, SERVICE, OPTION);
    const sibAfterSibling = rowsFor(afterSiblingView, SERVICE, SIBLING);
    record("standard_unchanged_by_sibling_write",
        ownAfterSibling.length === 1 && windowShape(ownAfterSibling[0]) === "1:09:00:00-12:00:00",
        `standard=${ownAfterSibling[0] ? windowShape(ownAfterSibling[0]) : "none"}`);
    record("sibling_reads_its_own_window",
        sibAfterSibling.length === 1 && windowShape(sibAfterSibling[0]) === "2:10:00:00-11:00:00",
        `express=${sibAfterSibling[0] ? windowShape(sibAfterSibling[0]) : "none"}`);

    // ---------- 5. Legacy service-level row stays auditable and isolated ----------
    const legacyPut = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: null,
        timeZoneId: TZ,
        windows: [windowOf(3, "08:00:00", "09:00:00")],
    });
    record("legacy_service_level_row_created", legacyPut.status === 200, `http=${legacyPut.status}`);

    // Replace the method-owned windows (two windows) — the legacy row must not move.
    const replaced = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: OPTION,
        timeZoneId: TZ,
        windows: [windowOf(1, "09:00:00", "12:00:00"), windowOf(4, "13:00:00", "15:00:00")],
    });
    const replacedRow = unwrap(replaced.json) || {};
    record("standard_replace_is_full_window_set",
        replaced.status === 200
        && windowShape(replacedRow) === "1:09:00:00-12:00:00,4:13:00:00-15:00:00"
        && prop(replacedRow, "scheduleId") === createdId,
        `id-stable=${prop(replacedRow, "scheduleId") === createdId} windows=${windowShape(replacedRow)}`);

    const afterReplace = await listSchedules(actorUserId);
    const afterReplaceView = unwrap(afterReplace.json) || {};
    const legacyRows = rowsFor(afterReplaceView, SERVICE, null);
    record("legacy_row_untouched_by_method_edit",
        legacyRows.length === 1 && windowShape(legacyRows[0]) === "3:08:00:00-09:00:00",
        `legacy=${legacyRows[0] ? windowShape(legacyRows[0]) : "none"}`);
    record("sibling_untouched_by_standard_edit",
        windowShape(rowsFor(afterReplaceView, SERVICE, SIBLING)[0] ?? {}) === "2:10:00:00-11:00:00",
        `express=${windowShape(rowsFor(afterReplaceView, SERVICE, SIBLING)[0] ?? {})}`);
    record("db_three_distinct_identities",
        psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}'`) === "3"
        && psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}' and option_code is null`) === "1",
        "service-level + standard + express, all separate rows");

    // ---------- 6. Restricted-empty: active schedule with zero windows ----------
    const emptyPut = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: OPTION,
        timeZoneId: TZ,
        windows: [],
    });
    const emptyRow = unwrap(emptyPut.json) || {};
    record("restricted_empty_accepted_as_real_row",
        emptyPut.status === 200
        && prop(emptyRow, "isActive") === true
        && (prop(emptyRow, "windows") || []).length === 0,
        `http=${emptyPut.status} windows=${(prop(emptyRow, "windows") || []).length}`);
    record("restricted_empty_is_not_deleted",
        psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}' and option_code='${OPTION}'`) === "1"
        && psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}' and option_code='${OPTION}' and is_active`) === "1",
        "row kept active with no window; not silently reverted to unrestricted");

    // Restore the two-window method policy for the final read-back.
    const restored = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: OPTION,
        timeZoneId: TZ,
        windows: [windowOf(1, "09:00:00", "12:00:00"), windowOf(4, "13:00:00", "15:00:00")],
    });
    record("method_policy_restored", restored.status === 200, `http=${restored.status}`);

    // ---------- 7. Fail-closed writes ----------
    const beforeFailures = psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}'`);

    const unknown = await putSchedule(actorUserId, SERVICE, {
        serviceCode: SERVICE,
        optionCode: UNKNOWN_OPTION,
        timeZoneId: TZ,
        windows: [windowOf(1, "09:00:00", "10:00:00")],
    });
    record("unknown_option_rejected",
        unknown.status >= 400 && unknown.status < 500,
        `http=${unknown.status} code=${errorCode(unknown.json)}`);

    const mismatch = await putSchedule(actorUserId, SERVICE, {
        serviceCode: OTHER_SERVICE,
        optionCode: OPTION,
        timeZoneId: TZ,
        windows: [windowOf(1, "09:00:00", "10:00:00")],
    });
    record("body_path_service_mismatch_rejected",
        mismatch.status >= 400 && mismatch.status < 500,
        `http=${mismatch.status} code=${errorCode(mismatch.json)}`);

    const crossService = await call("CROSS_SERVICE_PATCH", `${schedulesUrl(OTHER_SERVICE)}/${createdId}/active`, {
        method: "PATCH",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({ isActive: false }),
    });
    record("cross_service_active_toggle_rejected",
        crossService.status >= 400 && crossService.status < 500,
        `http=${crossService.status} code=${errorCode(crossService.json)}`);

    const anonymousGet = await fetch(`${BASE}${schedulesUrl(SERVICE)}`, { headers: { Accept: "application/json" } });
    record("anonymous_read_rejected", anonymousGet.status === 401 || anonymousGet.status === 403, `http=${anonymousGet.status}`);

    const anonymousPut = await fetch(`${BASE}${schedulesUrl(SERVICE)}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            serviceCode: SERVICE, optionCode: OPTION, timeZoneId: TZ,
            windows: [windowOf(1, "09:00:00", "10:00:00")],
        }),
    });
    record("anonymous_write_rejected", anonymousPut.status === 401 || anonymousPut.status === 403, `http=${anonymousPut.status}`);

    const afterFailures = psql(`select count(*) from shipping.operating_schedules where service_code='${SERVICE}'`);
    record("rejected_writes_changed_no_row", beforeFailures === afterFailures, `rows=${afterFailures}`);
    record("standard_still_active_after_rejections",
        psql(`select is_active from shipping.operating_schedules where service_code='${SERVICE}' and option_code='${OPTION}'`) === "t",
        "cross-service toggle did not deactivate the real row");

    // ---------- 8. Final authoritative read-back ----------
    const final = await listSchedules(actorUserId);
    const finalView = unwrap(final.json) || {};
    const finalOwn = rowsFor(finalView, SERVICE, OPTION);
    record("final_read_back_is_method_owned",
        final.status === 200
        && finalOwn.length === 1
        && windowShape(finalOwn[0]) === "1:09:00:00-12:00:00,4:13:00:00-15:00:00"
        && (prop(finalOwn[0], "optionCode") ?? null) === OPTION,
        `windows=${finalOwn[0] ? windowShape(finalOwn[0]) : "none"}`);
    record("final_legacy_and_sibling_still_distinct",
        windowShape(rowsFor(finalView, SERVICE, null)[0] ?? {}) === "3:08:00:00-09:00:00"
        && windowShape(rowsFor(finalView, SERVICE, SIBLING)[0] ?? {}) === "2:10:00:00-11:00:00",
        "service-level and sibling identities unchanged");

    const passed = checks.filter((c) => c.ok).length;
    fs.writeFileSync(path.join(EVIDENCE, "assertion-matrix-175.json"), JSON.stringify({
        generatedAtUtc: new Date().toISOString(),
        host: BASE,
        task: "TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175",
        identity: { serviceCode: SERVICE, optionCode: OPTION, sibling: SIBLING, legacyOption: null },
        total: checks.length,
        passed,
        failed: checks.length - passed,
        checks,
    }, null, 2) + "\n");
    fs.writeFileSync(path.join(EVIDENCE, "http-trace-175.txt"), trace.join("\n") + "\n");
    console.log(`TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
})().catch((err) => {
    console.error("HARNESS_ERROR", err && err.stack);
    fs.writeFileSync(path.join(EVIDENCE, "http-trace-175.txt"), trace.join("\n") + "\n");
    process.exitCode = 2;
});
