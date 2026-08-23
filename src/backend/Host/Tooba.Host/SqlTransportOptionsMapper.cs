using MassTransit;
using Npgsql;

namespace Tooba.Host;

/// <summary>
/// نگاشت ConnectionReference استقرار به <see cref="SqlTransportOptions"/> بدون لاگ رشتهٔ اتصال.
/// </summary>
internal static class SqlTransportOptionsMapper
{
    /// <summary>
    /// Host/Port/Database/Username را از Npgsql می‌خواند. schema کسب‌وکار ماژول را لمس نمی‌کند.
    /// </summary>
    /// <param name="options">گزینه‌های SQL Transport MassTransit 8.5.10.</param>
    /// <param name="connectionString">رشتهٔ اتصال پایگاه messaging استقرار.</param>
    /// <param name="schema">schema زیرساخت مثل transport.</param>
    public static void Apply(SqlTransportOptions options, string connectionString, string schema)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        options.Host = builder.Host;
        if (builder.Port > 0)
        {
            options.Port = builder.Port;
        }

        options.Database = builder.Database;
        options.Username = builder.Username;
        options.Password = builder.Password;
        options.Schema = schema;
        options.Role = builder.Username;
        options.AdminUsername = builder.Username;
        options.AdminPassword = builder.Password;
        options.ConnectionString = connectionString;
    }
}
