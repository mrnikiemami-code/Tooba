# TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047 — deterministic seed generator.
#
# Two jobs, both deliberately mechanical:
#   1. Transcribe the legacy catalogue VERBATIM from its source of truth
#      (src/backend/Modules/Order/Tooba.Order.Application/Storefront/Geography/StorefrontIranGeography.cs)
#      so no province/city name is retyped by hand and no value is invented.
#   2. Derive stable, RFC 4122 v5 (SHA-1, name-based) UUIDs so the same place always gets the same id
#      and re-running the seed is a no-op.
#
# UUIDv5 namespace: the RFC 4122 DNS namespace 6ba7b810-9dad-11d1-80b4-00c04fd430c8.
# Name strings (ASCII, stable, traceable):
#   country        : geography.tooba/<COUNTRY CODE>
#   area           : geography.tooba/<CC>/area/<legacy province code>
#   city           : geography.tooba/<CC>/city/<legacy province code>/<2-digit position in the legacy list>
#   *_name rows    : the owning place's name string + "/name/fa-IR"
# City codes are positional on purpose (e.g. IR-tehran-01): the legacy catalogue gives cities NO code at
# all, and a transliteration would be a guess, whereas the position is exactly reproducible from the
# reviewed legacy list.

param(
    [string]$LegacySource = "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Geography/StorefrontIranGeography.cs",
    [string]$OutFile = "docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047/seed-geography.sql",
    [string]$MapFile = "docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047/seed-id-map.txt"
)

$ErrorActionPreference = "Stop"

# ---- UUIDv5 -----------------------------------------------------------------
$DnsNamespaceBytes = [byte[]]@(
    0x6b, 0xa7, 0xb8, 0x10, 0x9d, 0xad, 0x11, 0xd1,
    0x80, 0xb4, 0x00, 0xc0, 0x4f, 0xd4, 0x30, 0xc8
)

function New-UuidV5 {
    param([byte[]]$Namespace, [string]$Name)
    $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($Name)
    $input = New-Object byte[] ($Namespace.Length + $nameBytes.Length)
    [Array]::Copy($Namespace, 0, $input, 0, $Namespace.Length)
    [Array]::Copy($nameBytes, 0, $input, $Namespace.Length, $nameBytes.Length)
    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    try { $hash = $sha1.ComputeHash($input) } finally { $sha1.Dispose() }
    $bytes = New-Object byte[] 16
    [Array]::Copy($hash, 0, $bytes, 0, 16)
    $bytes[6] = [byte](($bytes[6] -band 0x0f) -bor 0x50)  # version 5
    $bytes[8] = [byte](($bytes[8] -band 0x3f) -bor 0x80)  # RFC 4122 variant
    # Render the SHA-1 octets in canonical RFC 4122 order. [Guid]::new([byte[]]) must NOT be used here:
    # it applies the .NET mixed-endian layout to the first three groups and would move the version nibble.
    $hex = -join ($bytes | ForEach-Object { $_.ToString("x2") })
    return "$($hex.Substring(0,8))-$($hex.Substring(8,4))-$($hex.Substring(12,4))-$($hex.Substring(16,4))-$($hex.Substring(20,12))"
}

# Self-test against the RFC 4122 test vector: UUIDv5(DNS namespace, "www.example.com")
$knownVector = "2ed6657d-e927-568b-95e1-2665a8aea6a2"
$actualVector = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "www.example.com"
if ($actualVector -ne $knownVector) {
    throw "UUIDv5 self-test failed: expected $knownVector, got $actualVector"
}
Write-Host "UUIDv5 self-test ok ($actualVector)"

# ---- parse the legacy catalogue, verbatim ----------------------------------
$source = [System.IO.File]::ReadAllText((Resolve-Path $LegacySource), [System.Text.Encoding]::UTF8)
$pattern = 'new\("(?<code>[a-z_]+)",\s*"(?<label>[^"]+)",\s*\[(?<cities>[^\]]*)\]\)'
$matchesProvinces = [regex]::Matches($source, $pattern)
if ($matchesProvinces.Count -ne 12) {
    throw "expected 12 legacy provinces, parsed $($matchesProvinces.Count)"
}

$provinces = @()
foreach ($m in $matchesProvinces) {
    $cityMatches = [regex]::Matches($m.Groups["cities"].Value, '"([^"]+)"')
    $cities = @()
    $index = 0
    foreach ($c in $cityMatches) {
        $index++
        $cities += [pscustomobject]@{ Position = $index; PersianName = $c.Groups[1].Value }
    }
    $provinces += [pscustomobject]@{
        Code = $m.Groups["code"].Value
        PersianName = $m.Groups["label"].Value
        Cities = $cities
    }
}

