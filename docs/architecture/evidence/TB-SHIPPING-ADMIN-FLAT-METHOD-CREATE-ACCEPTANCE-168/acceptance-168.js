/**
 * TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168 — authenticated private-Admin HTTP acceptance.
 *
 * Real acceptance over the private isolated Host only (127.0.0.1:5422). No shared Host/DB/port and no
 * fabricated access header: the dev actor id comes from the Host's own dev-context seam, and its
 * capabilities come from the product's own access-control bootstrap (persisted role + assignment +
 * real authorization tuples). The create leg uses the exact canonical POST shipped by TB-167.
 *
 * Usage: node acceptance-168.js
 */
const fs = require('fs');
const path = require('path');

const HOST = process.env.TOOBA_168_HOST || 'http://127.0.0.1:5422';
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), '.tmp-168-run');

const CREATE_PATH = '/v1/admin/shipping-services/shipping-methods/one-level';
const UNKNOWN_ACTOR = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';

const checks = [];
const trace = [];
const httpLog = [];

function record(id, ok, detail) {
    checks.push({ id, ok, detail });
    trace.push(`- ${ok ? 'PASS' : 'FAIL'} :: ${id} :: ${detail}`);
    console.log(`${ok ? 'PASS' : 'FAIL'} ${id} ${detail}`);
}

