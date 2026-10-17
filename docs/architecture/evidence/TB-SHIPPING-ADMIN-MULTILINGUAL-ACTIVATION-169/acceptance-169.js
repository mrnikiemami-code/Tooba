/**
 * TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169 — authorized Admin HTTP + SQL acceptance.
 *
 * Real acceptance over the private isolated Host only (127.0.0.1:5423) and the disposable scratch
 * PostgreSQL (127.0.0.1:55488). No shared Host/DB/port, no fabricated access header: the dev actor id
 * comes from the Host's own dev-context seam and its capabilities from the product's own access-control
 * bootstrap. Language ids come from the live Localization registry.
 *
 * Proves: per-language name save -> authoritative GET -> reopen, draft isolation across languages,
 * sibling isolation, fail-closed negatives, and truthful activation behaviour for a newly created
 * (unbound) method and for an existing fully bound method.
 *
 * Usage: node acceptance-169.js
 */
const fs = require('fs');
const path = require('path');

const HOST = process.env.TOOBA_169_HOST || 'http://127.0.0.1:5423';
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), '.tmp-169-run');
const METHOD = process.env.TOOBA_169_METHOD || 'tipax:scratch169';
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
    httpLog.push(`${method} ${url} -> ${response.status} :: ${text.slice(0, 700).replace(/\s+/g, ' ')}`);
    return { status: response.status, json, text };
}

const errorCode = (payload) => {
    if (!payload || typeof payload !== 'object') return null;
    return payload.errorCode || payload.code || (payload.error && payload.error.code) || null;
};
const data = (payload) => (payload && typeof payload === 'object' && payload.data !== undefined ? payload.data : payload);

function catalogRows(payload) {
    const body = data(payload) || {};
    const rows = body.methods || body.Methods || body.items || body.Items || [];
    return Array.isArray(rows) ? rows : [];
}

function methodRow(payload, code) {
    return catalogRows(payload).find((m) => (m.methodCode || m.MethodCode) === code) || null;
}

function translationsOf(row) {
    const raw = (row && (row.translations || row.Translations)) || [];
    return Array.isArray(raw)
        ? raw.map((t) => ({ languageId: t.languageId || t.LanguageId, name: t.name || t.Name }))
        : [];
}

