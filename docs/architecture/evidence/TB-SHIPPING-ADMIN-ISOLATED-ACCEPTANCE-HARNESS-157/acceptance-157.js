/**
 * TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — authenticated private-Admin acceptance.
 *
 * Real acceptance over the private isolated Host only (127.0.0.1:5417). No shared Host/DB/port and no
 * fabricated access header: the dev actor id comes from the Host's own dev-context seam, and its
 * capabilities come from the product's own access-control bootstrap (persisted role + assignment +
 * real authorization tuples written through IAuthorizationTupleWriter). Nothing is disabled or bypassed.
 *
 * Usage: node acceptance-157.js
 */
const fs = require('fs');
const path = require('path');

const HOST = process.env.TOOBA_157_HOST || 'http://127.0.0.1:5417';
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), '.tmp-157-run');

const RANDOM_ACTOR = '11111111-2222-4333-8444-555555555555';
const DEAD_OPTION_ACTOR = '99999999-8888-4777-8666-555555555555';

const checks = [];
const trace = [];

function record(id, ok, detail) {
    checks.push({ id, ok, detail });
    trace.push(`- ${ok ? 'PASS' : 'FAIL'} :: ${id} :: ${detail}`);
    console.log(`${ok ? 'PASS' : 'FAIL'} ${id} ${detail}`);
}

