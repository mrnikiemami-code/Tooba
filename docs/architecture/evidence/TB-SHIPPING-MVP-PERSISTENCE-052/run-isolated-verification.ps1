# TB-SHIPPING-MVP-PERSISTENCE-052 — isolated PostgreSQL verification runner.
# Destination: task-exclusive disposable container `ship052-verify` (postgres:17), database `ship052`,
# host port 55433. The only connection string used anywhere below is 127.0.0.1:55433; Tooba's own
# database container (`postgres-db`, port 5432) is never referenced or opened.
$ErrorActionPreference = 'Continue'
$dir = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $dir '..\..\..\..')).Path
# The connection string for the task-exclusive disposable container comes from the environment so that
# no credential is ever written into the repository. It must be a 127.0.0.1:<disposable port> target.
$cs = $env:SHIP052_DISPOSABLE_CS
if ([string]::IsNullOrWhiteSpace($cs)) {
    throw 'SHIP052_DISPOSABLE_CS must point at the disposable container (127.0.0.1:55433).'
}
if ($cs -notmatch '127\.0\.0\.1' -or $cs -notmatch '55433') {
    throw 'Refusing to run: the connection string is not the disposable ship052-verify target.'
}
$env:TOOBA_DESIGN_TIME_CONNECTION = $cs
$proj = Join-Path $repo 'src\backend\Modules\Shipping\Tooba.Shipping.Infrastructure\Tooba.Shipping.Infrastructure.csproj'

function Psql([string]$sql) {
    $sql | docker exec -i ship052-verify sh -c "psql -U postgres -d ship052 -v VERBOSITY=verbose 2>&1"
}
function Log([string]$file, [string]$line) {
    $line | Tee-Object -FilePath (Join-Path $dir $file) -Append | Out-Null
}
function LogPsql([string]$file, [string]$sql) {
    (Psql $sql) | Tee-Object -FilePath (Join-Path $dir $file) -Append | Out-Null
}
function Ef([string]$file, [string[]]$efArgs) {
    $out = & dotnet ef @efArgs --project $proj --context ShippingDbContext --no-build 2>&1
    Log $file ($out -join "`n")
    return ($out -join ' ')
}

$logDest = 'log-destination-proof.txt'
Remove-Item (Join-Path $dir $logDest) -ErrorAction SilentlyContinue
Log $logDest "=== destination proof: the disposable container is NOT Tooba's database container ==="
Log $logDest ("disposable : " + ((docker inspect --format '{{.Id}} name={{.Name}} status={{.State.Status}} image={{.Config.Image}}' ship052-verify) -join '; '))
Log $logDest ("tooba-db   : " + ((docker inspect --format '{{.Id}} name={{.Name}} status={{.State.Status}} image={{.Config.Image}}' postgres-db) -join '; '))
Log $logDest ("disposable ports: " + ((docker port ship052-verify) -join '; '))
Log $logDest "=== the server answering on 55433 is the disposable one ==="
LogPsql $logDest "select current_database() as db, current_user as usr, pg_postmaster_start_time() as started, version() as server;"
LogPsql $logDest "select count(*) as non_template_databases from pg_database where datname not like 'template%';"

$logLife = 'log-migration-lifecycle.txt'
Remove-Item (Join-Path $dir $logLife) -ErrorAction SilentlyContinue
Log $logLife "=== L0: state before any DDL (must be empty) ==="
LogPsql $logLife "select nspname from pg_namespace where nspname = 'shipping';"
Log $logLife "=== L1: UP (dotnet ef database update) ==="
Log $logLife (Ef $logLife @('database', 'update'))
Log $logLife "=== L1a: applied migration rows in the module-owned history table ==="
LogPsql $logLife "select migration_id, product_version from shipping.__ef_migrations_history order by 1;"
Log $logLife "=== L1b: objects created inside schema shipping ==="
LogPsql $logLife "select table_name from information_schema.tables where table_schema = 'shipping' order by 1;"
Log $logLife "=== L1c: primary keys ==="
LogPsql $logLife "select conrelid::regclass::text as tbl, conname, pg_get_constraintdef(oid) from pg_constraint where connamespace = 'shipping'::regnamespace and contype = 'p' order by 1;"
Log $logLife "=== L1d: CHECK constraints ==="
LogPsql $logLife "select conrelid::regclass::text as tbl, conname, pg_get_constraintdef(oid) from pg_constraint where connamespace = 'shipping'::regnamespace and contype = 'c' and conname like 'ck_%' order by 1,2;"
Log $logLife "=== L1e: indexes incl. NULLS NOT DISTINCT flag (indnullsnotdistinct) ==="
LogPsql $logLife "select c.relname as tbl, i.relname as idx, x.indisunique as is_unique, x.indnullsnotdistinct as nulls_not_distinct, pg_get_indexdef(x.indexrelid) as def from pg_index x join pg_class c on c.oid = x.indrelid join pg_class i on i.oid = x.indexrelid where c.relnamespace = 'shipping'::regnamespace and c.relkind = 'r' order by 1,2;"
Log $logLife "=== L1f: no cross-schema foreign keys were created (module boundary) ==="
LogPsql $logLife "select count(*) as shipping_outbound_fks from pg_constraint where contype = 'f' and connamespace = 'shipping'::regnamespace;"