async function main() {
    // 1. Dev actor seam + fail-closed anonymous/unknown reads.
    const devContext = await call('GET', '/v1/admin/dev-context');
    const actorUserId = data(devContext.json) && (data(devContext.json).actorUserId || data(devContext.json).ActorUserId);
    record('dev_context_available', devContext.status === 200 && !!actorUserId,
        `http=${devContext.status} actorResolved=${!!actorUserId}`);

    const anonList = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level');
    record('one_level_list_anonymous_fail_closed', anonList.status === 401 || anonList.status === 403,
        `http=${anonList.status} errorCode=${errorCode(anonList.json)}`);

    const unknownList = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level', UNKNOWN_ACTOR);
    record('one_level_list_unknown_actor_fail_closed', unknownList.status === 401 || unknownList.status === 403,
        `http=${unknownList.status} errorCode=${errorCode(unknownList.json)}`);

    const bootstrap = await call('POST', '/v1/admin/access-control/bootstrap', actorUserId, {});
    record('access_control_bootstrap_ok', bootstrap.status === 200 || bootstrap.status === 204,
        `http=${bootstrap.status}`);

    const capabilities = await call('GET', '/v1/admin/access-control/me/capabilities', actorUserId);
    const permissionIds = ((data(capabilities.json) && data(capabilities.json).permissions) || [])
        .map((p) => (typeof p === 'string' ? p : (p && (p.permissionId || p.PermissionId)) || null))
        .filter(Boolean);
    record('fulfillment_manage_granted', permissionIds.includes('fulfillment.manage'),
        `http=${capabilities.status} grants=${permissionIds.length}`);

    // 2. Live registry languages (never invented).
    const languages = await call('GET', '/v1/admin/languages', actorUserId);
    const languageBody = data(languages.json) || {};
    const languageRows = languageBody.items || languageBody.Items || languageBody || [];
    const langs = (Array.isArray(languageRows) ? languageRows : [])
        .map((l) => ({
            id: l.languageId || l.LanguageId || l.id,
            code: l.code || l.Code,
            active: l.isActive === undefined ? l.IsActive : l.isActive,
        }))
        .filter((l) => l.id && l.code);
    const fa = langs.find((l) => l.code === 'fa-IR');
    const en = langs.find((l) => l.code === 'en-US');
    fs.writeFileSync(path.join(OUT_DIR, 'languages-169.json'), JSON.stringify(languages.json, null, 2));
    fs.writeFileSync(path.join(EVIDENCE, 'registry-languages-169.json'), JSON.stringify(
        { status: languages.status, languages: langs }, null, 2) + '\n');
    record('real_language_ids_read', languages.status === 200 && !!fa && !!en,
        `http=${languages.status} languages=${langs.map((l) => `${l.code}:${l.id}`).join(',')}`);

    // 3. Baseline authoritative catalog.
    const baseline = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const baselineRows = catalogRows(baseline.json);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-baseline-169.json'), JSON.stringify(baseline.json, null, 2));
    const baselineTipaxStandard = methodRow(baseline.json, 'tipax:standard');
    const baselineTipaxExpress = methodRow(baseline.json, 'tipax:express');
    record('baseline_catalog_present', baseline.status === 200 && baselineRows.length >= 7,
        `http=${baseline.status} rows=${baselineRows.length}`);
    record('baseline_siblings_active',
        !!baselineTipaxStandard && baselineTipaxStandard.isActive === true
        && !!baselineTipaxExpress && baselineTipaxExpress.isActive === true,
        `tipax:standard=${baselineTipaxStandard && baselineTipaxStandard.isActive} tipax:express=${baselineTipaxExpress && baselineTipaxExpress.isActive}`);

    // 4. Create the independent scratch-only canonical method (unique option => no legacy binding).
    const create = await call('POST', '/v1/admin/shipping-services/shipping-methods/one-level', actorUserId, {
        methodCode: METHOD,
        providerKind: 'tipax',
        translations: [{ languageId: fa.id, name: 'تیپاکس آزمایشی ۱۶۹' }],
    });
    record('canonical_create_2xx', create.status >= 200 && create.status < 300,
        `http=${create.status} errorCode=${errorCode(create.json)}`);

    const afterCreate = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const createdRow = methodRow(afterCreate.json, METHOD);
    record('created_row_inactive',
        !!createdRow && createdRow.isActive === false,
        `found=${!!createdRow} isActive=${createdRow && createdRow.isActive}`);

    const binding = await call('GET', `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/binding`, actorUserId);
    const bindingBody = data(binding.json) || {};
    fs.writeFileSync(path.join(OUT_DIR, 'binding-169.json'), JSON.stringify(binding.json, null, 2));
    record('binding_derived_from_code',
        binding.status === 200 && bindingBody.serviceCode === 'tipax' && bindingBody.optionCode === 'scratch169',
        `http=${binding.status} serviceCode=${bindingBody.serviceCode} optionCode=${bindingBody.optionCode} resolved=${bindingBody.resolved}`);

    // 5. Per-language save -> authoritative read-back (the TB-148 gap).
    const saveFa = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/${fa.id}`,
        actorUserId, { name: 'تیپاکس چندزبانه ۱۶۹' });
    record('save_fa_translation_2xx', saveFa.status >= 200 && saveFa.status < 300,
        `http=${saveFa.status} errorCode=${errorCode(saveFa.json)}`);

    const saveEn = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/${en.id}`,
        actorUserId, { name: 'Tipax Multilingual 169' });
    record('save_en_translation_2xx', saveEn.status >= 200 && saveEn.status < 300,
        `http=${saveEn.status} errorCode=${errorCode(saveEn.json)}`);

    const readFa = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const readEn = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=en-US', actorUserId);
    const faRow = methodRow(readFa.json, METHOD);
    const enRow = methodRow(readEn.json, METHOD);
    const faTr = translationsOf(faRow);
    const enTr = translationsOf(enRow);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-after-names-169.json'),
        JSON.stringify({ fa: readFa.json, en: readEn.json }, null, 2));

    record('readback_fa_name_persisted',
        faTr.some((t) => t.languageId === fa.id && t.name === 'تیپاکس چندزبانه ۱۶۹'),
        `fa=${JSON.stringify(faTr)}`);
    record('readback_en_name_persisted',
        enTr.some((t) => t.languageId === en.id && t.name === 'Tipax Multilingual 169'),
        `en=${JSON.stringify(enTr)}`);
    record('sibling_translations_unchanged',
        JSON.stringify(translationsOf(methodRow(readFa.json, 'tipax:standard')))
        === JSON.stringify(translationsOf(baselineTipaxStandard))
        && JSON.stringify(translationsOf(methodRow(readFa.json, 'tipax:express')))
        === JSON.stringify(translationsOf(baselineTipaxExpress)),
        `readback tipax:standard=${JSON.stringify(translationsOf(methodRow(readFa.json, 'tipax:standard')))}`);

    // 6. Fail-closed negative writes: nothing may change a persisted name.
    const invalidLanguage = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/00000000-0000-0000-0000-000000000000`,
        actorUserId, { name: 'زبان جعلی' });
    record('unknown_language_rejected', invalidLanguage.status >= 400 && invalidLanguage.status < 500,
        `http=${invalidLanguage.status} errorCode=${errorCode(invalidLanguage.json)}`);

    const emptyName = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/${fa.id}`,
        actorUserId, { name: '   ' });
    record('empty_name_rejected', emptyName.status >= 400 && emptyName.status < 500,
        `http=${emptyName.status} errorCode=${errorCode(emptyName.json)}`);

    const anonWrite = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/${fa.id}`,
        null, { name: 'بدون هویت' });
    record('anonymous_write_rejected', anonWrite.status === 401 || anonWrite.status === 403,
        `http=${anonWrite.status} errorCode=${errorCode(anonWrite.json)}`);

    const ungrantedWrite = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/translations/${fa.id}`,
        UNKNOWN_ACTOR, { name: 'بدون مجوز' });
    record('ungranted_write_rejected', ungrantedWrite.status === 401 || ungrantedWrite.status === 403,
        `http=${ungrantedWrite.status} errorCode=${errorCode(ungrantedWrite.json)}`);

    const afterNegatives = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const faAfter = translationsOf(methodRow(afterNegatives.json, METHOD));
    record('negatives_changed_nothing',
        JSON.stringify(faAfter) === JSON.stringify(faTr),
        `fa=${JSON.stringify(faAfter)}`);

    // 7. Activation truthfulness: the new method has NO legacy option row by construction.
    const activateNew = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/activation`,
        actorUserId, { isActive: true });
    record('new_method_activation_refused',
        activateNew.status >= 400 && activateNew.status < 500,
        `http=${activateNew.status} errorCode=${errorCode(activateNew.json)}`);

    const afterActivateAttempt = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const stillInactive = methodRow(afterActivateAttempt.json, METHOD);
    record('new_method_still_inactive_after_refusal',
        !!stillInactive && stillInactive.isActive === false,
        `isActive=${stillInactive && stillInactive.isActive}`);

    // 8. Independent activation/deactivation of an existing fully bound method (scratch data only).
    const deactivate = await call('PUT',
        '/v1/admin/shipping-services/shipping-methods/one-level/tipax:express/activation',
        actorUserId, { isActive: false });
    const deactivateBody = data(deactivate.json) || {};
    record('sibling_deactivation_applied',
        deactivate.status >= 200 && deactivate.status < 300
        && deactivateBody.isActive === false && deactivateBody.changed === true,
        `http=${deactivate.status} isActive=${deactivateBody.isActive} changed=${deactivateBody.changed}`);

    const afterDeactivate = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    record('sibling_deactivation_persisted',
        (methodRow(afterDeactivate.json, 'tipax:express') || {}).isActive === false
        && (methodRow(afterDeactivate.json, 'tipax:standard') || {}).isActive === true
        && (methodRow(afterDeactivate.json, METHOD) || {}).isActive === false,
        `tipax:express=${(methodRow(afterDeactivate.json, 'tipax:express') || {}).isActive} tipax:standard=${(methodRow(afterDeactivate.json, 'tipax:standard') || {}).isActive}`);

    const reactivate = await call('PUT',
        '/v1/admin/shipping-services/shipping-methods/one-level/tipax:express/activation',
        actorUserId, { isActive: true });
    const reactivateBody = data(reactivate.json) || {};
    record('sibling_reactivation_restored',
        reactivate.status >= 200 && reactivate.status < 300
        && reactivateBody.isActive === true,
        `http=${reactivate.status} isActive=${reactivateBody.isActive}`);

    const finalRead = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const finalRow = methodRow(finalRead.json, METHOD);
    fs.writeFileSync(path.join(OUT_DIR, 'catalog-final-169.json'), JSON.stringify(finalRead.json, null, 2));
    record('final_new_method_inactive_with_names',
        !!finalRow && finalRow.isActive === false
        && translationsOf(finalRow).some((t) => t.languageId === fa.id)
        && translationsOf(finalRow).some((t) => t.languageId === en.id),
        `isActive=${finalRow && finalRow.isActive} translations=${JSON.stringify(translationsOf(finalRow))}`);

    const passed = checks.filter((c) => c.ok).length;
    const matrix = { generatedAtUtc: new Date().toISOString(), host: HOST, methodCode: METHOD, total: checks.length, passed, failed: checks.length - passed, checks };
    fs.writeFileSync(path.join(EVIDENCE, 'assertion-matrix-http-169.json'), JSON.stringify(matrix, null, 2) + '\n');
    fs.writeFileSync(path.join(EVIDENCE, 'http-trace-169.txt'), httpLog.join('\n') + '\n');
    console.log(`TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
}

main().catch((err) => {
    console.error('HARNESS_ERROR', err && err.stack);
    process.exitCode = 2;
});