async function call(method, url, actorId, body, extraHeaders) {
    const headers = { Accept: 'application/json', 'Accept-Language': 'fa-IR' };
    if (actorId) headers['X-Tooba-Dev-Actor-User-Id'] = actorId;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    Object.assign(headers, extraHeaders || {});
    const response = await fetch(`${HOST}${url}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await response.text();
    let json = null;
    try { json = JSON.parse(text); } catch { }
    return { status: response.status, json, text };
}

/** Stable error code from the platform envelope, without leaking internals. */
function errorCode(payload) {
    if (!payload || typeof payload !== 'object') return null;
    return payload.errorCode || payload.code || (payload.error && payload.error.code) || null;
}

async function main() {
    // 1. Sanctioned Development-only dev actor seam.
    const devContext = await call('GET', '/v1/admin/dev-context');
    const actorUserId = devContext.json && (devContext.json.data
        ? (devContext.json.data.actorUserId || devContext.json.data.ActorUserId)
        : (devContext.json.actorUserId || devContext.json.ActorUserId));
    record('dev_context_available', devContext.status === 200 && !!actorUserId,
        `http=${devContext.status} actorResolved=${!!actorUserId}`);

    // 2. Fail-closed read before any grant exists (no actor header).
    const anon = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard');
    record('financial_status_anonymous_fail_closed', anon.status === 401 || anon.status === 403,
        `http=${anon.status} errorCode=${errorCode(anon.json)}`);

    // 3. Fail-closed for a well-formed but unknown actor header (no membership, no grants): the
    //    guard is real, not a global allowlist. NOTE: the pre-grant probe for the *sanctioned* dev
    //    actor is order-dependent (bootstrap is idempotent); it was observed as 401/403 in the first,
    //    pre-bootstrap run and is recorded in RESULT.md rather than asserted here on a granted actor.
    const UNKNOWN_ACTOR = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
    const before = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', UNKNOWN_ACTOR);
    record('financial_status_unknown_actor_fail_closed', before.status === 401 || before.status === 403,
        `http=${before.status} errorCode=${errorCode(before.json)}`);

    // 4. Product's own access-control bootstrap: creates the system platform-admin role with the real
    //    permission catalog and assigns it to this actor (persisted role + assignment + tuples).
    const bootstrap = await call('POST', '/v1/admin/access-control/bootstrap', actorUserId, {});
    record('access_control_bootstrap_ok', bootstrap.status === 200 || bootstrap.status === 204,
        `http=${bootstrap.status} body=${(bootstrap.text || '').slice(0, 160).replace(/\s+/g, ' ')}`);

    // 5. Prove the capability came from real grants, not a global allowlist.
    const capabilities = await call('GET', '/v1/admin/access-control/me/capabilities', actorUserId);
    const permissions = (capabilities.json && (capabilities.json.data && capabilities.json.data.permissions
        ? capabilities.json.data.permissions
        : capabilities.json.permissions)) || [];
    const permissionIds = (Array.isArray(permissions) ? permissions : [])
        .map((p) => (typeof p === 'string' ? p : (p && (p.permissionId || p.PermissionId)) || null))
        .filter(Boolean);
    record('fulfillment_view_granted', permissionIds.includes('fulfillment.view'),
        `http=${capabilities.status} grants=${permissionIds.length}`);
    record('fulfillment_manage_granted', permissionIds.includes('fulfillment.manage'),
        `http=${capabilities.status} grants=${permissionIds.length}`);

    // 6. Different-but-valid actor still fails closed (no shared/global allow).
    const other = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', DEAD_OPTION_ACTOR);
    record('financial_status_other_actor_fail_closed', other.status === 401 || other.status === 403,
        `http=${other.status} errorCode=${errorCode(other.json)}`);

    // 7. Authoritative server binding for the canonical one-level method (never split client-side).
    const binding = await call('GET',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent('tipax:standard')}/binding`,
        actorUserId);
    const bindingPayload = binding.json && (binding.json.data || binding.json);
    const bindingService = bindingPayload && (bindingPayload.serviceCode || bindingPayload.ServiceCode);
    const bindingOption = bindingPayload && (bindingPayload.optionCode === undefined
        ? bindingPayload.OptionCode : bindingPayload.optionCode);
    record('authoritative_binding_resolved',
        binding.status === 200 && bindingService === 'tipax' && (bindingOption === 'standard' || bindingOption === null),
        `http=${binding.status} serviceCode=${bindingService} optionCode=${bindingOption}`);

    // 8. Live initial financial status (the real first state of the canonical method in scratch).
    const initial = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    const initialState = initial.json && (initial.json.data || initial.json);
    fs.writeFileSync(path.join(OUT_DIR, 'financial-status-initial-157.json'), JSON.stringify(initial.json, null, 2));
    record('financial_status_authorized_read_ok', initial.status === 200,
        `http=${initial.status} state=${initialState && (initialState.state || initialState.State)} reason=${initialState && (initialState.reasonCode || initialState.ReasonCode)}`);

    // 9. Legacy provider-level (optionless) rule is a service-level row, never this method's tariff.
    const countryId = '11111111-1111-1111-1111-111111111111';
    const scope = { countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null };
    const legacyWrite = await call('POST', '/v1/admin/shipping-services/tipax/rates', actorUserId, {
        serviceCode: 'tipax',
        scope,
        chargeAmount: 99000,
        currencyCode: 'IRR',
        preparationDays: 1,
        leadDays: 3,
        freeAboveThreshold: null,
        optionCode: null,
        condition: null,
    });
    record('legacy_service_level_write_accepted', legacyWrite.status === 200,
        `http=${legacyWrite.status} errorCode=${errorCode(legacyWrite.json)}`);

    const afterLegacy = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    const afterLegacyState = afterLegacy.json && (afterLegacy.json.data || afterLegacy.json);
    fs.writeFileSync(path.join(OUT_DIR, 'financial-status-after-legacy-157.json'), JSON.stringify(afterLegacy.json, null, 2));
    record('legacy_row_is_not_projected_as_method_price',
        afterLegacy.status === 200
        && afterLegacyState.state === 'LegacySharedUnresolved'
        && afterLegacyState.mode === 'None'
        && (afterLegacyState.currencyCode === null || afterLegacyState.currencyCode === undefined),
        `http=${afterLegacy.status} state=${afterLegacyState.state} mode=${afterLegacyState.mode} currency=${afterLegacyState.currencyCode} serviceLevelRules=${afterLegacyState.activeServiceLevelRuleCount}`);

    // 10. Real owned fixed IRR rule for canonical tipax:standard through the existing write seam only.
    const ownedWrite = await call('POST', '/v1/admin/shipping-services/tipax/rates', actorUserId, {
        serviceCode: 'tipax',
        scope,
        chargeAmount: 185000,
        currencyCode: 'IRR',
        preparationDays: 1,
        leadDays: 3,
        freeAboveThreshold: null,
        optionCode: 'standard',
        condition: null,
    });
    const ownedPayload = ownedWrite.json && (ownedWrite.json.data || ownedWrite.json);
    const ownedRateId = ownedPayload && (ownedPayload.ratePolicyId || ownedPayload.RatePolicyId);
    fs.writeFileSync(path.join(OUT_DIR, 'owned-rate-write-157.json'), JSON.stringify(ownedWrite.json, null, 2));
    record('owned_fixed_irr_rule_saved', ownedWrite.status === 200 && !!ownedRateId,
        `http=${ownedWrite.status} ratePolicyId=${ownedRateId} errorCode=${errorCode(ownedWrite.json)}`);

    // 11. Read back: persisted amount + currency + verified state from the server.
    const afterOwned = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    const ownedState = afterOwned.json && (afterOwned.json.data || afterOwned.json);
    fs.writeFileSync(path.join(OUT_DIR, 'financial-status-after-owned-157.json'), JSON.stringify(afterOwned.json, null, 2));
    const ownedRule = ownedState && (ownedState.rules || ownedState.Rules || [])[0];
    record('owned_amount_read_back_from_server',
        afterOwned.status === 200
        && ownedState.state === 'FinanciallyConfigured'
        && ownedState.currencyCode === 'IRR'
        && Number(ownedRule && (ownedRule.chargeAmount !== undefined ? ownedRule.chargeAmount : ownedRule.ChargeAmount)) === 185000,
        `http=${afterOwned.status} state=${ownedState.state} currency=${ownedState.currencyCode} amount=${ownedRule && (ownedRule.chargeAmount !== undefined ? ownedRule.chargeAmount : ownedRule.ChargeAmount)}`);

    record('legacy_row_preserved_after_owned_write',
        Number(ownedState.activeServiceLevelRuleCount) === 1 && Number(ownedState.ruleCount) === 1,
        `ruleCount=${ownedState.ruleCount} serviceLevelRules=${ownedState.activeServiceLevelRuleCount}`);

    // 12. Sibling isolation: tipax:express must be untouched by the standard write.
    const sibling = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=express', actorUserId);
    const siblingState = sibling.json && (sibling.json.data || sibling.json);
    fs.writeFileSync(path.join(OUT_DIR, 'financial-status-sibling-157.json'), JSON.stringify(sibling.json, null, 2));
    record('sibling_option_unchanged',
        sibling.status === 200
        && siblingState.state === 'LegacySharedUnresolved'
        && Number(siblingState.ruleCount) === 0
        && Number(siblingState.activeSiblingOptionRuleCount) === 1,
        `http=${sibling.status} state=${siblingState.state} ruleCount=${siblingState.ruleCount} siblingRules=${siblingState.activeSiblingOptionRuleCount}`);

    // 13. Rejected conflicting duplicate write must not mutate the owned rule.
    const duplicate = await call('POST', '/v1/admin/shipping-services/tipax/rates', actorUserId, {
        serviceCode: 'tipax',
        scope,
        chargeAmount: 260000,
        currencyCode: 'IRR',
        preparationDays: 1,
        leadDays: 3,
        freeAboveThreshold: null,
        optionCode: 'standard',
        condition: null,
    });
    record('conflicting_duplicate_denied', duplicate.status >= 400,
        `http=${duplicate.status} errorCode=${errorCode(duplicate.json)}`);

    const afterDuplicate = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    const afterDuplicateState = afterDuplicate.json && (afterDuplicate.json.data || afterDuplicate.json);
    const afterDuplicateRule = afterDuplicateState && (afterDuplicateState.rules || afterDuplicateState.Rules || [])[0];
    record('rejected_write_did_not_mutate',
        afterDuplicate.status === 200
        && Number(afterDuplicateState.ruleCount) === 1
        && Number(afterDuplicateRule && (afterDuplicateRule.chargeAmount !== undefined ? afterDuplicateRule.chargeAmount : afterDuplicateRule.ChargeAmount)) === 185000,
        `http=${afterDuplicate.status} ruleCount=${afterDuplicateState.ruleCount} amount=${afterDuplicateRule && (afterDuplicateRule.chargeAmount !== undefined ? afterDuplicateRule.chargeAmount : afterDuplicateRule.ChargeAmount)}`);

    // 14. Unauthorized write must be rejected (fail-closed), not silently accepted.
    const unauthorizedWrite = await call('POST', '/v1/admin/shipping-services/tipax/rates', DEAD_OPTION_ACTOR, {
        serviceCode: 'tipax', scope, chargeAmount: 777000, currencyCode: 'IRR',
        preparationDays: 1, leadDays: 3, freeAboveThreshold: null, optionCode: 'standard', condition: null,
    });
    record('unauthorized_write_rejected', unauthorizedWrite.status === 401 || unauthorizedWrite.status === 403,
        `http=${unauthorizedWrite.status} errorCode=${errorCode(unauthorizedWrite.json)}`);

    const afterUnauthorized = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    const afterUnauthorizedState = afterUnauthorized.json && (afterUnauthorized.json.data || afterUnauthorized.json);
    const afterUnauthorizedRule = afterUnauthorizedState && (afterUnauthorizedState.rules || afterUnauthorizedState.Rules || [])[0];
    record('unauthorized_write_did_not_mutate',
        Number(afterUnauthorizedState.ruleCount) === 1
        && Number(afterUnauthorizedRule && (afterUnauthorizedRule.chargeAmount !== undefined ? afterUnauthorizedRule.chargeAmount : afterUnauthorizedRule.ChargeAmount)) === 185000,
        `ruleCount=${afterUnauthorizedState.ruleCount} amount=${afterUnauthorizedRule && (afterUnauthorizedRule.chargeAmount !== undefined ? afterUnauthorizedRule.chargeAmount : afterUnauthorizedRule.ChargeAmount)}`);

    // 15. Unused constant kept explicit so the harness never silently drops the dead-actor probe.
    void RANDOM_ACTOR;

    const failed = checks.filter((c) => !c.ok);
    const matrix = {
        generatedAtUtc: new Date().toISOString(),
        host: HOST,
        total: checks.length,
        passed: checks.length - failed.length,
        failed: failed.length,
        checks,
    };
    fs.writeFileSync(path.join(EVIDENCE, 'assertion-matrix-157.json'), JSON.stringify(matrix, null, 2) + '\n');
    fs.writeFileSync(path.join(EVIDENCE, 'acceptance-157-trace.txt'), trace.join('\n') + '\n');
    console.log(`SUMMARY total=${matrix.total} passed=${matrix.passed} failed=${matrix.failed}`);
    if (failed.length > 0) process.exitCode = 1;
}

main().catch((err) => {
    console.error('HARNESS_ERROR', err && err.message);
    process.exitCode = 2;
});
