# TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042 — reproduce the 041 INSERT loophole and prove the repair closes it.
# Runs against the disposable container geo042-verify only (fresh container, empty db geo042).
$ErrorActionPreference = 'Continue'
$dir = $PSScriptRoot
$log = Join-Path $dir 'log-insert-gap.txt'
$C = '00000000-0000-0000-0000-000000000001'
$G1 = '00000000-0000-0000-0000-000000000061'
$G2 = '00000000-0000-0000-0000-000000000062'

function Psql([string]$sql) {
    $sql | docker exec -i geo042-verify sh -c "psql -U postgres -d geo042 -v VERBOSITY=verbose 2>&1"
}
function Log([string]$line) { $line | Tee-Object -FilePath $log -Append | Out-Null }
function LogPsql([string]$sql) { (Psql $sql) | Tee-Object -FilePath $log -Append | Out-Null }

Remove-Item $log -ErrorAction SilentlyContinue
Log "=== G0: two triggers present, country seed ==="
LogPsql "select tgname from pg_trigger where tgrelid='geography.administrative_areas'::regclass and not tgisinternal order by tgname;"
LogPsql "insert into geography.countries (country_id, country_code, is_active, created_at, updated_at) values ('$C','IRN',true,now(),now());"

Log "=== G1: 041 BEHAVIOUR REPRODUCED — insert trigger disabled, same-statement two-row INSERT cycle ==="
LogPsql "alter table geography.administrative_areas disable trigger trg_administrative_areas_ancestry_guard_insert;"
LogPsql "insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values ('$G1','$C','$G2','G1',true,now(),now()), ('$G2','$C','$G1','G2',true,now(),now());"
Log "--- the two rows now reference each other: a committed cycle exists (the 041 loophole) ---"
LogPsql "select code, parent_administrative_area_id from geography.administrative_areas order by code;"

Log "=== G2: REPAIR — insert trigger re-enabled, identical statement must raise 23514 ==="
LogPsql "delete from geography.administrative_areas where administrative_area_id in ('$G1','$G2');"
LogPsql "alter table geography.administrative_areas enable trigger trg_administrative_areas_ancestry_guard_insert;"
LogPsql "insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values ('$G1','$C','$G2','G1',true,now(),now()), ('$G2','$C','$G1','G2',true,now(),now());"
Log "--- table state after the rejected statement (must be empty: nothing committed) ---"
LogPsql "select count(*) as rows_after_rejected_insert from geography.administrative_areas;"

Log "=== G3: UPDATE-only comparison — the same pair created by two UPDATEs is also rejected ==="
LogPsql "insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values ('$G1','$C',null,'G1',true,now(),now()), ('$G2','$C',null,'G2',true,now(),now());"
LogPsql "update geography.administrative_areas set parent_administrative_area_id='$G2' where administrative_area_id='$G1';"
LogPsql "update geography.administrative_areas set parent_administrative_area_id='$G1' where administrative_area_id='$G2';"
Log "--- final table state (must remain acyclic) ---"
LogPsql "select code, parent_administrative_area_id from geography.administrative_areas order by code;"
