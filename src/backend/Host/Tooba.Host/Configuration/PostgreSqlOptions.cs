
namespace Tooba.Host.Configuration;

/// <summary>
/// نقشهٔ مراجع اتصال به رشتهٔ Npgsql.
/// </summary>
internal sealed class PostgreSqlOptions
{
    /// <summary>
    /// کلید = ConnectionReference، مقدار = connection string. هرگز در ProblemDetails نیاید.
    /// </summary>
    public Dictionary<string, string> ConnectionReferences { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
