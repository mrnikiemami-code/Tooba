# TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042 — isolated PostgreSQL verification runner.
# Destination: disposable task-exclusive container geo042-verify (postgres:17), db geo042, host port 55432.
# It never references Tooba's own database: the only connection string used is 127.0.0.1:55432.
$ErrorActionPreference = 'Continue'
$dir = $PSScriptRoot
$C = '00000000-0000-0000-0000-000000000001'

function Psql([string]$sql) {
    $sql | docker exec -i geo042-verify sh -c "psql -U postgres -d geo042 -v VERBOSITY=verbose 2>&1"
}
function Log([string]$file, [string]$line) {
    $line | Tee-Object -FilePath (Join-Path $dir $file) -Append | Out-Null
}
function LogPsql([string]$file, [string]$sql) {
    (Psql $sql) | Tee-Object -FilePath (Join-Path $dir $file) -Append | Out-Null
}

$log1 = 'log-concurrency-1.txt'
$log2 = 'log-concurrency-2.txt'
$logDest = 'log-destination-proof.txt'

# ---- destination proof (independently verified) ----
Remove-Item (Join-Path $dir $logDest) -ErrorAction SilentlyContinue
Log $logDest "=== container identity (must differ from the existing postgres-db container) ==="
Log $logDest ((docker inspect --format '{{.Id}} {{.Name}} {{.State.Status}} {{.Config.Image}}' geo042-verify) -join '; ')
Log $logDest ((docker inspect --format '{{.Id}} {{.Name}} {{.State.Status}} {{.Config.Image}}' postgres-db) -join '; ')
Log $logDest ((docker port geo042-verify) -join '; ')
Log $logDest "=== server answering on the 55432 socket inside the disposable container ==="
LogPsql $logDest "select current_database() as db, current_user as usr, pg_postmaster_start_time() as started; select count(*) as non_template_databases from pg_database where datname not like 'template%';"

# ---- scenario 1: two overlapping sessions attempting opposite reparentings (cycle race) ----
$P = '00000000-0000-0000-0000-000000000071'
$Q = '00000000-0000-0000-0000-000000000072'
Remove-Item (Join-Path $dir $log1) -ErrorAction SilentlyContinue
Log $log1 "=== S0: two independent roots P, Q ==="
LogPsql $log1 "insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values ('$P','$C',null,'P2',true,now(),now()), ('$Q','$C',null,'Q2',true,now(),now());"
Log $log1 "=== S1 (background): P -> Q, then hold the transaction open for 6s, then commit ==="
$sql1 = "begin; update geography.administrative_areas set parent_administrative_area_id='$Q' where administrative_area_id='$P'; select pg_sleep(6); commit;"
$job = Start-Job -ScriptBlock { param($s) $s | docker exec -i geo042-verify sh -c "psql -U postgres -d geo042 -v VERBOSITY=verbose 2>&1" } -ArgumentList $sql1
Start-Sleep -Milliseconds 2500
Log $log1 "=== S2 (foreground, started 2.5s later): Q -> P. Expect: blocks on the advisory lock, then 23514 ==="
$t0 = Get-Date
(Psql "begin; update geography.administrative_areas set parent_administrative_area_id='$P' where administrative_area_id='$Q'; commit;") |
    Tee-Object -FilePath (Join-Path $dir $log1) -Append | Out-Null
Log $log1 ("S2_ELAPSED_SEC " + [math]::Round(((Get-Date) - $t0).TotalSeconds, 2))
Wait-Job $job | Out-Null
Log $log1 "=== S1 output ==="
(Receive-Job $job) | Tee-Object -FilePath (Join-Path $dir $log1) -Append | Out-Null
Remove-Job $job
Log $log1 "=== final state (must be acyclic: P -> Q, Q root) ==="
LogPsql $log1 "select code, parent_administrative_area_id from geography.administrative_areas where code in ('P2','Q2') order by code;"

# ---- scenario 2: two overlapping sessions with non-conflicting reparentings (no spurious failure) ----
$A = '00000000-0000-0000-0000-000000000081'
$B = '00000000-0000-0000-0000-000000000082'
$D = '00000000-0000-0000-0000-000000000083'
Remove-Item (Join-Path $dir $log2) -ErrorAction SilentlyContinue
Log $log2 "=== S0: chain R3 <- C2 <- C3 ==="
LogPsql $log2 "insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values ('$A','$C',null,'R3',true,now(),now()), ('$B','$C','$A','C2b',true,now(),now()), ('$D','$C','$B','C3b',true,now(),now());"
Log $log2 "=== S1 (background): C3 -> R3, then hold the transaction open for 6s ==="
$sql1 = "begin; update geography.administrative_areas set parent_administrative_area_id='$A' where administrative_area_id='$D'; select pg_sleep(6); commit;"
$job = Start-Job -ScriptBlock { param($s) $s | docker exec -i geo042-verify sh -c "psql -U postgres -d geo042 -v VERBOSITY=verbose 2>&1" } -ArgumentList $sql1
Start-Sleep -Milliseconds 2500
Log $log2 "=== S2 (foreground, started 2.5s later): C2 -> R3. Expect: blocks, then SUCCEEDS (no cycle) ==="
$t0 = Get-Date
(Psql "begin; update geography.administrative_areas set parent_administrative_area_id='$A' where administrative_area_id='$B'; commit;") |
    Tee-Object -FilePath (Join-Path $dir $log2) -Append | Out-Null
Log $log2 ("S2_ELAPSED_SEC " + [math]::Round(((Get-Date) - $t0).TotalSeconds, 2))
Wait-Job $job | Out-Null
Log $log2 "=== S1 output ==="
(Receive-Job $job) | Tee-Object -FilePath (Join-Path $dir $log2) -Append | Out-Null
Remove-Job $job
Log $log2 "=== final state (C2b and C3b both under R3) ==="
LogPsql $log2 "select code, parent_administrative_area_id from geography.administrative_areas where code in ('R3','C2b','C3b') order by code;"
