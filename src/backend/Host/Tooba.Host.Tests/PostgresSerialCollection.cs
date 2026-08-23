using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های PostgreSQL نباید موازی شوند؛ DataSource/Npgsql و کانتینر با هم تداخل می‌کنند.
/// </summary>
[CollectionDefinition("PostgresSerial", DisableParallelization = true)]
public sealed class PostgresSerialCollection;
