using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Places;
using Tooba.Localization.Contracts.Ports;
using Tooba.Persistence;

var failures = new List<string>();
void Check(string name, bool ok, string? detail = null)
{
    Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name + (detail is null ? "" : " :: " + detail));
    if (!ok) failures.Add(name);
}

// Disposable, task-exclusive container geo043-verify (postgres:17), db geo043, host port 55433.
const string ConnectionString = "Host=127.0.0.1;Port=55433;Username=postgres;Password=geo043localonly;Database=geo043";
var options = new DbContextOptionsBuilder<GeographyDbContext>();
ToobaNpgsql.ConfigureModuleContext(options, ConnectionString, GeographyDbContext.Schema, typeof(GeographyDbContext));
var db = new GeographyDbContext(options.Options);

var lookup = new GeographyPlaceLookup(db);
var labels = new GeographyLabelLookup(db, new FakeLanguages(DefaultLanguages()));

var IRN = Guid.Parse("10000000-0000-0000-0000-000000000001");
var DEU = Guid.Parse("10000000-0000-0000-0000-000000000002");
var FRA = Guid.Parse("10000000-0000-0000-0000-000000000003");
var THR = Guid.Parse("20000000-0000-0000-0000-000000000001");
var ALB = Guid.Parse("20000000-0000-0000-0000-000000000002");
var MZK1 = Guid.Parse("20000000-0000-0000-0000-000000000003");
var MZK2 = Guid.Parse("20000000-0000-0000-0000-000000000004");
var TEH1 = Guid.Parse("30000000-0000-0000-0000-000000000001");
var KAR = Guid.Parse("30000000-0000-0000-0000-000000000002");
var SV1 = Guid.Parse("30000000-0000-0000-0000-000000000003");
var SV2 = Guid.Parse("30000000-0000-0000-0000-000000000004");
var EN1 = Guid.Parse("30000000-0000-0000-0000-000000000005");
var NOTR = Guid.Parse("30000000-0000-0000-0000-000000000006");
var INA = Guid.Parse("30000000-0000-0000-0000-000000000007");
var BER = Guid.Parse("30000000-0000-0000-0000-000000000008");
var SA1 = Guid.Parse("40000000-0000-0000-0000-000000000001");
var SA2 = Guid.Parse("40000000-0000-0000-0000-000000000002");
var SA3 = Guid.Parse("40000000-0000-0000-0000-000000000003");
var FA = Guid.Parse("fa000000-0000-0000-0000-000000000000");
var EN = Guid.Parse("ee000000-0000-0000-0000-000000000000");
var UNKNOWN_LANGUAGE = Guid.Parse("ab000000-0000-0000-0000-000000000000");
var MISSING = Guid.Parse("99999999-0000-0000-0000-000000000000");

var ct = CancellationToken.None;

async Task<int> TotalRowsAsync()
{
    var countries = await db.Countries.CountAsync(ct);
    var areas = await db.AdministrativeAreas.CountAsync(ct);
    var cities = await db.Cities.CountAsync(ct);
    var serviceAreas = await db.ServiceAreas.CountAsync(ct);
    var cityLabels = await db.CityNameTranslations.CountAsync(ct);
    return countries + areas + cities + serviceAreas + cityLabels;
}

var rowsBefore = await TotalRowsAsync();

// ---------------- A1: listings ----------------
var countries = await lookup.ListCountriesAsync(ct);
Check("L1 ListCountries: 3 rows ordered by code incl. inactive FRA", countries.Count == 3
    && countries.Select(x => x.CountryCode).SequenceEqual(new[] { "DEU", "FRA", "IRN" })
    && countries.Single(x => x.CountryId == FRA).IsActive == false,
    string.Join(",", countries.Select(x => $"{x.CountryCode}:{x.IsActive}")));

var areas = await lookup.ListAdministrativeAreasAsync(IRN, ct);
Check("L2 ListAdministrativeAreas(IRN): 4 rows ordered by code, parents surfaced", areas.Count == 4
    && areas.Select(x => x.Code).SequenceEqual(new[] { "ALB", "MZK1", "MZK2", "THR" })
    && areas.All(x => x.CountryId == IRN && x.ParentAdministrativeAreaId is null && x.IsActive),
    string.Join(",", areas.Select(x => x.Code)));

var allCities = await lookup.ListCitiesAsync(IRN, null, ct);
Check("L3 ListCities(IRN): 7 rows ordered by code with correct area links, other country excluded", allCities.Count == 7
    && allCities.Select(x => x.Code).SequenceEqual(new[] { "EN1", "INA", "KAR", "NOTR", "SV1", "SV2", "TEH1" })
    && allCities.Single(x => x.CityId == KAR).AdministrativeAreaId == ALB
    && allCities.Single(x => x.CityId == SV2).AdministrativeAreaId == MZK2
    && allCities.All(x => x.CountryId == IRN && x.CityId != BER),
    string.Join(",", allCities.Select(x => x.Code)));