$totalCities = ($provinces | ForEach-Object { $_.Cities.Count } | Measure-Object -Sum).Sum
Write-Host "parsed provinces=$($provinces.Count) cities=$totalCities"

# ---- derive ids -------------------------------------------------------------
$countryCode = "IR"
$countryId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode"
$countryNameId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode/name/fa-IR"

$map = New-Object System.Collections.Generic.List[string]
$map.Add("country	$countryCode	$countryId")
$map.Add("country_name	$countryCode/fa-IR	$countryNameId")

$countryRows = New-Object System.Collections.Generic.List[string]
$countryRows.Add("    ('$countryId', '$countryCode', 'ایران', '$countryNameId')")

$areaRows = New-Object System.Collections.Generic.List[string]
$areaNameRows = New-Object System.Collections.Generic.List[string]
$cityRows = New-Object System.Collections.Generic.List[string]
$cityNameRows = New-Object System.Collections.Generic.List[string]
foreach ($p in $provinces) {
    $areaId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode/area/$($p.Code)"
    $areaNameId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode/area/$($p.Code)/name/fa-IR"
    $map.Add("area	$($p.Code)	$areaId")
    $map.Add("area_name	$($p.Code)/fa-IR	$areaNameId")
    $areaRows.Add("    ('$areaId', '$($p.Code)', '$($p.PersianName)')")
    $areaNameRows.Add("    ('$areaNameId', '$($p.Code)', '$($p.PersianName)')")
    foreach ($c in $p.Cities) {
        $position = "{0:d2}" -f $c.Position
        $cityCode = "$countryCode-$($p.Code)-$position"
        $cityId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode/city/$($p.Code)/$position"
        $cityNameId = New-UuidV5 -Namespace $DnsNamespaceBytes -Name "geography.tooba/$countryCode/city/$($p.Code)/$position/name/fa-IR"
        $map.Add("city	$($p.Code)/$position	$cityCode	$cityId")
        $map.Add("city_name	$($p.Code)/$position/fa-IR	$cityNameId")
        $cityRows.Add("    ('$cityId', '$($p.Code)', '$position', '$cityCode', '$($c.PersianName)')")
        $cityNameRows.Add("    ('$cityNameId', '$($p.Code)', '$position', '$($c.PersianName)')")
    }
}