async function call(method, url, actorId, body) {
    const headers = { Accept: 'application/json', 'Accept-Language': 'fa-IR' };
    if (actorId) headers['X-Tooba-Dev-Actor-User-Id'] = actorId;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    const response = await fetch(`${HOST}${url}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await response.text();
    let json = null;
    try { json = JSON.parse(text); } catch { }
    httpLog.push(`${method} ${url} -> ${response.status} :: ${text.slice(0, 900).replace(/\s+/g, ' ')}`);
    return { status: response.status, json, text };
}

function errorCode(payload) {
    if (!payload || typeof payload !== 'object') return null;
    return payload.errorCode || payload.code || (payload.error && payload.error.code) || null;
}

function data(payload) {
    return payload && typeof payload === 'object' && payload.data !== undefined ? payload.data : payload;
}

/** Authoritative one-level catalog rows (server shape: { gate, ready, languageId, methods: [...] }). */
function catalogRows(payload) {
    const body = data(payload) || {};
    const rows = body.methods || body.Methods || body.items || body.Items || [];
    return Array.isArray(rows) ? rows : [];
}

main().catch((err) => {
    console.error('HARNESS_ERROR', err && err.stack);
    process.exitCode = 2;
});

async function main() {
    // 1. Sanctioned Development-only dev actor seam.
    const devContext = await call('GET', '/v1/admin/dev-context');
    const actorUserId = data(devContext.json) && (data(devContext.json).actorUserId || data(devContext.json).ActorUserId);
    record('dev_context_available', devContext.status === 200 && !!actorUserId,
        `http=${devContext.status} actorResolved=${!!actorUserId}`);

    // 2. Fail-closed anonymous read.
    const anonList = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level');
    record('one_level_list_anonymous_fail_closed', anonList.status === 401 || anonList.status === 403,
        `http=${anonList.status} errorCode=${errorCode(anonList.json)}`);

    // 3. Unknown but well-formed actor fails closed (no global allowlist).
    const unknownList = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level', UNKNOWN_ACTOR);
    record('one_level_list_unknown_actor_fail_closed', unknownList.status === 401 || unknownList.status === 403,
        `http=${unknownList.status} errorCode=${errorCode(unknownList.json)}`);

    // 4. Product's own access-control bootstrap (idempotent) for the sanctioned dev actor.
    const bootstrap = await call('POST', '/v1/admin/access-control/bootstrap', actorUserId, {});
    record('access_control_bootstrap_ok', bootstrap.status === 200 || bootstrap.status === 204,
        `http=${bootstrap.status}`);

    const capabilities = await call('GET', '/v1/admin/access-control/me/capabilities', actorUserId);
    const permissions = (data(capabilities.json) && data(capabilities.json).permissions) || [];
    const permissionIds = (Array.isArray(permissions) ? permissions : [])
        .map((p) => (typeof p === 'string' ? p : (p && (p.permissionId || p.PermissionId)) || null))
        .filter(Boolean);
    record('fulfillment_manage_granted', permissionIds.includes('fulfillment.manage'),
        `http=${capabilities.status} grants=${permissionIds.length}`);

    // 5. Real registry languages (never invented ids).
    const languages = await call('GET', '/v1/admin/languages', actorUserId);
    const languageRows = (data(languages.json) && (data(languages.json).items || data(languages.json).Items || data(languages.json))) || [];
    const langs = (Array.isArray(languageRows) ? languageRows : [])
        .map((l) => ({
            id: l.languageId || l.LanguageId || l.id,
            code: l.code || l.Code,
            active: l.isActive === undefined ? l.IsActive : l.isActive,
        }))
        .filter((l) => l.id && l.code);
    const fa = langs.find((l) => l.code === 'fa-IR');
    const en = langs.find((l) => l.code === 'en-US');
    fs.writeFileSync(path.join(OUT_DIR, 'languages-168.json'), JSON.stringify(languages.json, null, 2));
    record('real_language_ids_read', languages.status === 200 && !!fa && !!en,
        `http=${languages.status} languages=${langs.map((l) => `${l.code}:${l.id}`).join(',')}`);

    // 6. Baseline authoritative catalog (7 seeded canonical methods).
    const baseline = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const baselineCodes = catalogRows(baseline.json).map((m) => m.methodCode || m.MethodCode);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-baseline-168.json'), JSON.stringify(baseline.json, null, 2));
    record('baseline_seven_methods_present',
        baseline.status === 200 && baselineCodes.length === 7
        && ['post:standard', 'post:express', 'tipax:standard', 'tipax:express', 'snapp_courier', 'store_courier', 'in_person']
            .every((c) => baselineCodes.includes(c)),
        `http=${baseline.status} codes=${baselineCodes.join(',')}`);

    // 7. Fail-closed binding before the row exists: gate ready, nothing resolved, no invented pair.
    const preBinding = await call('GET',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent('tipax:sameday')}/binding`, actorUserId);
    const prePayload = data(preBinding.json) || {};
    record('binding_unresolved_before_create',
        preBinding.status === 200 && prePayload.resolved === false
        && !prePayload.serviceCode && !prePayload.optionCode,
        `http=${preBinding.status} resolved=${prePayload.resolved} serviceCode=${prePayload.serviceCode} optionCode=${prePayload.optionCode}`);

    // 8. REAL canonical create with a genuinely new code and both real language ids.
    const createBody = {
        methodCode: 'tipax:sameday',
        providerKind: 'tipax',
        translations: [
            { languageId: fa.id, name: 'تیپاکس همان‌روز' },
            { languageId: en.id, name: 'Tipax Same Day' },
        ],
    };
    const created = await call('POST', CREATE_PATH, actorUserId, createBody);
    fs.writeFileSync(path.join(OUT_DIR, 'create-tipax-sameday-168.json'), JSON.stringify(created.json, null, 2));
    const createdRow = data(created.json);
    record('canonical_create_2xx', created.status >= 200 && created.status < 300,
        `http=${created.status} errorCode=${errorCode(created.json)} body=${(created.text || '').slice(0, 200)}`);
    record('created_identity_is_method_code', !!createdRow && (createdRow.methodCode === 'tipax:sameday'),
        `methodCode=${createdRow && createdRow.methodCode}`);
    record('created_is_inactive', !!createdRow && (createdRow.isActive === false),
        `isActive=${createdRow && createdRow.isActive}`);

    // 9. Client cannot smuggle legacy binding / activation fields: the server derives the binding from
    //    the canonical code alone (`post:smuggled` -> post/smuggled) and forces isActive=false.
    const smuggled = await call('POST', CREATE_PATH, actorUserId, {
        methodCode: 'post:smuggled',
        providerKind: 'post',
        serviceCode: 'tipax',
        optionCode: 'sameday',
        isActive: true,
        translations: [{ languageId: fa.id, name: 'اسمگل' }],
    });
    const smuggledRow = data(smuggled.json) || {};
    record('smuggled_binding_fields_ignored', smuggled.status >= 200 && smuggled.status < 300
        && smuggledRow.isActive === false
        && (smuggledRow.legacyServiceCode === 'post')
        && (smuggledRow.legacyOptionCode === 'smuggled'),
        `http=${smuggled.status} isActive=${smuggledRow.isActive} legacy=${smuggledRow.legacyServiceCode}/${smuggledRow.legacyOptionCode}`);

    // 10. Authoritative read-back of the created method.
    const after = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const afterRows = catalogRows(after.json);
    const rowSa = afterRows.filter((m) => (m.methodCode || m.MethodCode) === 'tipax:sameday');
    const afterCodes = afterRows.map((m) => m.methodCode || m.MethodCode);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-after-create-168.json'), JSON.stringify(after.json, null, 2));
    record('readback_exactly_one_new_row', after.status === 200 && rowSa.length === 1,
        `http=${after.status} matches=${rowSa.length} total=${afterCodes.length}`);
    const saRow = rowSa[0] || {};
    const saName = saRow.name || saRow.Name || saRow.displayName || saRow.DisplayName;
    record('readback_inactive_canonical', saRow.isActive === false && saRow.methodCode === 'tipax:sameday',
        `methodCode=${saRow.methodCode} isActive=${saRow.isActive} name=${saName}`);
    record('readback_localized_fa_name', saName === 'تیپاکس همان‌روز', `name=${saName}`);

    const afterEn = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=en-US', actorUserId);
    const saEn = catalogRows(afterEn.json).find((m) => (m.methodCode || m.MethodCode) === 'tipax:sameday') || {};
    const saEnName = saEn.name || saEn.Name || saEn.displayName || saEn.DisplayName;
    record('readback_localized_en_name', saEnName === 'Tipax Same Day', `name=${saEnName}`);

    // 11. SERVER-DERIVED legacy binding for the new canonical code.
    const binding = await call('GET',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent('tipax:sameday')}/binding`, actorUserId);
    const bindingPayload = data(binding.json);
    const bService = bindingPayload && (bindingPayload.serviceCode === undefined ? bindingPayload.ServiceCode : bindingPayload.serviceCode);
    const bOption = bindingPayload && (bindingPayload.optionCode === undefined ? bindingPayload.OptionCode : bindingPayload.optionCode);
    fs.writeFileSync(path.join(OUT_DIR, 'binding-tipax-sameday-168.json'), JSON.stringify(binding.json, null, 2));
    record('server_derived_binding_post_option', binding.status === 200 && bService === 'tipax' && bOption === 'sameday',
        `http=${binding.status} serviceCode=${bService} optionCode=${bOption}`);

    // 12. Existing seven codes/bindings untouched.
    const intactChecks = [];
    for (const [methodCode, service, option] of [
        ['tipax:standard', 'tipax', 'standard'],
        ['tipax:express', 'tipax', 'express'],
        ['post:standard', 'post', 'standard'],
    ]) {
        const b = await call('GET',
            `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(methodCode)}/binding`, actorUserId);
        const p = data(b.json) || {};
        intactChecks.push(`${methodCode}:${b.status}:${p.serviceCode || p.ServiceCode}/${p.optionCode === undefined ? p.OptionCode : p.optionCode}`);
        record(`existing_binding_intact_${methodCode.replace(':', '_')}`,
            b.status === 200 && (p.serviceCode || p.ServiceCode) === service
            && ((p.optionCode === undefined ? p.OptionCode : p.optionCode) === option),
            `http=${b.status} service=${p.serviceCode || p.ServiceCode} option=${p.optionCode === undefined ? p.OptionCode : p.optionCode}`);
    }
    record('existing_codes_still_present', afterCodes.length >= 8
        && ['tipax:standard', 'tipax:express', 'post:standard', 'post:express', 'snapp_courier', 'store_courier', 'in_person']
            .every((c) => afterCodes.includes(c)),
        `codes=${afterCodes.join(',')}`);

    // 13. Duplicate canonical code is fail-closed with a stable code and no extra row.
    const duplicate = await call('POST', CREATE_PATH, actorUserId, createBody);
    record('duplicate_code_rejected', duplicate.status >= 400,
        `http=${duplicate.status} errorCode=${errorCode(duplicate.json)}`);
    record('duplicate_stable_error_code', errorCode(duplicate.json) === 'shipping_method.code.duplicate',
        `errorCode=${errorCode(duplicate.json)}`);

    // 14. Unknown language id must fail without persistence.
    const unknownLanguage = await call('POST', CREATE_PATH, actorUserId, {
        methodCode: 'tipax:unknownlang',
        providerKind: 'tipax',
        translations: [{ languageId: '11111111-2222-4333-8444-555555555555', name: 'x' }],
    });
    record('unknown_language_rejected', unknownLanguage.status >= 400,
        `http=${unknownLanguage.status} errorCode=${errorCode(unknownLanguage.json)}`);

    // 15. Malformed canonical code must fail without persistence.
    const malformed = await call('POST', CREATE_PATH, actorUserId, {
        methodCode: 'Tipax Sameday!!',
        translations: [{ languageId: fa.id, name: 'بد' }],
    });
    record('malformed_code_rejected', malformed.status >= 400,
        `http=${malformed.status} errorCode=${errorCode(malformed.json)}`);

    // 16. Parallel duplicate creates: at most one wins, never two rows.
    const parallelBodies = [1, 2, 3].map(() => ({
        methodCode: 'mahex:parallel',
        providerKind: 'mahex',
        translations: [{ languageId: fa.id, name: 'موازی' }],
    }));
    const parallel = await Promise.all(parallelBodies.map((b) => call('POST', CREATE_PATH, actorUserId, b)));
    const winners = parallel.filter((r) => r.status >= 200 && r.status < 300).length;
    record('parallel_duplicate_creates_single_winner', winners === 1,
        `winnerCount=${winners} statuses=${parallel.map((r) => r.status).join(',')}`);

    const finalList = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const finalRows = catalogRows(finalList.json);
    const finalCodes = finalRows.map((m) => m.methodCode || m.MethodCode);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-final-168.json'), JSON.stringify(finalList.json, null, 2));
    record('no_rows_from_rejected_requests',
        finalCodes.filter((c) => c === 'tipax:unknownlang').length === 0
        && finalCodes.filter((c) => c === 'Tipax Sameday!!').length === 0
        && finalCodes.filter((c) => c === 'tipax:sameday').length === 1
        && finalCodes.filter((c) => c === 'mahex:parallel').length === 1,
        `codes=${finalCodes.join(',')}`);

    // 17. Unauthenticated / ungranted writes never reach the write seam.
    const anonCreate = await call('POST', CREATE_PATH, null, {
        methodCode: 'tipax:anonymous', translations: [{ languageId: fa.id, name: 'ناشناس' }],
    });
    record('anonymous_create_rejected', anonCreate.status === 401 || anonCreate.status === 403,
        `http=${anonCreate.status} errorCode=${errorCode(anonCreate.json)}`);
    const unknownCreate = await call('POST', CREATE_PATH, UNKNOWN_ACTOR, {
        methodCode: 'tipax:ungranted', translations: [{ languageId: fa.id, name: 'بی‌مجوز' }],
    });
    record('ungranted_create_rejected', unknownCreate.status === 401 || unknownCreate.status === 403,
        `http=${unknownCreate.status} errorCode=${errorCode(unknownCreate.json)}`);

    const finalCheck = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const finalCheckCodes = catalogRows(finalCheck.json).map((m) => m.methodCode || m.MethodCode);
    record('unauthorized_requests_persisted_nothing',
        !finalCheckCodes.includes('tipax:anonymous') && !finalCheckCodes.includes('tipax:ungranted'),
        `codes=${finalCheckCodes.join(',')}`);

    const failed = checks.filter((c) => !c.ok);
    const matrix = {
        generatedAtUtc: new Date().toISOString(),
        host: HOST,
        total: checks.length,
        passed: checks.length - failed.length,
        failed: failed.length,
        checks,
    };
    fs.writeFileSync(path.join(EVIDENCE, 'assertion-matrix-http-168.json'), JSON.stringify(matrix, null, 2) + '\n');
    fs.writeFileSync(path.join(EVIDENCE, 'http-trace-168.txt'), httpLog.join('\n') + '\n');
    console.log(`SUMMARY total=${matrix.total} passed=${matrix.passed} failed=${matrix.failed}`);
    if (failed.length > 0) process.exitCode = 1;
}
