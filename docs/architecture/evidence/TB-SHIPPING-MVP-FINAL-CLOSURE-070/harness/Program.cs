using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Places;
using Tooba.Persistence;

// Focused, read-only isolation of ResolveLegacyAsync("IR","البرز",null,null) against the REAL
// Development database tooba_alpha (127.0.0.1:5432), exactly as the shadow observer calls it.
const string ConnectionString = "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

var options = new DbContextOptionsBuilder<GeographyDbContext>();
ToobaNpgsql.ConfigureModuleContext(options, ConnectionString, GeographyDbContext.Schema, typeof(GeographyDbContext));
var db = new GeographyDbContext(options.Options);
var lookup = new GeographyPlaceLookup(db);

var ct = CancellationToken.None;

Console.WriteLine("--- step 1: country lookup (CountryCode == 'IR') ---");
var countries = await db.Countries.AsNoTracking().ToListAsync(ct);
foreach (var c in countries)
{
    Console.WriteLine($"    country id={c.CountryId} code='{c.CountryCode}' active={c.IsActive} bytes={Bytes(c.CountryCode)}");
}

Console.WriteLine("--- step 2: administrative areas for IR ---");
var ir = countries.Single(c => string.Equals(c.CountryCode.Trim(), "IR", StringComparison.OrdinalIgnoreCase));
var areas = await db.AdministrativeAreas.AsNoTracking().Where(x => x.CountryId == ir.CountryId).ToListAsync(ct);
var areaIds = areas.Select(x => x.AdministrativeAreaId).ToArray();
var areaLabels = await db.AdministrativeAreaNameTranslations.AsNoTracking().Where(x => areaIds.Contains(x.AdministrativeAreaId)).ToListAsync(ct);
foreach (var a in areas)
{
    var labels = areaLabels.Where(l => l.AdministrativeAreaId == a.AdministrativeAreaId).ToList();
    Console.WriteLine($"    area code='{a.Code}' active={a.IsActive} labels={string.Join('/', labels.Select(l => $"'{l.Name}'[{Bytes(l.Name)}]"))}");
}

const string Input = "البرز";
Console.WriteLine($"--- step 3: exact comparison of the input '{Input}' bytes={Bytes(Input)} ---");
foreach (var a in areas)
{
    var stored = string.Equals(a.Code.Trim(), Input.Trim(), StringComparison.OrdinalIgnoreCase);
    var labels = areaLabels.Where(l => l.AdministrativeAreaId == a.AdministrativeAreaId).ToList();
    var anyLabel = labels.Any(l => string.Equals(l.Name.Trim(), Input.Trim(), StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"    area code='{a.Code}' codeMatch={stored} labelMatch={anyLabel}");
}

Console.WriteLine("--- step 4: the exact observer call ResolveLegacyAsync(\"IR\",\"البرز\",null,null) ---");
var r = await lookup.ResolveLegacyAsync("IR", "البرز", null, null, ct);
Console.WriteLine($"    outcome={r.Outcome} candidateCount={r.CandidateCount} identifiers={r.Identifiers}");

Console.WriteLine("--- step 5: variants ---");
foreach (var (c, a, city) in new (string?, string?, string?)[]
{
    ("IR", "البرز", null),
    ("IR", "alborz", null),
    ("IR", "البرز", "کرج"),
    ("IR", null, "کرج"),
    ("IR", "تهران", null),
})
{
    var x = await lookup.ResolveLegacyAsync(c, a, city, null, ct);
    Console.WriteLine($"    ({c},{a},{city}) => {x.Outcome} count={x.CandidateCount} ids={x.Identifiers}");
}

static string Bytes(string s) => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(s)).ToLowerInvariant();