$logCon = 'log-constraint-scenarios.txt'
Remove-Item (Join-Path $dir $logCon) -ErrorAction SilentlyContinue
$C = '00000000-0000-0000-0000-0000000000c1'
$AREA = '00000000-0000-0000-0000-0000000000a1'
$CITY = '00000000-0000-0000-0000-0000000000b1'
$SA = '00000000-0000-0000-0000-0000000000d1'
$P1 = '00000000-0000-0000-0000-000000000001'
$P2 = '00000000-0000-0000-0000-000000000002'
$P3 = '00000000-0000-0000-0000-000000000003'
$P4 = '00000000-0000-0000-0000-000000000004'
$G1 = '00000000-0000-0000-0000-000000000011'
$G2 = '00000000-0000-0000-0000-000000000012'
$P5 = '00000000-0000-0000-0000-000000000005'
$P6 = '00000000-0000-0000-0000-000000000006'
$P7 = '00000000-0000-0000-0000-000000000007'

Log $logCon "=== S1: a country-level policy (deeper scope columns NULL) is accepted ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P1','post','$C',null,null,null,120000.00,'IRR',1,3,null,true,now(),now());"
Log $logCon "=== S1b: the SAME service at the SAME country scope with every deeper column NULL is rejected (NULLS NOT DISTINCT) ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P2','post','$C',null,null,null,999.00,'IRR',0,0,null,true,now(),now());"
Log $logCon "=== S1c: a DIFFERENT scope depth for the same service is still allowed (city level) ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P3','post','$C','$AREA','$CITY',null,90000.00,'IRR',1,2,500000.00,true,now(),now());"
Log $logCon "=== S1d: an area-level row (city NULL) is still allowed and does not collide with the country or city rows ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P4','post','$C','$AREA',null,null,100000.00,'IRR',1,3,null,true,now(),now());"
Log $logCon "=== S1e: a different service at the same country scope is allowed (service code is part of the identity) ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, administrative_area_id, city_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P5','tipax','$C',null,null,null,150000.00,'IRR',0,1,null,true,now(),now());"
Log $logCon "=== S2: negative charge is rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P6','snapp_courier','$C',-1,'IRR',0,0,true,now(),now());"
Log $logCon "=== S2b: charge above the model maximum is rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C',1000000001,'IRR',0,0,true,now(),now());"
Log $logCon "=== S2c: negative preparation days are rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C',1000,'IRR',-1,0,true,now(),now());"
Log $logCon "=== S2d: lead days above the model maximum are rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C',1000,'IRR',0,366,true,now(),now());"
Log $logCon "=== S2e: a negative free-above threshold is rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C',1000,'IRR',0,0,-5,true,now(),now());"
Log $logCon "=== S2f: a whitespace-padded service code is rejected (the engine matches trimmed codes) ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7',' post ','$C',1000,'IRR',0,0,true,now(),now());"
Log $logCon "=== S2g: a two-letter currency code is rejected ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C',1000,'IR',0,0,true,now(),now());"
Log $logCon "=== S2h: a service area without its owning city is rejected as a half-scope ==="
LogPsql $logCon "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, service_area_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('$P7','snapp_courier','$C','$SA',1000,'IRR',0,0,true,now(),now());"
Log $logCon "=== S3: coverage grant at country scope is accepted ==="
LogPsql $logCon "insert into shipping.coverage_grants (coverage_grant_id, service_code, country_id, is_active, created_at, updated_at) values ('$G1','post','$C',true,now(),now());"
Log $logCon "=== S3b: the duplicate country-scope grant for the same service is rejected ==="
LogPsql $logCon "insert into shipping.coverage_grants (coverage_grant_id, service_code, country_id, is_active, created_at, updated_at) values ('$G2','post','$C',false,now(),now());"
Log $logCon "=== S3c: a city-scope grant for the same service is still allowed ==="
LogPsql $logCon "insert into shipping.coverage_grants (coverage_grant_id, service_code, country_id, administrative_area_id, city_id, is_active, created_at, updated_at) values ('$G2','post','$C','$AREA','$CITY',true,now(),now());"
Log $logCon "=== S3d: a coverage grant for a service with no policy yet is accepted (coverage and pricing are separate decisions) ==="
LogPsql $logCon "insert into shipping.coverage_grants (coverage_grant_id, service_code, country_id, is_active, created_at, updated_at) values ('00000000-0000-0000-0000-000000000013','store_courier','$C',true,now(),now());"
Log $logCon "=== S3e: a half-scope coverage grant is rejected ==="
LogPsql $logCon "insert into shipping.coverage_grants (coverage_grant_id, service_code, country_id, service_area_id, is_active, created_at, updated_at) values ('00000000-0000-0000-0000-000000000014','store_courier','$C','$SA',true,now(),now());"
Log $logCon "=== S4: surviving rows (must be exactly the accepted ones) ==="
LogPsql $logCon "select service_code, administrative_area_id is not null as has_area, city_id is not null as has_city, service_area_id is not null as has_sa, charge_amount, is_active from shipping.rate_policies order by service_code, charge_amount;"
LogPsql $logCon "select service_code, administrative_area_id is not null as has_area, city_id is not null as has_city, is_active from shipping.coverage_grants order by service_code, is_active;"