var scopedCities = await lookup.ListCitiesAsync(IRN, MZK1, ct);
Check("L4 ListCities(IRN, MZK1): exactly SV1", scopedCities.Count == 1 && scopedCities[0].CityId == SV1);

Check("L5 ListCities(empty country): empty, no exception",
    (await lookup.ListCitiesAsync(Guid.Empty, null, ct)).Count == 0);

var serviceAreas = await lookup.ListServiceAreasAsync(TEH1, ct);
Check("L6 ListServiceAreas(TEH1): SA1, SA2 only (SA3 belongs to another city)", serviceAreas.Count == 2
    && serviceAreas.Select(x => x.Code).SequenceEqual(new[] { "SA1", "SA2" }),
    string.Join(",", serviceAreas.Select(x => x.Code)));

Check("L7 ListServiceAreas(empty city): empty, no exception",
    (await lookup.ListServiceAreasAsync(Guid.Empty, ct)).Count == 0);

// ---------------- A2: GetByIds validation ----------------
var full = new DestinationIdentifiers(IRN, THR, TEH1, SA1);
var g1 = await lookup.GetByIdsAsync(full, ct);
Check("G1 GetByIds full valid chain accepted", g1 == full, g1?.ToString());

var noService = new DestinationIdentifiers(IRN, THR, TEH1);
Check("G2 GetByIds without service area accepted",
    await lookup.GetByIdsAsync(noService, ct) == noService);

var countryOnly = new DestinationIdentifiers(IRN);
Check("G3 GetByIds country only accepted",
    await lookup.GetByIdsAsync(countryOnly, ct) == countryOnly);

Check("G4 GetByIds inactive country rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(FRA), ct) is null);
Check("G5 GetByIds inactive city rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, THR, INA), ct) is null);
Check("G6 GetByIds city from another country rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, THR, BER), ct) is null);
Check("G7 GetByIds area/city mismatch rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, MZK1, TEH1), ct) is null);
Check("G8 GetByIds service area of another city rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, THR, TEH1, SA3), ct) is null);
Check("G9 GetByIds service area without owning city rejected", await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, null, null, SA1), ct) is null);
Check("G10 GetByIds Guid.Empty rejected (country and optional slots)",
    await lookup.GetByIdsAsync(new DestinationIdentifiers(Guid.Empty), ct) is null
    && await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, null, null, Guid.Empty), ct) is null
    && await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, Guid.Empty), ct) is null);
Check("G11 GetByIds unknown ids rejected",
    await lookup.GetByIdsAsync(new DestinationIdentifiers(MISSING), ct) is null
    && await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, MISSING), ct) is null
    && await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, null, MISSING), ct) is null
    && await lookup.GetByIdsAsync(new DestinationIdentifiers(IRN, THR, TEH1, MISSING), ct) is null);

// ---------------- B: labels / fallback chain ----------------
var cityLabelsEn = await labels.GetCityLabelsAsync(new[] { TEH1, EN1, NOTR, MISSING }, EN, ct);
Check("B1 labels(requested=en): requested language wins; en-only label kept; code fallback; missing id omitted",
    cityLabelsEn.Count == 3
    && cityLabelsEn[TEH1] == "Tehran"
    && cityLabelsEn[EN1] == "EnOnlyCity"
    && cityLabelsEn[NOTR] == "NOTR"
    && !cityLabelsEn.ContainsKey(MISSING),
    string.Join("; ", cityLabelsEn.Select(x => $"{x.Key.ToString()[..8]}={x.Value}")));

var cityLabelsFa = await labels.GetCityLabelsAsync(new[] { TEH1, EN1 }, FA, ct);
Check("B2 labels(requested=fa): fa label for TEH1, en-only city falls back to its stable code",
    cityLabelsFa[TEH1] == "تهران" && cityLabelsFa[EN1] == "EN1",
    string.Join("; ", cityLabelsFa.Select(x => $"{x.Key.ToString()[..8]}={x.Value}")));

var cityLabelsNull = await labels.GetCityLabelsAsync(new[] { TEH1, EN1 }, null, ct);
Check("B3 labels(languageId=null): default language (fa) used",
    cityLabelsNull[TEH1] == "تهران" && cityLabelsNull[EN1] == "EN1");

