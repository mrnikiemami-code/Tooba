-- TB-SHIPPING-GROUP-ID-CONTINUITY-153 — minimum scratch-only tax fixture.
-- The disposable scratch bootstrap mints fresh offer/category ids on every seed, so this insert targets
-- the two task-owned offer categories of THIS run (seller A = PHONE line, seller B = ARM line). It only
-- unblocks the read-only payment-draft seam (HTTP 200); production code and shared targets are untouched.
delete from tax.rules where rule_id in ('01a1460a-5000-7000-9000-000000000153', '01a1460a-5000-7000-9000-000000000154');
insert into tax.rules (rule_id, jurisdiction, market, category_id, kind, rate, effective_from, effective_to, status, specificity, override_policy, created_at, updated_at)
values
  ('01a1460a-5000-7000-9000-000000000155', 'IR-NAT', 'IR', '01a1462e-2929-7000-9228-77c089efccbe', 'Percentage', 0.09000000, '2026-01-01 00:00:00+00', null, 'Active', 10, 'Disabled', now(), now()),
  ('01a1460a-5000-7000-9000-000000000156', 'IR-NAT', 'IR', '01a1462e-2b33-7000-987b-40fbd240ad40', 'Percentage', 0.09000000, '2026-01-01 00:00:00+00', null, 'Active', 10, 'Disabled', now(), now())
on conflict (rule_id) do nothing;