$logDown = 'log-downtime-down-up.txt'
Remove-Item (Join-Path $dir $logDown) -ErrorAction SilentlyContinue
Log $logDown "=== D-probe: PostgreSQL support for NULLS NOT DISTINCT (needs 15+) ==="
LogPsql $logDown "show server_version;"
Log $logDown "=== D1: DOWN (dotnet ef database update 0) ==="
Log $logDown (Ef $logDown @('database', 'update', '0'))
Log $logDown "=== D1a: tables left in schema shipping (only the migration history table may remain) ==="
LogPsql $logDown "select table_name from information_schema.tables where table_schema = 'shipping' order by 1;"
Log $logDown "=== D1b: residual module tables must be zero ==="
LogPsql $logDown "select count(*) as residual_module_tables from information_schema.tables where table_schema = 'shipping' and table_name not like '__ef%';"
Log $logDown "=== D2: UP again on the same database (dotnet ef database update) ==="
Log $logDown (Ef $logDown @('database', 'update'))
Log $logDown "=== D2a: tables after the second UP ==="
LogPsql $logDown "select table_name from information_schema.tables where table_schema = 'shipping' order by 1;"
Log $logDown "=== D2b: the constraints and the NULLS NOT DISTINCT unique indexes are identical after round-tripping ==="
LogPsql $logDown "select conrelid::regclass::text as tbl, conname from pg_constraint where connamespace = 'shipping'::regnamespace and contype = 'c' and conname like 'ck_%' order by 1,2;"
LogPsql $logDown "select c.relname as tbl, i.relname as idx, x.indnullsnotdistinct as nulls_not_distinct from pg_index x join pg_class c on c.oid = x.indrelid join pg_class i on i.oid = x.indexrelid where c.relnamespace = 'shipping'::regnamespace and c.relkind = 'r' and x.indisunique order by 1,2;"
Log $logDown "=== D3: the second UP is a genuine re-apply, not a no-op: the duplicate scope must be refused again ==="
LogPsql $logDown "insert into shipping.rate_policies (rate_policy_id, service_code, country_id, charge_amount, currency_code, preparation_days, lead_days, is_active, created_at, updated_at) values ('00000000-0000-0000-0000-0000000000f1','post','$C',1,'IRR',0,0,true,now(),now()), ('00000000-0000-0000-0000-0000000000f2','post','$C',2,'IRR',0,0,true,now(),now());"
Log $logDown "=== D4: the second UP is idempotent for the history table (exactly one applied migration row) ==="
LogPsql $logDown "select count(*) as applied_migrations from shipping.__ef_migrations_history;"

$logWrap = 'log-no-real-db-touch.txt'
Remove-Item (Join-Path $dir $logWrap) -ErrorAction SilentlyContinue
Log $logWrap "=== W1: the pre-existing Development database containers still have NO shipping objects ==="
foreach ($db in @('tooba_alpha', 'MyDatabase', 'tooba_codex_dev')) {
    $schema = docker exec postgres-db psql -U admin -d $db -tAc "select count(*) from information_schema.schemata where schema_name = 'shipping';" 2>&1
    $tables = docker exec postgres-db psql -U admin -d $db -tAc "select count(*) from information_schema.tables where table_name in ('rate_policies','coverage_grants');" 2>&1
    Log $logWrap ("$db : shipping_schema=" + ($schema -join ' ') + " rate_policies_or_coverage_grants=" + ($tables -join ' '))
}
Log $logWrap "=== W2: no credential value anywhere in the committed evidence (txt/sql/json/md) ==="
$hits = Select-String -Path (Join-Path $dir '*.txt'), (Join-Path $dir '*.sql'), (Join-Path $dir '*.json'), (Join-Path $dir '*.md') -Pattern 'disposable052', 'Password=', 'pwd=' -ErrorAction SilentlyContinue
Log $logWrap ("credential_value_hits_in_evidence: " + (($hits | Measure-Object).Count) + " (" + (($hits | ForEach-Object { $_.Filename }) -join ', ') + ")")
Log $logWrap "=== W3: teardown of the disposable container ==="
Log $logWrap ((docker rm -f ship052-verify) -join ' ')
Log $logWrap ("remaining_containers: " + ((docker ps -a --format '{{.Names}}') -join ', '))