var cityLabelsEmptyLang = await labels.GetCityLabelsAsync(new[] { TEH1 }, Guid.Empty, ct);
Check("B4 labels(languageId=Guid.Empty): treated as default language (fa)", cityLabelsEmptyLang[TEH1] == "تهران");

var cityLabelsUnknownLang = await labels.GetCityLabelsAsync(new[] { TEH1 }, UNKNOWN_LANGUAGE, ct);
Check("B5 labels(unknown language): not honoured, falls through to default (fa)", cityLabelsUnknownLang[TEH1] == "تهران");

Check("B6 labels(empty id set): empty dictionary, no query side effect",
    (await labels.GetCityLabelsAsync(Array.Empty<Guid>(), FA, ct)).Count == 0);

var countryLabels = await labels.GetCountryLabelsAsync(new[] { IRN, MISSING }, FA, ct);
Check("B7 country labels: label from translation, unknown id omitted",
    countryLabels.Count == 1 && countryLabels[IRN] == "ایران");

var areaLabels = await labels.GetAdministrativeAreaLabelsAsync(new[] { THR, ALB }, FA, ct);
Check("B8 administrative area labels(FA)", areaLabels[THR] == "تهران" && areaLabels[ALB] == "البرز");

var areaLabelsEn = await labels.GetAdministrativeAreaLabelsAsync(new[] { THR, ALB }, EN, ct);
Check("B9 administrative area labels(EN)", areaLabelsEn[THR] == "Tehran" && areaLabelsEn[ALB] == "Alborz");

var serviceLabelsEn = await labels.GetServiceAreaLabelsAsync(new[] { SA1, SA2 }, EN, ct);
Check("B10 service area labels(EN): translated SA1 in the requested language, untranslated SA2 falls through to the default language (fa)",
    serviceLabelsEn[SA1] == "Zone 1" && serviceLabelsEn[SA2] == "منطقه ۲",
    string.Join("; ", serviceLabelsEn.Select(x => $"{x.Key.ToString()[..8]}={x.Value}")));

// ---------------- B2: no default language flag at all ----------------
var noDefault = new GeographyLabelLookup(db, new FakeLanguages(DefaultLanguages().Select(x => x with { IsDefault = false }).ToList()));
var noDefaultLabels = await noDefault.GetCityLabelsAsync(new[] { TEH1, EN1 }, null, ct);
Check("B11 no `IsDefault` language: null request cannot pick a language, stable code is used",
    noDefaultLabels[TEH1] == "TEH1" && noDefaultLabels[EN1] == "EN1");

var emptyLanguages = new GeographyLabelLookup(db, new FakeLanguages(Array.Empty<LanguageLookupSnapshot>()));
var emptyLanguageLabels = await emptyLanguages.GetCityLabelsAsync(new[] { TEH1 }, FA, ct);
Check("B12 empty language set: requested language not honoured, stable code used",
    emptyLanguageLabels[TEH1] == "TEH1");

// ---------------- C: legacy resolution ----------------
async Task<(DestinationResolutionOutcome Outcome, Guid? Country, Guid? Area, Guid? City, Guid? Service, int Count)> Resolve(
    string? country, string? area, string? city, string? service)
{
    var result = await lookup.ResolveLegacyAsync(country, area, city, service, ct);
    return (result.Outcome, result.Identifiers?.CountryId, result.Identifiers?.AdministrativeAreaId,
        result.Identifiers?.CityId, result.Identifiers?.ServiceAreaId, result.CandidateCount);
}

var r1 = await Resolve("IRN", "البرز", "کرج", null);
Check("R1 area+city labels resolve exactly", r1 == (DestinationResolutionOutcome.Resolved, IRN, ALB, KAR, null, 1), r1.ToString());

var r2 = await Resolve("IRN", null, "ساوه", null);
Check("R2 duplicated city label in two different areas => Ambiguous(2), not collapsed",
    r2.Outcome == DestinationResolutionOutcome.Ambiguous && r2.Count == 2 && r2.City is null, r2.ToString());

var r3 = await Resolve("IRN", "MZK1", "ساوه", null);
Check("R3 supplying the area disambiguates the duplicated city label", r3 == (DestinationResolutionOutcome.Resolved, IRN, MZK1, SV1, null, 1), r3.ToString());

var r4 = await Resolve("IRN", "مرکزی", "ساوه", null);
Check("R4 duplicated area label => Ambiguous(2)", r4.Outcome == DestinationResolutionOutcome.Ambiguous && r4.Count == 2, r4.ToString());

var r5 = await Resolve("IRN", null, "TEH1", null);
Check("R5 stable code accepted as exact key; area filled from the city's own row",
    r5 == (DestinationResolutionOutcome.Resolved, IRN, THR, TEH1, null, 1), r5.ToString());

