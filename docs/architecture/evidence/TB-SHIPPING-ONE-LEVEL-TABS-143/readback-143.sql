-- TB-SHIPPING-ONE-LEVEL-TABS-143 — readback of the per-group pending shipping-fee ledger
-- produced by a REAL paid storefront journey (no event injection, no direct handler/Directory call).
-- Read-only: no UPDATE/INSERT/DELETE.
\echo '--- A. FULFILLMENT HANDOFF (preserved from TB-140) ---'
select count(*) as paid_seller_orders
from "order".seller_orders
where checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c';

select count(*) as fulfillment_unit_rows,
       count(distinct fulfillment_id) as distinct_fulfillment_ids
from fulfillment.fulfillments
where checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c';

select f.fulfillment_id, f.seller_order_id, g.shipping_group_id, g.method_code
from fulfillment.fulfillments f
join fulfillment.shipping_groups g on g.fulfillment_id = f.fulfillment_id
where f.checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c'
order by g.method_code;

-- The two persisted group ids the Fulfillment handoff received must be EXACTLY the two group ids
-- the Settlement pending ledger recorded (same stable identities, not a re-derivation).
select
    (select count(*) from fulfillment.shipping_groups g
     join fulfillment.fulfillments f on f.fulfillment_id = g.fulfillment_id
     where f.checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c') as fulfillment_group_ids,
    (select count(distinct shipping_group_id) from settlement.shipping_group_fees) as ledger_group_ids,
    (select count(*) from fulfillment.shipping_groups g
     join fulfillment.fulfillments f on f.fulfillment_id = g.fulfillment_id
     where f.checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c'
       and g.shipping_group_id in (select shipping_group_id from settlement.shipping_group_fees)) as identical_group_ids;

\echo '--- B. PAYMENT ---'
select payment_id, checkout_id, amount, currency, status
from payment.payments
where checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c';

select seller_order_id, allocated_amount, currency, target_kind
from payment.allocations
where payment_id = '01a143f3-33ac-7000-9de7-4af5ecb38a4e'
order by allocated_amount;

\echo '--- C. ORDER ACCEPTED GROUP CHANNEL (authoritative eligibility source) ---'
select checkout_id, channel, currency, shipping_amount
from "order".checkouts
where checkout_id = '01a143f3-323e-7000-916b-8e9dd109e38c';

\echo '--- D. SETTLEMENT PENDING LEDGER (the invariant under proof) ---'
select shipping_group_fee_id, payment_id, shipping_group_id, checkout_id, seller_order_id,
       seller_party_id, method_code, service_code, option_code, accepted_quote_version,
       customer_charge_amount, currency, allocation_state, record_kind,
       allocation_policy_version, allocation_policy_reference,
       carrier_payable_amount, seller_share_amount, marketplace_share_amount,
       shipping_discount_amount, shipping_tax_amount, funder_adjustment_amount, funder_reference,
       reversal_of_shipping_group_fee_id, idempotency_key
from settlement.shipping_group_fees
order by customer_charge_amount;

select count(*) as charge_rows,
       count(distinct shipping_group_id) as distinct_groups,
       count(distinct idempotency_key) as distinct_idempotency_keys,
       sum(customer_charge_amount) as total_customer_charge,
       count(*) filter (where allocation_state = 'PendingAllocation') as pending_rows,
       count(*) filter (where record_kind = 'Charge') as charge_kind_rows,
       count(*) filter (where record_kind = 'Reversal') as reversal_kind_rows,
       count(*) filter (where carrier_payable_amount is not null
                           or seller_share_amount is not null
                           or marketplace_share_amount is not null
                           or shipping_discount_amount is not null
                           or shipping_tax_amount is not null
                           or funder_adjustment_amount is not null
                           or funder_reference is not null
                           or allocation_policy_version is not null
                           or allocation_policy_reference is not null) as rows_with_commercial_value
from settlement.shipping_group_fees;

\echo '--- E. NO SELLER CREDIT / NO PAYOUT FROM THE SHIPPING CHARGE ---'
select entry_type, gross_amount, commission_amount, net_amount, source_type, seller_order_id
from settlement.settlement_entries;
select count(*) as settlement_account_rows from settlement.settlement_accounts;
select count(*) as payout_request_rows from settlement.payout_requests;
select count(*) as seller_credit_rows_carrying_a_group_charge
from settlement.settlement_entries
where entry_type = 'Credit' and gross_amount in (200000, 180000);
select sum(gross_amount) as seller_merchandise_accrual_total,
       sum(gross_amount) - 380000 as accrual_total_minus_both_shipping_charges
from settlement.settlement_entries
where entry_type = 'Credit' and source_type = 'payment';

\echo '--- F. CROSS-CHECK: ledger rows match the paid per-group allocations ---'
-- payment.allocations stores the group id in seller_order_id for TargetKind=StoreShippingGroup,
-- so identity is joined on the shipping group id, which is the stable identity under proof.
select f.shipping_group_id,
       f.method_code,
       f.customer_charge_amount as ledger_charge,
       a.allocated_amount as paid_group_allocation,
       f.currency,
       (f.customer_charge_amount = a.allocated_amount) as charge_equals_paid
from settlement.shipping_group_fees f
join payment.allocations a
  on a.payment_id = f.payment_id and a.seller_order_id = f.shipping_group_id and a.target_kind = 'StoreShippingGroup'
order by f.customer_charge_amount;

\echo '--- G. SETTLEMENT MIGRATION PROVENANCE ---'
select migration_id from settlement.__ef_migrations_history order by migration_id;

\echo '--- H. IDEMPOTENCY OF THE REAL DELIVERY ---'
select count(*) as groups_with_more_than_one_charge_row
from (
    select shipping_group_id, count(*) as n
    from settlement.shipping_group_fees
    where record_kind = 'Charge'
    group by shipping_group_id
) t
where n > 1;
