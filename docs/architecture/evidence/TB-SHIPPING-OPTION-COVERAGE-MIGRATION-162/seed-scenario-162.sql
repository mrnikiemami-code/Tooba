-- Remove the mixed-case probe row: it was inserted only to record that service_code uniqueness is
-- case-sensitive at the DB level (see RESULT §findings). The app write path always sends the route code.
delete from shipping.method_coverages where service_code = 'TIPAX';

-- Scenario fixtures for owner-code quote proofs (all task-scratch only).
INSERT INTO shipping.rate_policies (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at, min_weight_grams, max_weight_grams) VALUES
 ('dddddddd-1111-4111-8111-dddddddddddd','tpost','standard','e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5',NULL,NULL,150000.00,'IRR',0,2,NULL,true,now(),now(),NULL,NULL),
 ('eeeeeeee-2222-4222-8222-eeeeeeeeeeee','strictonly','standard','e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5',NULL,NULL,170000.00,'IRR',0,2,NULL,true,now(),now(),NULL,NULL),
 ('ffffffff-3333-4333-8333-ffffffffffff','legacyonly',NULL,'e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5',NULL,NULL,100000.00,'IRR',0,2,NULL,true,now(),now(),NULL,NULL),
 ('abababab-4444-4444-8444-abababababab','cond','standard','e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5',NULL,NULL,175000.00,'IRR',0,2,NULL,true,now(),now(),1000,2000);

INSERT INTO shipping.method_coverages (method_coverage_id, service_code, option_code, mode, created_at, updated_at) VALUES
 ('12121212-aaaa-4aaa-8aaa-121212121212','strictonly','standard',1,now(),now());

INSERT INTO shipping.coverage_grants (coverage_grant_id, service_code, option_code, country_id, administrative_area_id, city_id, is_active, created_at, updated_at) VALUES
 ('77777777-7777-4777-8777-777777777777','legacyonly','standard','e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5','c841be1e-0f47-54aa-8be8-a7b5c33f6f64',true,now(),now());
