# R1 fresh DB migrate

`OfferMigrationIntegrityTests.Fresh_database_migrates_offer_to_head_without_collision` + `Apply_on_empty_database_succeeds_and_reapply_is_idempotent`.

Empty PostgreSQL Testcontainer → all module migrations to HEAD. Offer history is the three canonical rows. Return-policy + min/max columns coexist once. Re-apply pending=0.