var r6 = await Resolve("IRN", null, null, "منطقه ۱");
Check("R6 service-area label without a city => Unresolved (never guessed across cities)",
    r6.Outcome == DestinationResolutionOutcome.Unresolved && r6.Count == 0, r6.ToString());

var r7 = await Resolve("IRN", "تهران", "تهران", "منطقه ۲");
Check("R7 full legacy text resolves to the 4 stable ids",
    r7 == (DestinationResolutionOutcome.Resolved, IRN, THR, TEH1, SA2, 1), r7.ToString());

var r8 = await Resolve("IRN", null, null, null);
Check("R8 country alone resolves to the country id", r8 == (DestinationResolutionOutcome.Resolved, IRN, null, null, null, 1), r8.ToString());

var r9 = await Resolve(null, null, "کرج", null);
var r9b = await Resolve("   ", "البرز", "کرج", null);
Check("R9 blank/missing country => Unresolved (no cross-country search)",
    r9.Outcome == DestinationResolutionOutcome.Unresolved && r9b.Outcome == DestinationResolutionOutcome.Unresolved);

var r10 = await Resolve("XXX", null, "کرج", null);
Check("R10 unknown country code => Unresolved", r10.Outcome == DestinationResolutionOutcome.Unresolved);

var r11 = await Resolve(" irn ", " البرز ", " کرج ", null);
Check("R11 trim + case-insensitive normalisation (country code and labels)", r11 == (DestinationResolutionOutcome.Resolved, IRN, ALB, KAR, null, 1), r11.ToString());

var r12 = await Resolve("IRN", "THR", null, null);
Check("R12 area resolvable by stable code with no city", r12 == (DestinationResolutionOutcome.Resolved, IRN, THR, null, null, 1), r12.ToString());

var r13 = await Resolve("IRN", "البرز", "ساوه", null);
Check("R13 city outside the supplied area => Unresolved (area is a real scope)", r13.Outcome == DestinationResolutionOutcome.Unresolved, r13.ToString());

var r14 = await Resolve("IRN", "تهران", "تهران", "منطقه ۹");
Check("R14 unknown service-area label => Unresolved", r14.Outcome == DestinationResolutionOutcome.Unresolved, r14.ToString());

var r15 = await Resolve("IRN", "THR", "NOTR", null);
Check("R15 city without any label still resolvable by stable code",
    r15 == (DestinationResolutionOutcome.Resolved, IRN, THR, NOTR, null, 1), r15.ToString());

// determinism: the whole resolution suite twice must be identical
var firstPass = string.Join("|", new[] { r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15 });
var secondPass = string.Join("|", new[] { await Resolve("IRN", "البرز", "کرج", null), await Resolve("IRN", null, "ساوه", null), await Resolve("IRN", "MZK1", "ساوه", null), await Resolve("IRN", "مرکزی", "ساوه", null), await Resolve("IRN", null, "TEH1", null), await Resolve("IRN", null, null, "منطقه ۱"), await Resolve("IRN", "تهران", "تهران", "منطقه ۲"), await Resolve("IRN", null, null, null), await Resolve(null, null, "کرج", null), await Resolve("XXX", null, "کرج", null), await Resolve(" irn ", " البرز ", " کرج ", null), await Resolve("IRN", "THR", null, null), await Resolve("IRN", "البرز", "ساوه", null), await Resolve("IRN", "تهران", "تهران", "منطقه ۹"), await Resolve("IRN", "THR", "NOTR", null) });
Check("R16 resolution is deterministic across runs", firstPass == secondPass);

// ---------------- D: read-only proof ----------------
var rowsAfter = await TotalRowsAsync();
Check("D1 read surface performed no write (row counts unchanged)", rowsBefore == rowsAfter, $"{rowsBefore} -> {rowsAfter}");

Console.WriteLine();
Console.WriteLine(failures.Count == 0 ? "ALL CHECKS PASSED" : $"FAILURES: {string.Join(", ", failures)}");
return failures.Count == 0 ? 0 : 1;

static IReadOnlyList<LanguageLookupSnapshot> DefaultLanguages() =>
[
    new LanguageLookupSnapshot(Guid.Parse("fa000000-0000-0000-0000-000000000000"), "fa-IR", "fa-IR", "fa", true),
    new LanguageLookupSnapshot(Guid.Parse("ee000000-0000-0000-0000-000000000000"), "en-US", "en-US", "en", false),
];

/// <summary>Test-only language port: the real one is Localization's registered adapter and is not wired here.</summary>
file sealed class FakeLanguages(IReadOnlyList<LanguageLookupSnapshot> languages) : ILanguageLookup
{
    public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult(languages);
}