# ---- emit the seed script ---------------------------------------------------
$sql = New-Object System.Collections.Generic.List[string]
$sql.Add("-- TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047 — MINIMAL, EXPLICIT geography reference data for the")
$sql.Add("-- proven LOCAL DEV tenant database (tooba_alpha / tenant store-alpha). GENERATED — do not hand-edit;")
$sql.Add("-- regenerate with seed-generate.ps1, which transcribes the values verbatim from")
$sql.Add("-- StorefrontIranGeography.Provinces and derives the ids with RFC 4122 UUIDv5 (see seed-id-map.txt).")
$sql.Add("--")
$sql.Add("-- Scope (ADR-036 Phase 0a: 'no blanket nationwide import; nothing enabled by default'):")
$sql.Add("--   * country IR")
$sql.Add("--   * the 12 provinces and $totalCities cities the legacy catalogue already supports, nothing more")
$sql.Add("--   * Persian name translation rows, keyed by the Localization-owned fa-IR language id")
$sql.Add("-- NOT seeded here: service areas, coverage/verdicts, foreign countries, districts, serviceability.")
$sql.Add("--")
$sql.Add("-- Idempotent: every insert is 'where not exists' on the natural key, so a re-run inserts 0 rows.")
$sql.Add("-- Fails loud (GEO047_*) instead of guessing when a stable code is already owned by another id, when")
$sql.Add("-- an existing label for the same place+language carries different text, when the Localization")
$sql.Add("-- language row is missing, or when any post-condition of the reviewed shape does not hold.")
$sql.Add("")
$sql.Add("begin;")
$sql.Add("")
$sql.Add("-- Inputs of this run, kept explicit and reviewable.")
$sql.Add("create temp table seed_country (country_id uuid primary key, country_code text not null, name text not null, name_translation_id uuid not null) on commit drop;")
$sql.Add("insert into seed_country (country_id, country_code, name, name_translation_id) values")
$sql.Add("    ('$countryId', '$countryCode', 'ایران', '$countryNameId');")
$sql.Add("")
$sql.Add("create temp table seed_area (area_id uuid primary key, code text not null, name text not null) on commit drop;")
$sql.Add("insert into seed_area (area_id, code, name) values")
$sql.Add(($areaRows -join ",`n") + ";")
$sql.Add("")
$sql.Add("create temp table seed_area_name (translation_id uuid primary key, area_code text not null, name text not null) on commit drop;")
$sql.Add("insert into seed_area_name (translation_id, area_code, name) values")
$sql.Add(($areaNameRows -join ",`n") + ";")
$sql.Add("")
$sql.Add("create temp table seed_city (city_id uuid primary key, area_code text not null, position text not null, code text not null, name text not null) on commit drop;")
$sql.Add("insert into seed_city (city_id, area_code, position, code, name) values")
$sql.Add(($cityRows -join ",`n") + ";")
$sql.Add("")
$sql.Add("create temp table seed_city_name (translation_id uuid primary key, area_code text not null, position text not null, name text not null) on commit drop;")
$sql.Add("insert into seed_city_name (translation_id, area_code, position, name) values")
$sql.Add(($cityNameRows -join ",`n") + ";")
$sql.Add("")
$sql.Add("do `$`$")
$sql.Add("declare")
$sql.Add("    fa_language_id uuid;")
$sql.Add("    conflicting integer;")
$sql.Add("    n_country integer;")
$sql.Add("    n_area integer;")
$sql.Add("    n_city integer;")
$sql.Add("begin")
$sql.Add("    -- Read-only lookup of the Localization-owned Persian language id: Geography never owns the")
$sql.Add("    -- language list (ADR-036 D9), so the id is read from its owner instead of being duplicated here.")
$sql.Add("    select language_id into fa_language_id from localization.languages where code = 'fa-IR';")
$sql.Add("    if fa_language_id is null then")
$sql.Add("        raise exception 'GEO047_LANGUAGE_MISSING: localization.languages has no fa-IR row';")
$sql.Add("    end if;")
$sql.Add("")
$sql.Add("    -- Ambiguity guards: a stable code must never be silently re-pointed at another id.")
$sql.Add("    select count(*) into conflicting from geography.countries x join seed_country s on x.country_code = s.country_code where x.country_id <> s.country_id;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_AMBIGUOUS_COUNTRY: country_code already owned by a different id';")
$sql.Add("    end if;")
$sql.Add("    select count(*) into conflicting from geography.administrative_areas x join seed_area s on x.code = s.code where x.administrative_area_id <> s.area_id;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_AMBIGUOUS_AREA: area code already owned by a different id';")
$sql.Add("    end if;")
$sql.Add("    select count(*) into conflicting from geography.cities x join seed_city s on x.code = s.code where x.city_id <> s.city_id;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_AMBIGUOUS_CITY: city code already owned by a different id';")
$sql.Add("    end if;")
$sql.Add("")
$sql.Add("    -- Drift guards: an existing label for the same place+language carrying different text is not a")
$sql.Add("    -- rerun of this seed, it is a conflict; report it instead of overwriting.")
$sql.Add("    select count(*) into conflicting from geography.country_name_translations x join seed_country s on x.country_id = s.country_id where x.language_id = fa_language_id and x.name <> s.name;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_CONFLICTING_COUNTRY_LABEL';")
$sql.Add("    end if;")
$sql.Add("    select count(*) into conflicting from geography.administrative_area_name_translations x join geography.administrative_areas a on a.administrative_area_id = x.administrative_area_id join seed_area s on s.code = a.code where x.language_id = fa_language_id and x.name <> s.name;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_CONFLICTING_AREA_LABEL';")
$sql.Add("    end if;")
$sql.Add("    select count(*) into conflicting from geography.city_name_translations x join geography.cities c on c.city_id = x.city_id join seed_city s on s.code = c.code where x.language_id = fa_language_id and x.name <> s.name;")
$sql.Add("    if conflicting > 0 then")
$sql.Add("        raise exception 'GEO047_CONFLICTING_CITY_LABEL';")
$sql.Add("    end if;")
$sql.Add("")
$sql.Add("    insert into geography.countries (country_id, country_code, is_active, created_at, updated_at)")
$sql.Add("    select s.country_id, s.country_code, true, now(), now() from seed_country s")
$sql.Add("    where not exists (select 1 from geography.countries x where x.country_id = s.country_id);")
$sql.Add("")
$sql.Add("    -- Provinces are roots here: D10 (administrative level semantics) is undecided, so no level")
$sql.Add("    -- discriminator and no invented intermediate node is introduced.")
$sql.Add("    insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)")
$sql.Add("    select s.area_id, c.country_id, null, s.code, true, now(), now()")
$sql.Add("    from seed_area s cross join seed_country c")
$sql.Add("    where not exists (select 1 from geography.administrative_areas x where x.administrative_area_id = s.area_id);")
$sql.Add("")
$sql.Add("    insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)")
$sql.Add("    select s.city_id, c.country_id, a.area_id, s.code, true, now(), now()")
$sql.Add("    from seed_city s join seed_area a on a.code = s.area_code cross join seed_country c")
$sql.Add("    where not exists (select 1 from geography.cities x where x.city_id = s.city_id);")
$sql.Add("")
$sql.Add("    insert into geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)")
$sql.Add("    select s.name_translation_id, s.country_id, fa_language_id, s.name, now(), now() from seed_country s")
$sql.Add("    where not exists (select 1 from geography.country_name_translations x where x.country_id = s.country_id and x.language_id = fa_language_id);")
$sql.Add("")
$sql.Add("    insert into geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at)")
$sql.Add("    select n.translation_id, a.area_id, fa_language_id, n.name, now(), now()")
$sql.Add("    from seed_area_name n join seed_area a on a.code = n.area_code")
$sql.Add("    where not exists (select 1 from geography.administrative_area_name_translations x where x.administrative_area_id = a.area_id and x.language_id = fa_language_id);")
$sql.Add("")
$sql.Add("    insert into geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)")
$sql.Add("    select n.translation_id, c.city_id, fa_language_id, n.name, now(), now()")
$sql.Add("    from seed_city_name n")
$sql.Add("    join seed_city c on c.area_code = n.area_code and c.position = n.position")
$sql.Add("    where not exists (select 1 from geography.city_name_translations x where x.city_id = c.city_id and x.language_id = fa_language_id);")
$sql.Add("")
$sql.Add("    -- Post-conditions: the stored shape must equal the reviewed legacy shape exactly.")
$sql.Add("    select count(*) into n_country from seed_country;")
$sql.Add("    select count(*) into n_area from seed_area;")
$sql.Add("    select count(*) into n_city from seed_city;")
$sql.Add("    if (select count(*) from geography.countries) <> n_country then")
$sql.Add("        raise exception 'GEO047_POST_COUNTRY_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if (select count(*) from geography.administrative_areas) <> n_area then")
$sql.Add("        raise exception 'GEO047_POST_AREA_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if (select count(*) from geography.cities) <> n_city then")
$sql.Add("        raise exception 'GEO047_POST_CITY_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if (select count(*) from geography.country_name_translations) <> n_country then")
$sql.Add("        raise exception 'GEO047_POST_COUNTRY_LABEL_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if (select count(*) from geography.administrative_area_name_translations) <> n_area then")
$sql.Add("        raise exception 'GEO047_POST_AREA_LABEL_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if (select count(*) from geography.city_name_translations) <> n_city then")
$sql.Add("        raise exception 'GEO047_POST_CITY_LABEL_COUNT';")
$sql.Add("    end if;")
$sql.Add("    if exists (select 1 from geography.administrative_areas where parent_administrative_area_id is not null) then")
$sql.Add("        raise exception 'GEO047_POST_UNEXPECTED_AREA_PARENT';")
$sql.Add("    end if;")
$sql.Add("    if exists (select 1 from geography.cities c left join geography.administrative_areas a on a.administrative_area_id = c.administrative_area_id where a.administrative_area_id is null) then")
$sql.Add("        raise exception 'GEO047_POST_ORPHAN_CITY';")
$sql.Add("    end if;")
$sql.Add("    if exists (select 1 from geography.service_areas) then")
$sql.Add("        raise exception 'GEO047_POST_UNEXPECTED_SERVICE_AREA';")
$sql.Add("    end if;")
$sql.Add("end `$`$;")
$sql.Add("")
$sql.Add("commit;")
$sql.Add("")
$sql.Add("-- Observed shape after this run.")
$sql.Add("select 'countries=' || (select count(*) from geography.countries)")
$sql.Add("    || ' areas=' || (select count(*) from geography.administrative_areas)")
$sql.Add("    || ' cities=' || (select count(*) from geography.cities)")
$sql.Add("    || ' service_areas=' || (select count(*) from geography.service_areas)")
$sql.Add("    || ' country_names=' || (select count(*) from geography.country_name_translations)")
$sql.Add("    || ' area_names=' || (select count(*) from geography.administrative_area_name_translations)")
$sql.Add("    || ' city_names=' || (select count(*) from geography.city_name_translations);")

[System.IO.File]::WriteAllText((Join-Path (Get-Location) $OutFile), ($sql -join "`n") + "`n", (New-Object System.Text.UTF8Encoding($false)))
[System.IO.File]::WriteAllText((Join-Path (Get-Location) $MapFile), (($map -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "wrote $OutFile and $MapFile"
