-- TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 rollback of the accidental writes that landed in tooba_alpha
-- instead of the intended scratch clone (env var name mismatch: Tooba__PostgreSQL__ConnectionReferences__tenant_alpha
-- does not override the configuration key "tenant-alpha"; the isolated instance therefore used
-- appsettings.Development.json -> Database=tooba_alpha).
--
-- Scope: ONLY the 3 shipping.rate_policies rows, the 2 shipping.coverage_grants rows and the 5
-- identity.auth_sessions rows this task created (all with created_at inside 2026-10-14 20:28Z..20:32Z,
-- and shipping was verified 0/0 before the task). Nothing else is touched.

begin;

delete from shipping.rate_policies
where rate_policy_id in (
    '01a13c1b-b335-7000-9cec-842c94cd5683',
    '01a13c1b-b3f5-7000-81e1-9b35ae612abb',
    '01a13c1c-892c-7000-b311-78f8a1aa1d10'
);

delete from shipping.coverage_grants
where coverage_grant_id in (
    '01a13c1b-b35f-7000-b36c-042adc85d4f4',
    '01a13c1c-8945-7000-8de8-adb61d2ff926'
);

delete from identity.auth_sessions
where user_id = '01a036c2-970e-7000-8eb7-94bf5cc2d8db'
  and created_at >= '2026-10-14 20:28:00+00'
  and created_at <  '2026-10-14 20:33:00+00';

commit;

select 'rate_policies_after=' || (select count(*) from shipping.rate_policies)
    || ' coverage_grants_after=' || (select count(*) from shipping.coverage_grants)
    || ' auth_sessions_after=' || (select count(*) from identity.auth_sessions);
